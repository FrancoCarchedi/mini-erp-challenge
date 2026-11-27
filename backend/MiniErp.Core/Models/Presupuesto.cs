namespace MiniErp.Core.Models;

public class Presupuesto
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public DateTime Fecha { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public EstadoPresupuesto Estado { get; set; }
    public int ValidezDias { get; set; }

    /// <summary>
    /// Baja logica: fecha (UTC) en que se elimino, o null si esta vigente. La fila no
    /// se borra, y el filtro global de AppDbContext la excluye de las consultas.
    /// </summary>
    public DateTime? EliminadoEn { get; set; }

    public List<PresupuestoItem> Items { get; set; } = new();
}
