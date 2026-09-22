using BibliotecaEscolar.Api.Configuration;
using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BibliotecaEscolar.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AutenticacaoController(AutenticacaoService service) : ControllerBase
{
    /// <summary>Autentica o bibliotecário previamente cadastrado no banco.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(RequestSecurityOptions.LoginRateLimitPolicy)]
    [HttpPost("login")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken ct) =>
        service.EntrarAsync(request, ct);

    /// <summary>Revoga a sessão atual.</summary>
    [Authorize(Policy = "Operador")]
    [HttpPost("logout")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await service.SairAsync(Request.Headers.Authorization, ct);
        return NoContent();
    }
}
