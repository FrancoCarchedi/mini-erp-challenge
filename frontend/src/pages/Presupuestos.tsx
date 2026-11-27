import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { EstadoBadge } from '../components/EstadoBadge'
import { Alerta, Cargando, ErrorDeApi } from '../components/Mensajes'
import { Vencimiento } from '../components/Vencimiento'
import { useDuplicar } from '../hooks/useDuplicar'
import { useEliminarPresupuesto } from '../hooks/useEliminarPresupuesto'
import { useFacturar } from '../hooks/useFacturar'
import { usePresupuestos } from '../hooks/usePresupuestos'
import { formatearFecha, formatearMoneda } from '../lib/format'
import type { EstadoPresupuesto, Presupuesto } from '../types/api'

type Aviso = { tipo: 'exito'; contenido: ReactNode } | { tipo: 'error'; titulo: string; error: unknown }

const precargarAlta = () => void import('./NuevoPresupuesto')

// Los vencidos no se deshabilitan: el backend explica por qué no se pueden facturar.
const MOTIVO_NO_FACTURABLE: Partial<Record<EstadoPresupuesto, string>> = {
  Facturado: 'Ya fue facturado',
  Rechazado: 'Está rechazado',
}

export function Presupuestos() {
  const { data: presupuestos, isPending, error } = usePresupuestos()
  const eliminar = useEliminarPresupuesto()
  const facturar = useFacturar()
  const [aviso, setAviso] = useState<Aviso | null>(null)
  // Una acción a la vez, hasta que la grilla quede actualizada.
  const duplicar = useDuplicar()
  const ocupado = eliminar.isPending || facturar.isPending || duplicar.isPending

  function handleDuplicar({ id, numero }: Presupuesto) {
    setAviso(null)
    duplicar.mutate(id, {
      onSuccess: (copia) =>
        setAviso({
          tipo: 'exito',
          contenido: (
            <>
              Se creó el presupuesto{' '}
              <Link className="font-semibold underline" to={`/presupuestos/${copia.id}`}>
                N° {copia.numero}
              </Link>{' '}
              como copia del N° {numero}, con los precios actuales.
            </>
          ),
        }),
      onError: (error) =>
        setAviso({ tipo: 'error', titulo: `No se pudo duplicar el presupuesto N° ${numero}`, error }),
    })
  }

  function handleFacturar({ id, numero, total }: Presupuesto) {
    if (!window.confirm(`¿Facturar el presupuesto N° ${numero} por ${formatearMoneda(total)}?`)) return
    setAviso(null)
    facturar.mutate(id, {
      onSuccess: (factura) =>
        setAviso({
          tipo: 'exito',
          contenido: `Se generó la factura N° ${factura.numero} del presupuesto N° ${numero}.`,
        }),
      onError: (error) =>
        setAviso({ tipo: 'error', titulo: `No se pudo facturar el presupuesto N° ${numero}`, error }),
    })
  }

  function handleEliminar({ id, numero }: Presupuesto) {
    if (!window.confirm(`¿Eliminar el presupuesto N° ${numero}?`)) return
    setAviso(null)
    eliminar.mutate(id, {
      onSuccess: () => setAviso({ tipo: 'exito', contenido: `Se eliminó el presupuesto N° ${numero}.` }),
      onError: (error) =>
        setAviso({ tipo: 'error', titulo: `No se pudo eliminar el presupuesto N° ${numero}`, error }),
    })
  }

  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Presupuestos</h1>
        {/* Al apuntar el enlace se empieza a descargar la pantalla de alta. */}
        <Link
          className="btn btn-primario"
          to="/presupuestos/nuevo"
          onMouseEnter={precargarAlta}
          onFocus={precargarAlta}
        >
          Nuevo presupuesto
        </Link>
      </div>

      {aviso === null ? null : aviso.tipo === 'error' ? (
        <ErrorDeApi titulo={aviso.titulo} error={aviso.error} />
      ) : (
        <Alerta tipo="exito">{aviso.contenido}</Alerta>
      )}

      {error ? <ErrorDeApi titulo="No se pudieron cargar los presupuestos" error={error} /> : null}

      {isPending ? (
        <Cargando />
      ) : presupuestos === undefined ? null : presupuestos.length === 0 ? (
        <p className="tarjeta p-6 text-center text-sm text-slate-500">Todavía no hay presupuestos.</p>
      ) : (
        <div className="tarjeta overflow-x-auto">
          <table className="tabla">
            <thead>
              <tr>
                <th>Número</th>
                <th>Fecha</th>
                <th>Cliente</th>
                <th>Estado</th>
                <th>Vence</th>
                <th className="num">Total</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {presupuestos.map((presupuesto) => (
                <tr key={presupuesto.id}>
                  <td className="font-medium">{presupuesto.numero}</td>
                  <td>{formatearFecha(presupuesto.fecha)}</td>
                  <td>{presupuesto.clienteRazonSocial}</td>
                  <td>
                    <EstadoBadge estado={presupuesto.estado} />
                  </td>
                  <td>
                    <Vencimiento presupuesto={presupuesto} />
                  </td>
                  <td className="num">{formatearMoneda(presupuesto.total)}</td>
                  <td>
                    <div className="flex gap-2">
                      <Link className="btn" to={`/presupuestos/${presupuesto.id}`}>
                        Ver
                      </Link>
                      <button
                        type="button"
                        className="btn btn-primario"
                        disabled={ocupado || MOTIVO_NO_FACTURABLE[presupuesto.estado] !== undefined}
                        title={MOTIVO_NO_FACTURABLE[presupuesto.estado]}
                        onClick={() => handleFacturar(presupuesto)}
                      >
                        Facturar
                      </button>
                      <button
                        type="button"
                        className="btn"
                        disabled={ocupado}
                        onClick={() => handleDuplicar(presupuesto)}
                      >
                        Duplicar
                      </button>
                      <button
                        type="button"
                        className="btn btn-peligro"
                        disabled={ocupado || presupuesto.estado === 'Facturado'}
                        title={presupuesto.estado === 'Facturado' ? 'No se puede eliminar: ya fue facturado' : undefined}
                        onClick={() => handleEliminar(presupuesto)}
                      >
                        Eliminar
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
