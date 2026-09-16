using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Models;
using BibliotecaEscolar.Api.Queries;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Services;

public sealed class AlunoService(BibliotecaDbContext db, AuditoriaService auditoria)
{
    public async Task<Pagina<AlunoResponse>> ListarAsync(ConsultaPaginada consulta, CancellationToken ct)
    {
        var query = db.Alunos.AsNoTracking();
        var busca = Texto.Opcional(consulta.Busca);
        if (busca is not null)
        {
            if (db.Database.IsNpgsql())
            {
                var padrao = PadraoBuscaSql.Contem(busca);
                query = query.Where(x => EF.Functions.ILike(x.Nome, padrao, PadraoBuscaSql.CaractereEscape)
                    || EF.Functions.ILike(x.Matricula, padrao, PadraoBuscaSql.CaractereEscape)
                    || (x.Turma != null && EF.Functions.ILike(x.Turma, padrao, PadraoBuscaSql.CaractereEscape)));
            }
            else
            {
                busca = busca.ToLowerInvariant();
                // O SQLite de demonstração registra lower() com suporte Unicode.
#pragma warning disable CA1304, CA1311, CA1862
                query = query.Where(x => x.Nome.ToLower().Contains(busca) || x.Matricula.ToLower().Contains(busca)
                    || (x.Turma != null && x.Turma.ToLower().Contains(busca)));
#pragma warning restore CA1304, CA1311, CA1862
            }
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Nome).ThenBy(x => x.Id)
            .Skip((consulta.Page - 1) * consulta.PageSize).Take(consulta.PageSize)
            .Select(x => new AlunoResponse(x.Id, x.Nome, x.Matricula, x.Turma, x.Email, x.Versao)).ToListAsync(ct);
        return new(items, consulta.Page, consulta.PageSize, total);
    }

    public async Task<AlunoResponse> ObterAsync(Guid id, CancellationToken ct) =>
        await db.Alunos.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new AlunoResponse(x.Id, x.Nome, x.Matricula, x.Turma, x.Email, x.Versao))
            .SingleOrDefaultAsync(ct)
        ?? throw new RecursoNaoEncontradoException("Aluno não encontrado.");

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
        // Dados pessoais do aluno não são copiados para a trilha de auditoria.
        auditoria.Registrar("Criar", "Aluno", aluno.Id);
        await db.SaveChangesAsync(ct);
        return new(aluno.Id, aluno.Nome, aluno.Matricula, aluno.Turma, aluno.Email, aluno.Versao);
    }

    public async Task<AlunoResponse> AtualizarAsync(Guid id, AtualizarAlunoRequest request, CancellationToken ct)
    {
        var versaoEsperada = ValidarVersao(request.Versao);
        var nome = Texto.Obrigatorio(request.Nome, "nome");
        var matricula = Texto.Obrigatorio(request.Matricula, "matrícula");
        var turma = Texto.Opcional(request.Turma);
        var email = Texto.Opcional(request.Email);
        var novaVersao = Guid.NewGuid();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var alterados = await db.Alunos.Where(x => x.Id == id && x.Versao == versaoEsperada)
            .ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Nome, nome).SetProperty(x => x.Matricula, matricula)
            .SetProperty(x => x.Turma, turma).SetProperty(x => x.Email, email)
            .SetProperty(x => x.Versao, novaVersao), ct);
        if (alterados == 0)
        {
            if (!await db.Alunos.AnyAsync(x => x.Id == id, ct))
                throw new RecursoNaoEncontradoException("Aluno não encontrado.");
            throw new ConflitoDeDominioException(
                "O aluno foi alterado por outro pedido. Recarregue os dados e tente novamente.");
        }

        auditoria.Registrar("Atualizar", "Aluno", id);
        await db.SaveChangesAsync(ct);
        var response = await ObterAsync(id, ct);
        await transaction.CommitAsync(ct);
        return response;
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var excluidos = await db.Alunos.Where(x => x.Id == id && !db.Emprestimos.Any(e => e.AlunoId == x.Id))
            .ExecuteDeleteAsync(ct);
        if (excluidos > 0)
        {
            auditoria.Registrar("Excluir", "Aluno", id);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return;
        }
        if (!await db.Alunos.AnyAsync(x => x.Id == id, ct))
            throw new RecursoNaoEncontradoException("Aluno não encontrado.");
        throw new ConflitoDeDominioException("O aluno possui histórico de empréstimos e não pode ser excluído.");
    }

    private static Guid ValidarVersao(Guid? versao) => versao is { } valor && valor != Guid.Empty
        ? valor
        : throw new RequisicaoInvalidaException("Informe uma versão válida do aluno.");
}
