using System.ComponentModel.DataAnnotations;

namespace BibliotecaEscolar.Api.DTOs;

public class ConsultaPaginada
{
    [Range(1, 1_000_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(200)]
    public string? Busca { get; set; }
}

public sealed record Pagina<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
