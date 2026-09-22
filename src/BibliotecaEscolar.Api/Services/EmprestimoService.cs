using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Models;
using BibliotecaEscolar.Api.Queries;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Services;

public sealed class EmprestimoService(
    BibliotecaDbContext db,
    EmprestimoQueries queries,
    BibliotecaClock clock,
    AuditoriaService auditoria)
{
    private const int DiasPorRenovacao = 14;
    private const int MaximoRenovacoes = 2;

    public Task<Pagina<EmprestimoResponse>> ListarAsync(ConsultaEmprestimos consulta, CancellationToken ct)
    {
        var status = Texto.Opcional(consulta.Status)?.ToLowerInvariant();
        if (status is not null and not "ativo" and not "devolvido" and not "atrasado" and not "cancelado")
            throw new RequisicaoInvalidaException(
                "Status inválido. Use Ativo, Devolvido, Atrasado ou Cancelado, ou omita o filtro.");
        return queries.ListarAsync(consulta, status, clock.Today, ct);
    }

    public async Task<EmprestimoResponse> ObterAsync(Guid id, CancellationToken ct) =>
        await queries.ObterAsync(id, clock.Today, ct)
        ?? throw new RecursoNaoEncontradoException("Empréstimo não encontrado.");

    public async Task<EmprestimoResponse> CriarAsync(CriarEmprestimoRequest request, CancellationToken ct)
    {
        if (request.LivroId == Guid.Empty)
            throw new RequisicaoInvalidaException("Informe um livroId válido.");
        var hoje = clock.Today;
        var prevista = request.DataPrevistaDevolucao ?? hoje.AddDays(14);
        var observacao = ValidarObservacao(request.Observacao);
        if (prevista < hoje)
            throw new RequisicaoInvalidaException("A data prevista de devolução não pode ser anterior ao empréstimo.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var alunoNome = ValidarAlunoNome(request.AlunoNome);
        var livroTitulo = await db.Livros.Where(x => x.Id == request.LivroId).Select(x => x.Titulo).SingleOrDefaultAsync(ct)
            ?? throw new RecursoNaoEncontradoException("Livro não encontrado.");
        // A condição é avaliada pelo banco durante a escrita: dois pedidos não reservam a última cópia.
        var reservados = await db.Livros.Where(x => x.Id == request.LivroId && x.QuantidadeDisponivel > 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.QuantidadeDisponivel, x => x.QuantidadeDisponivel - 1)
                .SetProperty(x => x.Versao, Guid.NewGuid()), ct);
        if (reservados == 0)
            throw new ConflitoDeDominioException("Não há exemplares disponíveis para empréstimo.");

        var emprestimo = new Emprestimo
        {
            AlunoNome = alunoNome,
            LivroId = request.LivroId,
            DataEmprestimo = hoje,
            DataPrevistaDevolucao = prevista,
            Observacao = observacao
        };
        db.Emprestimos.Add(emprestimo);
        auditoria.Registrar("Criar", "Emprestimo", emprestimo.Id,
            new { request.LivroId, DataPrevistaDevolucao = prevista });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(emprestimo.Id, alunoNome, request.LivroId, livroTitulo,
            hoje, prevista, null, "Ativo", false, 0, observacao);
    }

    public async Task<EmprestimoResponse> DevolverAsync(Guid id, CancellationToken ct)
    {
        var hoje = clock.Today;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var emprestimo = await db.Emprestimos.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.CanceladoEm == null, ct)
            ?? throw new RecursoNaoEncontradoException("Empréstimo não encontrado.");
        if (emprestimo.DataDevolucao.HasValue)
            throw new ConflitoDeDominioException("Este empréstimo já foi devolvido.");
        if (hoje < emprestimo.DataEmprestimo)
            throw new ConflitoDeDominioException("A data atual é anterior à data do empréstimo.");

        var alterados = await db.Emprestimos.Where(x => x.Id == id && x.CanceladoEm == null && x.DataDevolucao == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.DataDevolucao, (DateOnly?)hoje), ct);
        if (alterados == 0)
            throw new ConflitoDeDominioException("O empréstimo foi alterado por outro pedido. Atualize a lista.");
        await ReporExemplarAsync(emprestimo.LivroId, ct);
        auditoria.Registrar("Devolver", "Emprestimo", id, new { emprestimo.LivroId });
        await db.SaveChangesAsync(ct);
        var response = await ObterAsync(id, ct);
        await transaction.CommitAsync(ct);
        return response;
    }

    public async Task<EmprestimoResponse> RenovarAsync(Guid id, CancellationToken ct)
    {
        var hoje = clock.Today;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var emprestimo = await db.Emprestimos.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.CanceladoEm == null, ct)
            ?? throw new RecursoNaoEncontradoException("Empréstimo não encontrado.");

        if (emprestimo.DataDevolucao.HasValue)
            throw new ConflitoDeDominioException("Um empréstimo devolvido não pode ser renovado.");
        if (emprestimo.DataPrevistaDevolucao < hoje)
            throw new ConflitoDeDominioException("Um empréstimo atrasado não pode ser renovado.");
        if (emprestimo.QuantidadeRenovacoes >= MaximoRenovacoes)
            throw new ConflitoDeDominioException($"O limite de {MaximoRenovacoes} renovações foi atingido.");

        var novaData = emprestimo.DataPrevistaDevolucao.AddDays(DiasPorRenovacao);
        var novaQuantidade = emprestimo.QuantidadeRenovacoes + 1;
        var alterados = await db.Emprestimos.Where(x => x.Id == id
                && x.CanceladoEm == null
                && x.DataDevolucao == null
                && x.DataPrevistaDevolucao == emprestimo.DataPrevistaDevolucao
                && x.QuantidadeRenovacoes == emprestimo.QuantidadeRenovacoes)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.DataPrevistaDevolucao, novaData)
                .SetProperty(x => x.QuantidadeRenovacoes, novaQuantidade), ct);
        if (alterados == 0)
            throw new ConflitoDeDominioException("O empréstimo foi alterado por outro pedido. Atualize a lista.");

        auditoria.Registrar("Renovar", "Emprestimo", id,
            new { DataPrevistaDevolucao = novaData, QuantidadeRenovacoes = novaQuantidade });
        await db.SaveChangesAsync(ct);
        var response = await ObterAsync(id, ct);
        await transaction.CommitAsync(ct);
        return response;
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var emprestimo = await db.Emprestimos.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.CanceladoEm == null, ct)
            ?? throw new RecursoNaoEncontradoException("Empréstimo não encontrado.");
        var canceladoEm = clock.UtcNow;
        // Excluir oculta o registro, preserva o histórico e repõe estoque somente se estava ativo.
        // O predicado impede que devolução e exclusão simultâneas creditem a mesma cópia duas vezes.
        var alterados = await db.Emprestimos.Where(x => x.Id == id && x.CanceladoEm == null
                && x.DataDevolucao == emprestimo.DataDevolucao)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CanceladoEm, (DateTimeOffset?)canceladoEm), ct);
        if (alterados == 0)
            throw new ConflitoDeDominioException("O empréstimo foi alterado por outro pedido. Atualize a lista.");
        if (!emprestimo.DataDevolucao.HasValue) await ReporExemplarAsync(emprestimo.LivroId, ct);
        auditoria.Registrar("Cancelar", "Emprestimo", id,
            new { emprestimo.LivroId, EstavaAtivo = !emprestimo.DataDevolucao.HasValue });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task ReporExemplarAsync(Guid livroId, CancellationToken ct)
    {
        var alterados = await db.Livros.Where(x => x.Id == livroId && x.QuantidadeDisponivel < x.QuantidadeTotal)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.QuantidadeDisponivel, x => x.QuantidadeDisponivel + 1)
                .SetProperty(x => x.Versao, Guid.NewGuid()), ct);
        if (alterados == 0)
            throw new ConflitoDeDominioException(
                "O estoque está inconsistente. A operação foi desfeita; revise os dados da biblioteca.");
    }

    private static string ValidarAlunoNome(string? valor)
    {
        var nome = Texto.Obrigatorio(valor, "nome do aluno");
        if (nome.Length > 150)
            throw new RequisicaoInvalidaException("O nome do aluno deve ter no máximo 150 caracteres.");
        return nome;
    }

    private static string? ValidarObservacao(string? valor)
    {
        var observacao = Texto.Opcional(valor);
        if (observacao?.Length > 500)
            throw new RequisicaoInvalidaException("A observação deve ter no máximo 500 caracteres.");
        return observacao;
    }
}
