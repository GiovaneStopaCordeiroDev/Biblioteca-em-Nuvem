using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace BibliotecaEscolar.Api.Tests;

public sealed class EmprestimosIntegrationTests : IDisposable
{
    private readonly BibliotecaApiFactory _factory = new();
    private readonly HttpClient _client;
    private const string Livros = "/api/v1/livros";
    private const string Emprestimos = "/api/v1/emprestimos";

    public EmprestimosIntegrationTests() => _client = _factory.CreateClient();

    [Fact]
    public async Task CriarListarDevolver_AtualizaStatusEDisponibilidade()
    {
        var livro = await CriarLivro(quantidade: 2);
        var emprestimo = await CriarEmprestimo("Ana Beatriz", livro);

        Assert.Equal("Ativo", Texto(emprestimo, "status"));
        Assert.Equal("Ana Beatriz", Texto(emprestimo, "alunoNome"));
        Assert.Equal("Dom Casmurro", Texto(emprestimo, "livroTitulo"));
        Assert.Null(emprestimo["dataDevolucao"]);
        Assert.False(emprestimo["atrasado"]!.GetValue<bool>());
        await AssertSaldo(livro, total: 2, disponivel: 1);

        var lista = await ObterObjeto($"{Emprestimos}?busca=Ana&status=Ativo&page=1&pageSize=10");
        Assert.Equal(1, Numero(lista, "totalCount"));
        Assert.Equal(1, Numero(lista, "page"));
        Assert.Equal(10, Numero(lista, "pageSize"));
        Assert.Equal(1, Numero(lista, "totalPages"));
        Assert.Single(lista["items"]!.AsArray());

        using var resposta = await _client.PatchAsync($"{Emprestimos}/{Id(emprestimo)}/devolucao", null);
        await AssertStatus(resposta, HttpStatusCode.OK);
        var devolvido = await LerObjeto(resposta);
        Assert.Equal("Devolvido", Texto(devolvido, "status"));
        Assert.NotNull(devolvido["dataDevolucao"]);
        await AssertSaldo(livro, total: 2, disponivel: 2);

        using var repetida = await _client.PatchAsync($"{Emprestimos}/{Id(emprestimo)}/devolucao", null);
        await AssertProblema(repetida, HttpStatusCode.Conflict);
        await AssertSaldo(livro, total: 2, disponivel: 2);
    }

