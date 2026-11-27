using Microsoft.EntityFrameworkCore;
using MiniErp.Core.Data;
using MiniErp.Core.Models;

namespace MiniErp.Core.Services;

public class FacturacionService
{
    private readonly AppDbContext _db;
    private readonly PresupuestoService _presupuestos;
    private readonly NumeracionService _numeracion;

    public FacturacionService(AppDbContext db, PresupuestoService presupuestos, NumeracionService numeracion)
    {
        _db = db;
        _presupuestos = presupuestos;
        _numeracion = numeracion;
    }

    public async Task<Factura> FacturarAsync(int presupuestoId)
    {
        // La transaccion se abre antes de leer: en SQLite toma el lock de escritura
        // de entrada, asi que dos facturaciones simultaneas no pueden validar contra
        // el mismo stock ni facturar dos veces el mismo presupuesto.
        await using var transaccion = await _db.Database.BeginTransactionAsync();

        var presupuesto = await _db.Presupuestos
            .Include(p => p.Items)
                .ThenInclude(i => i.Articulo)
            .FirstOrDefaultAsync(p => p.Id == presupuestoId)
            ?? throw new InvalidOperationException("El presupuesto no existe.");

        // Primero todas las validaciones; recien despues se modifican datos.
        await ValidarEstadoAsync(presupuesto);
        ValidarVigencia(presupuesto);
        ValidarStock(presupuesto);

        foreach (var item in presupuesto.Items)
        {
            item.Articulo!.StockActual -= item.Cantidad;
        }

        var totales = _presupuestos.CalcularTotales(presupuesto);

        var factura = new Factura
        {
            Numero = await _numeracion.ProximoNumeroFacturaAsync(),
            Fecha = DateTime.UtcNow,
            PresupuestoId = presupuesto.Id,
            Subtotal = totales.Subtotal,
            Iva = totales.Iva,
            Total = totales.Total
        };

        presupuesto.Estado = EstadoPresupuesto.Facturado;
        _db.Facturas.Add(factura);
        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();
        return factura;
    }

    // Borrador y Aprobado se pueden facturar: todo presupuesto nace en Borrador
    // y la API no tiene un paso de aprobacion.
    private async Task ValidarEstadoAsync(Presupuesto presupuesto)
    {
        if (presupuesto.Estado == EstadoPresupuesto.Facturado ||
            await _db.Facturas.AnyAsync(f => f.PresupuestoId == presupuesto.Id))
            throw new InvalidOperationException("El presupuesto ya fue facturado.");

        if (presupuesto.Estado == EstadoPresupuesto.Rechazado)
            throw new InvalidOperationException("El presupuesto esta rechazado y no se puede facturar.");
    }

    private static void ValidarVigencia(Presupuesto presupuesto)
    {
        // El dia del vencimiento todavia se puede facturar.
        if (ZonaArgentina.Hoy() > presupuesto.FechaVencimiento)
            throw new InvalidOperationException("El presupuesto esta vencido y no se puede facturar.");
    }

    // Un articulo puede venir en varias lineas: el stock se valida contra la suma.
    private static void ValidarStock(Presupuesto presupuesto)
    {
        var faltantes = presupuesto.Items
            .GroupBy(i => i.ArticuloId)
            .Select(g => new { g.First().Articulo, Pedido = g.Sum(i => i.Cantidad) })
            .Where(x => x.Pedido > x.Articulo!.StockActual)
            .Select(x => $"{x.Articulo!.Codigo} {x.Articulo.Descripcion} (pedido {x.Pedido}, disponible {x.Articulo.StockActual})")
            .ToList();

        if (faltantes.Count > 0)
            throw new InvalidOperationException($"Stock insuficiente: {string.Join("; ", faltantes)}.");
    }
}
