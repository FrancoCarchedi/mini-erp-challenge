import type { Factura } from '../types/api'
import { request } from './client'

export const listarFacturas = (signal?: AbortSignal) => request<Factura[]>('/api/facturas', { signal })

export const facturarPresupuesto = (presupuestoId: number) =>
  request<Factura>(`/api/facturas/facturar/${presupuestoId}`, { method: 'POST' })
