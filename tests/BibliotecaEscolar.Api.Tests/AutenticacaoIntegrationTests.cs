using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.DTOs;
using BibliotecaEscolar.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BibliotecaEscolar.Api.Tests;

public sealed class AutenticacaoIntegrationTests : IDisposable
{
    private const string Login = "bibliotecario";
    private const string Senha = "UmaSenhaForte!2026";
    private readonly BibliotecaApiFactory _factory = new("Database");
    private readonly HttpClient _client;

    public AutenticacaoIntegrationTests()
    {
        _client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var usuario = new Usuario { Login = Login, Nome = "Bibliotecário de teste" };
        usuario.SenhaHash = hasher.HashPassword(usuario, Senha);
        db.Usuarios.Add(usuario);
        db.SaveChanges();
    }

    [Fact]
    public async Task LoginSessaoELogout_FormamFluxoAutenticadoCompleto()
    {
        using var semSessao = await _client.GetAsync("/api/v1/usuarios/me");
        Assert.Equal(HttpStatusCode.Unauthorized, semSessao.StatusCode);

        using var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            usuario = Login,
            senha = Senha
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var sessao = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(sessao);
        Assert.False(string.IsNullOrWhiteSpace(sessao.AccessToken));
        Assert.Equal("Bibliotecario", sessao.Usuario.Perfil);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", sessao.AccessToken);
        using var autenticado = await _client.GetAsync("/api/v1/usuarios/me");
        Assert.Equal(HttpStatusCode.OK, autenticado.StatusCode);

        using var logout = await _client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        using var revogada = await _client.GetAsync("/api/v1/usuarios/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revogada.StatusCode);
    }

    [Fact]
    public async Task LoginComSenhaIncorreta_RetornaRespostaGenerica()
    {
        using var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            usuario = Login,
            senha = "SenhaIncorreta!2026"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Usuário ou senha inválidos.", problem?.Detail);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
