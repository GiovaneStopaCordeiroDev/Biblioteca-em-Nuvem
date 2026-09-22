namespace BibliotecaEscolar.Api.Models;

public sealed class SessaoUsuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CriadaEm { get; set; }
    public DateTimeOffset ExpiraEm { get; set; }
    public DateTimeOffset? RevogadaEm { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
