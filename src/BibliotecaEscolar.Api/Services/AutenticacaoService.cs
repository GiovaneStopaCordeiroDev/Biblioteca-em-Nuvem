using System.Security.Cryptography;
using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Models;
using BibliotecaEscolar.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Services;

public sealed class AutenticacaoService(
    BibliotecaDbContext db,
    IPasswordHasher<Usuario> passwordHasher,
    TimeProvider timeProvider,
    IConfiguration configuration)
{
    public async Task<LoginResponse> EntrarAsync(LoginRequest request, CancellationToken ct)
    {
        var login = Texto.Obrigatorio(request.Usuario, "usuário").ToLowerInvariant();
        var senha = Texto.Obrigatorio(request.Senha, "senha");
        var usuario = await db.Usuarios.SingleOrDefaultAsync(x => x.Login == login && x.Ativo, ct);
        var verification = usuario is null || string.IsNullOrWhiteSpace(usuario.SenhaHash)
            ? PasswordVerificationResult.Failed
            : passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
        if (usuario is null || verification == PasswordVerificationResult.Failed)
            throw new CredenciaisInvalidasException();

        var now = timeProvider.GetUtcNow();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            usuario.SenhaHash = passwordHasher.HashPassword(usuario, senha);
        var sessionHours = Math.Clamp(configuration.GetValue("Auth:SessionHours", 8), 1, 24);
        var expiresAt = now.AddHours(sessionHours);
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        db.SessoesUsuarios.Add(new SessaoUsuario
        {
            UsuarioId = usuario.Id,
            TokenHash = DatabaseSessionAuthHandler.HashToken(token),
            CriadaEm = now,
            ExpiraEm = expiresAt
        });
        await db.SaveChangesAsync(ct);
        return new(token, expiresAt,
            new UsuarioAtualResponse(usuario.Id, usuario.Nome, usuario.Perfil, false));
    }

    public async Task SairAsync(string? authorization, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(authorization)
            || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return;
        var token = authorization["Bearer ".Length..].Trim();
        if (token.Length is < 40 or > 200) return;
        var hash = DatabaseSessionAuthHandler.HashToken(token);
        await db.SessoesUsuarios.Where(x => x.TokenHash == hash && x.RevogadaEm == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevogadaEm, timeProvider.GetUtcNow()), ct);
    }
}
