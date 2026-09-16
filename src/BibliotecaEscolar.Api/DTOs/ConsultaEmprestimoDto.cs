namespace BibliotecaEscolar.Api.DTOs;

public sealed class ConsultaEmprestimos : ConsultaPaginada
{
    public StatusEmprestimo? Status { get; set; }

    public Guid? AlunoId { get; set; }

    public Guid? LivroId { get; set; }
}