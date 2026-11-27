import { describe, expect, it } from 'vitest'
import type { Articulo } from '../types/api'
import { agregarArticulo, armarRequest, CANTIDAD_MAXIMA, leerDescuento, leerEntero } from './formulario'

const articulo: Articulo = {
  id: 1,
  codigo: 'ART-001',
  descripcion: 'Teclado',
  precioUnitario: 35000,
  stockActual: 50,
  alicuotaIva: 21,
}

const linea = (cantidad = '1', descuento = '0') => ({ articulo, cantidad, descuento })

describe('leerEntero', () => {
  it.each(['', '0', '-1', '1.5', '1e3', 'abc', String(CANTIDAD_MAXIMA + 1)])('rechaza "%s"', (texto) => {
    expect(leerEntero(texto, CANTIDAD_MAXIMA)).toBeNull()
  })

  it('acepta enteros dentro del rango', () => {
    expect(leerEntero(' 12 ', CANTIDAD_MAXIMA)).toBe(12)
  })
})

describe('leerDescuento', () => {
  it.each(['-1', '100.01', '1.005', 'abc', '1,2,3'])('rechaza "%s"', (texto) => {
    expect(leerDescuento(texto)).toBeNull()
  })

  it.each([
    ['', 0],
    ['100', 100],
    ['5,5', 5.5],
    ['12.25', 12.25],
  ])('lee "%s" como %s', (texto, esperado) => {
    expect(leerDescuento(texto)).toBe(esperado)
  })
})

describe('agregarArticulo', () => {
  it('agrega una línea nueva con cantidad 1', () => {
    expect(agregarArticulo([], articulo)).toEqual([linea()])
  })

  it('si el artículo ya está, suma cantidad y conserva el descuento', () => {
    expect(agregarArticulo([linea('2', '10')], articulo)).toEqual([linea('3', '10')])
  })
})

describe('armarRequest', () => {
  it('arma el body del POST', () => {
    expect(armarRequest('1', '15', [linea('2', '5,5')])).toEqual({
      ok: true,
      request: { clienteId: 1, validezDias: 15, items: [{ articuloId: 1, cantidad: 2, descuentoPct: 5.5 }] },
    })
  })

  it.each([
    ['sin cliente', '', '15', [linea()]],
    ['validez 0', '1', '0', [linea()]],
    ['sin líneas', '1', '15', []],
    ['cantidad 0', '1', '15', [linea('0')]],
    ['descuento 150', '1', '15', [linea('1', '150')]],
  ])('rechaza: %s', (_caso, cliente, validez, lineas) => {
    const resultado = armarRequest(cliente, validez, lineas)

    expect(resultado.ok).toBe(false)
    expect(!resultado.ok && resultado.errores).toHaveLength(1)
  })
})
