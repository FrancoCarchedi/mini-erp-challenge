import type { EstadoPresupuesto } from '../types/api'

const ESTILOS: Record<EstadoPresupuesto, string> = {
  Borrador: 'bg-slate-100 text-slate-700',
  Aprobado: 'bg-blue-100 text-blue-800',
  Rechazado: 'bg-red-100 text-red-800',
  Facturado: 'bg-green-100 text-green-800',
}

export function EstadoBadge({ estado }: { estado: EstadoPresupuesto }) {
  return <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${ESTILOS[estado]}`}>{estado}</span>
}
