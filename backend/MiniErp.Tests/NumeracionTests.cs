using MiniErp.Core.Data;
using MiniErp.Core.Models;
using MiniErp.Core.Services;
using Xunit;

namespace MiniErp.Tests;

public class NumeracionTests
{
    private static async Task<(PresupuestoService service, Func<Task<Presupuesto>> crear, AppDbContext db)> ArmarAsync()
    {
        var db = TestDbFactory.Create();
        var service = new PresupuestoService(db, new NumeracionService(db));
        var (cliente, articulo) = await Datos.SeedAsync(db);
        return (service,
            () => service.CrearAsync(cliente.Id, 15, new List<PresupuestoItem> { Datos.Item(articulo.Id) }),
            db);
    }

    [Fact]
    public async Task CrearBorrarYCrear_NoRepiteNumeros()
    {
        var (service, crear, _) = await ArmarAsync();
        var primero = await crear();
        var segundo = await crear();

        await service.EliminarAsync(primero.Id);
        var tercero = await crear();

        Assert.Equal(new[] { 1, 2, 3 }, new[] { primero.Numero, segundo.Numero, tercero.Numero });
    }

    [Fact]
    public async Task BorrarElUltimo_NoHaceRetrocederLaSecuencia()
    {
        var (service, crear, _) = await ArmarAsync();
        await crear();
        var ultimo = await crear();

        await service.EliminarAsync(ultimo.Id);

        Assert.Equal(ultimo.Numero + 1, (await crear()).Numero);
    }

    [Fact]
    public async Task ConPresupuestosPreexistentes_ContinuaDesdeElMaximo()
    {
        var (_, crear, db) = await ArmarAsync();
        var primero = await crear();
        db.Presupuestos.Add(new Presupuesto { Numero = 50, Fecha = DateTime.UtcNow, ClienteId = primero.ClienteId, ValidezDias = 15 });
        await db.SaveChangesAsync();

        Assert.Equal(51, (await crear()).Numero);
    }

    [Fact]
    public async Task NumeroConsumidoEnUnaTransaccionDescartada_VuelveAEstarDisponible()
    {
        var db = TestDbFactory.Create();
        var numeracion = new NumeracionService(db);

        await using (await db.Database.BeginTransactionAsync())
        {
            Assert.Equal(1, await numeracion.ProximoNumeroPresupuestoAsync());
        } // sin commit

        Assert.Equal(1, await numeracion.ProximoNumeroPresupuestoAsync());
    }
}
