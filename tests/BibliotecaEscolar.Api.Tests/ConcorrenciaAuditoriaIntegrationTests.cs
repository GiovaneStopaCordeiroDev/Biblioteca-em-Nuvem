using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Nodes;
using BibliotecaEscolar.Api.Controllers;
using BibliotecaEscolar.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BibliotecaEscolar.Api.Tests;

public sealed class ConcorrenciaAuditoriaIntegrationTests : IDisposable
{
    private static readonly Guid OperadorDemonstracaoId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly BibliotecaApiFactory _factory = new();
    private readonly HttpClient _client;

    public ConcorrenciaAuditoriaIntegrationTests() => _client = _factory.CreateClient();

    [Fact]
    public async Task AtualizarLivroComVersaoAntiga_RetornaConflitoESemSobrescreverDados()
    {
        var livro = await CriarLivroAsync("Versão original");
        var versaoAntiga = Texto(livro, "versao");

        using var primeira = await _client.PutAsJsonAsync($"/api/v1/livros/{Id(livro)}",
            DadosLivro("Primeira alteração", versaoAntiga));
        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        var atualizado = await LerObjetoAsync(primeira);
        Assert.NotEqual(versaoAntiga, Texto(atualizado, "versao"));

        using var obsoleta = await _client.PutAsJsonAsync($"/api/v1/livros/{Id(livro)}",
            DadosLivro("Alteração obsoleta", versaoAntiga));
        Assert.Equal(HttpStatusCode.Conflict, obsoleta.StatusCode);

        var persistido = await ObterObjetoAsync($"/api/v1/livros/{Id(livro)}");
        Assert.Equal("Primeira alteração", Texto(persistido, "titulo"));
        Assert.Equal(Texto(atualizado, "versao"), Texto(persistido, "versao"));
    }

    [Fact]
    public async Task AtualizarAlunoComVersaoAntiga_RetornaConflitoESemSobrescreverDados()
    {
        var aluno = await CriarAlunoAsync("Aluno original", "MAT-VERSAO");
        var versaoAntiga = Texto(aluno, "versao");

        using var primeira = await _client.PutAsJsonAsync($"/api/v1/alunos/{Id(aluno)}", new
        {
            nome = "Primeira alteração",
            matricula = "MAT-VERSAO",
            turma = "T1",
            versao = versaoAntiga
        });
        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        var atualizado = await LerObjetoAsync(primeira);

        using var obsoleta = await _client.PutAsJsonAsync($"/api/v1/alunos/{Id(aluno)}", new
        {
            nome = "Alteração obsoleta",
            matricula = "MAT-VERSAO",
            turma = "T2",
            versao = versaoAntiga
        });
        Assert.Equal(HttpStatusCode.Conflict, obsoleta.StatusCode);

        var persistido = await ObterObjetoAsync($"/api/v1/alunos/{Id(aluno)}");
        Assert.Equal("Primeira alteração", Texto(persistido, "nome"));
        Assert.Equal(Texto(atualizado, "versao"), Texto(persistido, "versao"));
    }

    [Fact]
    public async Task MutacoesPersistemAuditoriaAtomicaSemDadosPessoais()
    {
        const string nomeSigiloso = "Pessoa Sigilosa";
        const string matriculaSigilosa = "MATRICULA-SIGILOSA";
        const string emailSigiloso = "sigiloso@example.com";

        var livro = await CriarLivroAsync("Livro auditado");
        using (var atualizacao = await _client.PutAsJsonAsync($"/api/v1/livros/{Id(livro)}",
            DadosLivro("Livro auditado atualizado", Texto(livro, "versao"))))
        {
            Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);
            livro = await LerObjetoAsync(atualizacao);
        }

