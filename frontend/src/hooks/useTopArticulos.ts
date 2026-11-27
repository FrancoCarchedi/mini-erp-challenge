import { useQuery } from '@tanstack/react-query'
import { obtenerTopArticulos, type FiltroTopArticulos } from '../api/reportes'
import { queryKeys } from './queryKeys'

export function useTopArticulos(filtro: FiltroTopArticulos) {
  return useQuery({
    queryKey: [...queryKeys.reportes, 'top-articulos', filtro],
    queryFn: ({ signal }) => obtenerTopArticulos(filtro, signal),
  })
}
