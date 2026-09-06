using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BibliotecaEscolar.Api.Data;

// O lower nativo do SQLite só converte ASCII; a demo também precisa buscar Érica/érica.
public sealed class SqliteUnicodeInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) => Configure(connection);

    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Configure(connection);
        return Task.CompletedTask;
    }

    private static void Configure(DbConnection connection)
    {
        if (connection is SqliteConnection sqlite)
            sqlite.CreateFunction<string?, string?>("lower", value => value?.ToLowerInvariant(), isDeterministic: true);
    }
}
