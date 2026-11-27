import { useQuery } from '@tanstack/react-query'
import { listarPresupuestos, obtenerPresupuesto } from '../api/presupuestos'
import { queryKeys } from './queryKeys'

export function usePresupuestos() {
  return useQuery({
    queryKey: queryKeys.presupuestos,
    queryFn: ({ signal }) => listarPresupuestos(signal),
  })
}

export function usePresupuesto(id: number) {
  return useQuery({
    queryKey: [...queryKeys.presupuestos, id],
    queryFn: ({ signal }) => obtenerPresupuesto(id, signal),
  })
}
