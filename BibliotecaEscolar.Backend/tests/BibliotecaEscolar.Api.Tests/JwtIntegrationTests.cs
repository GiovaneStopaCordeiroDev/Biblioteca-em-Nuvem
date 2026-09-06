using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using BibliotecaEscolar.Api.Configuration;
using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace BibliotecaEscolar.Api.Tests;

public sealed class JwtIntegrationTests : IDisposable
{
    private const string Issuer = "https://teste.supabase.co/auth/v1";
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly JwtApiFactory _factory;
    private readonly HttpClient _client;

    public JwtIntegrationTests()
    {
        var publicKey = new RsaSecurityKey(_rsa.ExportParameters(false)) { KeyId = "teste-local" };
        _factory = new JwtApiFactory(publicKey);
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SemToken_Retorna401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/v1/livros")).StatusCode);
    }

    [Theory]
    [InlineData("assinatura")]
    [InlineData("expirado")]
    [InlineData("audiencia")]
    [InlineData("emissor")]
    public async Task TokenInvalido_Retorna401(string falha)
    {
        using var outraChave = RSA.Create(2048);
        var token = CriarToken(Guid.NewGuid(), falha == "assinatura" ? outraChave : _rsa,
            falha == "audiencia" ? "outra-audiencia" : "authenticated",
            falha == "emissor" ? "https://outro.supabase.co/auth/v1" : Issuer,
            falha == "expirado");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/v1/livros")).StatusCode);
    }

    [Fact]
    public async Task TokenValidoSemOperador_Retorna403()
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", CriarToken(Guid.NewGuid(), _rsa));
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/v1/livros")).StatusCode);
    }

    [Fact]
    public async Task TokenValidoComOperadorAtivo_Retorna200()
    {
        var authId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
            db.Usuarios.Add(new Usuario { SupabaseAuthId = authId, Nome = "Operador teste JWT", Perfil = "Bibliotecario", Ativo = true });
            await db.SaveChangesAsync();
        }
        _client.DefaultRequestHeaders.Authorization = new("Bearer", CriarToken(authId, _rsa));
        var response = await _client.GetAsync("/api/v1/usuarios/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Operador teste JWT", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void ConfiguracaoDeAutenticacaoDemoEmProducao_FalhaNaInicializacao()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Auth:Mode"] = "Development" }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection()
            .AddBibliotecaAuthentication(config, new TestEnvironment { EnvironmentName = "Production" }));
    }

    private static string CriarToken(Guid subject, RSA rsa, string audience = "authenticated",
        string issuer = Issuer, bool expired = false)
    {
        var key = new RsaSecurityKey(rsa) { KeyId = "teste-local" };
        var jwt = new JwtSecurityToken(issuer, audience, [new Claim("sub", subject.ToString())],
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddMinutes(expired ? -5 : 5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _rsa.Dispose();
    }

    private sealed class JwtApiFactory(SecurityKey publicKey) : BibliotecaApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            var values = new Dictionary<string, string?>
            {
                ["Auth:Mode"] = "Supabase",
                ["Auth:SupabaseUrl"] = "https://teste.supabase.co"
            };
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(values));
            builder.ConfigureTestServices(services =>
            {
                // Executa a mesma configuração JWT da aplicação. Apenas a busca das chaves
                // externas é substituída por uma chave pública local; assinatura é real.
                services.AddBibliotecaAuthentication(new ConfigurationBuilder().AddInMemoryCollection(values).Build(), new TestEnvironment());
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
                    metadata.SigningKeys.Add(publicKey);
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
                });
            });
        }
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "BibliotecaEscolar.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
