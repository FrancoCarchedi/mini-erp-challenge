export class ApiError extends Error {
  readonly status: number
  /** Un mensaje por cada problema informado por la API. */
  readonly mensajes: string[]

  constructor(status: number, mensajes: string[]) {
    super(mensajes.join(' '))
    this.name = 'ApiError'
    this.status = status
    this.mensajes = mensajes
  }
}

/**
 * La API responde errores en dos formatos: `{ error }` (reglas de negocio) y
 * ValidationProblem `{ errors: { campo: string[] } }` (validacion de los DTOs).
 */
export function mensajesDeError(status: number, cuerpo: unknown): string[] {
  if (typeof cuerpo === 'object' && cuerpo !== null) {
    if ('error' in cuerpo && typeof cuerpo.error === 'string') return [cuerpo.error]

    if ('errors' in cuerpo && typeof cuerpo.errors === 'object' && cuerpo.errors !== null) {
      const mensajes = Object.values(cuerpo.errors).flat().map(String)
      if (mensajes.length > 0) return [...new Set(mensajes)]
    }
  }
  if (status === 404) return ['No se encontró el recurso solicitado.']
  return [`La API respondió con un error (HTTP ${status}).`]
}
