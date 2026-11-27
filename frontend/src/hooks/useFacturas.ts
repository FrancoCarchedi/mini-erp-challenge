import { useQuery } from '@tanstack/react-query'
import { listarFacturas } from '../api/facturas'
import { queryKeys } from './queryKeys'

export function useFacturas() {
  return useQuery({ queryKey: queryKeys.facturas, queryFn: ({ signal }) => listarFacturas(signal) })
}
