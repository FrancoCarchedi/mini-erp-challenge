using Microsoft.AspNetCore.Mvc;
using MiniErp.Api.Dtos;
using MiniErp.Core.Models;
using MiniErp.Core.Services;

namespace MiniErp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PresupuestosController : ControllerBase
{
    private readonly PresupuestoService _service;

    public PresupuestosController(PresupuestoService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<PresupuestoDto>>> Listar()
    {
        var lista = await _service.ListarAsync();
        return Ok(lista.Select(MapToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PresupuestoDto>> Obtener(int id)
    {
        var presupuesto = await _service.ObtenerAsync(id);
        if (presupuesto is null) return NotFound();
        return Ok(MapToDto(presupuesto));
    }

    [HttpPost]
    public async Task<ActionResult<PresupuestoDto>> Crear([FromBody] CrearPresupuestoRequest request)
    {
        // Los DataAnnotations no cubren un elemento nulo dentro de la lista.
        if (request.Items.Any(i => i is null))
            return BadRequest(new { error = "La lista de items tiene elementos vacios." });

        var items = request.Items.Select(i => new PresupuestoItem
        {
            ArticuloId = i.ArticuloId,
            Cantidad = i.Cantidad,
            DescuentoPct = i.DescuentoPct
        }).ToList();

        try
        {
            var creado = await _service.CrearAsync(request.ClienteId, request.ValidezDias, items);
            var completo = await _service.ObtenerAsync(creado.Id);
            return Ok(MapToDto(completo!));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST /api/presupuestos/5/duplicar
    [HttpPost("{id:int}/duplicar")]
    public async Task<ActionResult<PresupuestoDto>> Duplicar(int id)
    {
        try
        {
            var copia = await _service.DuplicarAsync(id);
            if (copia is null) return NotFound();

            var completo = await _service.ObtenerAsync(copia.Id);
            return Ok(MapToDto(completo!));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            return await _service.EliminarAsync(id) ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private PresupuestoDto MapToDto(Presupuesto p)
    {
        var totales = _service.CalcularTotales(p);

        var items = p.Items.Select(i => new PresupuestoItemDto(
            i.ArticuloId,
            i.Articulo?.Codigo ?? "",
            i.Articulo?.Descripcion ?? "",
            i.Cantidad,
            i.PrecioUnitario,
            i.DescuentoPct,
            i.AlicuotaIva,
            CalculoLinea.Subtotal(i)
        )).ToList();

        return new PresupuestoDto(
            p.Id,
            p.Numero,
            p.Fecha,
            p.ClienteId,
            p.Cliente?.RazonSocial ?? "",
            p.Estado.ToString(),
            p.ValidezDias,
            items,
            totales.Subtotal,
            totales.Iva,
            totales.Total
        );
    }
}
