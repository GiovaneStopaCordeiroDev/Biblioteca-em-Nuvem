using System.ComponentModel.DataAnnotations;

namespace BibliotecaEscolar.Api.DTOs;

public sealed class SalvarLivroRequest
{
    /// <summary>Título do livro.</summary>
    /// <example>Dom Casmurro</example>
    [Required, StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Nome do autor ou da autoria principal.</summary>
    /// <example>Machado de Assis</example>
    [Required, StringLength(150)]
    public string Autor { get; set; } = string.Empty;

    /// <summary>ISBN-10 ou ISBN-13, com ou sem hífens.</summary>
    /// <example>978-85-359-0277-8</example>
    [StringLength(32)]
    public string? Isbn { get; set; }

    /// <summary>Categoria utilizada para organizar o acervo.</summary>
    /// <example>Literatura brasileira</example>
    [StringLength(80)]
    public string? Categoria { get; set; }

    /// <summary>Total de exemplares cadastrados.</summary>
    /// <example>3</example>
    [Range(1, 1_000_000)]
    public int QuantidadeTotal { get; set; } = 1;
}

public sealed class AtualizarLivroRequest
{
    /// <inheritdoc cref="SalvarLivroRequest.Titulo"/>
    [Required, StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    /// <inheritdoc cref="SalvarLivroRequest.Autor"/>
    [Required, StringLength(150)]
    public string Autor { get; set; } = string.Empty;

    /// <inheritdoc cref="SalvarLivroRequest.Isbn"/>
    [StringLength(32)]
    public string? Isbn { get; set; }

    /// <inheritdoc cref="SalvarLivroRequest.Categoria"/>
    [StringLength(80)]
    public string? Categoria { get; set; }

    /// <inheritdoc cref="SalvarLivroRequest.QuantidadeTotal"/>
    [Range(1, 1_000_000)]
    public int QuantidadeTotal { get; set; } = 1;

    /// <summary>Versão retornada pela consulta mais recente, usada para impedir sobrescritas concorrentes.</summary>
    [Required]
    public Guid? Versao { get; set; }
}

/// <summary>Representação pública de um livro do acervo.</summary>
/// <param name="Id">Identificador único.</param>
/// <param name="Titulo">Título do livro.</param>
/// <param name="Autor">Autor ou autoria principal.</param>
/// <param name="Isbn">ISBN normalizado, sem hífens, quando informado.</param>
/// <param name="Categoria">Categoria opcional.</param>
/// <param name="QuantidadeTotal">Total de exemplares.</param>
/// <param name="QuantidadeDisponivel">Exemplares disponíveis para empréstimo.</param>
/// <param name="Versao">Versão necessária para uma atualização segura.</param>
public sealed record LivroResponse(Guid Id, string Titulo, string Autor, string? Isbn,
    string? Categoria, int QuantidadeTotal, int QuantidadeDisponivel, Guid Versao);
