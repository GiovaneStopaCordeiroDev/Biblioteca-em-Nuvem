using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaEscolar.Api.Controllers;

[ApiController]
[Route("api/v1/livros")]
[Authorize(Policy = "Operador")]
[Tags("Livros")]
public sealed class LivrosController(LivroService service) : ControllerBase
{
    /// <summary>Lista o acervo com paginação e busca.</summary>
    /// <remarks>
    /// A busca é opcional, não diferencia maiúsculas de minúsculas e consulta título,
    /// autor e ISBN. O resultado é ordenado por título.
    /// </remarks>
    /// <param name="consulta">Parâmetros de busca e paginação.</param>
    /// <param name="ct">Sinal de cancelamento do pedido HTTP.</param>
    [HttpGet]
    [ProducesResponseType(typeof(Pagina<LivroResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public Task<Pagina<LivroResponse>> Listar([FromQuery] ConsultaPaginada consulta, CancellationToken ct) =>
        service.ListarAsync(consulta, ct);

    /// <summary>Consulta um livro pelo identificador.</summary>
    /// <param name="id">Identificador único do livro.</param>
    /// <param name="ct">Sinal de cancelamento do pedido HTTP.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LivroResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public Task<LivroResponse> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);

    /// <summary>Cadastra um livro no acervo.</summary>
    /// <remarks>
    /// A quantidade informada representa o total inicial de exemplares. O ISBN é
    /// opcional, mas, quando informado, deve ser um ISBN-10 ou ISBN-13 válido e único.
    /// </remarks>
    /// <param name="request">Dados do novo livro.</param>
    /// <param name="ct">Sinal de cancelamento do pedido HTTP.</param>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(LivroResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LivroResponse>> Criar(SalvarLivroRequest request, CancellationToken ct)
    {
        var livro = await service.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = livro.Id }, livro);
    }

    /// <summary>Atualiza os dados e o total de exemplares de um livro.</summary>
    /// <remarks>
    /// Envie a versão recebida na consulta mais recente. A quantidade total não pode
    /// ser menor que o número de exemplares atualmente emprestados.
    /// </remarks>
    /// <param name="id">Identificador único do livro.</param>
    /// <param name="request">Novos dados e versão atual do livro.</param>
    /// <param name="ct">Sinal de cancelamento do pedido HTTP.</param>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(LivroResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public Task<LivroResponse> Atualizar(Guid id, AtualizarLivroRequest request, CancellationToken ct) =>
        service.AtualizarAsync(id, request, ct);

    /// <summary>Exclui um livro sem histórico de empréstimos.</summary>
    /// <remarks>A operação é restrita a administradores e preserva livros que possuam histórico.</remarks>
    /// <param name="id">Identificador único do livro.</param>
    /// <param name="ct">Sinal de cancelamento do pedido HTTP.</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await service.ExcluirAsync(id, ct);
        return NoContent();
    }
}
