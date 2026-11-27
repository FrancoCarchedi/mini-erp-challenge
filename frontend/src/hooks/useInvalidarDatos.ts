import { useQueryClient } from '@tanstack/react-query'
import { queryKeys } from './queryKeys'

/** Refresca todo lo que puede cambiar al modificar presupuestos (incluido el stock). */
export function useInvalidarDatos() {
  const queryClient = useQueryClient()

  return () =>
    Promise.all(
      [queryKeys.presupuestos, queryKeys.facturas, queryKeys.articulos, queryKeys.reportes].map((queryKey) =>
        queryClient.invalidateQueries({ queryKey }),
      ),
    )
}
