using BibliotecaEscolar.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BibliotecaEscolar.Api.Tests;

/// <summary>
/// Cada teste inicia a API real com seu próprio banco temporário, sem acessar o Supabase.
/// </summary>
public class BibliotecaApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(), $"biblioteca-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:Biblioteca"] = $"Data Source={_databasePath};Pooling=False",
                ["Auth:Mode"] = "Development",
                ["Demo:SeedData"] = "false"
            }));
        builder.ConfigureTestServices(services =>
        {
            // Minimal hosting pode capturar a connection string antes da configuração
            // acima. Substituir explicitamente a configuração EF garante isolamento.
            services.RemoveAll<DbContextOptions<SqliteBibliotecaDbContext>>();
            services.RemoveAll<SqliteBibliotecaDbContext>();
            services.RemoveAll<BibliotecaDbContext>();
            services.AddDbContext<SqliteBibliotecaDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath};Pooling=False"));
            services.AddScoped<BibliotecaDbContext>(services =>
                services.GetRequiredService<SqliteBibliotecaDbContext>());
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        // Pooling=False permite apagar o banco assim que a API fecha as conexões.
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = _databasePath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
