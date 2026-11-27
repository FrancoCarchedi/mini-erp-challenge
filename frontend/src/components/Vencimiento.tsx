import { formatearFecha } from '../lib/format'
import { estaVencido, fechaDeVencimiento } from '../lib/vencimiento'
import type { Presupuesto } from '../types/api'

export function Vencimiento({ presupuesto }: { presupuesto: Presupuesto }) {
  const { fecha, validezDias, estado } = presupuesto
  // En un presupuesto ya facturado el vencimiento dejó de importar.
  const vencido = estado !== 'Facturado' && estaVencido(fecha, validezDias)

  return (
    <span className={vencido ? 'text-red-700' : undefined}>
      {formatearFecha(fechaDeVencimiento(fecha, validezDias))}
      {vencido ? <span className="ml-2 rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium">Vencido</span> : null}
    </span>
  )
}
