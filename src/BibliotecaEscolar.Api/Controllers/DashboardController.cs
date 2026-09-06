using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaEscolar.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Policy = "Operador")]
public sealed class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet]
    public Task<DashboardResponse> Obter(CancellationToken ct) => service.ObterAsync(ct);
}
