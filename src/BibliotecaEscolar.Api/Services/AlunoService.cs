using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Services;

public sealed class AlunoService(BibliotecaDbContext db)
{
    public async Task<Pagina<AlunoResponse>> ListarAsync(ConsultaPaginada consulta, CancellationToken ct)
    {
        var query = db.Alunos.AsNoTracking();
        var busca = Texto.Opcional(consulta.Busca)?.ToLowerInvariant();
        if (busca is not null)
            query = query.Where(x => x.Nome.ToLower().Contains(busca) || x.Matricula.ToLower().Contains(busca)
                || (x.Turma != null && x.Turma.ToLower().Contains(busca)));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Nome).ThenBy(x => x.Id)
            .Skip((consulta.Page - 1) * consulta.PageSize).Take(consulta.PageSize)
            .Select(x => new AlunoResponse(x.Id, x.Nome, x.Matricula, x.Turma, x.Email)).ToListAsync(ct);
        return new(items, consulta.Page, consulta.PageSize, total);
    }

    public async Task<AlunoResponse> ObterAsync(Guid id, CancellationToken ct) =>
        await db.Alunos.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new AlunoResponse(x.Id, x.Nome, x.Matricula, x.Turma, x.Email)).SingleOrDefaultAsync(ct)
        ?? throw new ApiException(404, "Aluno não encontrado.");

    public async Task<AlunoResponse> CriarAsync(SalvarAlunoRequest request, CancellationToken ct)
    {
        var aluno = new Aluno
        {
            Nome = Texto.Obrigatorio(request.Nome, "nome"),
            Matricula = Texto.Obrigatorio(request.Matricula, "matrícula"),
            Turma = Texto.Opcional(request.Turma),
            Email = Texto.Opcional(request.Email)
        };
        db.Alunos.Add(aluno);
        await db.SaveChangesAsync(ct);
        return new(aluno.Id, aluno.Nome, aluno.Matricula, aluno.Turma, aluno.Email);
    }

    public async Task<AlunoResponse> AtualizarAsync(Guid id, SalvarAlunoRequest request, CancellationToken ct)
    {
        var nome = Texto.Obrigatorio(request.Nome, "nome");
        var matricula = Texto.Obrigatorio(request.Matricula, "matrícula");
        var turma = Texto.Opcional(request.Turma);
        var email = Texto.Opcional(request.Email);
        var alterados = await db.Alunos.Where(x => x.Id == id).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Nome, nome).SetProperty(x => x.Matricula, matricula)
            .SetProperty(x => x.Turma, turma).SetProperty(x => x.Email, email), ct);
        if (alterados == 0) throw new ApiException(404, "Aluno não encontrado.");
        return await ObterAsync(id, ct);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        var excluidos = await db.Alunos.Where(x => x.Id == id && !db.Emprestimos.Any(e => e.AlunoId == x.Id))
            .ExecuteDeleteAsync(ct);
        if (excluidos > 0) return;
        if (!await db.Alunos.AnyAsync(x => x.Id == id, ct))
            throw new ApiException(404, "Aluno não encontrado.");
        throw new ApiException(409, "O aluno possui histórico de empréstimos e não pode ser excluído.");
    }
}
