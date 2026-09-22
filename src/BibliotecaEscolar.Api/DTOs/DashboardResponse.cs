namespace BibliotecaEscolar.Api.DTOs;

public sealed record DashboardResponse(int TotalLivros, int TotalExemplares,
    int ExemplaresDisponiveis, int EmprestimosAtivos, int EmprestimosAtrasados);
