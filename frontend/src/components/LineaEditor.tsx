import { formatearMoneda, formatearPorcentaje } from '../lib/format'
import { CANTIDAD_MAXIMA, interpretarLinea, leerDescuento, leerEntero, type LineaForm } from '../lib/formulario'
import { calcularSubtotalLinea } from '../lib/totales'

interface Props {
  linea: LineaForm
  onCambiar: (articuloId: number, cambios: Partial<Pick<LineaForm, 'cantidad' | 'descuento'>>) => void
  onQuitar: (articuloId: number) => void
}

export function LineaEditor({ linea, onCambiar, onQuitar }: Props) {
  const { articulo, cantidad, descuento } = linea
  const cantidadLeida = leerEntero(cantidad, CANTIDAD_MAXIMA)
  const descuentoValido = leerDescuento(descuento) !== null
  const leida = interpretarLinea(linea)

  return (
    <tr>
      <td>
        <span className="font-mono text-xs text-slate-500">{articulo.codigo}</span> {articulo.descripcion}
      </td>
      <td>
        <input
          className="campo w-24 text-right"
          inputMode="numeric"
          aria-label={`Cantidad de ${articulo.codigo}`}
          aria-invalid={cantidadLeida === null}
          value={cantidad}
          onChange={(e) => onCambiar(articulo.id, { cantidad: e.target.value })}
        />
        {cantidadLeida === null ? (
          <p className="mt-1 text-xs text-red-700">Entero de 1 a {CANTIDAD_MAXIMA.toLocaleString('es-AR')}.</p>
        ) : cantidadLeida > articulo.stockActual ? (
          // Solo avisa: el stock se valida al facturar.
          <p className="mt-1 text-xs text-amber-700">Supera el stock ({articulo.stockActual}).</p>
        ) : null}
      </td>
      <td className="num">{formatearMoneda(articulo.precioUnitario)}</td>
      <td>
        <input
          className="campo w-20 text-right"
          inputMode="decimal"
          aria-label={`Descuento de ${articulo.codigo}`}
          aria-invalid={!descuentoValido}
          value={descuento}
          onChange={(e) => onCambiar(articulo.id, { descuento: e.target.value })}
        />
        {descuentoValido ? null : <p className="mt-1 text-xs text-red-700">De 0 a 100, hasta 2 decimales.</p>}
      </td>
      <td className="num">{formatearPorcentaje(articulo.alicuotaIva)}</td>
      <td className="num">{leida === null ? '—' : formatearMoneda(calcularSubtotalLinea(leida))}</td>
      <td>
        <button
          type="button"
          className="btn btn-peligro"
          aria-label={`Quitar ${articulo.codigo}`}
          onClick={() => onQuitar(articulo.id)}
        >
          Quitar
        </button>
      </td>
    </tr>
  )
}
