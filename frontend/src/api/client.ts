import { ApiError, mensajesDeError } from './errores'

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

interface Opciones {
  method?: 'GET' | 'POST' | 'DELETE'
  body?: unknown
  signal?: AbortSignal
}

export async function request<T>(path: string, { method = 'GET', body, signal }: Opciones = {}): Promise<T> {
  let respuesta: Response
  try {
    respuesta = await fetch(`${BASE_URL}${path}`, {
      method,
      signal,
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch (error) {
    if (signal?.aborted) throw error
    throw new ApiError(0, [`No se pudo conectar con la API (${BASE_URL}). ¿Está levantada?`])
  }

  if (!respuesta.ok) {
    const cuerpo: unknown = await respuesta.json().catch(() => null)
    throw new ApiError(respuesta.status, mensajesDeError(respuesta.status, cuerpo))
  }

  if (respuesta.status === 204) return undefined as T
  return (await respuesta.json()) as T
}
