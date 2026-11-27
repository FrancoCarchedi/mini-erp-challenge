using Microsoft.EntityFrameworkCore;
using MiniErp.Core.Data;
using MiniErp.Core.Models;
using MiniErp.Core.Services;
using Xunit;

namespace MiniErp.Tests;

public class ValidacionesTests
{
    private static async Task<(PresupuestoService service, AppDbContext db, int clienteId, int articuloId)> ArmarAsync()
    {
        var db = TestDbFactory.Create();
        var (cliente, articulo) = await Datos.SeedAsync(db);
        return (new PresupuestoService(db, new NumeracionService(db)), db, cliente.Id, articulo.Id);
    }

    [Theory]
    [InlineData(0, "0", 15)]
    [InlineData(-3, "0", 15)]
    [InlineData(ReglasPresupuesto.CantidadMaxima + 1, "0", 15)]
    [InlineData(1, "-0.01", 15)]
    [InlineData(1, "100.01", 15)]
    [InlineData(1, "1.005", 15)]
    [InlineData(1, "0", 0)]
    [InlineData(1, "0", ReglasPresupuesto.ValidezMaximaDias + 1)]
    public async Task Crear_ConDatosInvalidos_LanzaYNoGuarda(int cantidad, string descuento, int validezDias)
    {
        var (service, db, clienteId, articuloId) = await ArmarAsync();
        var item = Datos.Item(articuloId, cantidad, decimal.Parse(descuento, System.Globalization.CultureInfo.InvariantCulture));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearAsync(clienteId, validezDias, new List<PresupuestoItem> { item }));

        Assert.Equal(0, await db.Presupuestos.CountAsync());
    }

    [Fact]
    public async Task Crear_SinItems_Lanza()
    {
        var (service, _, clienteId, _) = await ArmarAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearAsync(clienteId, 15, new List<PresupuestoItem>()));
    }

    [Fact]
    public async Task Crear_ConClienteOArticuloInexistente_Lanza()
    {
        var (service, _, clienteId, articuloId) = await ArmarAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearAsync(999, 15, new List<PresupuestoItem> { Datos.Item(articuloId) }));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearAsync(clienteId, 15, new List<PresupuestoItem> { Datos.Item(999) }));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("100")]
    public async Task Crear_ConDescuentoEnElLimite_Guarda(string descuento)
    {
        var (service, _, clienteId, articuloId) = await ArmarAsync();

        var creado = await service.CrearAsync(clienteId, 1,
            new List<PresupuestoItem> { Datos.Item(articuloId, 1, decimal.Parse(descuento)) });

        Assert.True(creado.Id > 0);
    }
}
