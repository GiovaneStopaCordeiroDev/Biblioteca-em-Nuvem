using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace BibliotecaEscolar.Api.Tests;

public sealed class DashboardAndValidationIntegrationTests : IDisposable
{
    private const string Emprestimos = "/api/v1/emprestimos";
    private const string Livros = "/api/v1/livros";
    private readonly BibliotecaApiFactory _factory = new();
    private readonly HttpClient _client;

    public DashboardAndValidationIntegrationTests() => _client = _factory.CreateClient();

    [Fact]
    public async Task HealthChecks_ConfirmamProcessoEBancoDisponiveis()
    {
        var live = await _client.GetFromJsonAsync<JsonObject>("/health/live");
        Assert.Equal("ok", live?["status"]?.GetValue<string>());

        using var readyResponse = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
        var ready = await readyResponse.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("ok", ready?["status"]?.GetValue<string>());
    }

    [Fact]
    public async Task Dashboard_ResumeCadastrosEstoqueEEmprestimosAtivos()
    {
        var livro = await PostAndRead(Livros, new
        {
            titulo = "O Cortiço",
            autor = "Aluísio Azevedo",
            isbn = "978-3-16-148410-0",
            quantidadeTotal = 3
        });
        using var emprestimo = await _client.PostAsJsonAsync(Emprestimos, new
        {
            alunoNome = "Carolina Maria",
            livroId = livro["id"]!.GetValue<Guid>()
        });
        Assert.Equal(HttpStatusCode.Created, emprestimo.StatusCode);

        var dashboard = await _client.GetFromJsonAsync<JsonObject>("/api/v1/dashboard");
        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard["totalLivros"]!.GetValue<int>());
        Assert.Equal(3, dashboard["totalExemplares"]!.GetValue<int>());
        Assert.Equal(2, dashboard["exemplaresDisponiveis"]!.GetValue<int>());
        Assert.Equal(1, dashboard["emprestimosAtivos"]!.GetValue<int>());
        Assert.Equal(0, dashboard["emprestimosAtrasados"]!.GetValue<int>());
    }

    [Fact]
    public async Task IsbnDuplicado_RetornaConflito()
    {
        const string isbn = "978-0-306-40615-7";
        await PostAndRead(Livros, new
        {
            titulo = "Primeiro livro",
            autor = "Autora",
            isbn,
            quantidadeTotal = 1
        });
        using var livroDuplicado = await _client.PostAsJsonAsync(Livros, new
        {
            titulo = "Segundo livro",
            autor = "Autor",
            isbn,
            quantidadeTotal = 1
        });
        await AssertProblemDetails(livroDuplicado, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CamposForaDosLimites_RetornamValidacao()
    {
        using var livroSemQuantidade = await _client.PostAsJsonAsync(Livros, new
        {
            titulo = "Livro",
            autor = "Autor",
            quantidadeTotal = 0
        });
        await AssertProblemDetails(livroSemQuantidade, HttpStatusCode.BadRequest);

        using var tituloLongo = await _client.PostAsJsonAsync(Livros, new
        {
            titulo = new string('x', 201),
            autor = "Autor",
            quantidadeTotal = 1
        });
        await AssertProblemDetails(tituloLongo, HttpStatusCode.BadRequest);

        using var nomeDoAlunoLongo = await _client.PostAsJsonAsync(Emprestimos, new
        {
            alunoNome = new string('x', 151),
            livroId = Guid.NewGuid()
        });
        await AssertProblemDetails(nomeDoAlunoLongo, HttpStatusCode.BadRequest);
    }

    private async Task<JsonObject> PostAndRead(string route, object body)
    {
        using var response = await _client.PostAsJsonAsync(route, body);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created,
            $"Esperado 201; recebido {(int)response.StatusCode}. Corpo: {content}");
        return JsonNode.Parse(content)?.AsObject()
            ?? throw new InvalidOperationException("A API retornou uma resposta JSON vazia.");
    }

    private static async Task AssertProblemDetails(HttpResponseMessage response, HttpStatusCode expected)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected,
            $"Esperado {(int)expected}; recebido {(int)response.StatusCode}. Corpo: {content}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = JsonNode.Parse(content)?.AsObject();
        Assert.Equal((int)expected, problem?["status"]?.GetValue<int>());
        Assert.False(string.IsNullOrWhiteSpace(problem?["traceId"]?.GetValue<string>()));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
