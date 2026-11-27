# Plan de acción — Challenge Mini ERP

> **Este es el plan inicial**, escrito antes de implementar, y se conserva como registro del
> proceso. Hubo cambios posteriores que no refleja, como la baja lógica al eliminar
> presupuestos y la columna `FechaVencimiento`. **El estado final está documentado en
> [`NOTES.md`](NOTES.md)**; si difieren, vale `NOTES.md`.

## Contexto

Backend heredado en .NET 8 + EF Core + SQLite (presupuestos, facturación, artículos, stock), con
bugs plantados y sin cliente. Objetivos:

- **Parte A:** encontrar, arreglar y documentar los bugs.
- **Parte B:** construir el cliente React + TypeScript.
- **Parte C:** completar los stretch (duplicar, reporte, tests propios).

Reglas de entrega: los tests existentes **no se modifican** y tienen que quedar en verde. Se
entrega un PR a `main` con commits frecuentes y el `NOTES.md` completo.

### Decisiones tomadas

| Tema | Decisión |
|---|---|
| Cliente | Vite + React + TS + TanStack Query + Tailwind + React Router |
| Skill de React | `react-best-practices` (`vercel-labs/agent-skills`) en todo el desarrollo del cliente |
| Numeración | Tabla de secuencia: un número nunca se reutiliza, aunque se borre |
| Git | Rama `challenge/solucion`, un commit por bug o feature, PR final a `main` |
| Tests propios | Backend (xUnit, en archivos nuevos) + front (Vitest para los totales) |

**Entorno verificado:** SDK .NET 11 con runtime 8.0.34 instalado y Node 24.

> La lista de bugs sale de **leer el código**; todavía no se corrieron los tests ni la API.
> Cada uno se confirma (reproducirlo) antes de arreglarlo.

---

## Orden de ejecución

1. Paso 0: preparación.
2. Bugs que tienen test: 1 y 4.
3. Bugs sin test: 2, 3, 5, 6 y 7.
4. Cliente React: instalar la skill, scaffold y funciones obligatorias (grilla, alta con totales, facturar).
5. Duplicar (endpoint + UI).
6. Reporte (endpoint + UI).
7. Tests propios.
8. `NOTES.md` final.
9. Verificación end-to-end y PR.

Si falta tiempo, se recorta en este orden: UI del reporte, tests extra, bug 7.

---

## Paso 0 — Preparación

- [x] `git checkout -b challenge/solucion`
- [x] `cd backend && dotnet test` y anotar la línea base. Se espera que fallen: alícuotas mixtas, sin stock y doble facturación.
- [x] Commit: `docs: plan de acción`

---

## Parte A — Bugs del backend

Para cada bug: reproducir → arreglar → verificar → anotar en `NOTES.md` (qué pasaba, por qué,
cómo se resolvió, cómo se verificó) → commit `fix: …`.

### [x] Bug 1 — IVA fijo al 21% (con test)

- **Dónde:** `backend/MiniErp.Core/Services/PresupuestoService.cs`, `CalcularTotales`, que hace `subtotal * 0.21m`.
- **Fix:** calcular por línea y después sumar:
  - `subLinea = Round2(cantidad × precio × (1 − descuento/100))`
  - `ivaLinea = Round2(subLinea × alicuota/100)`
- **Redondeo:** `Math.Round(x, 2, MidpointRounding.AwayFromZero)`. El default de .NET es *banker's rounding*, que podría no coincidir con el front.
- **Reutilización:** extraer un helper estático de cálculo de línea y usarlo también en `PresupuestosController.MapToDto`, que hoy recalcula `SubtotalLinea` por su cuenta y sin redondear.
- **Verificar:** `CalcularTotales_ConAlicuotasMixtas_CalculaIvaPorLinea` y flujo 2 del README (39.500 / 7.822,50 / 47.322,50).

### [x] Bug 4 — Facturación sin control de stock ni idempotencia (con test)

- **Dónde:** `backend/MiniErp.Core/Services/FacturacionService.cs`. Descuenta stock sin validar y no controla si ya se facturó.
- **Fix**, en este orden y validando todo **antes de modificar datos**:
  1. Rechazar si el estado es `Facturado` o ya existe una factura para ese presupuesto. Rechazar también `Rechazado`.
  2. Validar el vencimiento (ya existe).
  3. Agrupar los ítems por `ArticuloId` (un artículo puede venir en dos líneas) y validar el stock de la cantidad total. El error indica qué artículos no alcanzan.
  4. Descontar stock, crear la factura y marcar `Facturado`, todo en una transacción.
