using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Queries;

// Objeto de consulta especializado; o DbContext continua sendo a unidade de trabalho das escritas.
public sealed class EmprestimoQueries(BibliotecaDbContext db)
{
    public async Task<Pagina<EmprestimoResponse>> ListarAsync(ConsultaEmprestimos consulta,
        string? statusNormalizado, DateOnly hoje, CancellationToken ct)
    {
        var query = db.Emprestimos.AsNoTracking().Where(x => x.CanceladoEm == null);
        var busca = string.IsNullOrWhiteSpace(consulta.Busca) ? null : consulta.Busca.Trim();
        if (busca is not null)
        {
            if (db.Database.IsNpgsql())
            {
                var padrao = PadraoBuscaSql.Contem(busca);
                query = query.Where(x => EF.Functions.ILike(x.Aluno.Nome, padrao, PadraoBuscaSql.CaractereEscape)
                    || EF.Functions.ILike(x.Livro.Titulo, padrao, PadraoBuscaSql.CaractereEscape));
            }
            else
            {
                busca = busca.ToLowerInvariant();
                // O SQLite de demonstração registra lower() com suporte Unicode.
#pragma warning disable CA1304, CA1311, CA1862
                query = query.Where(x => x.Aluno.Nome.ToLower().Contains(busca) || x.Livro.Titulo.ToLower().Contains(busca));
#pragma warning restore CA1304, CA1311, CA1862
            }
        }
        if (statusNormalizado == "ativo") query = query.Where(x => x.DataDevolucao == null);
        if (statusNormalizado == "devolvido") query = query.Where(x => x.DataDevolucao != null);
        if (consulta.AlunoId.HasValue) query = query.Where(x => x.AlunoId == consulta.AlunoId.Value);
        if (consulta.LivroId.HasValue) query = query.Where(x => x.LivroId == consulta.LivroId.Value);
        var total = await query.CountAsync(ct);
        var items = await Projetar(query.OrderByDescending(x => x.DataEmprestimo).ThenBy(x => x.Id)
            .Skip((consulta.Page - 1) * consulta.PageSize).Take(consulta.PageSize), hoje).ToListAsync(ct);
        return new(items, consulta.Page, consulta.PageSize, total);
    }

    public Task<EmprestimoResponse?> ObterAsync(Guid id, DateOnly hoje, CancellationToken ct) =>
        Projetar(db.Emprestimos.AsNoTracking().Where(x => x.Id == id && x.CanceladoEm == null), hoje)
            .SingleOrDefaultAsync(ct);

    private static IQueryable<EmprestimoResponse> Projetar(IQueryable<Emprestimo> query, DateOnly hoje) =>
        query.Select(x => new EmprestimoResponse(x.Id, x.AlunoId, x.Aluno.Nome, x.LivroId, x.Livro.Titulo,
            x.DataEmprestimo, x.DataPrevistaDevolucao, x.DataDevolucao,
            x.DataDevolucao == null ? "Ativo" : "Devolvido",
            x.DataDevolucao == null && x.DataPrevistaDevolucao < hoje));
}
