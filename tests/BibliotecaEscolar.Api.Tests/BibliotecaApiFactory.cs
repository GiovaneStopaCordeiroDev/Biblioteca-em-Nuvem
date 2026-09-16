using BibliotecaEscolar.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace BibliotecaEscolar.Api.Tests;

/// <summary>
/// Cada factory inicia a API real com seu próprio banco temporário, sem acessar o Supabase.
/// SQLite é o padrão local. Quando BIBLIOTECA_TEST_POSTGRES está definida, os testes
/// usam um schema PostgreSQL exclusivo e descartável.
/// </summary>
public class BibliotecaApiFactory : WebApplicationFactory<Program>
{
    private static readonly object PostgresExtensionLock = new();

    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(), $"biblioteca-test-{Guid.NewGuid():N}.db");
    private readonly string? _postgresAdminConnectionString;
    private readonly string? _postgresConnectionString;
    private readonly string? _postgresSchema;

    public BibliotecaApiFactory()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable("BIBLIOTECA_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(configuredConnectionString)) return;

        _postgresSchema = $"biblioteca_test_{Guid.NewGuid():N}";
        var adminBuilder = new NpgsqlConnectionStringBuilder(configuredConnectionString);
        adminBuilder.Remove("Search Path");
        _postgresAdminConnectionString = adminBuilder.ConnectionString;
        lock (PostgresExtensionLock)
        {
            ExecutePostgresAdminCommand("CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public");
        }
        var testBuilder = new NpgsqlConnectionStringBuilder(_postgresAdminConnectionString)
        {
            SearchPath = $"{_postgresSchema},public",
            Pooling = false
        };
        _postgresConnectionString = testBuilder.ConnectionString;
        ExecutePostgresAdminCommand($"CREATE SCHEMA {QuoteIdentifier(_postgresSchema)}");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        var usesPostgres = _postgresConnectionString is not null;
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Database:Provider"] = usesPostgres ? "Postgres" : "Sqlite",
                ["ConnectionStrings:Biblioteca"] = usesPostgres
                    ? _postgresConnectionString
                    : $"Data Source={_databasePath};Pooling=False",
                ["Auth:Mode"] = "Development",
                ["Auth:Development:Enabled"] = "true",
                ["Demo:SeedData"] = "false"
            }));
        builder.ConfigureTestServices(services =>
        {
            // Minimal hosting pode capturar a connection string antes da configuração
            // acima. Substituir explicitamente a configuração EF garante isolamento.
            services.RemoveAll<DbContextOptions<SqliteBibliotecaDbContext>>();
            services.RemoveAll<SqliteBibliotecaDbContext>();
            services.RemoveAll<DbContextOptions<PostgresBibliotecaDbContext>>();
            services.RemoveAll<PostgresBibliotecaDbContext>();
            services.RemoveAll<BibliotecaDbContext>();

            if (usesPostgres)
            {
                services.AddDbContext<PostgresBibliotecaDbContext>(options =>
                    options.UseNpgsql(_postgresConnectionString));
                services.AddScoped<BibliotecaDbContext>(serviceProvider =>
                    serviceProvider.GetRequiredService<PostgresBibliotecaDbContext>());
            }
            else
            {
                services.AddDbContext<SqliteBibliotecaDbContext>(options =>
                    options.UseSqlite($"Data Source={_databasePath};Pooling=False"));
                services.AddScoped<BibliotecaDbContext>(serviceProvider =>
                    serviceProvider.GetRequiredService<SqliteBibliotecaDbContext>());
            }
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        try
        {
            var host = base.CreateHost(builder);
            if (_postgresConnectionString is null) return host;

            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>().Database.Migrate();
            return host;
        }
        catch
        {
            DropPostgresTestSchema();
            throw;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        if (_postgresSchema is not null)
        {
            DropPostgresTestSchema();
            return;
        }

        // Pooling=False permite apagar o banco assim que a API fecha as conexões.
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = _databasePath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private void ExecutePostgresAdminCommand(string commandText)
    {
        using var connection = new NpgsqlConnection(_postgresAdminConnectionString
            ?? throw new InvalidOperationException("A conexão administrativa PostgreSQL não foi configurada."));
        connection.Open();
        using var command = new NpgsqlCommand(commandText, connection);
        command.ExecuteNonQuery();
    }

    private void DropPostgresTestSchema()
    {
        if (_postgresSchema is not null)
            ExecutePostgresAdminCommand($"DROP SCHEMA IF EXISTS {QuoteIdentifier(_postgresSchema)} CASCADE");
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
