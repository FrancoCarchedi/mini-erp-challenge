import { formatearMoneda } from '../lib/format'

interface Props {
  subtotal: number
  iva: number
  total: number
}

export function TotalesBox({ subtotal, iva, total }: Props) {
  return (
    <dl className="tarjeta ml-auto w-full max-w-xs space-y-1 p-4 text-sm tabular-nums">
      <div className="flex justify-between">
        <dt className="text-slate-600">Subtotal</dt>
        <dd>{formatearMoneda(subtotal)}</dd>
      </div>
      <div className="flex justify-between">
        <dt className="text-slate-600">IVA</dt>
        <dd>{formatearMoneda(iva)}</dd>
      </div>
      <div className="flex justify-between border-t border-slate-200 pt-2 text-base font-bold">
        <dt>Total</dt>
        <dd>{formatearMoneda(total)}</dd>
      </div>
    </dl>
  )
}
