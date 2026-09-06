using BibliotecaEscolar.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Security;

public sealed class OperadorRequirement : IAuthorizationRequirement;

public sealed class OperadorAuthorizationHandler(
    BibliotecaDbContext db,
    IConfiguration configuration,
    IHostEnvironment environment) : AuthorizationHandler<OperadorRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, OperadorRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;
        if (environment.IsDevelopment() && configuration["Auth:Mode"] == "Development"
            && context.User.Identity.AuthenticationType == DevelopmentAuthHandler.SchemeName)
        {
            context.Succeed(requirement);
            return;
        }
        if (!Guid.TryParse(context.User.FindFirst("sub")?.Value, out var authId)) return;
        // O papel é definido no banco pela equipe; nunca confiamos em user_metadata do JWT.
        if (await db.Usuarios.AsNoTracking().AnyAsync(u => u.SupabaseAuthId == authId && u.Ativo
            && (u.Perfil == "Administrador" || u.Perfil == "Bibliotecario")))
            context.Succeed(requirement);
    }
}
