using MiniErp.Core.Models;
using MiniErp.Core.Services;
using Xunit;

namespace MiniErp.Tests;

public class CalcularTotalesExtraTests
{
    private static Totales Calcular(params PresupuestoItem[] items)
    {
        var db = TestDbFactory.Create();
        var service = new PresupuestoService(db, new NumeracionService(db));
        return service.CalcularTotales(new Presupuesto { Items = items.ToList() });
    }

    private static PresupuestoItem Linea(int cantidad, decimal precio, decimal alicuota, decimal descuento = 0m)
        => new() { Cantidad = cantidad, PrecioUnitario = precio, AlicuotaIva = alicuota, DescuentoPct = descuento };

    [Fact]
    public void ArticulosDelSeedConAlicuotasMixtas_DaElResultadoDelReadme()
    {
        var t = Calcular(Linea(1, 35000m, 21m), Linea(1, 4500m, 10.5m));

        Assert.Equal(new Totales(39500m, 7822.50m, 47322.50m), t);
    }

    [Fact]
    public void ElIvaSeRedondeaPorLinea_NoSobreElTotal()
    {
        // IVA por linea: 0,105 -> 0,11, tres veces = 0,33.
        // Redondeando el total (0,315) daria 0,32.
        var t = Calcular(Linea(1, 1m, 10.5m), Linea(1, 1m, 10.5m), Linea(1, 1m, 10.5m));

        Assert.Equal(new Totales(3m, 0.33m, 3.33m), t);
    }

    [Fact]
    public void LosMediosCentavosSeRedondeanHaciaArriba()
    {
        // 0,25 con 50% de descuento = 0,125 -> 0,13 (banker's rounding daria 0,12).
        var t = Calcular(Linea(1, 0.25m, 21m, 50m));

        Assert.Equal(0.13m, t.Subtotal);
    }

    [Fact]
    public void ImportesGrandes_DanLoMismoQueElCliente()
    {
        // El mismo caso esta en el cliente (totales.test.ts).
        var t = Calcular(Linea(999_999, 220000.01m, 21m, 33.33m));

        Assert.Equal(146673859992.99m, t.Subtotal);
    }

    [Fact]
    public void ElIvaSeCalculaSobreElSubtotalYaRedondeado()
    {
        // Subtotal 0,115 -> 0,12; IVA 0,12 x 21% = 0,0252 -> 0,03.
        // Sobre el subtotal sin redondear daria 0,02415 -> 0,02.
        var t = Calcular(Linea(1, 0.23m, 21m, 50m));

        Assert.Equal(new Totales(0.12m, 0.03m, 0.15m), t);
    }
}
