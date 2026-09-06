using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Repositories;

// Reúne a consulta especializada da tela, incluindo nomes, filtros e paginação.
public sealed class EmprestimoRepository(BibliotecaDbContext db)
{
    public async Task<Pagina<EmprestimoResponse>> ListarAsync(ConsultaEmprestimos consulta,
        string? statusNormalizado, DateOnly hoje, CancellationToken ct)
    {
        var query = db.Emprestimos.AsNoTracking().Where(x => x.CanceladoEm == null);
        var busca = string.IsNullOrWhiteSpace(consulta.Busca) ? null : consulta.Busca.Trim().ToLowerInvariant();
        if (busca is not null)
            query = query.Where(x => x.Aluno.Nome.ToLower().Contains(busca) || x.Livro.Titulo.ToLower().Contains(busca));
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
