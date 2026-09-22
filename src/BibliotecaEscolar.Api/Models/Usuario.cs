namespace BibliotecaEscolar.Api.Models;

// Operador autenticado da biblioteca; não representa quem retira um livro.
public sealed class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SupabaseAuthId { get; set; }
    public string? Login { get; set; }
    public string? SenhaHash { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Perfil { get; set; } = "Bibliotecario";
    public bool Ativo { get; set; } = true;
}
