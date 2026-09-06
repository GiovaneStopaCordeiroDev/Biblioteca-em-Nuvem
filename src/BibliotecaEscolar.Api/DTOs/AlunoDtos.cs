using System.ComponentModel.DataAnnotations;

namespace BibliotecaEscolar.Api.DTOs;

public sealed class SalvarAlunoRequest
{
    [Required, StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Matricula { get; set; } = string.Empty;

    [StringLength(60)]
    public string? Turma { get; set; }

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }
}

public sealed record AlunoResponse(Guid Id, string Nome, string Matricula, string? Turma, string? Email);
