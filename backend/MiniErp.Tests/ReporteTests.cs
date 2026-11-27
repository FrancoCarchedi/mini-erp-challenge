using MiniErp.Core.Data;
using MiniErp.Core.Models;
using MiniErp.Core.Services;
using Xunit;

namespace MiniErp.Tests;

public class ReporteTests
{
    private static readonly DateOnly Primero = new(2026, 11, 1);
    private static readonly DateOnly Ultimo = new(2026, 11, 30);

    private static DateTime Utc(int mes, int dia, int hora, int minuto = 0)
        => new(2026, mes, dia, hora, minuto, 0, DateTimeKind.Utc);

    private static int _numero;

    private static async Task FacturarAsync(AppDbContext db, Cliente cliente, DateTime fecha, params PresupuestoItem[] items)
    {
        var numero = Interlocked.Increment(ref _numero);
        db.Facturas.Add(new Factura
        {
            Numero = numero,
            Fecha = fecha,
            Presupuesto = new Presupuesto
            {
                Numero = numero,
                Fecha = fecha,
                ClienteId = cliente.Id,
                ValidezDias = 15,
                Estado = EstadoPresupuesto.Facturado,
                Items = items.ToList()
            }
        });
        await db.SaveChangesAsync();
    }

    private static PresupuestoItem Linea(Articulo art, int cantidad, decimal precio, decimal descuento = 0m)
        => new()
        {
            ArticuloId = art.Id,
            Cantidad = cantidad,
            PrecioUnitario = precio,
            DescuentoPct = descuento,
            AlicuotaIva = art.AlicuotaIva
        };

    [Fact]
    public async Task Rango_UsaElDiaDeArgentina_IncluyendoAmbosExtremos()
    {
        var db = TestDbFactory.Create();
        var (cliente, art) = await Datos.SeedAsync(db);

        // Argentina es UTC-3: noviembre va del 01/11 03:00 UTC al 01/12 03:00 UTC.
        // Las potencias de 2 permiten ver exactamente que facturas entraron.
        await FacturarAsync(db, cliente, Utc(11, 1, 2, 59), Linea(art, 1, 100m));  // 31/10 23:59 AR: afuera
        await FacturarAsync(db, cliente, Utc(11, 1, 3), Linea(art, 2, 100m));      // 01/11 00:00 AR: adentro
        await FacturarAsync(db, cliente, Utc(12, 1, 1), Linea(art, 4, 100m));      // 30/11 22:00 AR: adentro
        await FacturarAsync(db, cliente, Utc(12, 1, 2, 59), Linea(art, 8, 100m));  // 30/11 23:59 AR: adentro
        await FacturarAsync(db, cliente, Utc(12, 1, 3), Linea(art, 16, 100m));     // 01/12 00:00 AR: afuera

        var top = await new ReporteService(db).TopArticulosAsync(Primero, Ultimo, 5);

        Assert.Equal(2 + 4 + 8, Assert.Single(top).Cantidad);
    }

    [Fact]
    public async Task Top_OrdenaPorMontoNetoGuardado_YRespetaElLimite()
    {
        var db = TestDbFactory.Create();
        var (cliente, a) = await Datos.SeedAsync(db, precio: 999_999m); // el precio actual no se usa
        var b = Datos.NuevoArticulo("B1", alicuota: 10.5m);
        var c = Datos.NuevoArticulo("C1");
        db.AddRange(b, c);
        await db.SaveChangesAsync();

        await FacturarAsync(db, cliente, Utc(11, 10, 12), Linea(a, 1, 100m), Linea(b, 10, 100m, 10m));
        await FacturarAsync(db, cliente, Utc(11, 20, 12), Linea(b, 1, 200m), Linea(c, 5, 100m));

        var top = await new ReporteService(db).TopArticulosAsync(Primero, Ultimo, 2);

        Assert.Equal(new[] { "B1", "C1" }, top.Select(x => x.Codigo));
        Assert.Equal((11, 1100m, 115.5m), (top[0].Cantidad, top[0].Monto, top[0].Iva));
        Assert.Equal(500m, top[1].Monto);
    }

    [Fact]
    public async Task SinFacturasEnElRango_DevuelveListaVacia()
    {
        var db = TestDbFactory.Create();

        Assert.Empty(await new ReporteService(db).TopArticulosAsync(Primero, Ultimo, 5));
    }

    [Theory]
    [InlineData("2026-11-30", "2026-11-01", 5)]
    [InlineData("2026-11-01", "2026-11-30", 0)]
    [InlineData("2026-11-01", "2026-11-30", 101)]
    [InlineData("2026-11-01", "9999-12-31", 5)]
    public async Task ParametrosInvalidos_Lanza(string desde, string hasta, int top)
    {
        var service = new ReporteService(TestDbFactory.Create());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TopArticulosAsync(DateOnly.Parse(desde), DateOnly.Parse(hasta), top));
    }

    [Fact]
    public async Task MedianocheInexistentePorHorarioDeVerano_NoFalla()
    {
        // El 19/10/2008 Argentina adelanto el reloj a las 00:00.
        var service = new ReporteService(TestDbFactory.Create());
        var dia = new DateOnly(2008, 10, 19);

        Assert.Empty(await service.TopArticulosAsync(dia, dia, 5));
    }
}
