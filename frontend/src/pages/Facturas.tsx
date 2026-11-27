import { Link } from 'react-router-dom'
import { Cargando, ErrorDeApi } from '../components/Mensajes'
import { useFacturas } from '../hooks/useFacturas'
import { usePresupuestos } from '../hooks/usePresupuestos'
import { formatearFecha, formatearMoneda } from '../lib/format'

export default function Facturas() {
  // Las dos consultas salen en paralelo.
  const { data: facturas, isPending, error } = useFacturas()
  const { data: presupuestos } = usePresupuestos()
  // La factura solo trae el id del presupuesto: el número sale de la otra lista.
  const numeroDePresupuesto = new Map(presupuestos?.map((p) => [p.id, p.numero]))

  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-bold">Facturas</h1>

      {error ? <ErrorDeApi titulo="No se pudieron cargar las facturas" error={error} /> : null}

      {isPending ? (
        <Cargando />
      ) : facturas === undefined ? null : facturas.length === 0 ? (
        <p className="tarjeta p-6 text-center text-sm text-slate-500">Todavía no hay facturas.</p>
      ) : (
        <div className="tarjeta overflow-x-auto">
          <table className="tabla">
            <thead>
              <tr>
                <th>Número</th>
                <th>Fecha</th>
                <th>Presupuesto</th>
                <th className="num">Subtotal</th>
                <th className="num">IVA</th>
                <th className="num">Total</th>
              </tr>
            </thead>
            <tbody>
              {facturas.map((factura) => (
                <tr key={factura.id}>
                  <td className="font-medium">{factura.numero}</td>
                  <td>{formatearFecha(factura.fecha)}</td>
                  <td>
                    <Link className="text-blue-700 hover:underline" to={`/presupuestos/${factura.presupuestoId}`}>
                      {numeroDePresupuesto.has(factura.presupuestoId)
                        ? `N° ${numeroDePresupuesto.get(factura.presupuestoId)}`
                        : 'Ver presupuesto'}
                    </Link>
                  </td>
                  <td className="num">{formatearMoneda(factura.subtotal)}</td>
                  <td className="num">{formatearMoneda(factura.iva)}</td>
                  <td className="num">{formatearMoneda(factura.total)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
