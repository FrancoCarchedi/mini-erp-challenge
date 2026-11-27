import { Link, useParams } from 'react-router-dom'
import { EstadoBadge } from '../components/EstadoBadge'
import { Cargando, ErrorDeApi } from '../components/Mensajes'
import { TotalesBox } from '../components/TotalesBox'
import { Vencimiento } from '../components/Vencimiento'
import { usePresupuesto } from '../hooks/usePresupuestos'
import { formatearFecha, formatearMoneda, formatearPorcentaje } from '../lib/format'

export default function PresupuestoDetalle() {
  const id = Number(useParams().id)
  const { data: presupuesto, isPending, error } = usePresupuesto(id)

  return (
    <section className="space-y-4">
      <Link className="text-sm text-blue-700 hover:underline" to="/">
        ← Volver a presupuestos
      </Link>

      {error ? <ErrorDeApi titulo="No se pudo cargar el presupuesto" error={error} /> : null}

      {isPending ? (
        <Cargando />
      ) : presupuesto === undefined ? null : (
        <>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold">Presupuesto N° {presupuesto.numero}</h1>
            <EstadoBadge estado={presupuesto.estado} />
          </div>

          <p className="text-sm text-slate-600">
            {presupuesto.clienteRazonSocial} · Fecha {formatearFecha(presupuesto.fecha)} · Vence{' '}
            <Vencimiento presupuesto={presupuesto} />
          </p>

          <div className="tarjeta overflow-x-auto">
            <table className="tabla">
              <thead>
                <tr>
                  <th>Artículo</th>
                  <th className="num">Cantidad</th>
                  <th className="num">Precio unitario</th>
                  <th className="num">Descuento</th>
                  <th className="num">IVA</th>
                  <th className="num">Subtotal</th>
                </tr>
              </thead>
              <tbody>
                {presupuesto.items.map((item, indice) => (
                  // La lista es de solo lectura y un artículo puede repetirse.
                  <tr key={`${item.articuloId}-${indice}`}>
                    <td>
                      <span className="font-mono text-xs text-slate-500">{item.articuloCodigo}</span>{' '}
                      {item.articuloDescripcion}
                    </td>
                    <td className="num">{item.cantidad}</td>
                    <td className="num">{formatearMoneda(item.precioUnitario)}</td>
                    <td className="num">{formatearPorcentaje(item.descuentoPct)}</td>
                    <td className="num">{formatearPorcentaje(item.alicuotaIva)}</td>
                    <td className="num">{formatearMoneda(item.subtotalLinea)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Totales calculados por el backend: son los que valen. */}
          <TotalesBox subtotal={presupuesto.subtotal} iva={presupuesto.iva} total={presupuesto.total} />
        </>
      )}
    </section>
  )
}
