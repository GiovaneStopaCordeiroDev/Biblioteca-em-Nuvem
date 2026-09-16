namespace BibliotecaEscolar.Api.Services;

public sealed class RequisicaoInvalidaException(string message) : Exception(message);

public sealed class RecursoNaoEncontradoException(string message) : Exception(message);

public sealed class ConflitoDeDominioException(string message) : Exception(message);
