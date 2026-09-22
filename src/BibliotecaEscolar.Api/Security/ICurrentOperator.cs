namespace BibliotecaEscolar.Api.Security;

public interface ICurrentOperator
{
    Guid AuthId { get; }
    bool IsDemonstracao { get; }
}
