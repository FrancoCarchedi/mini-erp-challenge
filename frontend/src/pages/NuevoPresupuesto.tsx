import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { BuscadorArticulos } from '../components/BuscadorArticulos'
import { LineaEditor } from '../components/LineaEditor'
import { Alerta, ErrorDeApi } from '../components/Mensajes'
import { TotalesBox } from '../components/TotalesBox'
import { useClientes } from '../hooks/useClientes'
import { useCrearPresupuesto } from '../hooks/useCrearPresupuesto'
import {
  agregarArticulo,
  armarRequest,
  interpretarLinea,
  leerEntero,
  VALIDEZ_MAXIMA_DIAS,
  type LineaForm,
} from '../lib/formulario'
import { diaEnArgentina, formatearDia } from '../lib/format'
import { calcularTotales } from '../lib/totales'
import { sumarDias } from '../lib/vencimiento'

export default function NuevoPresupuesto() {
  const navigate = useNavigate()
  const clientes = useClientes()
  const crear = useCrearPresupuesto()

  const [clienteId, setClienteId] = useState('')
  const [validez, setValidez] = useState('15')
  const [lineas, setLineas] = useState<LineaForm[]>([])
  const [intentoEnviar, setIntentoEnviar] = useState(false)
  const [hoy, setHoy] = useState(() => diaEnArgentina(new Date()))

  // Todo lo que sigue se deriva del estado en cada render.
  const validezDias = leerEntero(validez, VALIDEZ_MAXIMA_DIAS)
  const resultado = armarRequest(clienteId, validez, lineas, hoy)
  const lineasValidas = lineas.flatMap((linea) => interpretarLinea(linea) ?? [])
  const totales = calcularTotales(lineasValidas)
  const hayLineasInvalidas = lineasValidas.length < lineas.length

  function cambiarLinea(articuloId: number, cambios: Partial<LineaForm>) {
    setLineas((actuales) =>
      actuales.map((linea) => (linea.articulo.id === articuloId ? { ...linea, ...cambios } : linea)),
    )
  }

  function quitarLinea(articuloId: number) {
    setLineas((actuales) => actuales.filter((linea) => linea.articulo.id !== articuloId))
  }

  function handleSubmit(evento: FormEvent) {
    evento.preventDefault()
    setIntentoEnviar(true)
    // El formulario pudo quedar abierto de un día para otro: se actualiza "hoy" al enviar.
    const diaDeEnvio = diaEnArgentina(new Date())
    setHoy(diaDeEnvio)
    const envio = armarRequest(clienteId, validez, lineas, diaDeEnvio)
    if (!envio.ok) return
    crear.mutate(envio.request, {
      onSuccess: (creado) => navigate(`/presupuestos/${creado.id}`),
    })
  }

  return (
    <section className="space-y-4">
      <Link className="text-sm text-blue-700 hover:underline" to="/">
        ← Volver a presupuestos
      </Link>
      <h1 className="text-2xl font-bold">Nuevo presupuesto</h1>

      {clientes.error ? <ErrorDeApi titulo="No se pudieron cargar los clientes" error={clientes.error} /> : null}

      <form className="space-y-4" onSubmit={handleSubmit} noValidate>
        <div className="tarjeta flex flex-wrap gap-4 p-4">
          <label className="min-w-64 flex-1 text-sm font-medium">
            Cliente
            <select
              className="campo mt-1 block w-full"
              value={clienteId}
              onChange={(e) => setClienteId(e.target.value)}
              disabled={clientes.isPending}
            >
              <option value="">{clientes.isPending ? 'Cargando…' : 'Elegí un cliente'}</option>
              {clientes.data?.map((cliente) => (
                <option key={cliente.id} value={cliente.id}>
                  {cliente.razonSocial} ({cliente.cuit})
                </option>
              ))}
            </select>
          </label>
          <div className="text-sm">
            <label className="font-medium">
              Validez (días)
              <input
                type="number"
                className="campo mt-1 block w-32"
                min={1}
                max={VALIDEZ_MAXIMA_DIAS}
                aria-invalid={validezDias === null}
                aria-describedby="vencimiento-calculado"
                value={validez}
                onChange={(e) => setValidez(e.target.value)}
              />
            </label>
            <p id="vencimiento-calculado" className="mt-1 text-xs text-slate-500">
              {validezDias === null ? 'Ingresá los días.' : `Vence el ${formatearDia(sumarDias(hoy, validezDias))}`}
            </p>
          </div>
        </div>

        <BuscadorArticulos onAgregar={(articulo) => setLineas((actuales) => agregarArticulo(actuales, articulo))} />

        {lineas.length === 0 ? (
          <p className="tarjeta p-6 text-center text-sm text-slate-500">Buscá y agregá artículos al presupuesto.</p>
        ) : (
          <div className="tarjeta overflow-x-auto">
            <table className="tabla">
              <thead>
                <tr>
                  <th>Artículo</th>
                  <th>Cantidad</th>
                  <th className="num">Precio unitario</th>
                  <th>Descuento %</th>
                  <th className="num">IVA</th>
                  <th className="num">Subtotal</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {lineas.map((linea) => (
                  <LineaEditor key={linea.articulo.id} linea={linea} onCambiar={cambiarLinea} onQuitar={quitarLinea} />
                ))}
              </tbody>
            </table>
          </div>
        )}

        {hayLineasInvalidas ? (
          <Alerta tipo="aviso">Los totales no incluyen las líneas con errores.</Alerta>
        ) : null}
        <TotalesBox {...totales} />

        {intentoEnviar && !resultado.ok ? (
          <Alerta tipo="error">
            <ul>
              {resultado.errores.map((mensaje) => (
                <li key={mensaje}>{mensaje}</li>
              ))}
            </ul>
          </Alerta>
        ) : null}
        {crear.error ? <ErrorDeApi titulo="No se pudo crear el presupuesto" error={crear.error} /> : null}

        <div className="flex justify-end">
          <button type="submit" className="btn btn-primario" disabled={crear.isPending}>
            {crear.isPending ? 'Guardando…' : 'Crear presupuesto'}
          </button>
        </div>
      </form>
    </section>
  )
}
