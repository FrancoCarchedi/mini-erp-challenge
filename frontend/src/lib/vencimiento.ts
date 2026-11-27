/**
 * El vencimiento es un día calendario de Argentina (yyyy-MM-dd). No es un
 * instante: nunca se convierte a la zona horaria del navegador.
 */
import { diaEnArgentina } from './format'

export function sumarDias(dia: string, dias: number): string {
  const [anio, mes, diaDelMes] = dia.split('-').map(Number)
  // Se opera en UTC, donde todos los días duran lo mismo.
  return new Date(Date.UTC(anio, mes - 1, diaDelMes + dias)).toISOString().slice(0, 10)
}

/** El mismo criterio que el backend: el día del vencimiento todavía es válido. */
export function estaVencido(fechaVencimiento: string, hoy = diaEnArgentina(new Date())): boolean {
  // En formato yyyy-MM-dd el orden alfabético es el cronológico.
  return hoy > fechaVencimiento
}
