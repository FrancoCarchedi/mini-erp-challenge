import { lazy, Suspense } from 'react'
import { NavLink, Route, Routes } from 'react-router-dom'
import { Cargando } from './components/Mensajes'
import { Presupuestos } from './pages/Presupuestos'

// La grilla es la pantalla de entrada; el resto se descarga cuando se usa.
const PresupuestoDetalle = lazy(() => import('./pages/PresupuestoDetalle'))

const SECCIONES = [{ ruta: '/', titulo: 'Presupuestos' }]

const claseDeEnlace = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-1.5 text-sm font-medium ${isActive ? 'bg-blue-50 text-blue-800' : 'text-slate-600 hover:bg-slate-100'}`

export function App() {
  return (
    <div className="min-h-screen bg-slate-100 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl items-center gap-6 px-4 py-3">
          <span className="text-lg font-bold">Mini ERP</span>
          <nav className="flex gap-1">
            {SECCIONES.map(({ ruta, titulo }) => (
              <NavLink key={ruta} to={ruta} end className={claseDeEnlace}>
                {titulo}
              </NavLink>
            ))}
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6">
        <Suspense fallback={<Cargando />}>
          <Routes>
            <Route path="/" element={<Presupuestos />} />
            <Route path="/presupuestos/:id" element={<PresupuestoDetalle />} />
            <Route path="*" element={<p>La página no existe.</p>} />
          </Routes>
        </Suspense>
      </main>
    </div>
  )
}
