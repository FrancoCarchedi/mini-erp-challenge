using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MiniErp.Core.Data;

/// <summary>
/// Las fechas se guardan en UTC, pero SQLite no conserva el Kind: al leerlas hay
/// que volver a marcarlas como UTC para que el JSON salga con "Z".
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            // Unspecified se asume UTC: convertirlo lo correria segun la zona del servidor.
            valor => valor.Kind == DateTimeKind.Local ? valor.ToUniversalTime() : valor,
            valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc))
    {
    }
}
