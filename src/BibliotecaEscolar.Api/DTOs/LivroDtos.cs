using System.ComponentModel.DataAnnotations;

namespace BibliotecaEscolar.Api.DTOs;

public sealed class SalvarLivroRequest
{
    [Required, StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Autor { get; set; } = string.Empty;

    [StringLength(32)]
    public string? Isbn { get; set; }

    [StringLength(80)]
    public string? Categoria { get; set; }

    [Range(1, 1_000_000)]
    public int QuantidadeTotal { get; set; } = 1;
}

public sealed record LivroResponse(Guid Id, string Titulo, string Autor, string? Isbn,
    string? Categoria, int QuantidadeTotal, int QuantidadeDisponivel);
