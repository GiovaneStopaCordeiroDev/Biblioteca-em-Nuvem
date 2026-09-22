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
    /// <summary>Lista e pesquisa o histórico de empréstimos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(Pagina<EmprestimoResponse>), StatusCodes.Status200OK)]
    public Task<Pagina<EmprestimoResponse>> Listar(
        [FromQuery] ConsultaEmprestimos consulta, CancellationToken ct) =>
        service.ListarAsync(consulta, ct);

    /// <summary>Consulta um empréstimo pelo identificador.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmprestimoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmprestimoResponse> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);

    /// <summary>Registra uma retirada com o nome do aluno digitado manualmente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmprestimoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmprestimoResponse>> Criar(
        CriarEmprestimoRequest request, CancellationToken ct)
    {
        var emprestimo = await service.CriarAsync(request, ct);

        return CreatedAtAction(nameof(Obter), new { id = emprestimo.Id }, emprestimo);
    }

    /// <summary>Registra a devolução e repõe um exemplar no estoque.</summary>
    [HttpPatch("{id:guid}/devolucao")]
    [ProducesResponseType(typeof(EmprestimoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmprestimoResponse> Devolver(Guid id, CancellationToken ct) => service.DevolverAsync(id, ct);

    /// <summary>Renova o prazo por 14 dias, respeitando o limite de duas renovações.</summary>
    [HttpPatch("{id:guid}/renovar")]
    [ProducesResponseType(typeof(EmprestimoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmprestimoResponse> Renovar(Guid id, CancellationToken ct) => service.RenovarAsync(id, ct);

    /// <summary>Cancela um lançamento de empréstimo.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await service.ExcluirAsync(id, ct);
        return NoContent();
    }
}
