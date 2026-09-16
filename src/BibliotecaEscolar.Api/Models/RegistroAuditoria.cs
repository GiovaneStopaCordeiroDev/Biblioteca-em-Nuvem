namespace BibliotecaEscolar.Api.Models;

public sealed class RegistroAuditoria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OperadorAuthId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public Guid EntidadeId { get; set; }
    public DateTimeOffset OcorridoEm { get; set; }
    public string? Detalhes { get; set; }
}
