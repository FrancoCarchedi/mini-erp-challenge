export const ZONA_ARGENTINA = 'America/Argentina/Buenos_Aires'

// Los formateadores se crean una sola vez: construirlos es lo costoso.
const moneda = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })
const porcentaje = new Intl.NumberFormat('es-AR', { maximumFractionDigits: 2 })
const fecha = new Intl.DateTimeFormat('es-AR', {
  timeZone: ZONA_ARGENTINA,
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})
// en-CA formatea como yyyy-MM-dd, el formato de <input type="date">.
const fechaIso = new Intl.DateTimeFormat('en-CA', {
  timeZone: ZONA_ARGENTINA,
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})

export const formatearMoneda = (valor: number) => moneda.format(valor)
export const formatearPorcentaje = (valor: number) => `${porcentaje.format(valor)}%`
export const formatearFecha = (valor: string | Date) => fecha.format(new Date(valor))

/**
 * Muestra un día calendario (yyyy-MM-dd) sin pasar por Date: `new Date()` lo
 * tomaría como medianoche UTC, que en Argentina es el día anterior.
 */
export const formatearDia = (dia: string) => dia.split('-').reverse().join('/')

/** Día calendario en Argentina, como yyyy-MM-dd. */
export const diaEnArgentina = (instante: Date) => fechaIso.format(instante)
