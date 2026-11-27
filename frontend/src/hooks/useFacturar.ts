import { useMutation } from '@tanstack/react-query'
import { facturarPresupuesto } from '../api/facturas'
import { useInvalidarDatos } from './useInvalidarDatos'

export function useFacturar() {
  const invalidar = useInvalidarDatos()
  // También ante un error: "ya facturado" indica que la grilla está desactualizada.
  return useMutation({ mutationFn: facturarPresupuesto, onSettled: invalidar })
}
