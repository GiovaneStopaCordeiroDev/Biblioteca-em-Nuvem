using BibliotecaEscolar.Api.Data;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaEscolar.Api.Security;

public sealed class OperadorRequirement : IAuthorizationRequirement;

public sealed class OperadorAuthorizationHandler : AuthorizationHandler<OperadorRequirement>
{
    private readonly UsuarioAuthorizationResolver _resolver;

    public OperadorAuthorizationHandler(UsuarioAuthorizationResolver resolver) =>
        _resolver = resolver;

    // Mantém a construção direta usada por testes unitários e ferramentas locais.
    public OperadorAuthorizationHandler(
        BibliotecaDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment)
        : this(new UsuarioAuthorizationResolver(db, configuration, environment)) { }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, OperadorRequirement requirement)
    {
        var cancellationToken = (context.Resource as HttpContext)?.RequestAborted ?? default;
        var profile = await _resolver.GetActiveProfileAsync(context.User, cancellationToken);
        if (profile is "Administrador" or "Bibliotecario")
            context.Succeed(requirement);
    }
}
