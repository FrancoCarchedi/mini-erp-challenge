import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { buscarArticulos } from '../api/articulos'
import { queryKeys } from './queryKeys'

export function useArticulos(busqueda: string) {
  return useQuery({
    queryKey: [...queryKeys.articulos, busqueda],
    queryFn: ({ signal }) => buscarArticulos(busqueda, signal),
    // Mientras llega la búsqueda nueva se sigue viendo la anterior.
    placeholderData: keepPreviousData,
  })
}
