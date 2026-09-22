using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Services;

public sealed class DashboardService(BibliotecaDbContext db, BibliotecaClock clock)
{
    public async Task<DashboardResponse> ObterAsync(CancellationToken ct)
    {
        var hoje = clock.Today;
        var livros = await db.Livros.CountAsync(ct);
        var exemplares = await db.Livros.SumAsync(x => (int?)x.QuantidadeTotal, ct) ?? 0;
        var disponiveis = await db.Livros.SumAsync(x => (int?)x.QuantidadeDisponivel, ct) ?? 0;
        var ativos = db.Emprestimos.Where(x => x.CanceladoEm == null && x.DataDevolucao == null);
        return new(livros, exemplares, disponiveis, await ativos.CountAsync(ct),
            await ativos.CountAsync(x => x.DataPrevistaDevolucao < hoje, ct));
    }
}
