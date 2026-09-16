using System.Security.Claims;
using BibliotecaEscolar.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Security;

/// <summary>
/// Resolve o perfil confiável do operador uma vez por escopo de requisição.
/// O perfil enviado no JWT nunca participa da decisão de autorização.
/// </summary>
public sealed class UsuarioAuthorizationResolver(
    BibliotecaDbContext db,
    IConfiguration configuration,
    IHostEnvironment environment)
{
    private Guid? _cachedAuthId;
    private Task<string?>? _cachedProfile;

    public Task<string?> GetActiveProfileAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (user.Identity?.IsAuthenticated != true) return Task.FromResult<string?>(null);

        if (environment.IsDevelopment()
            && configuration["Auth:Mode"] == DevelopmentAuthHandler.SchemeName
            && user.Identity.AuthenticationType == DevelopmentAuthHandler.SchemeName)
            return Task.FromResult<string?>("Administrador");

        if (!Guid.TryParse(user.FindFirst("sub")?.Value, out var authId))
            return Task.FromResult<string?>(null);

        if (_cachedProfile is null || _cachedAuthId != authId)
        {
            _cachedAuthId = authId;
            _cachedProfile = db.Usuarios.AsNoTracking()
                .Where(usuario => usuario.SupabaseAuthId == authId && usuario.Ativo)
                .Select(usuario => usuario.Perfil)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return _cachedProfile;
    }
}
