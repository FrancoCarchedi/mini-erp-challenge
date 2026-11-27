using System.ComponentModel.DataAnnotations;
using MiniErp.Core.Services;

namespace MiniErp.Api.Dtos;

// ---- Requests ----
public record CrearPresupuestoItemDto(
    int ArticuloId,
    [Range(1, ReglasPresupuesto.CantidadMaxima, ErrorMessage = "La cantidad debe estar entre {1} y {2}.")]
    int Cantidad,
    [Range(0d, 100d, ErrorMessage = "El descuento debe estar entre {1} y {2}.")]
    decimal DescuentoPct);

/// <summary>El vencimiento se indica con uno solo de los dos: FechaVencimiento o ValidezDias.</summary>
public record CrearPresupuestoRequest(
    int ClienteId,
    [Range(1, ReglasPresupuesto.ValidezMaximaDias, ErrorMessage = "La validez debe estar entre {1} y {2} dias.")]
    int? ValidezDias,
    DateOnly? FechaVencimiento,
    [Required(ErrorMessage = "El presupuesto necesita al menos un item.")]
    [MinLength(1, ErrorMessage = "El presupuesto necesita al menos un item.")]
    List<CrearPresupuestoItemDto> Items);

// ---- Responses ----
public record ArticuloDto(
    int Id,
    string Codigo,
    string Descripcion,
    decimal PrecioUnitario,
    int StockActual,
    decimal AlicuotaIva);

public record PresupuestoItemDto(
    int ArticuloId,
    string ArticuloCodigo,
    string ArticuloDescripcion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal DescuentoPct,
    decimal AlicuotaIva,
    decimal SubtotalLinea);

public record PresupuestoDto(
    int Id,
    int Numero,
    DateTime Fecha,
    int ClienteId,
    string ClienteRazonSocial,
    string Estado,
    int ValidezDias,
    DateOnly FechaVencimiento,
    List<PresupuestoItemDto> Items,
    decimal Subtotal,
    decimal Iva,
    decimal Total);

public record FacturaDto(
    int Id,
    int Numero,
    DateTime Fecha,
    int PresupuestoId,
    decimal Subtotal,
    decimal Iva,
    decimal Total);

public record ClienteDto(
    int Id,
    string RazonSocial,
    string Cuit,
    string CondicionIva);
