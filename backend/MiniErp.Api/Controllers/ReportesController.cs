using Microsoft.AspNetCore.Mvc;
using MiniErp.Core.Services;

namespace MiniErp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly ReporteService _reportes;

    public ReportesController(ReporteService reportes) => _reportes = reportes;

    // GET /api/reportes/top-articulos?desde=2026-11-01&hasta=2026-11-30&top=5
    [HttpGet("top-articulos")]
    public async Task<ActionResult<List<TopArticulo>>> TopArticulos(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int top = 5)
    {
        if (desde is null || hasta is null)
            return BadRequest(new { error = "Los parametros 'desde' y 'hasta' son obligatorios (yyyy-MM-dd)." });

        try
        {
            return Ok(await _reportes.TopArticulosAsync(desde.Value, hasta.Value, top));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
