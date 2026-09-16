using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaEscolar.Api.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
[Authorize(Policy = "Operador")]
public sealed class UsuariosController(UsuarioService service) : ControllerBase
{
    [HttpGet("me")]
    public Task<UsuarioAtualResponse> Me(CancellationToken ct) => service.ObterAtualAsync(ct);
}
