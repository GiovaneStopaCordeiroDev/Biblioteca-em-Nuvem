using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BibliotecaEscolar.Api.Data;

// Cada provedor tem seu próprio snapshot/migrations; o modelo de negócio é compartilhado.
public sealed class SqliteBibliotecaDbContext(DbContextOptions<SqliteBibliotecaDbContext> options)
    : BibliotecaDbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.AddInterceptors(new SqliteUnicodeInterceptor());
}

public sealed class PostgresBibliotecaDbContext(DbContextOptions<PostgresBibliotecaDbContext> options)
    : BibliotecaDbContext(options);

public sealed class SqliteDesignFactory : IDesignTimeDbContextFactory<SqliteBibliotecaDbContext>
{
    public SqliteBibliotecaDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<SqliteBibliotecaDbContext>()
            .UseSqlite(Environment.GetEnvironmentVariable("ConnectionStrings__Biblioteca")
                ?? "Data Source=biblioteca-demo.db").Options);
}

public sealed class PostgresDesignFactory : IDesignTimeDbContextFactory<PostgresBibliotecaDbContext>
{
    public PostgresBibliotecaDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<PostgresBibliotecaDbContext>()
            // O valor local permite gerar SQL/migrations sem credenciais. Não conecta ao gerar.
            .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Biblioteca")
                ?? "Host=localhost;Database=biblioteca;Username=postgres").Options);
}
