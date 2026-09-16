using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Models;
using BibliotecaEscolar.Api.Queries;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Services;

public sealed class LivroService(BibliotecaDbContext db, AuditoriaService auditoria)
{
    public async Task<Pagina<LivroResponse>> ListarAsync(ConsultaPaginada consulta, CancellationToken ct)
    {
        var query = db.Livros.AsNoTracking();
        var busca = Texto.Opcional(consulta.Busca);
        if (busca is not null)
        {
            if (db.Database.IsNpgsql())
            {
                var padrao = PadraoBuscaSql.Contem(busca);
                query = query.Where(x => EF.Functions.ILike(x.Titulo, padrao, PadraoBuscaSql.CaractereEscape)
                    || EF.Functions.ILike(x.Autor, padrao, PadraoBuscaSql.CaractereEscape)
                    || (x.Isbn != null && EF.Functions.ILike(x.Isbn, padrao, PadraoBuscaSql.CaractereEscape)));
            }
            else
            {
                busca = busca.ToLowerInvariant();
                // O SQLite de demonstração registra lower() com suporte Unicode.
#pragma warning disable CA1304, CA1311, CA1862
                query = query.Where(x => x.Titulo.ToLower().Contains(busca) || x.Autor.ToLower().Contains(busca)
                    || (x.Isbn != null && x.Isbn.ToLower().Contains(busca)));
#pragma warning restore CA1304, CA1311, CA1862
            }
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Titulo).ThenBy(x => x.Id)
            .Skip((consulta.Page - 1) * consulta.PageSize).Take(consulta.PageSize)
            .Select(x => new LivroResponse(x.Id, x.Titulo, x.Autor, x.Isbn, x.Categoria,
                x.QuantidadeTotal, x.QuantidadeDisponivel, x.Versao)).ToListAsync(ct);
        return new(items, consulta.Page, consulta.PageSize, total);
    }

    public async Task<LivroResponse> ObterAsync(Guid id, CancellationToken ct) =>
        await db.Livros.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new LivroResponse(x.Id, x.Titulo, x.Autor, x.Isbn, x.Categoria,
                x.QuantidadeTotal, x.QuantidadeDisponivel, x.Versao)).SingleOrDefaultAsync(ct)
        ?? throw new RecursoNaoEncontradoException("Livro não encontrado.");

    public async Task<LivroResponse> CriarAsync(SalvarLivroRequest request, CancellationToken ct)
    {
        ValidarQuantidade(request.QuantidadeTotal);
        var isbn = Isbn.NormalizarEValidar(request.Isbn);
        await ValidarIsbnDisponivelAsync(isbn, null, ct);
        var livro = new Livro
        {
            Titulo = Texto.Obrigatorio(request.Titulo, "título"),
            Autor = Texto.Obrigatorio(request.Autor, "autor"),
            Isbn = isbn,
            Categoria = Texto.Opcional(request.Categoria),
            QuantidadeTotal = request.QuantidadeTotal,
            QuantidadeDisponivel = request.QuantidadeTotal
        };
        db.Livros.Add(livro);
        auditoria.Registrar("Criar", "Livro", livro.Id, new { livro.QuantidadeTotal });
        await db.SaveChangesAsync(ct);
        return new(livro.Id, livro.Titulo, livro.Autor, livro.Isbn, livro.Categoria,
            livro.QuantidadeTotal, livro.QuantidadeDisponivel, livro.Versao);
    }

    public async Task<LivroResponse> AtualizarAsync(Guid id, AtualizarLivroRequest request, CancellationToken ct)
    {
        ValidarQuantidade(request.QuantidadeTotal);
        var versaoEsperada = ValidarVersao(request.Versao);
        var titulo = Texto.Obrigatorio(request.Titulo, "título");
        var autor = Texto.Obrigatorio(request.Autor, "autor");
        var isbn = Isbn.NormalizarEValidar(request.Isbn);
        var categoria = Texto.Opcional(request.Categoria);
        var novaVersao = Guid.NewGuid();
        await ValidarIsbnDisponivelAsync(isbn, id, ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Um único UPDATE valida a versão, preserva empréstimos e impede escrita perdida.
        var alterados = await db.Livros.Where(x => x.Id == id
                && x.Versao == versaoEsperada
                && x.QuantidadeTotal - x.QuantidadeDisponivel <= request.QuantidadeTotal)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Titulo, titulo)
                .SetProperty(x => x.Autor, autor)
                .SetProperty(x => x.Isbn, isbn)
                .SetProperty(x => x.Categoria, categoria)
                .SetProperty(x => x.QuantidadeDisponivel,
                    x => request.QuantidadeTotal - (x.QuantidadeTotal - x.QuantidadeDisponivel))
                .SetProperty(x => x.QuantidadeTotal, request.QuantidadeTotal)
                .SetProperty(x => x.Versao, novaVersao), ct);
        if (alterados == 0)
        {
            var estado = await db.Livros.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { x.Versao, Emprestados = x.QuantidadeTotal - x.QuantidadeDisponivel })
                .SingleOrDefaultAsync(ct);
            if (estado is null)
                throw new RecursoNaoEncontradoException("Livro não encontrado.");
            if (estado.Versao != versaoEsperada)
                throw new ConflitoDeDominioException("O livro foi alterado por outro pedido. Recarregue os dados e tente novamente.");
            throw new ConflitoDeDominioException(
                "A quantidade total não pode ser menor que a quantidade de exemplares emprestados.");
        }

        auditoria.Registrar("Atualizar", "Livro", id, new { request.QuantidadeTotal });
        await db.SaveChangesAsync(ct);
        var response = await ObterAsync(id, ct);
        await transaction.CommitAsync(ct);
        return response;
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var excluidos = await db.Livros.Where(x => x.Id == id && !db.Emprestimos.Any(e => e.LivroId == x.Id))
            .ExecuteDeleteAsync(ct);
        if (excluidos > 0)
        {
            auditoria.Registrar("Excluir", "Livro", id);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return;
        }
        if (!await db.Livros.AnyAsync(x => x.Id == id, ct))
            throw new RecursoNaoEncontradoException("Livro não encontrado.");
        throw new ConflitoDeDominioException("O livro possui histórico de empréstimos e não pode ser excluído.");
    }

    private static void ValidarQuantidade(int quantidade)
    {
        if (quantidade < 1 || quantidade > 1_000_000)
            throw new RequisicaoInvalidaException("A quantidade total deve estar entre 1 e 1000000.");
    }

    private static Guid ValidarVersao(Guid? versao) => versao is { } valor && valor != Guid.Empty
        ? valor
        : throw new RequisicaoInvalidaException("Informe uma versão válida do livro.");

    private async Task ValidarIsbnDisponivelAsync(string? isbn, Guid? livroId, CancellationToken ct)
    {
        if (isbn is not null && await db.Livros.AnyAsync(
                livro => livro.Isbn == isbn && (!livroId.HasValue || livro.Id != livroId.Value), ct))
            throw new ConflitoDeDominioException("Já existe um livro cadastrado com este ISBN.");
    }
}
