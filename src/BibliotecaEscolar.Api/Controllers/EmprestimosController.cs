using BibliotecaEscolar.Api.DTOs.Emprestimos;
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
    [ProducesResponseType(typeof(Pagina<EmprestimoResponseDto>), StatusCodes.Status200OK)]
    public Task<Pagina<EmprestimoResponseDto>> Listar(
        [FromQuery] ConsultaEmprestimosDto consulta,
        CancellationToken ct)
    {
        return service.ListarAsync(consulta, ct);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmprestimoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmprestimoResponseDto> Obter(
        Guid id,
        CancellationToken ct)
    {
        return service.ObterAsync(id, ct);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmprestimoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmprestimoResponseDto>> Criar(
        CreateEmprestimoDto request,
        CancellationToken ct)
    {
        var emprestimo = await service.CriarAsync(request, ct);

        return CreatedAtAction(
            nameof(Obter),
            new { id = emprestimo.Id },
            emprestimo);
    }

    [HttpPatch("{id:guid}/devolucao")]
    [ProducesResponseType(typeof(EmprestimoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmprestimoResponseDto> Devolver(
        Guid id,
        CancellationToken ct)
    {
        return service.DevolverAsync(id, ct);
    }

    [HttpPatch("{id:guid}/renovar")]
    [ProducesResponseType(typeof(EmprestimoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmprestimoResponseDto> Renovar(
        Guid id,
        CancellationToken ct)
    {
        return service.RenovarAsync(id, ct);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(
        Guid id,
        CancellationToken ct)
    {
        await service.ExcluirAsync(id, ct);

        return NoContent();
    }
}