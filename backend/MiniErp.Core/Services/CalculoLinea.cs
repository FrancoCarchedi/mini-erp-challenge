using MiniErp.Core.Models;

namespace MiniErp.Core.Services;

/// <summary>
/// Reglas de calculo de una linea. Es el unico lugar donde se redondea: los
/// totales de un presupuesto son la suma de valores ya redondeados por linea.
/// </summary>
public static class CalculoLinea
{
    public static decimal Subtotal(PresupuestoItem item)
        => Redondear(item.Cantidad * item.PrecioUnitario * (1 - item.DescuentoPct / 100m));

    public static decimal Iva(PresupuestoItem item)
        => Redondear(Subtotal(item) * item.AlicuotaIva / 100m);

    // El default de .NET es banker's rounding (al par mas cercano); aca se usa
    // el redondeo comercial, que es el mismo que aplica el cliente.
    private static decimal Redondear(decimal valor)
        => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
