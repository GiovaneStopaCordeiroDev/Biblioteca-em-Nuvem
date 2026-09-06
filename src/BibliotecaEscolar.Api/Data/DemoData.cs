using BibliotecaEscolar.Api.Models;
using BibliotecaEscolar.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Data;

public static class DemoData
{
    public static async Task SeedAsync(BibliotecaDbContext db, BibliotecaClock clock)
    {
        // Apenas um banco local vazio recebe exemplos. Não altera cadastros existentes.
        if (await db.Livros.AnyAsync() || await db.Alunos.AnyAsync() || await db.Emprestimos.AnyAsync()) return;
        var ana = new Aluno { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Nome = "Ana Beatriz (exemplo)", Matricula = "DEMO-001", Turma = "9º A" };
        var lucas = new Aluno { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Nome = "Lucas Almeida (exemplo)", Matricula = "DEMO-002", Turma = "8º B" };
        var dom = new Livro { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Titulo = "Dom Casmurro", Autor = "Machado de Assis", Categoria = "Literatura brasileira", QuantidadeTotal = 3, QuantidadeDisponivel = 2 };
        var principe = new Livro { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Titulo = "O Pequeno Príncipe", Autor = "Antoine de Saint-Exupéry", Categoria = "Literatura", QuantidadeTotal = 2, QuantidadeDisponivel = 2 };
        db.Alunos.AddRange(ana, lucas);
        db.Livros.AddRange(dom, principe);
        db.Emprestimos.AddRange(
            new Emprestimo { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), AlunoId = ana.Id, LivroId = dom.Id, DataEmprestimo = clock.Today.AddDays(-1), DataPrevistaDevolucao = clock.Today.AddDays(13) },
            new Emprestimo { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), AlunoId = lucas.Id, LivroId = principe.Id, DataEmprestimo = clock.Today.AddDays(-4), DataPrevistaDevolucao = clock.Today.AddDays(10), DataDevolucao = clock.Today.AddDays(-1) });
        await db.SaveChangesAsync();
    }
}
