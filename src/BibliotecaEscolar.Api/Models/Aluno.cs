namespace BibliotecaEscolar.Api.Models;

public sealed class Aluno
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid Versao { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public string? Turma { get; set; }
    public string? Email { get; set; }
}
