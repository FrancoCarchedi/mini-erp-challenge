import { describe, expect, it } from 'vitest'
import { calcularSubtotalLinea, calcularTotales, type LineaCalculo } from './totales'

const linea = (cantidad: number, precioUnitario: number, alicuotaIva = 21, descuentoPct = 0): LineaCalculo => ({
  cantidad,
  precioUnitario,
  alicuotaIva,
  descuentoPct,
})

describe('calcularTotales: los mismos casos que el backend', () => {
  it('todo al 21% sin descuento', () => {
    expect(calcularTotales([linea(10, 100), linea(2, 50)])).toEqual({ subtotal: 1100, iva: 231, total: 1331 })
  })

  it('aplica el descuento antes del IVA', () => {
    expect(calcularTotales([linea(10, 100, 21, 10)])).toEqual({ subtotal: 900, iva: 189, total: 1089 })
  })

  it('calcula el IVA por línea con alícuotas mixtas', () => {
    expect(calcularTotales([linea(10, 100), linea(4, 50, 10.5)])).toEqual({ subtotal: 1200, iva: 231, total: 1431 })
  })

  it('flujo 2 del README: ART-001 + ART-004', () => {
    expect(calcularTotales([linea(1, 35000), linea(1, 4500, 10.5)])).toEqual({
      subtotal: 39500,
      iva: 7822.5,
      total: 47322.5,
    })
  })
})

describe('calcularTotales: redondeo', () => {
  it('redondea el IVA por línea y no sobre el total', () => {
    // 0,105 -> 0,11 tres veces = 0,33; redondeando 0,315 daría 0,32.
    const lineas = [linea(1, 1, 10.5), linea(1, 1, 10.5), linea(1, 1, 10.5)]

    expect(calcularTotales(lineas)).toEqual({ subtotal: 3, iva: 0.33, total: 3.33 })
  })

  it('redondea los medios centavos hacia arriba, no al par', () => {
    expect(calcularSubtotalLinea(linea(1, 0.25, 21, 50))).toBe(0.13)
  })

  it('no arrastra errores de punto flotante en un caso .5', () => {
    // 2,01 × 0,5 = 1,005; con `number`, Math.round(1.005 * 100) / 100 da 1.
    expect(calcularSubtotalLinea(linea(1, 2.01, 21, 50))).toBe(1.01)
  })

  it('calcula el IVA sobre el subtotal ya redondeado', () => {
    // 0,115 -> 0,12; IVA 0,0252 -> 0,03 (sobre 0,115 daría 0,02).
    expect(calcularTotales([linea(1, 0.23, 21, 50)])).toEqual({ subtotal: 0.12, iva: 0.03, total: 0.15 })
  })

  it('suma centavos sin errores de punto flotante', () => {
    expect(calcularTotales([linea(1, 0.1, 0), linea(1, 0.2, 0)]).total).toBe(0.3)
  })

  it('no pierde precisión con las cantidades máximas', () => {
    // El producto intermedio supera Number.MAX_SAFE_INTEGER.
    // El mismo caso está en el backend (CalcularTotalesExtraTests).
    expect(calcularSubtotalLinea(linea(999_999, 220000.01, 21, 33.33))).toBe(146673859992.99)
  })

  it('un descuento del 100% deja todo en cero, y sin líneas también', () => {
    const cero = { subtotal: 0, iva: 0, total: 0 }

    expect(calcularTotales([linea(3, 100, 21, 100)])).toEqual(cero)
    expect(calcularTotales([])).toEqual(cero)
  })
})