    [Fact]
    public async Task Renovar_AmpliaPrazoDuasVezesEPreservaObservacao()
    {
        var livro = await CriarLivro();
        var prazoInicial = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        using var criacao = await _client.PostAsJsonAsync(Emprestimos, new
        {
            alunoNome = "Ana Beatriz",
            livroId = Id(livro),
            dataPrevistaDevolucao = prazoInicial,
            observacao = "  Entregar na biblioteca central  "
        });
        await AssertStatus(criacao, HttpStatusCode.Created);
        var emprestimo = await LerObjeto(criacao);
        Assert.Equal(0, Numero(emprestimo, "quantidadeRenovacoes"));
        Assert.Equal("Entregar na biblioteca central", Texto(emprestimo, "observacao"));

        using var primeira = await _client.PatchAsync($"{Emprestimos}/{Id(emprestimo)}/renovar", null);
        await AssertStatus(primeira, HttpStatusCode.OK);
        var primeiraRenovacao = await LerObjeto(primeira);
        Assert.Equal(1, Numero(primeiraRenovacao, "quantidadeRenovacoes"));
        Assert.Equal(FormatarData(prazoInicial.AddDays(14)), Texto(primeiraRenovacao, "dataPrevistaDevolucao"));

        using var segunda = await _client.PatchAsync($"{Emprestimos}/{Id(emprestimo)}/renovar", null);
        await AssertStatus(segunda, HttpStatusCode.OK);
        var segundaRenovacao = await LerObjeto(segunda);
        Assert.Equal(2, Numero(segundaRenovacao, "quantidadeRenovacoes"));
        Assert.Equal(FormatarData(prazoInicial.AddDays(28)), Texto(segundaRenovacao, "dataPrevistaDevolucao"));

        using var excedente = await _client.PatchAsync($"{Emprestimos}/{Id(emprestimo)}/renovar", null);
        await AssertProblema(excedente, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task NomesIguais_NaoSaoTratadosComoIdentidadeDoAluno()
    {
        var livro = await CriarLivro(quantidade: 2);
        await CriarEmprestimo("João Silva", livro);

        using var segundo = await _client.PostAsJsonAsync(Emprestimos, Pedido("João Silva", livro));
        await AssertStatus(segundo, HttpStatusCode.Created);
        await AssertSaldo(livro, total: 2, disponivel: 0);
    }

    [Fact]
    public async Task SemExemplarDisponivel_RejeitaEmprestimoDeOutroAluno()
    {
        var livro = await CriarLivro(quantidade: 1);
        await CriarEmprestimo("Ana", livro);

        using var semEstoque = await _client.PostAsJsonAsync(Emprestimos, Pedido("Lucas", livro));
        await AssertProblema(semEstoque, HttpStatusCode.Conflict);
        await AssertSaldo(livro, total: 1, disponivel: 0);
    }

    [Fact]
    public async Task ExcluirEmprestimoAtivo_LiberaExemplarUmaVezEOcultaRegistro()
    {
        var livro = await CriarLivro(quantidade: 1);
        const string alunoNome = "Ana Beatriz";
        var emprestimo = await CriarEmprestimo(alunoNome, livro);
        var rota = $"{Emprestimos}/{Id(emprestimo)}";

        using var exclusao = await _client.DeleteAsync(rota);
        await AssertStatus(exclusao, HttpStatusCode.NoContent);
        await AssertSaldo(livro, total: 1, disponivel: 1);

        using var excluido = await _client.GetAsync(rota);
        await AssertProblema(excluido, HttpStatusCode.NotFound);
        using var repetida = await _client.DeleteAsync(rota);
        await AssertProblema(repetida, HttpStatusCode.NotFound);
        await AssertSaldo(livro, total: 1, disponivel: 1);
        Assert.Equal(0, Numero(await ObterObjeto(Emprestimos), "totalCount"));
        var cancelados = await ObterObjeto($"{Emprestimos}?status=Cancelado");
        Assert.Equal(1, Numero(cancelados, "totalCount"));
        Assert.Equal("Cancelado", Texto(cancelados["items"]![0]!.AsObject(), "status"));

        // O cancelamento não impede que o mesmo aluno retire o livro novamente.
        await CriarEmprestimo(alunoNome, livro);
        await AssertSaldo(livro, total: 1, disponivel: 0);
    }

    [Fact]
    public async Task ExcluirEmprestimoDevolvido_NaoAumentaQuantidadeDisponivel()
    {
        var livro = await CriarLivro(quantidade: 1);
        var emprestimo = await CriarEmprestimo("Ana Beatriz", livro);
        using var devolucao = await _client.PatchAsync($"{Emprestimos}/{Id(emprestimo)}/devolucao", null);
        await AssertStatus(devolucao, HttpStatusCode.OK);

        using var exclusao = await _client.DeleteAsync($"{Emprestimos}/{Id(emprestimo)}");
        await AssertStatus(exclusao, HttpStatusCode.NoContent);
        await AssertSaldo(livro, total: 1, disponivel: 1);
    }

    [Fact]
    public async Task PrevisaoDeDevolucaoNoPassado_RetornaValidacaoSemConsumirExemplar()
    {
        var livro = await CriarLivro();
        using var resposta = await _client.PostAsJsonAsync(Emprestimos, new
        {
            alunoNome = "Ana Beatriz",
            livroId = Id(livro),
            dataPrevistaDevolucao = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7))
        });

        await AssertProblema(resposta, HttpStatusCode.BadRequest);
        await AssertSaldo(livro, total: 1, disponivel: 1);
    }

    [Fact]
    public async Task NomeDoAlunoAusente_Retorna400SemConsumirExemplar()
    {
        var livro = await CriarLivro();
        using var resposta = await _client.PostAsJsonAsync(Emprestimos, new
        {
            livroId = Id(livro)
        });

        await AssertProblema(resposta, HttpStatusCode.BadRequest);
        await AssertSaldo(livro, total: 1, disponivel: 1);
    }

