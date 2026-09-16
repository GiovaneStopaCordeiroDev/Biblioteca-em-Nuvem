using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.IIS;

namespace BibliotecaEscolar.Api.Configuration;

public sealed class RequestSecurityOptions
{
    public const string SectionName = "RequestSecurity";
    public const string ApiRateLimitPolicy = "Api";

    public long MaxRequestBodySizeBytes { get; init; } = 1_048_576;
    public int RateLimitPermitLimit { get; init; } = 120;
    public int RateLimitWindowSeconds { get; init; } = 60;
}

public static class RequestSecurityExtensions
{
    private const long MaximumConfigurableBodySize = 100 * 1024 * 1024;

    public static WebApplicationBuilder AddRequestSecurity(this WebApplicationBuilder builder)
    {
        ValidateAllowedHosts(builder.Configuration["AllowedHosts"]);

        var securityOptions = builder.Configuration
            .GetSection(RequestSecurityOptions.SectionName)
            .Get<RequestSecurityOptions>() ?? new RequestSecurityOptions();
        ValidateOptions(securityOptions);

        builder.Services.AddSingleton(securityOptions);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = securityOptions.MaxRequestBodySizeBytes;
        });
        builder.Services.Configure<IISServerOptions>(options =>
            options.MaxRequestBodySize = securityOptions.MaxRequestBodySizeBytes);
        builder.Services.Configure<FormOptions>(options =>
            options.MultipartBodyLengthLimit = securityOptions.MaxRequestBodySizeBytes);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] =
                    securityOptions.RateLimitWindowSeconds.ToString(CultureInfo.InvariantCulture);
                await Results.Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Muitas requisições",
                    detail: "Aguarde antes de tentar novamente.")
                    .ExecuteAsync(context.HttpContext);
            };
            options.AddPolicy(RequestSecurityOptions.ApiRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = securityOptions.RateLimitPermitLimit,
                        QueueLimit = 0,
                        Window = TimeSpan.FromSeconds(securityOptions.RateLimitWindowSeconds)
                    }));
        });

        return builder;
    }

    private static string GetPartitionKey(HttpContext context)
    {
        var subject = context.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(subject)) return $"usuario:{subject}";

        var address = context.Connection.RemoteIpAddress;
        if (address?.IsIPv4MappedToIPv6 == true) address = address.MapToIPv4();
        return $"endereco:{address?.ToString() ?? "desconhecido"}";
    }

    private static void ValidateAllowedHosts(string? allowedHosts)
    {
        var hosts = allowedHosts?.Split(
            ';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (hosts.Length == 0 || hosts.Any(host => host is "*" or "+"))
            throw new InvalidOperationException(
                "AllowedHosts deve listar hosts explícitos; curingas globais não são permitidos.");
    }

    private static void ValidateOptions(RequestSecurityOptions options)
    {
        if (options.MaxRequestBodySizeBytes is <= 0 or > MaximumConfigurableBodySize)
            throw new InvalidOperationException(
                $"RequestSecurity:MaxRequestBodySizeBytes deve estar entre 1 e {MaximumConfigurableBodySize}.");
        if (options.RateLimitPermitLimit <= 0)
            throw new InvalidOperationException(
                "RequestSecurity:RateLimitPermitLimit deve ser maior que zero.");
        if (options.RateLimitWindowSeconds is <= 0 or > 3_600)
            throw new InvalidOperationException(
                "RequestSecurity:RateLimitWindowSeconds deve estar entre 1 e 3600.");
    }
}
