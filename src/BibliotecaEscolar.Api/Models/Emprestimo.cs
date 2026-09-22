namespace BibliotecaEscolar.Api.Models;

public sealed class Emprestimo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string AlunoNome { get; set; } = string.Empty;

    public Guid LivroId { get; set; }

    public DateOnly DataEmprestimo { get; set; }

    public DateOnly DataPrevistaDevolucao { get; set; }

    public DateOnly? DataDevolucao { get; set; }

    public int QuantidadeRenovacoes { get; set; }

    public string? Observacao { get; set; }

    public DateTimeOffset? CanceladoEm { get; set; }

    public Livro Livro { get; set; } = null!;
}
