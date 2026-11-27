namespace MiniErp.Core.Models;

/// <summary>
/// Ultimo numero entregado por cada serie. Solo avanza: borrar un documento no
/// libera su numero.
/// </summary>
public class Secuencia
{
    public string Nombre { get; set; } = "";
    public int UltimoValor { get; set; }
}
