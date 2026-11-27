import { useMutation } from '@tanstack/react-query'
import { duplicarPresupuesto } from '../api/presupuestos'
import { useInvalidarDatos } from './useInvalidarDatos'

export function useDuplicar() {
  const invalidar = useInvalidarDatos()
  return useMutation({ mutationFn: duplicarPresupuesto, onSettled: invalidar })
}
