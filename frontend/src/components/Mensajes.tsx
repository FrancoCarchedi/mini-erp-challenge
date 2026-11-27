import type { ReactNode } from 'react'
import { ApiError } from '../api/errores'

const ESTILOS = {
  error: 'border-red-200 bg-red-50 text-red-800',
  exito: 'border-green-200 bg-green-50 text-green-800',
  aviso: 'border-amber-200 bg-amber-50 text-amber-800',
}

export function Alerta({ tipo, children }: { tipo: keyof typeof ESTILOS; children: ReactNode }) {
  return (
    <div role={tipo === 'error' ? 'alert' : 'status'} className={`rounded-md border px-4 py-3 text-sm ${ESTILOS[tipo]}`}>
      {children}
    </div>
  )
}

export function ErrorDeApi({ error, titulo }: { error: unknown; titulo: string }) {
  const mensajes = error instanceof ApiError ? error.mensajes : ['Ocurrió un error inesperado.']

  return (
    <Alerta tipo="error">
      <p className="font-semibold">{titulo}</p>
      {mensajes.map((mensaje) => (
        <p key={mensaje}>{mensaje}</p>
      ))}
    </Alerta>
  )
}

export function Cargando() {
  return <p className="py-6 text-center text-sm text-slate-500">Cargando…</p>
}
