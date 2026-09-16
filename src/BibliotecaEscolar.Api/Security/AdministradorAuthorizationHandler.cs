using Microsoft.AspNetCore.Authorization;

namespace BibliotecaEscolar.Api.Security;

public sealed class AdministradorRequirement : IAuthorizationRequirement;

public sealed class AdministradorAuthorizationHandler(UsuarioAuthorizationResolver resolver)
    : AuthorizationHandler<AdministradorRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdministradorRequirement requirement)
    {
        var cancellationToken = (context.Resource as HttpContext)?.RequestAborted ?? default;
        if (await resolver.GetActiveProfileAsync(context.User, cancellationToken) == "Administrador")
            context.Succeed(requirement);
    }
}
