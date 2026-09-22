using System.Security.Claims;
using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.Models;
using BibliotecaEscolar.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace BibliotecaEscolar.Api.Tests;

public sealed class OperadorAuthorizationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly BibliotecaDbContext _db;

    public OperadorAuthorizationTests()
    {
        _connection.Open();
        _db = new BibliotecaDbContext(new DbContextOptionsBuilder<BibliotecaDbContext>()
            .UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
    }

    [Theory]
    [InlineData("Bibliotecario", true, true)]
    [InlineData("Bibliotecario", false, false)]
    public async Task AcessoExigeOperadorAtivoCadastradoNoBanco(string perfil, bool ativo, bool permitido)
    {
        var authId = Guid.NewGuid();
        _db.Usuarios.Add(new Usuario
        {
            SupabaseAuthId = authId,
            Nome = "Operador de teste",
            Perfil = perfil,
            Ativo = ativo
        });
        await _db.SaveChangesAsync();

        var contexto = await Autorizar(new ClaimsIdentity(
            [new Claim("sub", authId.ToString())], "Test"));

        Assert.Equal(permitido, contexto.HasSucceeded);
    }

    [Fact]
    public async Task PapelInformadoNoToken_NaoConcedeAcessoSemCadastroNoBanco()
    {
        var identidade = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Bibliotecario"),
            new Claim("user_metadata", "{\"role\":\"Bibliotecario\"}")
        ], "Test");

        Assert.False((await Autorizar(identidade)).HasSucceeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("identificador-invalido")]
    public async Task IdentificadorAusenteOuInvalido_NaoConcedeAcesso(string? subject)
    {
        var identidade = new ClaimsIdentity("Test");
        if (subject is not null) identidade.AddClaim(new Claim("sub", subject));

        Assert.False((await Autorizar(identidade)).HasSucceeded);
    }

    [Fact]
    public async Task IdentidadeNaoAutenticada_NaoConcedeAcessoMesmoComOperadorCadastrado()
    {
        var authId = Guid.NewGuid();
        _db.Usuarios.Add(new Usuario { SupabaseAuthId = authId, Nome = "Operador" });
        await _db.SaveChangesAsync();
        var identidade = new ClaimsIdentity([new Claim("sub", authId.ToString())]);

        Assert.False((await Autorizar(identidade)).HasSucceeded);
    }

    [Theory]
    [InlineData("Development", "Development", "Development", true)]
    [InlineData("Production", "Development", "Development", false)]
    [InlineData("Development", "Supabase", "Development", false)]
    [InlineData("Development", "Development", "Test", false)]
    public async Task AcessoDemonstrativo_ExigeAmbienteModoEIdentidadeDeDesenvolvimento(
        string ambiente, string modo, string esquema, bool permitido)
    {
        var contexto = await Autorizar(new ClaimsIdentity(esquema), ambiente, modo);
        Assert.Equal(permitido, contexto.HasSucceeded);
    }

    private async Task<AuthorizationHandlerContext> Autorizar(
        ClaimsIdentity identidade, string ambiente = "Production", string modo = "Supabase")
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Auth:Mode"] = modo }).Build();
        var requisito = new OperadorRequirement();
        var contexto = new AuthorizationHandlerContext([requisito], new ClaimsPrincipal(identidade), null);
        var handler = new OperadorAuthorizationHandler(_db, configuracao,
            new TestHostEnvironment { EnvironmentName = ambiente });
        await handler.HandleAsync(contexto);
        return contexto;
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "BibliotecaEscolar.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
