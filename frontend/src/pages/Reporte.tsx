import { useState, type FormEvent } from 'react'
import type { FiltroTopArticulos } from '../api/reportes'
import { Cargando, ErrorDeApi } from '../components/Mensajes'
import { useTopArticulos } from '../hooks/useTopArticulos'
import { diaEnArgentina, formatearMoneda } from '../lib/format'

function filtroInicial(): FiltroTopArticulos {
  const hoy = diaEnArgentina(new Date())
  return { desde: `${hoy.slice(0, 8)}01`, hasta: hoy, top: '5' }
}

export default function Reporte() {
  // `borrador` es lo que se está editando; `filtro`, lo que se consultó.
  const [filtro, setFiltro] = useState(filtroInicial)
  const [borrador, setBorrador] = useState(filtro)
  const { data: articulos, isPending, error } = useTopArticulos(filtro)
  const incompleto = borrador.desde === '' || borrador.hasta === '' || borrador.top === ''

  const editar = (cambios: Partial<FiltroTopArticulos>) => setBorrador((actual) => ({ ...actual, ...cambios }))

  function handleSubmit(evento: FormEvent) {
    evento.preventDefault()
    setFiltro(borrador)
  }

  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-bold">Artículos con más monto facturado</h1>

      <form className="tarjeta flex flex-wrap items-end gap-4 p-4" onSubmit={handleSubmit} noValidate>
        <label className="text-sm font-medium">
          Desde
          <input
            type="date"
            className="campo mt-1 block"
            value={borrador.desde}
            onChange={(e) => editar({ desde: e.target.value })}
          />
        </label>
        <label className="text-sm font-medium">
          Hasta
          <input
            type="date"
            className="campo mt-1 block"
            value={borrador.hasta}
            onChange={(e) => editar({ hasta: e.target.value })}
          />
        </label>
        <label className="text-sm font-medium">
          Cantidad de artículos
          <input
            type="number"
            className="campo mt-1 block w-32"
            min={1}
            max={100}
            value={borrador.top}
            onChange={(e) => editar({ top: e.target.value })}
          />
        </label>
        <button type="submit" className="btn btn-primario" disabled={incompleto}>
          Consultar
        </button>
      </form>

      <p className="text-sm text-slate-600">
        Fechas según el día en Argentina, ambas incluidas. El monto es el neto sin IVA.
      </p>

      {error ? (
        <ErrorDeApi titulo="No se pudo generar el reporte" error={error} />
      ) : isPending ? (
        <Cargando />
      ) : articulos.length === 0 ? (
        <p className="tarjeta p-6 text-center text-sm text-slate-500">No hay facturas en ese período.</p>
      ) : (
        <div className="tarjeta overflow-x-auto">
          <table className="tabla">
            <thead>
              <tr>
                <th>#</th>
                <th>Código</th>
                <th>Artículo</th>
                <th className="num">Unidades</th>
                <th className="num">Monto neto</th>
                <th className="num">IVA</th>
              </tr>
            </thead>
            <tbody>
              {articulos.map((articulo, indice) => (
                <tr key={articulo.articuloId}>
                  <td>{indice + 1}</td>
                  <td className="font-mono text-xs">{articulo.codigo}</td>
                  <td>{articulo.descripcion}</td>
                  <td className="num">{articulo.cantidad}</td>
                  <td className="num">{formatearMoneda(articulo.monto)}</td>
                  <td className="num">{formatearMoneda(articulo.iva)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
