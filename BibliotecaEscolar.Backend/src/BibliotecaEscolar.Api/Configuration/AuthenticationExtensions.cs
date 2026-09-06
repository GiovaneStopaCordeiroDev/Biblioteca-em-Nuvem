using BibliotecaEscolar.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace BibliotecaEscolar.Api.Configuration;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddBibliotecaAuthentication(
        this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        var mode = config["Auth:Mode"] ?? "Supabase";
        if (mode == "Development")
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Auth:Mode=Development só é permitido no ambiente Development.");
            services.AddAuthentication(DevelopmentAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthHandler>(DevelopmentAuthHandler.SchemeName, _ => { });
        }
        else if (mode == "Supabase")
        {
            var url = config["Auth:SupabaseUrl"]?.TrimEnd('/');
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https"
                || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
                throw new InvalidOperationException("Configure Auth:SupabaseUrl com a URL HTTPS do seu projeto Supabase.");
            var issuer = url + "/auth/v1";
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    issuer + "/.well-known/jwks.json", new SupabaseConfigurationRetriever(issuer),
                    new HttpDocumentRetriever { RequireHttps = true });
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = issuer,
                    ValidateAudience = true, ValidAudience = "authenticated",
                    ValidateLifetime = true, RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true, RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSha256],
                    ClockSkew = TimeSpan.FromSeconds(30), NameClaimType = "sub"
                };
            });
        }
        else throw new InvalidOperationException("Auth:Mode deve ser Development ou Supabase.");
        services.AddAuthorization(options => options.AddPolicy("Operador", policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new OperadorRequirement())));
        services.AddScoped<IAuthorizationHandler, OperadorAuthorizationHandler>();
        return services;
    }
}
