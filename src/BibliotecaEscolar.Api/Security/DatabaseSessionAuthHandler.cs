using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using BibliotecaEscolar.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BibliotecaEscolar.Api.Security;

public sealed class DatabaseSessionAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    BibliotecaDbContext db,
    TimeProvider timeProvider) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DatabaseSession";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = authorization["Bearer ".Length..].Trim();
        if (token.Length is < 40 or > 200) return AuthenticateResult.Fail("Sessão inválida.");

        var tokenHash = HashToken(token);
        var now = timeProvider.GetUtcNow();
        var session = await db.SessoesUsuarios.AsNoTracking()
            .Where(x => x.TokenHash == tokenHash)
            .Select(x => new
            {
                x.ExpiraEm,
                x.RevogadaEm,
                UsuarioId = x.Usuario.Id,
                UsuarioNome = x.Usuario.Nome,
                UsuarioAtivo = x.Usuario.Ativo
            })
            .SingleOrDefaultAsync(Context.RequestAborted);
        if (session is null || session.RevogadaEm is not null || session.ExpiraEm <= now
            || !session.UsuarioAtivo)
            return AuthenticateResult.Fail("Sessão inválida ou expirada.");

        var identity = new ClaimsIdentity([
            new Claim("sub", session.UsuarioId.ToString()),
            new Claim("name", session.UsuarioNome)
        ], SchemeName);
        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
