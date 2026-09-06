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
        var criado = await client.PostAsJsonAsync("/api/v1/alunos", new { nome = "Érica Silva", matricula = "TESTE-UNICODE" });
        criado.EnsureSuccessStatusCode();
        var pagina = await client.GetFromJsonAsync<JsonElement>("/api/v1/alunos?busca=" + Uri.EscapeDataString("érica"));
        Assert.Equal(1, pagina.GetProperty("totalCount").GetInt32());
        Assert.Equal("Érica Silva", pagina.GetProperty("items")[0].GetProperty("nome").GetString());
    }
}
