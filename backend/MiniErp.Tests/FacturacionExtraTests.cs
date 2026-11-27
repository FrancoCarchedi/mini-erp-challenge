using Microsoft.EntityFrameworkCore;
using MiniErp.Core.Data;
using MiniErp.Core;
using MiniErp.Core.Models;
using MiniErp.Core.Services;
using Xunit;

namespace MiniErp.Tests;

public class FacturacionExtraTests
{
    private sealed record Escenario(
        AppDbContext Db, PresupuestoService Presupuestos, FacturacionService Facturacion,
        Articulo Articulo, Presupuesto Presupuesto);

    private static async Task<Escenario> ArmarAsync(int stock, params int[] cantidadesPorLinea)
    {
        var db = TestDbFactory.Create();
        var numeracion = new NumeracionService(db);
        var presupuestos = new PresupuestoService(db, numeracion);
        var (cliente, articulo) = await Datos.SeedAsync(db, stock);
        var items = cantidadesPorLinea.Select(c => Datos.Item(articulo.Id, c)).ToList();
        var presupuesto = await presupuestos.CrearAsync(cliente.Id, 15, items);
        return new Escenario(db, presupuestos, new FacturacionService(db, presupuestos, numeracion), articulo, presupuesto);
    }

    private static async Task AssertRechazadoAsync(Escenario e, string mensajeEsperado, int stockEsperado)
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Facturacion.FacturarAsync(e.Presupuesto.Id));

        Assert.Contains(mensajeEsperado, error.Message);
        Assert.Equal(0, await e.Db.Facturas.CountAsync());
        var enBase = await e.Db.Articulos.AsNoTracking().SingleAsync(a => a.Id == e.Articulo.Id);
        Assert.Equal(stockEsperado, enBase.StockActual);
    }

    [Fact]
    public async Task Borrador_SePuedeFacturar()
    {
        var e = await ArmarAsync(stock: 10, 2);

        var factura = await e.Facturacion.FacturarAsync(e.Presupuesto.Id);

        Assert.Equal(1, factura.Numero);
        Assert.Equal(EstadoPresupuesto.Facturado, e.Presupuesto.Estado);
    }

    [Fact]
    public async Task Vencido_NoSeFactura()
    {
        var e = await ArmarAsync(stock: 10, 2);
        e.Presupuesto.FechaVencimiento = ZonaArgentina.Hoy().AddDays(-1);
        await e.Db.SaveChangesAsync();

        await AssertRechazadoAsync(e, "vencido", stockEsperado: 10);
    }

    [Fact]
    public async Task Rechazado_NoSeFactura()
    {
        var e = await ArmarAsync(stock: 10, 2);
        e.Presupuesto.Estado = EstadoPresupuesto.Rechazado;
        await e.Db.SaveChangesAsync();

        await AssertRechazadoAsync(e, "rechazado", stockEsperado: 10);
    }

    [Fact]
    public async Task ArticuloRepetido_ValidaElStockContraLaSuma()
    {
        // Cada linea entra por separado, pero juntas piden 6 de 5.
        var e = await ArmarAsync(stock: 5, 3, 3);

        await AssertRechazadoAsync(e, "pedido 6, disponible 5", stockEsperado: 5);
    }

    [Fact]
    public async Task ArticuloRepetido_ConStockJusto_DejaElStockEnCero()
    {
        var e = await ArmarAsync(stock: 6, 3, 3);

        await e.Facturacion.FacturarAsync(e.Presupuesto.Id);

        Assert.Equal(0, e.Articulo.StockActual);
    }

    [Fact]
    public async Task Facturado_NoSePuedeEliminar_YLaFacturaSeConserva()
    {
        var e = await ArmarAsync(stock: 10, 2);
        await e.Facturacion.FacturarAsync(e.Presupuesto.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Presupuestos.EliminarAsync(e.Presupuesto.Id));

        Assert.Equal(1, await e.Db.Facturas.CountAsync());
    }
}
