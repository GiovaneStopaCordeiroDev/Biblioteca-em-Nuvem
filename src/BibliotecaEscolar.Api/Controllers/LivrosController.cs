using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaEscolar.Api.Controllers;

[ApiController]
[Route("api/v1/livros")]
[Authorize(Policy = "Operador")]
public sealed class LivrosController(LivroService service) : ControllerBase
{
    [HttpGet]
    public Task<Pagina<LivroResponse>> Listar([FromQuery] ConsultaPaginada consulta, CancellationToken ct) =>
        service.ListarAsync(consulta, ct);

    [HttpGet("{id:guid}")]
    public Task<LivroResponse> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);

    [HttpPost]
    [ProducesResponseType(typeof(LivroResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LivroResponse>> Criar(SalvarLivroRequest request, CancellationToken ct)
    {
        var livro = await service.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = livro.Id }, livro);
    }

    [HttpPut("{id:guid}")]
    public Task<LivroResponse> Atualizar(Guid id, AtualizarLivroRequest request, CancellationToken ct) =>
        service.AtualizarAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await service.ExcluirAsync(id, ct);
        return NoContent();
    }
}
