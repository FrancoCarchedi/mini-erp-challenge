using Microsoft.EntityFrameworkCore;
using MiniErp.Core.Data;

namespace MiniErp.Core.Services;

/// <summary>
/// Entrega numeros secuenciales. Hay que llamarlo dentro de la misma transaccion
/// que guarda el documento: si el alta falla, el numero no se consume.
/// </summary>
public class NumeracionService
{
    private readonly AppDbContext _db;

    public NumeracionService(AppDbContext db) => _db = db;

    public async Task<int> ProximoNumeroPresupuestoAsync()
    {
        var maximoUsado = await _db.Presupuestos.MaxAsync(p => (int?)p.Numero) ?? 0;
        return await AvanzarAsync("Presupuesto", maximoUsado);
    }

    public async Task<int> ProximoNumeroFacturaAsync()
    {
        var maximoUsado = await _db.Facturas.MaxAsync(f => (int?)f.Numero) ?? 0;
        return await AvanzarAsync("Factura", maximoUsado);
    }

    // Una sola sentencia crea o incrementa la secuencia, sin un "leer y despues
    // escribir" en memoria. El maximo usado cubre documentos que ya existan con un
    // numero mayor al de la secuencia (por ejemplo, insertados por fuera).
    private async Task<int> AvanzarAsync(string nombre, int maximoUsado)
    {
        var valores = await _db.Database
            .SqlQuery<int>($"""
                INSERT INTO Secuencias (Nombre, UltimoValor) VALUES ({nombre}, {maximoUsado} + 1)
                ON CONFLICT (Nombre) DO UPDATE SET UltimoValor = MAX(UltimoValor, {maximoUsado}) + 1
                RETURNING UltimoValor AS Value
                """)
            .ToListAsync();

        return valores.Single();
    }
}
