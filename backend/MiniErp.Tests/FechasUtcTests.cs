using Microsoft.EntityFrameworkCore;
using MiniErp.Core.Models;
using Xunit;

namespace MiniErp.Tests;

public class FechasUtcTests
{
    private static readonly DateTime Instante = new(2026, 11, 30, 15, 45, 30, DateTimeKind.Utc);

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)] // se asume UTC: no se tiene que correr
    [InlineData(DateTimeKind.Local)]
    public async Task Fecha_AlReleerla_VuelveEnUtcConElMismoInstante(DateTimeKind kind)
    {
        var db = TestDbFactory.Create();
        var guardada = kind == DateTimeKind.Local
            ? Instante.ToLocalTime()
            : DateTime.SpecifyKind(Instante, kind);

        db.Presupuestos.Add(new Presupuesto
        {
            Numero = 1,
            Fecha = guardada,
            ValidezDias = 15,
            Cliente = new Cliente { RazonSocial = "Cliente Test", Cuit = "20-00000000-0" }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear(); // obliga a leer de la base

        var leido = await db.Presupuestos.SingleAsync();

        Assert.Equal(DateTimeKind.Utc, leido.Fecha.Kind);
        Assert.Equal(Instante, leido.Fecha);
    }
}
