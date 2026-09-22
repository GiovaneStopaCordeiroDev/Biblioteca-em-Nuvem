using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

var writeToSqlite = args.Length == 4
    && string.Equals(args[2], "--sqlite", StringComparison.OrdinalIgnoreCase);
if (args.Length != 2 && !writeToSqlite)
{
    Console.Error.WriteLine(
        "Uso: dotnet run --project tools/BibliotecaEscolar.PasswordTool -- LOGIN \"NOME\" [--sqlite CAMINHO]");
    return 1;
}

var login = args[0].Trim().ToLowerInvariant();
var nome = args[1].Trim();
if (login.Length is < 3 or > 80 || login.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '.' and not '_' and not '-'))
{
    Console.Error.WriteLine("O login deve ter de 3 a 80 caracteres e usar letras, números, ponto, hífen ou sublinhado.");
    return 1;
}
if (nome.Length is < 2 or > 150)
{
    Console.Error.WriteLine("O nome deve ter de 2 a 150 caracteres.");
    return 1;
}

Console.Write("Senha (mínimo de 12 caracteres): ");
var senha = ReadSecret();
Console.WriteLine();
Console.Write("Confirme a senha: ");
var confirmacao = ReadSecret();
Console.WriteLine();
if (senha.Length < 12 || senha != confirmacao)
{
    Console.Error.WriteLine(senha.Length < 12
        ? "A senha deve ter pelo menos 12 caracteres."
        : "As senhas não coincidem.");
    return 1;
}

var hash = new PasswordHasher<object>().HashPassword(new object(), senha);
if (writeToSqlite)
{
    var databasePath = Path.GetFullPath(args[3]);
    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync();
    await using var transaction = connection.BeginTransaction();
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
        DELETE FROM "SessoesUsuarios"
        WHERE "UsuarioId" = (SELECT "Id" FROM "Usuarios" WHERE "Login" = $login);

        UPDATE "Usuarios"
        SET "Id" = upper("Id"), "Nome" = $nome, "SenhaHash" = $hash
        WHERE "Login" = $login;

        INSERT INTO "Usuarios"
            ("Id", "Nome", "Perfil", "Ativo", "Login", "SenhaHash", "SupabaseAuthId")
        SELECT $id, $nome, 'Bibliotecario', 1, $login, $hash, NULL
        WHERE NOT EXISTS (SELECT 1 FROM "Usuarios" WHERE "Login" = $login);
        """;
    command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString().ToUpperInvariant());
    command.Parameters.AddWithValue("$nome", nome);
    command.Parameters.AddWithValue("$login", login);
    command.Parameters.AddWithValue("$hash", hash);
    await command.ExecuteNonQueryAsync();
    await transaction.CommitAsync();
    Console.WriteLine($"Bibliotecário cadastrado no banco local: {databasePath}");
    return 0;
}

Console.WriteLine();
Console.WriteLine("Copie e execute este INSERT depois das migrations:");
Console.WriteLine($$"""
INSERT INTO public."Usuarios"
    ("Id", "Nome", "Perfil", "Ativo", "Login", "SenhaHash", "SupabaseAuthId")
VALUES
    (gen_random_uuid(), '{{Sql(nome)}}', 'Bibliotecario', true, '{{Sql(login)}}', '{{Sql(hash)}}', NULL);
""");
return 0;

static string ReadSecret()
{
    var characters = new List<char>();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) break;
        if (key.Key == ConsoleKey.Backspace)
        {
            if (characters.Count > 0) characters.RemoveAt(characters.Count - 1);
            continue;
        }
        if (!char.IsControl(key.KeyChar)) characters.Add(key.KeyChar);
    }
    return new string([.. characters]);
}

static string Sql(string value) => value.Replace("'", "''", StringComparison.Ordinal);
