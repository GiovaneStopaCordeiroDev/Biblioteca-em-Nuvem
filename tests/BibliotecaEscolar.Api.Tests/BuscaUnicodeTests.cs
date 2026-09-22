using System.Net.Http.Json;
using System.Text.Json;

namespace BibliotecaEscolar.Api.Tests;

public sealed class BuscaUnicodeTests
{
    [Fact]
    public async Task BuscaNaDemoIgnoraMaiusculasTambemEmNomesAcentuados()
    {
        using var factory = new BibliotecaApiFactory();
        using var client = factory.CreateClient();
        var livro = await client.PostAsJsonAsync("/api/v1/livros", new
        {
            titulo = "Livro para busca",
            autor = "Autora",
            quantidadeTotal = 1
        });
        livro.EnsureSuccessStatusCode();
        var livroCriado = await livro.Content.ReadFromJsonAsync<JsonElement>();
        var criado = await client.PostAsJsonAsync("/api/v1/emprestimos", new
        {
            alunoNome = "Érica Silva",
            livroId = livroCriado.GetProperty("id").GetGuid()
        });
        criado.EnsureSuccessStatusCode();
        var pagina = await client.GetFromJsonAsync<JsonElement>(
            "/api/v1/emprestimos?busca=" + Uri.EscapeDataString("érica"));
        Assert.Equal(1, pagina.GetProperty("totalCount").GetInt32());
        Assert.Equal("Érica Silva", pagina.GetProperty("items")[0].GetProperty("alunoNome").GetString());
    }
}
