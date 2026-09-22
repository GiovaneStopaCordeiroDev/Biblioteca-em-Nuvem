using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Services;

public sealed class UsuarioService(BibliotecaDbContext db, ICurrentOperator currentOperator)
{
    private static readonly Guid DemonstracaoId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public async Task<UsuarioAtualResponse> ObterAtualAsync(CancellationToken ct)
    {
        if (currentOperator.IsDemonstracao)
            return new(DemonstracaoId, "Bibliotecário de demonstração", "Bibliotecario", true);

        return await db.Usuarios.AsNoTracking()
            .Where(usuario => usuario.Id == currentOperator.AuthId
                || usuario.SupabaseAuthId == currentOperator.AuthId)
            .Select(usuario => new UsuarioAtualResponse(usuario.Id, usuario.Nome, usuario.Perfil, false))
            .SingleOrDefaultAsync(ct)
            ?? throw new RecursoNaoEncontradoException("Operador não encontrado.");
    }
}
