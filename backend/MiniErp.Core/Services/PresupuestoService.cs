using Microsoft.EntityFrameworkCore;
using MiniErp.Core.Data;
using MiniErp.Core.Models;

namespace MiniErp.Core.Services;

public record Totales(decimal Subtotal, decimal Iva, decimal Total);

public class PresupuestoService
{
    private readonly AppDbContext _db;
    private readonly NumeracionService _numeracion;

    public PresupuestoService(AppDbContext db, NumeracionService numeracion)
    {
        _db = db;
        _numeracion = numeracion;
    }

    /// <summary>
    /// Calcula subtotal, IVA y total de un presupuesto. El descuento se aplica
    /// por linea antes del IVA, y cada linea usa su propia alicuota.
    /// </summary>
    public Totales CalcularTotales(Presupuesto presupuesto)
    {
        var subtotal = presupuesto.Items.Sum(CalculoLinea.Subtotal);
        var iva = presupuesto.Items.Sum(CalculoLinea.Iva);
        var total = subtotal + iva;

        return new Totales(subtotal, iva, total);
    }

    /// <summary>Crea un presupuesto que vence <paramref name="validezDias"/> dias despues de hoy.</summary>
    public Task<Presupuesto> CrearAsync(int clienteId, int validezDias, List<PresupuestoItem> items)
    {
        if (validezDias < 1 || validezDias > ReglasPresupuesto.ValidezMaximaDias)
            throw new InvalidOperationException(
                $"La validez debe estar entre 1 y {ReglasPresupuesto.ValidezMaximaDias} dias.");

        return GuardarNuevoAsync(clienteId, DateTime.UtcNow, validezDias, items);
    }

    /// <summary>Crea un presupuesto que se puede facturar hasta ese dia inclusive.</summary>
    public Task<Presupuesto> CrearAsync(int clienteId, DateOnly fechaVencimiento, List<PresupuestoItem> items)
    {
        // El mismo instante define "hoy" y la fecha que se guarda.
        var ahora = DateTime.UtcNow;
        var dias = fechaVencimiento.DayNumber - ZonaArgentina.DiaDe(ahora).DayNumber;

        if (dias < 1 || dias > ReglasPresupuesto.ValidezMaximaDias)
            throw new InvalidOperationException(
                $"La fecha de vencimiento debe ser posterior a hoy y no superar los {ReglasPresupuesto.ValidezMaximaDias} dias.");

        return GuardarNuevoAsync(clienteId, ahora, dias, items);
    }

    private async Task<Presupuesto> GuardarNuevoAsync(
        int clienteId, DateTime ahora, int validezDias, List<PresupuestoItem> items)
    {
        ValidarItems(items);

        if (!await _db.Clientes.AnyAsync(c => c.Id == clienteId))
            throw new InvalidOperationException($"El cliente {clienteId} no existe.");

        foreach (var item in items)
        {
            var articulo = await _db.Articulos.FirstOrDefaultAsync(a => a.Id == item.ArticuloId)
                ?? throw new InvalidOperationException($"El articulo {item.ArticuloId} no existe.");

            // Tomamos precio y alicuota actuales del articulo como snapshot.
            item.PrecioUnitario = articulo.PrecioUnitario;
            item.AlicuotaIva = articulo.AlicuotaIva;
        }

        // El numero y el alta van juntos: si el guardado falla, el numero no se consume.
        await using var transaccion = await _db.Database.BeginTransactionAsync();

        var presupuesto = new Presupuesto
        {
            Numero = await _numeracion.ProximoNumeroPresupuestoAsync(),
            Fecha = ahora,
            ClienteId = clienteId,
            Estado = EstadoPresupuesto.Borrador,
            ValidezDias = validezDias,
            FechaVencimiento = ZonaArgentina.DiaDe(ahora).AddDays(validezDias),
            Items = items
        };

        _db.Presupuestos.Add(presupuesto);
        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();
        return presupuesto;
    }

    /// <summary>
    /// Clona un presupuesto como uno nuevo: numero y fecha nuevos, en Borrador y con
    /// el precio y la alicuota actuales de cada articulo. Devuelve null si no existe.
    /// </summary>
    public async Task<Presupuesto?> DuplicarAsync(int id)
    {
        var original = await _db.Presupuestos
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (original is null) return null;

        // Solo se copia lo que eligio el usuario; CrearAsync toma el resto.
        var items = original.Items
            .OrderBy(i => i.Id)
            .Select(i => new PresupuestoItem
            {
                ArticuloId = i.ArticuloId,
                Cantidad = i.Cantidad,
                DescuentoPct = i.DescuentoPct
            })
            .ToList();

        return await CrearAsync(original.ClienteId, PlazoOtorgado(original), items);
    }

    // El plazo que va desde la emision hasta el vencimiento. Si la fecha se edito a
    // mano y quedo fuera de rango, se usa el plazo con el que se emitio.
    private static int PlazoOtorgado(Presupuesto presupuesto)
    {
        var dias = presupuesto.FechaVencimiento.DayNumber - ZonaArgentina.DiaDe(presupuesto.Fecha).DayNumber;
        return dias is >= 1 and <= ReglasPresupuesto.ValidezMaximaDias ? dias : presupuesto.ValidezDias;
    }

    // Las mismas reglas que los DTOs, para que el servicio no dependa de la API.
    private static void ValidarItems(List<PresupuestoItem> items)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("El presupuesto necesita al menos un item.");

        if (items.Any(i => i.Cantidad < 1 || i.Cantidad > ReglasPresupuesto.CantidadMaxima))
            throw new InvalidOperationException(
                $"La cantidad debe estar entre 1 y {ReglasPresupuesto.CantidadMaxima}.");

        if (items.Any(i => i.DescuentoPct < 0m || i.DescuentoPct > 100m))
            throw new InvalidOperationException("El descuento debe estar entre 0 y 100.");

        if (items.Any(i => decimal.Round(i.DescuentoPct, 2) != i.DescuentoPct))
            throw new InvalidOperationException("El descuento admite hasta 2 decimales.");
    }

    public async Task<Presupuesto?> ObtenerAsync(int id)
    {
        return await _db.Presupuestos
            .Include(p => p.Cliente)
            .Include(p => p.Items)
                .ThenInclude(i => i.Articulo)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Presupuesto>> ListarAsync()
    {
        return await _db.Presupuestos
            .Include(p => p.Cliente)
            .Include(p => p.Items)
                .ThenInclude(i => i.Articulo)
            .OrderByDescending(p => p.Numero)
            .ToListAsync();
    }

    /// <summary>
    /// Baja logica: marca el presupuesto como eliminado y conserva la fila y sus items.
    /// Devuelve false si no existe o ya estaba eliminado.
    /// </summary>
    public async Task<bool> EliminarAsync(int id)
    {
        // Se abre antes de leer para que no se pueda facturar entre el control y la baja.
        await using var transaccion = await _db.Database.BeginTransactionAsync();

        // No se usa FindAsync: devolveria una entidad ya eliminada que siga en memoria.
        var presupuesto = await _db.Presupuestos.FirstOrDefaultAsync(p => p.Id == id);
        if (presupuesto is null) return false;

        if (presupuesto.Estado == EstadoPresupuesto.Facturado ||
            await _db.Facturas.AnyAsync(f => f.PresupuestoId == id))
            throw new InvalidOperationException("El presupuesto ya fue facturado y no se puede eliminar.");

        presupuesto.EliminadoEn = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();
        return true;
    }
}
