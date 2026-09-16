namespace BibliotecaEscolar.Api.Services;

internal static class Isbn
{
    public static string? NormalizarEValidar(string? value)
    {
        var informado = Texto.Opcional(value);
        if (informado is null) return null;

        var normalizado = string.Concat(informado.Where(caractere => caractere is not '-' && !char.IsWhiteSpace(caractere)))
            .ToUpperInvariant();
        if (!EhIsbn10Valido(normalizado) && !EhIsbn13Valido(normalizado))
            throw new RequisicaoInvalidaException("Informe um ISBN-10 ou ISBN-13 válido.");

        return normalizado;
    }

    private static bool EhIsbn10Valido(string value)
    {
        if (value.Length != 10) return false;

        var soma = 0;
        for (var indice = 0; indice < value.Length; indice++)
        {
            var digito = indice == 9 && value[indice] == 'X' ? 10 : value[indice] - '0';
            if (digito is < 0 or > 9 && !(indice == 9 && digito == 10)) return false;
            soma += digito * (10 - indice);
        }

        return soma % 11 == 0;
    }

    private static bool EhIsbn13Valido(string value)
    {
        if (value.Length != 13 || value.Any(caractere => !char.IsAsciiDigit(caractere))) return false;

        var soma = 0;
        for (var indice = 0; indice < value.Length; indice++)
            soma += (value[indice] - '0') * (indice % 2 == 0 ? 1 : 3);

        return soma % 10 == 0;
    }
}
