using Microsoft.EntityFrameworkCore;
using MiniErp.Core.Data;
using MiniErp.Core.Models;
using MiniErp.Core.Services;
using Xunit;

namespace MiniErp.Tests;

public class EliminacionTests
{
    private sealed record Escenario(
        AppDbContext Db, PresupuestoService Presupuestos, FacturacionService Facturacion,
        Func<Task<Presupuesto>> Crear);

    private static async Task<Escenario> ArmarAsync()
    {
        var db = TestDbFactory.Create();
        var numeracion = new NumeracionService(db);
        var presupuestos = new PresupuestoService(db, numeracion);
        var (cliente, articulo) = await Datos.SeedAsync(db, stock: 10);
        return new Escenario(
            db, presupuestos, new FacturacionService(db, presupuestos, numeracion),
            () => presupuestos.CrearAsync(cliente.Id, 15, new List<PresupuestoItem> { Datos.Item(articulo.Id, 2) }));
    }

    [Fact]
    public async Task Eliminar_ConservaLaFilaYSusItems_ConLaFechaDeBaja()
    {
        var e = await ArmarAsync();
        var creado = await e.Crear();
        var antes = DateTime.UtcNow;

        Assert.True(await e.Presupuestos.EliminarAsync(creado.Id));

        e.Db.ChangeTracker.Clear(); // obliga a leer de la base
        var guardado = await e.Db.Presupuestos
            .IgnoreQueryFilters()
            .Include(p => p.Items)
            .SingleAsync(p => p.Id == creado.Id);
        Assert.Equal(creado.Numero, guardado.Numero);
        Assert.Single(guardado.Items);
        Assert.NotNull(guardado.EliminadoEn);
        Assert.Equal(DateTimeKind.Utc, guardado.EliminadoEn.Value.Kind);
        Assert.InRange(guardado.EliminadoEn.Value, antes, DateTime.UtcNow);
    }

    [Fact]
    public async Task Eliminado_NoSeListaNiSeObtieneNiSeDuplica()
    {
        var e = await ArmarAsync();
        var eliminado = await e.Crear();
        var vigente = await e.Crear();

        await e.Presupuestos.EliminarAsync(eliminado.Id);

        Assert.Equal(vigente.Id, Assert.Single(await e.Presupuestos.ListarAsync()).Id);
        Assert.Null(await e.Presupuestos.ObtenerAsync(eliminado.Id));
        Assert.Null(await e.Presupuestos.DuplicarAsync(eliminado.Id));
    }

    [Fact]
    public async Task Eliminado_NoSeFacturaNiDescuentaStock()
    {
        var e = await ArmarAsync();
        var creado = await e.Crear();
        await e.Presupuestos.EliminarAsync(creado.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Facturacion.FacturarAsync(creado.Id));

        Assert.Equal(0, await e.Db.Facturas.CountAsync());
        Assert.Equal(10, (await e.Db.Articulos.AsNoTracking().SingleAsync()).StockActual);
    }

    [Fact]
    public async Task EliminarDosVeces_LaSegundaDevuelveFalse_YNoCambiaLaFecha()
    {
        var e = await ArmarAsync();
        var creado = await e.Crear();
        await e.Presupuestos.EliminarAsync(creado.Id);
        var fechaDeBaja = creado.EliminadoEn;

        Assert.False(await e.Presupuestos.EliminarAsync(creado.Id));

        var guardado = await e.Db.Presupuestos.IgnoreQueryFilters().AsNoTracking().SingleAsync();
        Assert.Equal(fechaDeBaja, guardado.EliminadoEn);
    }

    [Fact]
    public async Task SinLaSecuencia_ElNumeroDeUnEliminadoSigueOcupado()
    {
        var e = await ArmarAsync();
        var eliminado = await e.Crear();
        await e.Presupuestos.EliminarAsync(eliminado.Id);
        await e.Db.Secuencias.ExecuteDeleteAsync();

        Assert.Equal(eliminado.Numero + 1, (await e.Crear()).Numero);
    }

    [Fact]
    public async Task LaBaseRechazaMarcarComoEliminadoUnFacturado()
    {
        var e = await ArmarAsync();
        var creado = await e.Crear();
        await e.Facturacion.FacturarAsync(creado.Id);

        // Saltea el control del servicio.
        creado.EliminadoEn = DateTime.UtcNow;

        await Assert.ThrowsAsync<DbUpdateException>(() => e.Db.SaveChangesAsync());
    }
}
