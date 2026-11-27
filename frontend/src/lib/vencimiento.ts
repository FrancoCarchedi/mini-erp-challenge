const MS_POR_DIA = 24 * 60 * 60 * 1000

export function fechaDeVencimiento(fecha: string, validezDias: number): Date {
  return new Date(Date.parse(fecha) + validezDias * MS_POR_DIA)
}

/** El mismo criterio que el backend: vencido es haber pasado el instante de vencimiento. */
export function estaVencido(fecha: string, validezDias: number, ahora = Date.now()): boolean {
  return ahora > fechaDeVencimiento(fecha, validezDias).getTime()
}
