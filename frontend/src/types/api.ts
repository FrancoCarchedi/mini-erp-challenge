// Espejo de los DTOs del backend (backend/MiniErp.Api/Dtos/Dtos.cs).

export interface Cliente {
  id: number
  razonSocial: string
  cuit: string
  condicionIva: string
}

export interface Articulo {
  id: number
  codigo: string
  descripcion: string
  precioUnitario: number
  stockActual: number
  alicuotaIva: number
}

export type EstadoPresupuesto = 'Borrador' | 'Aprobado' | 'Rechazado' | 'Facturado'

export interface PresupuestoItem {
  articuloId: number
  articuloCodigo: string
  articuloDescripcion: string
  cantidad: number
  precioUnitario: number
  descuentoPct: number
  alicuotaIva: number
  subtotalLinea: number
}

export interface Presupuesto {
  id: number
  numero: number
  /** ISO 8601 en UTC. */
  fecha: string
  clienteId: number
  clienteRazonSocial: string
  estado: EstadoPresupuesto
  validezDias: number
  items: PresupuestoItem[]
  subtotal: number
  iva: number
  total: number
}

export interface CrearPresupuestoRequest {
  clienteId: number
  validezDias: number
  items: { articuloId: number; cantidad: number; descuentoPct: number }[]
}

export interface Factura {
  id: number
  numero: number
  fecha: string
  presupuestoId: number
  subtotal: number
  iva: number
  total: number
}

export interface TopArticulo {
  articuloId: number
  codigo: string
  descripcion: string
  cantidad: number
  /** Subtotal neto facturado, sin IVA. */
  monto: number
  iva: number
}
