using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaEscolar.Api.Controllers;

[ApiController]
[Route("api/v1/alunos")]
[Authorize(Policy = "Operador")]
public sealed class AlunosController(AlunoService service) : ControllerBase
{
    [HttpGet]
    public Task<Pagina<AlunoResponse>> Listar([FromQuery] ConsultaPaginada consulta, CancellationToken ct) =>
        service.ListarAsync(consulta, ct);

    [HttpGet("{id:guid}")]
    public Task<AlunoResponse> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);

    [HttpPost]
    [ProducesResponseType(typeof(AlunoResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AlunoResponse>> Criar(SalvarAlunoRequest request, CancellationToken ct)
    {
        var aluno = await service.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = aluno.Id }, aluno);
    }

    [HttpPut("{id:guid}")]
    public Task<AlunoResponse> Atualizar(Guid id, SalvarAlunoRequest request, CancellationToken ct) =>
        service.AtualizarAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await service.ExcluirAsync(id, ct);
        return NoContent();
    }
}
