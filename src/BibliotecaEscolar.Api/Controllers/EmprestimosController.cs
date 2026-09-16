using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaEscolar.Api.Controllers;

[ApiController]
[Route("api/v1/emprestimos")]
[Authorize(Policy = "Operador")]
public sealed class EmprestimosController(EmprestimoService service) : ControllerBase
{
    [HttpGet]
    public Task<Pagina<EmprestimoResponse>> Listar([FromQuery] ConsultaEmprestimos consulta, CancellationToken ct) =>
        service.ListarAsync(consulta, ct);

    [HttpGet("{id:guid}")]
    public Task<EmprestimoResponse> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);

    [HttpPost]
    [ProducesResponseType(typeof(EmprestimoResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<EmprestimoResponse>> Criar(CriarEmprestimoRequest request, CancellationToken ct)
    {
        var emprestimo = await service.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = emprestimo.Id }, emprestimo);
    }

    [HttpPatch("{id:guid}/devolucao")]
    public Task<EmprestimoResponse> Devolver(Guid id, CancellationToken ct) => service.DevolverAsync(id, ct);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await service.ExcluirAsync(id, ct);
        return NoContent();
    }
}
