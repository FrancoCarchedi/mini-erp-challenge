import { useQuery } from '@tanstack/react-query'
import { listarClientes } from '../api/clientes'
import { queryKeys } from './queryKeys'

export function useClientes() {
  return useQuery({ queryKey: queryKeys.clientes, queryFn: ({ signal }) => listarClientes(signal) })
}
