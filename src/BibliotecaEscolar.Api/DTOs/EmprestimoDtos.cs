using System.ComponentModel.DataAnnotations;

namespace BibliotecaEscolar.Api.DTOs;

public sealed class CriarEmprestimoRequest
{
    public Guid AlunoId { get; set; }
    public Guid LivroId { get; set; }
    public DateOnly? DataPrevistaDevolucao { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }
}

public sealed class ConsultaEmprestimos : ConsultaPaginada
{
    [StringLength(20)]
    public string? Status { get; set; }

    public Guid? AlunoId { get; set; }
    public Guid? LivroId { get; set; }
}

public sealed record EmprestimoResponse(
    Guid Id,
    Guid AlunoId,
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
