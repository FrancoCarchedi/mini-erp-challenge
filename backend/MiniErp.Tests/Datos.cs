using MiniErp.Core.Data;
using MiniErp.Core.Models;

namespace MiniErp.Tests;

/// <summary>Datos base compartidos por los tests propios.</summary>
internal static class Datos
{
    public static async Task<(Cliente cliente, Articulo articulo)> SeedAsync(
        AppDbContext db, int stock = 100, decimal precio = 100m, decimal alicuota = 21m)
    {
        var cliente = new Cliente { RazonSocial = "Cliente Test", Cuit = "20-00000000-0" };
        var articulo = NuevoArticulo("A1", stock, precio, alicuota);
        db.AddRange(cliente, articulo);
        await db.SaveChangesAsync();
        return (cliente, articulo);
    }

    public static Articulo NuevoArticulo(string codigo, int stock = 100, decimal precio = 100m, decimal alicuota = 21m)
        => new()
        {
            Codigo = codigo,
            Descripcion = $"Articulo {codigo}",
            PrecioUnitario = precio,
            StockActual = stock,
            AlicuotaIva = alicuota
        };

    public static PresupuestoItem Item(int articuloId, int cantidad = 1, decimal descuentoPct = 0m)
        => new() { ArticuloId = articuloId, Cantidad = cantidad, DescuentoPct = descuentoPct };
}
