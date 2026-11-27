import type { Articulo, CrearPresupuestoRequest } from '../types/api'
import type { LineaCalculo } from './totales'

// Los mismos límites que el backend (ReglasPresupuesto.cs).
export const CANTIDAD_MAXIMA = 1_000_000
export const VALIDEZ_MAXIMA_DIAS = 3650

/** Los campos se guardan como texto para no pisar lo que el usuario está escribiendo. */
export interface LineaForm {
  articulo: Articulo
  cantidad: string
  descuento: string
}

const ENTERO = /^\d+$/
const DECIMAL_DE_HASTA_DOS = /^\d+([.,]\d{1,2})?$/

export function leerEntero(texto: string, maximo: number): number | null {
  const limpio = texto.trim()
  if (!ENTERO.test(limpio)) return null
  const valor = Number(limpio)
  return valor >= 1 && valor <= maximo ? valor : null
}

/** Admite coma o punto decimal. Vacío equivale a sin descuento. */
export function leerDescuento(texto: string): number | null {
  const limpio = texto.trim()
  if (limpio === '') return 0
  if (!DECIMAL_DE_HASTA_DOS.test(limpio)) return null
  const valor = Number(limpio.replace(',', '.'))
  return valor <= 100 ? valor : null
}

/** null si la línea tiene algún dato inválido. */
export function interpretarLinea({ articulo, cantidad, descuento }: LineaForm): LineaCalculo | null {
  const cantidadLeida = leerEntero(cantidad, CANTIDAD_MAXIMA)
  const descuentoLeido = leerDescuento(descuento)
  if (cantidadLeida === null || descuentoLeido === null) return null

  return {
    cantidad: cantidadLeida,
    descuentoPct: descuentoLeido,
    precioUnitario: articulo.precioUnitario,
    alicuotaIva: articulo.alicuotaIva,
  }
}

/** Si el artículo ya está en el presupuesto, suma una unidad en lugar de duplicar la línea. */
export function agregarArticulo(lineas: LineaForm[], articulo: Articulo): LineaForm[] {
  if (!lineas.some((linea) => linea.articulo.id === articulo.id)) {
    return [...lineas, { articulo, cantidad: '1', descuento: '0' }]
  }

  return lineas.map((linea) => {
    if (linea.articulo.id !== articulo.id) return linea
    const actual = leerEntero(linea.cantidad, CANTIDAD_MAXIMA) ?? 0
    return { ...linea, cantidad: String(Math.min(actual + 1, CANTIDAD_MAXIMA)) }
  })
}

type Resultado = { ok: true; request: CrearPresupuestoRequest } | { ok: false; errores: string[] }

export function armarRequest(clienteId: string, validez: string, lineas: LineaForm[]): Resultado {
  const errores: string[] = []

  const cliente = Number(clienteId)
  if (clienteId === '' || !Number.isInteger(cliente)) errores.push('Elegí un cliente.')

  const validezDias = leerEntero(validez, VALIDEZ_MAXIMA_DIAS)
  if (validezDias === null) errores.push(`La validez debe estar entre 1 y ${VALIDEZ_MAXIMA_DIAS} días.`)

  if (lineas.length === 0) errores.push('Agregá al menos un artículo.')

  const items: CrearPresupuestoRequest['items'] = []
  for (const linea of lineas) {
    const leida = interpretarLinea(linea)
    if (leida === null) {
      errores.push(`Revisá la cantidad y el descuento de ${linea.articulo.codigo}.`)
    } else {
      items.push({ articuloId: linea.articulo.id, cantidad: leida.cantidad, descuentoPct: leida.descuentoPct })
    }
  }

  if (errores.length > 0 || validezDias === null) return { ok: false, errores }
  return { ok: true, request: { clienteId: cliente, validezDias, items } }
}