        var aluno = await CriarAlunoAsync(nomeSigiloso, matriculaSigilosa, emailSigiloso);
        using (var atualizacao = await _client.PutAsJsonAsync($"/api/v1/alunos/{Id(aluno)}", new
        {
            nome = "Outro Nome Sigiloso",
            matricula = matriculaSigilosa,
            email = emailSigiloso,
            versao = Texto(aluno, "versao")
        }))
        {
            Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);
            aluno = await LerObjetoAsync(atualizacao);
        }

        using var criacaoEmprestimo = await _client.PostAsJsonAsync("/api/v1/emprestimos", new
        {
            alunoId = Id(aluno),
            livroId = Id(livro)
        });
        Assert.Equal(HttpStatusCode.Created, criacaoEmprestimo.StatusCode);
        var emprestimo = await LerObjetoAsync(criacaoEmprestimo);

        using var renovacao = await _client.PatchAsync($"/api/v1/emprestimos/{Id(emprestimo)}/renovar", null);
        Assert.Equal(HttpStatusCode.OK, renovacao.StatusCode);
        using var devolucao = await _client.PatchAsync($"/api/v1/emprestimos/{Id(emprestimo)}/devolucao", null);
        Assert.Equal(HttpStatusCode.OK, devolucao.StatusCode);
        using var cancelamento = await _client.DeleteAsync($"/api/v1/emprestimos/{Id(emprestimo)}");
        Assert.Equal(HttpStatusCode.NoContent, cancelamento.StatusCode);

        var livroExcluido = await CriarLivroAsync("Livro descartável");
        using var exclusaoLivro = await _client.DeleteAsync($"/api/v1/livros/{Id(livroExcluido)}");
        Assert.Equal(HttpStatusCode.NoContent, exclusaoLivro.StatusCode);
        var alunoExcluido = await CriarAlunoAsync("Aluno descartável", "MAT-DESCARTAVEL");
        using var exclusaoAluno = await _client.DeleteAsync($"/api/v1/alunos/{Id(alunoExcluido)}");
        Assert.Equal(HttpStatusCode.NoContent, exclusaoAluno.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var registros = await scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>()
            .RegistrosAuditoria.AsNoTracking().ToListAsync();

        Assert.Contains(registros, x => x.Acao == "Criar" && x.Entidade == "Livro" && x.EntidadeId == Guid.Parse(Id(livro)));
        Assert.Contains(registros, x => x.Acao == "Atualizar" && x.Entidade == "Livro" && x.EntidadeId == Guid.Parse(Id(livro)));
        Assert.Contains(registros, x => x.Acao == "Criar" && x.Entidade == "Aluno" && x.EntidadeId == Guid.Parse(Id(aluno)));
        Assert.Contains(registros, x => x.Acao == "Atualizar" && x.Entidade == "Aluno" && x.EntidadeId == Guid.Parse(Id(aluno)));
        Assert.Contains(registros, x => x.Acao == "Criar" && x.Entidade == "Emprestimo" && x.EntidadeId == Guid.Parse(Id(emprestimo)));
        Assert.Contains(registros, x => x.Acao == "Renovar" && x.Entidade == "Emprestimo" && x.EntidadeId == Guid.Parse(Id(emprestimo)));
        Assert.Contains(registros, x => x.Acao == "Devolver" && x.Entidade == "Emprestimo" && x.EntidadeId == Guid.Parse(Id(emprestimo)));
        Assert.Contains(registros, x => x.Acao == "Cancelar" && x.Entidade == "Emprestimo" && x.EntidadeId == Guid.Parse(Id(emprestimo)));
        Assert.Contains(registros, x => x.Acao == "Excluir" && x.Entidade == "Livro" && x.EntidadeId == Guid.Parse(Id(livroExcluido)));
        Assert.Contains(registros, x => x.Acao == "Excluir" && x.Entidade == "Aluno" && x.EntidadeId == Guid.Parse(Id(alunoExcluido)));
        Assert.All(registros, registro =>
        {
            Assert.Equal(OperadorDemonstracaoId, registro.OperadorAuthId);
            Assert.NotEqual(default, registro.OcorridoEm);
            Assert.DoesNotContain(nomeSigiloso, registro.Detalhes ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(matriculaSigilosa, registro.Detalhes ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(emailSigiloso, registro.Detalhes ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Theory]
    [InlineData(typeof(LivrosController), nameof(LivrosController.Excluir))]
    [InlineData(typeof(AlunosController), nameof(AlunosController.Excluir))]
    [InlineData(typeof(EmprestimosController), nameof(EmprestimosController.Excluir))]
    public void RotasDestrutivas_ExigemPolicyAdministrador(Type controller, string action)
    {
        var method = controller.GetMethod(action, BindingFlags.Instance | BindingFlags.Public);
        var authorize = Assert.Single(method!.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("Administrador", authorize.Policy);
    }

    private async Task<JsonObject> CriarLivroAsync(string titulo)
    {
        using var response = await _client.PostAsJsonAsync("/api/v1/livros", DadosLivro(titulo));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await LerObjetoAsync(response);
    }

    private async Task<JsonObject> CriarAlunoAsync(
        string nome,
        string matricula,
        string? email = null)
    {
        using var response = await _client.PostAsJsonAsync("/api/v1/alunos", new
        {
            nome,
            matricula,
            email
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await LerObjetoAsync(response);
    }

    private async Task<JsonObject> ObterObjetoAsync(string rota)
    {
        using var response = await _client.GetAsync(rota);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await LerObjetoAsync(response);
    }

    private static object DadosLivro(string titulo, string? versao = null) => new
    {
        titulo,
        autor = "Autor de teste",
        quantidadeTotal = 1,
        versao
    };

    private static async Task<JsonObject> LerObjetoAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonObject>()
        ?? throw new InvalidOperationException("A API retornou JSON vazio.");

    private static string Id(JsonObject objeto) => Texto(objeto, "id");
    private static string Texto(JsonObject objeto, string propriedade) =>
        objeto[propriedade]!.GetValue<string>();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
