using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Options;

namespace BibliotecaEscolar.Api.Security;

// Registrado somente com ambiente Development + Auth:Mode=Development.
public sealed class DevelopmentAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    IHostEnvironment environment,
    IServer server) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Development";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsDevelopment()
            || configuration["Auth:Mode"] != SchemeName
            || !configuration.GetValue<bool>("Auth:Development:Enabled"))
            return Task.FromResult(AuthenticateResult.Fail(
                "A autenticação demonstrativa não está habilitada explicitamente."));

        if (!IsLoopbackRequest() && !IsTestServer(server))
        {
            Logger.LogWarning(
                "Autenticação demonstrativa recusada para o endereço remoto {RemoteAddress}.",
                Context.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail(
                "A autenticação demonstrativa aceita somente conexões locais."));
        }

        var identity = new ClaimsIdentity([
            new Claim("sub", "00000000-0000-0000-0000-000000000001"),
            new Claim("name", "Administrador de demonstração")
        ], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    private bool IsLoopbackRequest()
    {
        var remoteAddress = Context.Connection.RemoteIpAddress;
        if (remoteAddress is null) return false;
        if (remoteAddress.IsIPv4MappedToIPv6) remoteAddress = remoteAddress.MapToIPv4();
        return IPAddress.IsLoopback(remoteAddress);
    }

    private static bool IsTestServer(IServer server) =>
        server.GetType().FullName == "Microsoft.AspNetCore.TestHost.TestServer"
        && server.GetType().Assembly.GetName().Name == "Microsoft.AspNetCore.TestHost";
}
