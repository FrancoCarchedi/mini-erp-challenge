import { useMutation } from '@tanstack/react-query'
import { crearPresupuesto } from '../api/presupuestos'
import { useInvalidarDatos } from './useInvalidarDatos'

export function useCrearPresupuesto() {
  const invalidar = useInvalidarDatos()
  return useMutation({ mutationFn: crearPresupuesto, onSuccess: invalidar })
}