- **Cuidado:** `Borrador` tiene que poder facturarse, porque todo presupuesto nuevo nace así y no hay flujo de aprobación. `Aprobado` también (lo usan los tests).
- **Verificar:** `Facturar_SinStockSuficiente_…`, `Facturar_DosVeces_…` y flujo 5 del README.

### [x] Bug 2 — La lista no muestra los presupuestos nuevos (sin test)

- **Por qué:** `PresupuestoService.ListarAsync` filtra `Estado != Borrador` y todo presupuesto se crea como `Borrador`.
- **Fix:** quitar el filtro.
- **Verificar:** flujo 1 del README.

### [x] Bug 3 — La numeración colisiona después de borrar (sin test)

- **Por qué:** `NumeracionService.ProximoNumeroPresupuestoAsync` usa `Count() + 1`. Ejemplo: se crean el 1 y el 2, se borra el 1 y el siguiente vuelve a ser 2, que ya existe.
- **Fix:**
  - Nueva entidad `Secuencia { Nombre (PK), UltimoValor }` en `AppDbContext`.
  - Se obtiene o crea la secuencia, inicializándola con el `Max(Numero)` existente (los tests insertan presupuestos con número directo).
  - El incremento y el alta van en la misma transacción.
  - Aplicarlo también a las facturas.
- **Concurrencia:** el incremento no debe ser un "leer y después escribir" sin protección. Usar un `UPDATE` atómico dentro de la transacción o un token de concurrencia.
- **Defensa en la base:** índices **UNIQUE** en `Presupuesto.Numero`, `Factura.Numero` y `Factura.PresupuestoId` (este último refuerza la idempotencia).
- **Importante:** como se usa `EnsureCreated`, hay que borrar el `minierp.db` local para que se genere el esquema nuevo. Documentarlo en `NOTES.md`.
- **Verificar:** flujo 3 del README.

### [x] Bug 5 — No se validan los datos de entrada (sin test)

- **Por qué:** `POST /api/presupuestos` no controla cantidad ≤ 0, descuento fuera de rango, lista de ítems vacía, validez ≤ 0 ni cliente inexistente.
- **Fix en dos capas:**
  - **DTOs** (`backend/MiniErp.Api/Dtos/Dtos.cs`): `Cantidad ≥ 1`, `DescuentoPct` entre 0 y 100, `ValidezDias ≥ 1`, al menos un ítem. `[ApiController]` responde 400 solo.
  - **Servicio** (`CrearAsync`): las mismas reglas con `InvalidOperationException`, más la existencia del cliente.
- **Verificar:** flujo 4 del README.

### [x] Bug 6 — Borrar un presupuesto facturado borraría su factura (sin test, de datos)

- **Por qué:** `Factura.PresupuestoId` es una FK requerida, así que EF aplica borrado en cascada por defecto. Se perdería la factura con el stock ya descontado.
- **Fix:**
  - `EliminarAsync` rechaza los facturados y el controller responde con `{ error }`.
  - `OnDelete(DeleteBehavior.Restrict)` en la relación.
  - 404 si el presupuesto no existe.
- **Verificar:** facturar, intentar borrar y comprobar que la factura sigue en `GET /api/facturas`.

### [x] Bug 7 — Fechas sin zona horaria (menor, a confirmar)

- **Por qué:** SQLite devuelve los `DateTime` con `Kind=Unspecified`. El JSON saldría sin `Z` y el navegador lo tomaría como hora local (3 h de corrimiento).
- **Condición previa (revisada en el código):** el fix solo es válido si los datos se guardaron en UTC. Los únicos campos persistidos son `Presupuesto.Fecha` y `Factura.Fecha`, y la API los asigna con `DateTime.UtcNow` (`PresupuestoService.CrearAsync` y `FacturacionService.FacturarAsync`). Los requests no reciben fechas. No cubre una base editada a mano.
- **Fix en el mapeo del ORM**, que es donde se pierde el `Kind`:
  - Un `UtcDateTimeConverter` en `backend/MiniErp.Core/Data/`, aplicado como **convención global** en `AppDbContext.ConfigureConventions` (`Properties<DateTime>().HaveConversion<UtcDateTimeConverter>()`), para cubrir también las fechas futuras.
  - **Al escribir:** un valor `Local` se convierte con `ToUniversalTime()`. `Utc` y `Unspecified` se guardan sin convertir (`Unspecified` se asume UTC; convertirlo lo correría).
  - **Al leer:** `DateTime.SpecifyKind(valor, DateTimeKind.Utc)`.
