namespace BibliotecaEscolar.Api.Models;

public sealed class Livro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Categoria { get; set; }
    public int QuantidadeTotal { get; set; }
    public int QuantidadeDisponivel { get; set; }
}
