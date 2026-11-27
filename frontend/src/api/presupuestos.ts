import type { CrearPresupuestoRequest, Presupuesto } from '../types/api'
import { request } from './client'

export const listarPresupuestos = (signal?: AbortSignal) =>
  request<Presupuesto[]>('/api/presupuestos', { signal })

export const obtenerPresupuesto = (id: number, signal?: AbortSignal) =>
  request<Presupuesto>(`/api/presupuestos/${id}`, { signal })

export const crearPresupuesto = (body: CrearPresupuestoRequest) =>
  request<Presupuesto>('/api/presupuestos', { method: 'POST', body })

export const eliminarPresupuesto = (id: number) =>
  request<void>(`/api/presupuestos/${id}`, { method: 'DELETE' })

export const duplicarPresupuesto = (id: number) =>
  request<Presupuesto>(`/api/presupuestos/${id}/duplicar`, { method: 'POST' })