- **Alternativa descartada:** un conversor en la serialización JSON solo corrige la salida; la lógica del backend seguiría recibiendo `Unspecified`. Hacer las dos cosas sería redundante.
- **Reproducir antes del fix:** se espera que la respuesta del `POST` salga con `Z` (si el contexto devuelve la entidad que tiene en seguimiento) y que un `GET` posterior salga sin ella.
- **Verificar:**
  - Las fechas de `GET /api/presupuestos` y `GET /api/facturas` terminan en `Z`.
  - Test en un archivo nuevo (`FechasUtcTests`): guardar, limpiar el `ChangeTracker`, releer y comprobar `Kind=Utc` con el mismo instante.

### [x] Cierre de la Parte A

- [x] `dotnet test` en verde.
- [x] Recorrer los 5 flujos del README en Swagger. *(Hecho por HTTP con un script, no desde la UI de Swagger.)*

---

## Parte B — Cliente React (`frontend/`)

### [x] Setup

- [x] **Primero:** instalar las skills de Vercel con `npx skills add vercel-labs/agent-skills` (incluye `react-best-practices`). El comando puede pedir elegir opciones en forma interactiva; en ese caso, correrlo desde la terminal (`! npx skills add vercel-labs/agent-skills`).
- [x] Verificar que la skill quedó instalada en la carpeta de skills del proyecto. Si no aparece en la lista, recargar la sesión.
- [x] Commit: `chore: agregar skills de Vercel (react-best-practices)`.
- [x] **Invocar `react-best-practices` antes de escribir código React y seguirla en todo el cliente** (componentes, hooks, data fetching, rendimiento). Si algo choca con este plan, gana la skill y se anota el ajuste en `NOTES.md`.
- [x] `npm create vite@latest frontend -- --template react-ts`
- [x] Dependencias: `@tanstack/react-query`, `react-router-dom`, `tailwindcss` con su plugin de Vite (v4). De desarrollo: `vitest`.
- [x] `.env` y `.env.example` con `VITE_API_URL=http://localhost:5080`. La API ya permite CORS desde el puerto 5173.

### Estructura

```
frontend/src/
  api/         client.ts (fetch; convierte { error } y ValidationProblem en ApiError) + un archivo por recurso
  types/       DTOs espejo del backend
  hooks/       useClientes, useArticulos, usePresupuestos, useCrearPresupuesto, useFacturar, useDuplicar…
  lib/         totales.ts + totales.test.ts, format.ts (moneda es-AR, fechas)
  components/  Tabla, mensajes de error, BuscadorArticulos, LineaEditor, TotalesBox, EstadoBadge
  pages/       Presupuestos, NuevoPresupuesto, PresupuestoDetalle, Facturas, Reporte
```

### [x] Totales en vivo (`lib/totales.ts`)

- La misma regla que el backend, en **centavos enteros** para evitar errores de punto flotante:
  redondeo del subtotal por línea, redondeo del IVA por línea y después la suma.
- Redondeo *half away from zero*, igual que `MidpointRounding.AwayFromZero`.
- El total que se guarda lo calcula el backend; el del editor es una ayuda visual que debe coincidir.

### [x] Pantallas obligatorias

1. **Grilla de presupuestos**
   - Número, fecha, cliente, estado, vencimiento (marcando los vencidos) y total.
   - Acciones: Ver, Facturar, Eliminar (deshabilitadas según el estado).
   - Si facturar falla, se muestra el mensaje del backend (sin stock, vencido, ya facturado).
   - Si sale bien, se refrescan presupuestos, facturas y artículos.
2. **Alta de presupuesto**
   - Selección de cliente y validez (15 días por defecto).
   - Buscador de artículos con debounce; si el artículo ya está, suma cantidad.
   - Cantidad y descuento editables por línea; se pueden quitar líneas.
   - Aviso si la cantidad supera el stock. No bloquea: el stock se valida al facturar.
   - **Totales en vivo:** subtotal por línea, subtotal, IVA y total.
   - Mismas validaciones que el backend; sus errores 400 se muestran.

### [x] Pantallas complementarias

3. **Detalle** de un presupuesto, con los ítems y totales del backend.
4. **Facturas**, en forma de lista.

Commits sugeridos: `feat(front): scaffold`, `feat(front): grilla`, `feat(front): alta + totales`, `feat(front): facturar`.

---

## Parte C — Stretch

### [x] Duplicar presupuesto

- **Endpoint:** `POST /api/presupuestos/{id}/duplicar` → `PresupuestoService.DuplicarAsync(id)`.
- Número nuevo, `Fecha = UtcNow` y estado `Borrador`.
- Mismo cliente, validez, artículos, cantidades y descuentos.
- **Precio y alícuota actualizados** desde el artículo.
- 404 si no existe.
- **UI:** acción "Duplicar" en la grilla.

