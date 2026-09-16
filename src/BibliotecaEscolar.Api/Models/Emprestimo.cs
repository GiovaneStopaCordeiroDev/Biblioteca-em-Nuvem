namespace BibliotecaEscolar.Api.Models;

public enum StatusEmprestimo
{
    Ativo = 1,
    Devolvido = 2,
    Atrasado = 3,
    Cancelado = 4
}

namespace BibliotecaEscolar.Api.Models;

public sealed class Emprestimo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AlunoId { get; set; }

    public Guid LivroId { get; set; }

    public DateOnly DataEmprestimo { get; set; }

    public DateOnly DataPrevistaDevolucao { get; set; }

    public DateOnly? DataDevolucao { get; set; }

    public StatusEmprestimo Status { get; set; } = StatusEmprestimo.Ativo;

    public int QuantidadeRenovacoes { get; set; }

    public string? Observacao { get; set; }

    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? AtualizadoEm { get; set; }

    public DateTimeOffset? CanceladoEm { get; set; }

    public Aluno Aluno { get; set; } = null!;

    public Livro Livro { get; set; } = null!;
}