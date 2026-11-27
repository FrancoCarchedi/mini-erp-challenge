import { useMutation } from '@tanstack/react-query'
import { eliminarPresupuesto } from '../api/presupuestos'
import { useInvalidarDatos } from './useInvalidarDatos'

export function useEliminarPresupuesto() {
  const invalidar = useInvalidarDatos()
  // onSettled: si la API rechaza, la grilla probablemente estaba desactualizada.
  return useMutation({ mutationFn: eliminarPresupuesto, onSettled: invalidar })
}
