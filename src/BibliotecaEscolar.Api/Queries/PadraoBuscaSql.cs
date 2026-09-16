namespace BibliotecaEscolar.Api.Queries;

internal static class PadraoBuscaSql
{
    public const string CaractereEscape = "\\";

    public static string Contem(string valor) =>
        $"%{valor.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)}%";
}
