using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
[Authorize(Policy = "Operador")]
public sealed class UsuariosController(BibliotecaDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (User.Identity?.AuthenticationType == DevelopmentAuthHandler.SchemeName)
            return Ok(new { id = Guid.Parse("00000000-0000-0000-0000-000000000001"), nome = "Administrador de demonstração", perfil = "Administrador", demonstracao = true });
        var authId = Guid.Parse(User.FindFirst("sub")!.Value);
        var usuario = await db.Usuarios.AsNoTracking().SingleAsync(u => u.SupabaseAuthId == authId, ct);
        return Ok(new { usuario.Id, usuario.Nome, usuario.Perfil, demonstracao = false });
    }
}
