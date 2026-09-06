namespace BibliotecaEscolar.Api.Services;

internal static class Texto
{
    public static string Obrigatorio(string? value, string campo)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ApiException(400, $"O campo {campo} é obrigatório.");
        return value.Trim();
    }

    public static string? Opcional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
