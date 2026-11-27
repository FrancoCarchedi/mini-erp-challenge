using MiniErp.Core.Models;
using MiniErp.Core.Services;
using Xunit;

namespace MiniErp.Tests;

public class DuplicarTests
{
    [Fact]
    public async Task Duplicar_AsignaNumeroNuevoYRefrescaPrecioYAlicuota()
    {
        var db = TestDbFactory.Create();
        var service = new PresupuestoService(db, new NumeracionService(db));
        var (cliente, articulo) = await Datos.SeedAsync(db, precio: 100m, alicuota: 21m);
        var original = await service.CrearAsync(cliente.Id, 20, new List<PresupuestoItem> { Datos.Item(articulo.Id, 3, 10m) });
        original.Estado = EstadoPresupuesto.Facturado;
        original.Fecha = DateTime.UtcNow.AddDays(-90);
        articulo.PrecioUnitario = 150m;
        articulo.AlicuotaIva = 10.5m;
        await db.SaveChangesAsync();

        var copia = await service.DuplicarAsync(original.Id);

        Assert.NotNull(copia);
        Assert.NotEqual(original.Id, copia.Id);
        Assert.True(copia.Numero > original.Numero);
        Assert.Equal(EstadoPresupuesto.Borrador, copia.Estado);
        Assert.True(DateTime.UtcNow - copia.Fecha < TimeSpan.FromMinutes(1));
        Assert.Equal(cliente.Id, copia.ClienteId);
        Assert.Equal(20, copia.ValidezDias);

        var item = Assert.Single(copia.Items);
        Assert.Equal((articulo.Id, 3, 10m), (item.ArticuloId, item.Cantidad, item.DescuentoPct));
        Assert.Equal((150m, 10.5m), (item.PrecioUnitario, item.AlicuotaIva));

        // El original conserva su snapshot.
        var anterior = (await service.ObtenerAsync(original.Id))!.Items.Single();
        Assert.Equal((100m, 21m), (anterior.PrecioUnitario, anterior.AlicuotaIva));
    }

    [Fact]
    public async Task Duplicar_PresupuestoInexistente_DevuelveNull()
    {
        var db = TestDbFactory.Create();
        var service = new PresupuestoService(db, new NumeracionService(db));

        Assert.Null(await service.DuplicarAsync(999));
    }
}
