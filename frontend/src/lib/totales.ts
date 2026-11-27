/**
 * Totales en vivo, con la misma regla que el backend (CalculoLinea.cs): subtotal
 * e IVA se redondean a 2 decimales por línea y recién después se suman.
 *
 * Se calcula con enteros (centavos, y centésimos de punto porcentual) porque con
 * `number` 0.115 no es exactamente 0.115 y el redondeo puede salir distinto. Se
 * usa BigInt porque cantidad × precio × descuento supera la precisión de `number`.
 */
export interface LineaCalculo {
  cantidad: number
  precioUnitario: number
  descuentoPct: number
  alicuotaIva: number
}

export interface Totales {
  subtotal: number
  iva: number
  total: number
}

const CIEN_POR_CIENTO = 10_000n // en centésimos de punto porcentual

/** Para valores con hasta 2 decimales (precios, descuentos y alícuotas). */
const aCentesimos = (valor: number) => BigInt(Math.round(valor * 100))

/** División entera con redondeo "half away from zero" (MidpointRounding.AwayFromZero). */
function dividirRedondeando(numerador: bigint, divisor: bigint): bigint {
  const absoluto = numerador < 0n ? -numerador : numerador
  const cociente = (absoluto * 2n + divisor) / (divisor * 2n)
  return numerador < 0n ? -cociente : cociente
}

function calcularLineaEnCentavos({ cantidad, precioUnitario, descuentoPct, alicuotaIva }: LineaCalculo) {
  const bruto = BigInt(cantidad) * aCentesimos(precioUnitario)
  const subtotal = dividirRedondeando(bruto * (CIEN_POR_CIENTO - aCentesimos(descuentoPct)), CIEN_POR_CIENTO)
  const iva = dividirRedondeando(subtotal * aCentesimos(alicuotaIva), CIEN_POR_CIENTO)
  return { subtotal, iva }
}

const aPesos = (centavos: bigint) => Number(centavos) / 100

export function calcularSubtotalLinea(linea: LineaCalculo): number {
  return aPesos(calcularLineaEnCentavos(linea).subtotal)
}

export function calcularTotales(lineas: readonly LineaCalculo[]): Totales {
  let subtotal = 0n
  let iva = 0n
  for (const linea of lineas) {
    const calculada = calcularLineaEnCentavos(linea)
    subtotal += calculada.subtotal
    iva += calculada.iva
  }
  return { subtotal: aPesos(subtotal), iva: aPesos(iva), total: aPesos(subtotal + iva) }
}
