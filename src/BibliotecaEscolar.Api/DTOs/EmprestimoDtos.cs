using System.ComponentModel.DataAnnotations;

namespace BibliotecaEscolar.Api.DTOs;

public sealed class CriarEmprestimoRequest
{
    /// <summary>Nome do aluno digitado pelo bibliotecário no momento da retirada.</summary>
    /// <example>Ana Beatriz</example>
    [Required(AllowEmptyStrings = false), StringLength(150)]
    public string? AlunoNome { get; set; }

    /// <summary>Identificador do livro retirado.</summary>
    public Guid LivroId { get; set; }

    /// <summary>Prazo desejado. Quando omitido, a API usa 14 dias.</summary>
    public DateOnly? DataPrevistaDevolucao { get; set; }

    /// <summary>Observação opcional sobre a retirada.</summary>
    [StringLength(500)]
    public string? Observacao { get; set; }
}

public sealed class ConsultaEmprestimos : ConsultaPaginada
{
    [StringLength(20)]
    public string? Status { get; set; }

    public Guid? LivroId { get; set; }
}

public sealed record EmprestimoResponse(
    Guid Id,
    string AlunoNome,
    Guid LivroId,
    string LivroTitulo,
    DateOnly DataEmprestimo,
    DateOnly DataPrevistaDevolucao,
    DateOnly? DataDevolucao,
    string Status,
    bool Atrasado,
    int QuantidadeRenovacoes,
    string? Observacao);