    [Fact]
    public async Task BuscarPorAlunoOuLivroEStatus_RetornaSomenteRegistrosCorrespondentes()
    {
        var primeiroLivro = await CriarLivro(titulo: "Dom Casmurro");
        var segundoLivro = await CriarLivro(titulo: "O Pequeno Principe");
        var ativo = await CriarEmprestimo("Ana Beatriz", primeiroLivro);
        var devolvido = await CriarEmprestimo("Lucas Almeida", segundoLivro);
        using var devolucao = await _client.PatchAsync($"{Emprestimos}/{Id(devolvido)}/devolucao", null);
        await AssertStatus(devolucao, HttpStatusCode.OK);

        var porAluno = await ObterObjeto($"{Emprestimos}?busca=beatriz&status=Ativo");
        Assert.Equal(1, Numero(porAluno, "totalCount"));
        Assert.Equal(Id(ativo), Id(porAluno["items"]![0]!.AsObject()));

        var porLivro = await ObterObjeto($"{Emprestimos}?busca=pequeno&status=Devolvido");
        Assert.Equal(1, Numero(porLivro, "totalCount"));
        Assert.Equal(Id(devolvido), Id(porLivro["items"]![0]!.AsObject()));

        var semResultado = await ObterObjeto($"{Emprestimos}?busca=beatriz&status=Devolvido");
        Assert.Equal(0, Numero(semResultado, "totalCount"));
        Assert.Empty(semResultado["items"]!.AsArray());
    }

