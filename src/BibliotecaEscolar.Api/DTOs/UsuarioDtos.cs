namespace BibliotecaEscolar.Api.DTOs;

public sealed record UsuarioAtualResponse(Guid Id, string Nome, string Perfil, bool Demonstracao);
