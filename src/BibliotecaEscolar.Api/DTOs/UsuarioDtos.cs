namespace BibliotecaEscolar.Api.DTOs;

public sealed record UsuarioAtualResponse(Guid Id, string Nome, string Perfil, bool Demonstracao);

public sealed class LoginRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(80)]
    public string? Usuario { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 12)]
    public string? Senha { get; set; }
}

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiraEm, UsuarioAtualResponse Usuario);