    [Theory]
    [InlineData("/api/v1/emprestimos?status=Inexistente")]
    [InlineData("/api/v1/emprestimos?pageSize=101")]
    [InlineData("/api/v1/emprestimos?page=0")]
    [InlineData("/api/v1/livros?pageSize=101")]
    public async Task ConsultaInvalida_RetornaProblemDetails(string rota)
    {
        using var resposta = await _client.GetAsync(rota);
        await AssertProblema(resposta, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AlterarQuantidadeTotal_PreservaEmprestimosERejeitaReducaoInvalida()
    {
        var livro = await CriarLivro(quantidade: 3);
        await CriarEmprestimo("Ana", livro);
        await CriarEmprestimo("Lucas", livro);
        var livroAtual = await ObterObjeto($"{Livros}/{Id(livro)}");

        using var aumento = await _client.PutAsJsonAsync($"{Livros}/{Id(livro)}",
            DadosLivro(5, versao: Texto(livroAtual, "versao")));
        await AssertStatus(aumento, HttpStatusCode.OK);
        var livroAumentado = await LerObjeto(aumento);
        await AssertSaldo(livro, total: 5, disponivel: 3);

        using var reducao = await _client.PutAsJsonAsync($"{Livros}/{Id(livro)}",
            DadosLivro(1, versao: Texto(livroAumentado, "versao")));
        await AssertProblema(reducao, HttpStatusCode.Conflict);
        await AssertSaldo(livro, total: 5, disponivel: 3);
    }

    [Fact]
    public async Task LivroComHistorico_NaoPodeSerExcluido()
    {
        var livro = await CriarLivro();
        var emprestimo = await CriarEmprestimo("Ana Beatriz", livro);

        using var livroAtivo = await _client.DeleteAsync($"{Livros}/{Id(livro)}");
        await AssertProblema(livroAtivo, HttpStatusCode.Conflict);

        using var devolucao = await _client.PatchAsync($"{Emprestimos}/{Id(emprestimo)}/devolucao", null);
        await AssertStatus(devolucao, HttpStatusCode.OK);
        using var livroComHistorico = await _client.DeleteAsync($"{Livros}/{Id(livro)}");
        await AssertProblema(livroComHistorico, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DuasRetiradasSimultaneasDoUltimoExemplar_ApenasUmaTemSucesso()
    {
        var livro = await CriarLivro(quantidade: 1);
        var respostas = await Task.WhenAll(
            _client.PostAsJsonAsync(Emprestimos, Pedido("Ana", livro)),
            _client.PostAsJsonAsync(Emprestimos, Pedido("Lucas", livro)));
        try
        {
            var codigos = respostas.Select(r => r.StatusCode).OrderBy(c => (int)c).ToArray();
            var detalhes = string.Join("\n", await Task.WhenAll(
                respostas.Select(async r => $"{(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}")));
            Assert.True(codigos.SequenceEqual(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }), detalhes);
            await AssertSaldo(livro, total: 1, disponivel: 0);
            Assert.Equal(1, Numero(await ObterObjeto(Emprestimos), "totalCount"));
        }
        finally
        {
            foreach (var resposta in respostas) resposta.Dispose();
        }
    }

    [Fact]
    public async Task LivroSemHistorico_PodeSerExcluido()
    {
        var livro = await CriarLivro();
        using var exclusao = await _client.DeleteAsync($"{Livros}/{Id(livro)}");
        await AssertStatus(exclusao, HttpStatusCode.NoContent);
        using var consulta = await _client.GetAsync($"{Livros}/{Id(livro)}");
        await AssertProblema(consulta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LivroSemTitulo_RetornaValidacao()
    {
        using var resposta = await _client.PostAsJsonAsync(Livros, DadosLivro(1, titulo: ""));
        await AssertProblema(resposta, HttpStatusCode.BadRequest);
    }

    private async Task<JsonObject> CriarLivro(int quantidade = 1, string titulo = "Dom Casmurro")
    {
        using var resposta = await _client.PostAsJsonAsync(Livros, DadosLivro(quantidade, titulo));
        await AssertStatus(resposta, HttpStatusCode.Created);
        Assert.NotNull(resposta.Headers.Location);
        return await LerObjeto(resposta);
    }

    private static object DadosLivro(int quantidade, string titulo = "Dom Casmurro", string? versao = null) => new
    {
        titulo,
        autor = "Machado de Assis",
        categoria = "Literatura",
        quantidadeTotal = quantidade,
        versao
    };

    private async Task<JsonObject> CriarEmprestimo(string alunoNome, JsonObject livro)
    {
        using var resposta = await _client.PostAsJsonAsync(Emprestimos, Pedido(alunoNome, livro));
        await AssertStatus(resposta, HttpStatusCode.Created);
        Assert.NotNull(resposta.Headers.Location);
        return await LerObjeto(resposta);
    }

    private static object Pedido(string alunoNome, JsonObject livro) => new
    {
        alunoNome,
        livroId = Id(livro),
        dataPrevistaDevolucao = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))
    };

    private async Task AssertSaldo(JsonObject livro, int total, int disponivel)
    {
        var atual = await ObterObjeto($"{Livros}/{Id(livro)}");
        Assert.Equal(total, Numero(atual, "quantidadeTotal"));
        Assert.Equal(disponivel, Numero(atual, "quantidadeDisponivel"));
    }

    private async Task<JsonObject> ObterObjeto(string rota)
    {
        using var resposta = await _client.GetAsync(rota);
        await AssertStatus(resposta, HttpStatusCode.OK);
        return await LerObjeto(resposta);
    }

    private static async Task<JsonObject> LerObjeto(HttpResponseMessage resposta) =>
        await resposta.Content.ReadFromJsonAsync<JsonObject>()
        ?? throw new InvalidOperationException("A API retornou uma resposta JSON vazia.");

    private static async Task AssertStatus(HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.True(resposta.StatusCode == esperado,
            $"Esperado {(int)esperado}; recebido {(int)resposta.StatusCode}. Corpo: {corpo}");
    }

    private static async Task AssertProblema(HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        await AssertStatus(resposta, esperado);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await LerObjeto(resposta);
        Assert.Equal((int)esperado, Numero(problema, "status"));
        Assert.False(string.IsNullOrWhiteSpace(Texto(problema, "title")));
    }

    private static string Id(JsonObject objeto) => Texto(objeto, "id");
    private static string Texto(JsonObject objeto, string nome) => objeto[nome]!.GetValue<string>();
    private static int Numero(JsonObject objeto, string nome) => objeto[nome]!.GetValue<int>();
    private static string FormatarData(DateOnly data) => data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public void Dispose() => _factory.Dispose();
}
