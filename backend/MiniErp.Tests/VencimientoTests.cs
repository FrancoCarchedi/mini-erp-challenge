using Microsoft.EntityFrameworkCore;
using MiniErp.Core;
using MiniErp.Core.Data;
using MiniErp.Core.Models;
using MiniErp.Core.Services;
using Xunit;

namespace MiniErp.Tests;

public class VencimientoTests
{
    private sealed record Escenario(
        AppDbContext Db, PresupuestoService Presupuestos, FacturacionService Facturacion,
        int ClienteId, int ArticuloId)
    {
        public List<PresupuestoItem> Items() => new() { Datos.Item(ArticuloId) };
    }

    private static async Task<Escenario> ArmarAsync()
    {
        var db = TestDbFactory.Create();
        var numeracion = new NumeracionService(db);
        var presupuestos = new PresupuestoService(db, numeracion);
        var (cliente, articulo) = await Datos.SeedAsync(db);
        return new Escenario(
            db, presupuestos, new FacturacionService(db, presupuestos, numeracion), cliente.Id, articulo.Id);
    }

    [Theory]
    [InlineData("2026-12-01T02:59:59Z", "2026-11-30")] // 23:59 del 30/11 en Argentina
    [InlineData("2026-12-01T03:00:00Z", "2026-12-01")]
    public void DiaDe_UsaElDiaDeArgentina(string instante, string diaEsperado)
    {
        var utc = DateTime.Parse(instante).ToUniversalTime();

        Assert.Equal(DateOnly.Parse(diaEsperado), ZonaArgentina.DiaDe(utc));
    }

    [Fact]
    public async Task CrearConValidezDias_GuardaLaFechaDeVencimiento()
    {
        var e = await ArmarAsync();

        var creado = await e.Presupuestos.CrearAsync(e.ClienteId, 15, e.Items());

        e.Db.ChangeTracker.Clear(); // obliga a leer de la base
        var guardado = await e.Db.Presupuestos.SingleAsync();
        Assert.Equal(ZonaArgentina.DiaDe(creado.Fecha).AddDays(15), guardado.FechaVencimiento);
        Assert.Equal(15, guardado.ValidezDias);
    }

    [Fact]
    public async Task CrearConFechaDeVencimiento_LaGuardaYDerivaElPlazo()
    {
        var e = await ArmarAsync();
        var vence = ZonaArgentina.Hoy().AddDays(20);

        await e.Presupuestos.CrearAsync(e.ClienteId, vence, e.Items());

        e.Db.ChangeTracker.Clear();
        var guardado = await e.Db.Presupuestos.SingleAsync();
        Assert.Equal(vence, guardado.FechaVencimiento);
        Assert.Equal(20, guardado.ValidezDias);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)] // equivale a validez 0
    [InlineData(ReglasPresupuesto.ValidezMaximaDias + 1)]
    public async Task CrearConFechaFueraDeRango_LanzaYNoGuarda(int diasDesdeHoy)
    {
        var e = await ArmarAsync();
        var vence = ZonaArgentina.Hoy().AddDays(diasDesdeHoy);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Presupuestos.CrearAsync(e.ClienteId, vence, e.Items()));

        Assert.Equal(0, await e.Db.Presupuestos.CountAsync());
    }

    [Fact]
    public async Task GuardadoSoloConValidezDias_CompletaElVencimiento()
    {
        // Es lo que hacen los tests originales, que no conocen FechaVencimiento.
        var e = await ArmarAsync();
        var fecha = new DateTime(2026, 12, 1, 1, 0, 0, DateTimeKind.Utc); // 30/11 en Argentina

        e.Db.Presupuestos.Add(new Presupuesto { Numero = 1, Fecha = fecha, ClienteId = e.ClienteId, ValidezDias = 10 });
        await e.Db.SaveChangesAsync();

        e.Db.ChangeTracker.Clear();
        Assert.Equal(new DateOnly(2026, 12, 10), (await e.Db.Presupuestos.SingleAsync()).FechaVencimiento);
    }

    [Fact]
    public async Task ElDiaDelVencimiento_TodaviaSeFactura()
    {
        var e = await ArmarAsync();
        var creado = await e.Presupuestos.CrearAsync(e.ClienteId, 15, e.Items());
        creado.FechaVencimiento = ZonaArgentina.Hoy();
        await e.Db.SaveChangesAsync();

        await e.Facturacion.FacturarAsync(creado.Id);

        Assert.Equal(1, await e.Db.Facturas.CountAsync());
    }

    [Fact]
    public async Task EditandoSoloLaFechaEnLaBase_ElPresupuestoQuedaVencido()
    {
        var e = await ArmarAsync();
        var creado = await e.Presupuestos.CrearAsync(e.ClienteId, 3000, e.Items());
        var ayer = ZonaArgentina.Hoy().AddDays(-1).ToString("yyyy-MM-dd");
        await e.Db.Database.ExecuteSqlAsync(
            $"UPDATE Presupuestos SET FechaVencimiento = {ayer} WHERE Id = {creado.Id}");
        e.Db.ChangeTracker.Clear();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Facturacion.FacturarAsync(creado.Id));

        Assert.Contains("vencido", error.Message);
    }

    [Theory]
    [InlineData(40, 40)]  // vencimiento editado y todavia posterior a la emision: manda la fecha
    [InlineData(-5, 15)]  // anterior a la emision: se usa el plazo original
    public async Task Duplicar_ConservaElPlazoSegunLaFechaDeVencimiento(int diasEditados, int plazoEsperado)
    {
        var e = await ArmarAsync();
        var original = await e.Presupuestos.CrearAsync(e.ClienteId, 15, e.Items());
        original.FechaVencimiento = ZonaArgentina.DiaDe(original.Fecha).AddDays(diasEditados);
        await e.Db.SaveChangesAsync();

        var copia = await e.Presupuestos.DuplicarAsync(original.Id);

        Assert.Equal(ZonaArgentina.DiaDe(copia!.Fecha).AddDays(plazoEsperado), copia.FechaVencimiento);
        Assert.Equal(plazoEsperado, copia.ValidezDias);
    }
}
