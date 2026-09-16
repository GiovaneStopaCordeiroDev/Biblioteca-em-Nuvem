using System.ComponentModel.DataAnnotations;

namespace BibliotecaEscolar.Api.DTOs;

public sealed class CriarEmprestimoRequest
{
    [Required]
    public Guid AlunoId { get; set; }

    [Required]
    public Guid LivroId { get; set; }

    [Required]
    public DateOnly DataPrevistaDevolucao { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }
}