import { useState } from 'react'
import { useArticulos } from '../hooks/useArticulos'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { formatearMoneda, formatearPorcentaje } from '../lib/format'
import type { Articulo } from '../types/api'
import { Cargando, ErrorDeApi } from './Mensajes'

export function BuscadorArticulos({ onAgregar }: { onAgregar: (articulo: Articulo) => void }) {
  const [texto, setTexto] = useState('')
  const busqueda = useDebouncedValue(texto.trim(), 300)
  const { data: articulos, isPending, error } = useArticulos(busqueda)

  return (
    <div className="space-y-2">
      <label className="block text-sm font-medium">
        Buscar artículos
        <input
          type="search"
          className="campo mt-1 block w-full"
          placeholder="Código o descripción"
          value={texto}
          onChange={(e) => setTexto(e.target.value)}
          // Enter en el buscador no tiene que enviar el presupuesto.
          onKeyDown={(e) => {
            if (e.key === 'Enter') e.preventDefault()
          }}
        />
      </label>

      {error ? <ErrorDeApi titulo="No se pudieron buscar los artículos" error={error} /> : null}

      {isPending ? (
        <Cargando />
      ) : articulos === undefined ? null : articulos.length === 0 ? (
        <p className="text-sm text-slate-500">No hay artículos que coincidan.</p>
      ) : (
        <ul className="tarjeta max-h-64 divide-y divide-slate-100 overflow-y-auto">
          {articulos.map((articulo) => (
            <li key={articulo.id} className="flex items-center justify-between gap-3 px-3 py-2 text-sm">
              <div>
                <p>
                  <span className="font-mono text-xs text-slate-500">{articulo.codigo}</span> {articulo.descripcion}
                </p>
                <p className="text-xs text-slate-500">
                  {formatearMoneda(articulo.precioUnitario)} · IVA {formatearPorcentaje(articulo.alicuotaIva)} · Stock{' '}
                  {articulo.stockActual}
                </p>
              </div>
              <button
                type="button"
                className="btn"
                aria-label={`Agregar ${articulo.codigo}`}
                onClick={() => onAgregar(articulo)}
              >
                Agregar
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
