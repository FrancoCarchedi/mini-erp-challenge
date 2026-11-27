using Microsoft.EntityFrameworkCore;
using MiniErp.Core.Data;

namespace MiniErp.Core.Services;

/// <summary>Monto es el subtotal neto facturado, sin IVA.</summary>
public record TopArticulo(int ArticuloId, string Codigo, string Descripcion, int Cantidad, decimal Monto, decimal Iva);

public class ReporteService
{
    public const int TopMaximo = 100;

    private readonly AppDbContext _db;

    public ReporteService(AppDbContext db) => _db = db;

    /// <summary>
    /// Articulos con mayor monto facturado entre dos dias de Argentina, ambos inclusive.
    /// </summary>
    public async Task<List<TopArticulo>> TopArticulosAsync(DateOnly desde, DateOnly hasta, int top)
    {
        if (desde > hasta)
            throw new InvalidOperationException("La fecha 'desde' no puede ser posterior a 'hasta'.");
        if (hasta == DateOnly.MaxValue)
            throw new InvalidOperationException("La fecha 'hasta' esta fuera de rango.");
        if (top < 1 || top > TopMaximo)
            throw new InvalidOperationException($"El parametro 'top' debe estar entre 1 y {TopMaximo}.");

        var inicio = ZonaArgentina.InicioDelDiaEnUtc(desde);
        var fin = ZonaArgentina.InicioDelDiaEnUtc(hasta.AddDays(1)); // exclusivo

        var facturas = await _db.Facturas
            .AsNoTracking()
            .Where(f => f.Fecha >= inicio && f.Fecha < fin)
            .Include(f => f.Presupuesto!)
                .ThenInclude(p => p.Items)
                    .ThenInclude(i => i.Articulo)
            .ToListAsync();

        // SQLite no puede sumar decimales en SQL: se filtra en la base y se suma en memoria.
        // Los precios son los guardados en el presupuesto, no los actuales.
        return facturas
            .SelectMany(f => f.Presupuesto!.Items)
            .GroupBy(i => i.ArticuloId)
            .Select(g => new TopArticulo(
                g.Key,
                g.First().Articulo!.Codigo,
                g.First().Articulo!.Descripcion,
                g.Sum(i => i.Cantidad),
                g.Sum(CalculoLinea.Subtotal),
                g.Sum(CalculoLinea.Iva)))
            .OrderByDescending(a => a.Monto)
            .ThenBy(a => a.Codigo)
            .Take(top)
            .ToList();
    }
}
