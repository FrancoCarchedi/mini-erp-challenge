import type { TopArticulo } from '../types/api'
import { request } from './client'

export interface FiltroTopArticulos {
  /** Días de Argentina (yyyy-MM-dd), tal como los entrega el input de fecha. */
  desde: string
  hasta: string
  top: string
}

export const obtenerTopArticulos = (filtro: FiltroTopArticulos, signal?: AbortSignal) =>
  request<TopArticulo[]>(`/api/reportes/top-articulos?${new URLSearchParams({ ...filtro })}`, { signal })