### [x] Reporte: top N artículos por monto facturado

- **Endpoint:** `GET /api/reportes/top-articulos?desde=&hasta=&top=5` (`ReportesController` + `ReporteService`).
- Toma las facturas del rango por `Factura.Fecha`.
- **Zona horaria:** `desde` y `hasta` son **días de Argentina** (`America/Argentina/Buenos_Aires`, hoy UTC-3), ambos inclusivos. Se reciben como `DateOnly` (`yyyy-MM-dd`).
  - Se convierten a un rango UTC con `TimeZoneInfo`: `Fecha ≥ inicio de desde` y `Fecha < inicio del día siguiente a hasta` (con el offset actual, ambos límites caen a las 03:00 UTC).
  - Ejemplo: una factura del 30/11 a las 22:00 en Argentina se guarda como 01/12 01:00 UTC y tiene que entrar en noviembre.
  - Documentar el criterio en `NOTES.md`.
- Usa los ítems del presupuesto facturado, con sus precios guardados.
- El monto es el subtotal neto sin IVA (documentarlo); devuelve además cantidad e IVA.
- **SQLite:** EF no puede hacer `Sum` sobre `decimal`. Filtrar en SQL y sumar en memoria.
- Validar `desde ≤ hasta` y `top` entre 1 y 100.
- **UI:** página Reporte con rango de fechas y N. Envía las fechas como `yyyy-MM-dd`, tal como las entrega el input, sin pasarlas por `Date` ni `toISOString()`.

### [x] Tests propios del backend

Archivos **nuevos** en `backend/MiniErp.Tests/`, usando `TestDbFactory`:

| Archivo | Qué cubre |
|---|---|
| `NumeracionTests` | Crear, borrar y crear no repite números; la secuencia no retrocede |
| `ValidacionesTests` | Cantidad 0 o negativa, descuento fuera de rango, sin ítems, cliente inexistente |
| `FacturacionExtraTests` | Vencido, rechazado, artículo repetido sin stock suficiente |
| `CalcularTotalesExtraTests` | Un caso donde redondear por línea difiere del redondeo global; flujo 2 del README |
| `DuplicarTests` | Número nuevo y precios actualizados |
| `ReporteTests` | Rango de fechas (incluidos los bordes del día argentino), orden y límite N |

### [x] Tests del front

`totales.test.ts` (Vitest): los casos de `CalcularTotalesTests`, el flujo 2 del README y casos de redondeo en .5.

Commits sugeridos: `feat: duplicar presupuesto`, `feat: reporte top artículos`, `test(back): …`, `test(front): totales`.

---

## `NOTES.md` (se completa en cada paso)

- [x] **Bugs encontrados:** qué pasaba, por qué, cómo se resolvió y cómo se verificó.
- [x] **Decisiones del cliente:** estructura, librerías y cálculo de totales en vivo.
- [x] **Qué hice y qué dejé afuera**, con el criterio de priorización.
- [x] **Cómo usé IA:** qué pedí, qué acepté, qué descarté y dónde se equivocó. Anotarlo durante el trabajo.
- [x] **Qué falta para producción:** migraciones en lugar de `EnsureCreated`, autenticación, concurrencia sobre el stock, paginación, logging, tests e2e, flujo de estados Aprobado/Rechazado.
- [x] Cómo levantar el frontend y correr sus tests.

> La consigna sugiere 3 horas. Como se apunta a completar todo, conviene registrar el tiempo real.

---

## Verificación end-to-end

- [x] `cd backend && dotnet test`: todo en verde (tests existentes y nuevos).
- [x] Borrar `minierp.db`, correr `dotnet run` en `backend/MiniErp.Api` y probar en Swagger los 5 flujos, duplicar y el reporte. *(Hecho por HTTP sobre una base nueva, no desde la UI de Swagger.)*
- [x] `cd frontend && npm run test && npm run build`, sin errores de TypeScript.
- [x] Con `npm run dev`, probar a mano: *(Hecho con un script en Chrome headless.)*
  - ART-001 + ART-004 muestra 39.500 / 7.822,50 / 47.322,50 y coincide con el detalle.
  - Facturar 5 unidades de ART-006 muestra el error de stock.
  - Facturar dos veces muestra el error de "ya facturado".
  - Duplicar genera un número nuevo con los precios actuales.
- [x] Revisar que `git log` muestre un commit por paso.
- [ ] Abrir el PR `challenge/solucion` → `main` (o el `.zip` con `.git`, según lo que pidan).
