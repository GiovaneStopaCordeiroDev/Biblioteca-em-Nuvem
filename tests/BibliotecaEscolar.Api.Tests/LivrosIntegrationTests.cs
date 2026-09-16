using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace BibliotecaEscolar.Api.Tests;

public sealed class LivrosIntegrationTests : IDisposable
{
    private const string Livros = "/api/v1/livros";
    private readonly BibliotecaApiFactory _factory = new();
    private readonly HttpClient _client;

    public LivrosIntegrationTests() => _client = _factory.CreateClient();

    [Fact]
    public async Task FluxoCompleto_NormalizaIsbnEPreservaContratoDoAcervo()
    {
        using var criacao = await _client.PostAsJsonAsync(Livros, new
        {
            titulo = "  Cem Anos de Solidão  ",
            autor = "  Gabriel García Márquez  ",
            isbn = "978-0-306-40615-7",
            categoria = "  Romance  ",
            quantidadeTotal = 2
        });
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var criado = await LerObjeto(criacao);
        var id = criado["id"]!.GetValue<Guid>();
        Assert.Equal($"{Livros}/{id}", criacao.Headers.Location?.AbsolutePath);
        Assert.Equal("Cem Anos de Solidão", Texto(criado, "titulo"));
        Assert.Equal("Gabriel García Márquez", Texto(criado, "autor"));
        Assert.Equal("9780306406157", Texto(criado, "isbn"));
        Assert.Equal("Romance", Texto(criado, "categoria"));
        Assert.Equal(2, Numero(criado, "quantidadeTotal"));
        Assert.Equal(2, Numero(criado, "quantidadeDisponivel"));

        var pagina = await _client.GetFromJsonAsync<JsonObject>($"{Livros}?busca=garcía&page=1&pageSize=10");
        Assert.NotNull(pagina);
        Assert.Equal(1, Numero(pagina, "totalCount"));
        Assert.Equal(id, pagina["items"]![0]!["id"]!.GetValue<Guid>());

        using var atualizacao = await _client.PutAsJsonAsync($"{Livros}/{id}", new
        {
            titulo = "Cem Anos de Solidão — edição revista",
            autor = "Gabriel García Márquez",
            isbn = "9780306406157",
            categoria = "Literatura latino-americana",
            quantidadeTotal = 3,
            versao = criado["versao"]!.GetValue<Guid>()
        });
        Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);
        var atualizado = await LerObjeto(atualizacao);
        Assert.Equal(3, Numero(atualizado, "quantidadeTotal"));
        Assert.Equal(3, Numero(atualizado, "quantidadeDisponivel"));
        Assert.NotEqual(criado["versao"]!.GetValue<Guid>(), atualizado["versao"]!.GetValue<Guid>());

        using var exclusao = await _client.DeleteAsync($"{Livros}/{id}");
        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);
        using var consultaExcluido = await _client.GetAsync($"{Livros}/{id}");
        await AssertProblemDetails(consultaExcluido, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task IsbnInvalidoOuDuplicado_RetornaErroDeDominioLegivel()
    {
        using var invalido = await _client.PostAsJsonAsync(Livros, LivroRequest("9780306406158"));
        var problemaInvalido = await AssertProblemDetails(invalido, HttpStatusCode.BadRequest);
        Assert.Equal("Informe um ISBN-10 ou ISBN-13 válido.", Texto(problemaInvalido, "detail"));

        using var primeiro = await _client.PostAsJsonAsync(Livros, LivroRequest("0-306-40615-2"));
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        using var duplicado = await _client.PostAsJsonAsync(Livros, LivroRequest("0306406152"));
        var problemaDuplicado = await AssertProblemDetails(duplicado, HttpStatusCode.Conflict);
        Assert.Equal("Já existe um livro cadastrado com este ISBN.", Texto(problemaDuplicado, "detail"));
    }

    [Fact]
    public async Task Swagger_DescreveTodasAsOperacoesDoModuloDeLivros()
    {
        var document = await _client.GetFromJsonAsync<JsonObject>("/swagger/v1/swagger.json");
        Assert.NotNull(document);
        var paths = document["paths"]!.AsObject();
        var colecao = paths[Livros]!.AsObject();
        var recurso = paths[$"{Livros}/{{id}}"]!.AsObject();

        AssertOperacaoDocumentada(colecao, "get", "200", "400", "401", "429");
        AssertOperacaoDocumentada(colecao, "post", "201", "400", "401", "409", "413", "429");
        AssertOperacaoDocumentada(recurso, "get", "200", "401", "404", "429");
        AssertOperacaoDocumentada(recurso, "put", "200", "400", "401", "404", "409", "413", "429");
        AssertOperacaoDocumentada(recurso, "delete", "204", "401", "403", "404", "409", "429");
    }

    private static object LivroRequest(string isbn) => new
    {
        titulo = "Livro de teste",
        autor = "Autoria de teste",
        isbn,
        categoria = "Testes",
        quantidadeTotal = 1
    };

    private static void AssertOperacaoDocumentada(JsonObject path, string method, params string[] responses)
    {
        var operation = path[method]!.AsObject();
        Assert.False(string.IsNullOrWhiteSpace(Texto(operation, "summary")));
        var documentedResponses = operation["responses"]!.AsObject();
        foreach (var response in responses) Assert.True(documentedResponses.ContainsKey(response));
    }

    private static async Task<JsonObject> LerObjeto(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonObject>()
        ?? throw new InvalidOperationException("A API retornou uma resposta JSON vazia.");

    private static async Task<JsonObject> AssertProblemDetails(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        var body = await LerObjeto(response);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)expectedStatus, Numero(body, "status"));
        Assert.False(string.IsNullOrWhiteSpace(Texto(body, "traceId")));
        return body;
    }

    private static string Texto(JsonObject value, string property) =>
        value[property]?.GetValue<string>()
        ?? throw new InvalidOperationException($"A propriedade {property} não foi retornada.");

    private static int Numero(JsonObject value, string property) =>
        value[property]?.GetValue<int>()
        ?? throw new InvalidOperationException($"A propriedade {property} não foi retornada.");

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
