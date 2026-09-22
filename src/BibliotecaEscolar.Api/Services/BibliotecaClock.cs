namespace BibliotecaEscolar.Api.Services;

public sealed class BibliotecaClock(TimeProvider timeProvider)
{
    private static readonly TimeZoneInfo BibliotecaTimeZone = ResolveTimeZone();
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, BibliotecaTimeZone).DateTime);

    private static TimeZoneInfo ResolveTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }
}
