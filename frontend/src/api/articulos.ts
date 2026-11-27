import type { Articulo } from '../types/api'
import { request } from './client'

export const buscarArticulos = (busqueda: string, signal?: AbortSignal) =>
  request<Articulo[]>(`/api/articulos?${new URLSearchParams({ busqueda })}`, { signal })
