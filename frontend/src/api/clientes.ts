import type { Cliente } from '../types/api'
import { request } from './client'

export const listarClientes = (signal?: AbortSignal) => request<Cliente[]>('/api/clientes', { signal })
