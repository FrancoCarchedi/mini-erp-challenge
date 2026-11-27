namespace MiniErp.Core.Services;

/// <summary>Limites de los datos de entrada, compartidos por los DTOs y el servicio.</summary>
public static class ReglasPresupuesto
{
    // Los maximos son topes tecnicos: evitan desbordes al sumar cantidades o al
    // calcular la fecha de vencimiento.
    public const int CantidadMaxima = 1_000_000;
    public const int ValidezMaximaDias = 3650;
}
