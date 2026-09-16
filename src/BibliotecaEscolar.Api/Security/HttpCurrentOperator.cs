namespace BibliotecaEscolar.Api.Security;

public sealed class HttpCurrentOperator(IHttpContextAccessor httpContextAccessor) : ICurrentOperator
{
    private System.Security.Claims.ClaimsPrincipal User => httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("Não há uma requisição HTTP ativa para identificar o operador.");

    public Guid AuthId => Guid.TryParse(User.FindFirst("sub")?.Value, out var authId)
        ? authId
        : throw new InvalidOperationException("O operador autenticado não possui um identificador válido.");

    public bool IsDemonstracao => User.Identity?.AuthenticationType == DevelopmentAuthHandler.SchemeName;
}
