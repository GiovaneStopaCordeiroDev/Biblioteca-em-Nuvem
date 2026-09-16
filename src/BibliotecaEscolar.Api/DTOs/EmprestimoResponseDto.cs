namespace BibliotecaEscolar.Api.DTOs;

public sealed record EmprestimoResponseDto(
    Guid Id,
    Guid AlunoId,
    string AlunoNome,
    Guid LivroId,
    string LivroTitulo,
    DateOnly DataEmprestimo,
    DateOnly DataPrevistaDevolucao,
    DateOnly? DataDevolucao,
    StatusEmprestimo Status,
    bool Atrasado,
    int QuantidadeRenovacoes,
    string? Observacao
);