namespace MiniErp.Core;

/// <summary>
/// Los dias calendario del negocio (vencimientos y reportes) son dias de Argentina;
/// los instantes se guardan en UTC. Este es el unico lugar donde se convierte.
/// </summary>
public static class ZonaArgentina
{
    private static readonly TimeZoneInfo Zona =
        TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

    /// <summary>Dia en Argentina de un instante UTC (Unspecified se asume UTC).</summary>
    public static DateOnly DiaDe(DateTime instante)
    {
        var utc = instante.Kind == DateTimeKind.Local ? instante.ToUniversalTime() : instante;
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, Zona));
    }

    public static DateOnly Hoy() => DiaDe(DateTime.UtcNow);

    public static DateTime InicioDelDiaEnUtc(DateOnly dia)
    {
        var medianoche = dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        // En años con horario de verano hubo dias que empezaron a la 01:00.
        if (Zona.IsInvalidTime(medianoche))
            medianoche = medianoche.AddHours(1);

        return TimeZoneInfo.ConvertTimeToUtc(medianoche, Zona);
    }
}
