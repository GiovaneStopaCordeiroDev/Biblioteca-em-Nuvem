using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Services;

public sealed class LivroService(BibliotecaDbContext db)
{
    public async Task<Pagina<LivroResponse>> ListarAsync(ConsultaPaginada consulta, CancellationToken ct)
    {
        var query = db.Livros.AsNoTracking();
        var busca = Texto.Opcional(consulta.Busca)?.ToLowerInvariant();
        if (busca is not null)
            query = query.Where(x => x.Titulo.ToLower().Contains(busca) || x.Autor.ToLower().Contains(busca)
                || (x.Isbn != null && x.Isbn.ToLower().Contains(busca)));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Titulo).ThenBy(x => x.Id)
            .Skip((consulta.Page - 1) * consulta.PageSize).Take(consulta.PageSize)
            .Select(x => new LivroResponse(x.Id, x.Titulo, x.Autor, x.Isbn, x.Categoria,
                x.QuantidadeTotal, x.QuantidadeDisponivel)).ToListAsync(ct);
        return new(items, consulta.Page, consulta.PageSize, total);
    }

    public async Task<LivroResponse> ObterAsync(Guid id, CancellationToken ct) =>
        await db.Livros.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new LivroResponse(x.Id, x.Titulo, x.Autor, x.Isbn, x.Categoria,
                x.QuantidadeTotal, x.QuantidadeDisponivel)).SingleOrDefaultAsync(ct)
        ?? throw new ApiException(404, "Livro não encontrado.");

    public async Task<LivroResponse> CriarAsync(SalvarLivroRequest request, CancellationToken ct)
    {
        ValidarQuantidade(request.QuantidadeTotal);
        var livro = new Livro
        {
            Titulo = Texto.Obrigatorio(request.Titulo, "título"),
            Autor = Texto.Obrigatorio(request.Autor, "autor"),
            Isbn = Texto.Opcional(request.Isbn),
            Categoria = Texto.Opcional(request.Categoria),
            QuantidadeTotal = request.QuantidadeTotal,
            QuantidadeDisponivel = request.QuantidadeTotal
        };
        db.Livros.Add(livro);
        await db.SaveChangesAsync(ct);
        return new(livro.Id, livro.Titulo, livro.Autor, livro.Isbn, livro.Categoria,
            livro.QuantidadeTotal, livro.QuantidadeDisponivel);
    }

    public async Task<LivroResponse> AtualizarAsync(Guid id, SalvarLivroRequest request, CancellationToken ct)
    {
        ValidarQuantidade(request.QuantidadeTotal);
        var titulo = Texto.Obrigatorio(request.Titulo, "título");
        var autor = Texto.Obrigatorio(request.Autor, "autor");
        var isbn = Texto.Opcional(request.Isbn);
        var categoria = Texto.Opcional(request.Categoria);
        // O mesmo UPDATE verifica o estoque atual e preserva os exemplares emprestados.
        // Não há leitura seguida de escrita que possa perder um empréstimo concorrente.
        var alterados = await db.Livros.Where(x => x.Id == id
                && x.QuantidadeTotal - x.QuantidadeDisponivel <= request.QuantidadeTotal)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Titulo, titulo)
                .SetProperty(x => x.Autor, autor)
                .SetProperty(x => x.Isbn, isbn)
                .SetProperty(x => x.Categoria, categoria)
                .SetProperty(x => x.QuantidadeDisponivel,
                    x => request.QuantidadeTotal - (x.QuantidadeTotal - x.QuantidadeDisponivel))
                .SetProperty(x => x.QuantidadeTotal, request.QuantidadeTotal), ct);
        if (alterados == 0)
        {
            if (!await db.Livros.AnyAsync(x => x.Id == id, ct))
                throw new ApiException(404, "Livro não encontrado.");
            throw new ApiException(409, "A quantidade total não pode ser menor que a quantidade de exemplares emprestados.");
        }
        return await ObterAsync(id, ct);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        var excluidos = await db.Livros.Where(x => x.Id == id && !db.Emprestimos.Any(e => e.LivroId == x.Id))
            .ExecuteDeleteAsync(ct);
        if (excluidos > 0) return;
        if (!await db.Livros.AnyAsync(x => x.Id == id, ct))
            throw new ApiException(404, "Livro não encontrado.");
        throw new ApiException(409, "O livro possui histórico de empréstimos e não pode ser excluído.");
    }

    private static void ValidarQuantidade(int quantidade)
    {
        if (quantidade < 1 || quantidade > 1_000_000)
            throw new ApiException(400, "A quantidade total deve estar entre 1 e 1000000.");
    }
}
