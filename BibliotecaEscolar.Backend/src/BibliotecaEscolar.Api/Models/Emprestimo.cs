namespace BibliotecaEscolar.Api.Models;

public sealed class Emprestimo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AlunoId { get; set; }
    public Guid LivroId { get; set; }
    public DateOnly DataEmprestimo { get; set; }
    public DateOnly DataPrevistaDevolucao { get; set; }
    public DateOnly? DataDevolucao { get; set; }
    public DateTimeOffset? CanceladoEm { get; set; }
    public Aluno Aluno { get; set; } = null!;
    public Livro Livro { get; set; } = null!;
}
