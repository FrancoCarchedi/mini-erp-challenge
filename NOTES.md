# NOTES

## Cómo levantar y probar

```bash
# Backend: http://localhost:5080 (Swagger en /swagger)
cd backend/MiniErp.Api && dotnet run
cd backend && dotnet test

# Cliente (en otra terminal): http://localhost:5173
cd frontend && npm install && npm run dev
npm run test
npm run build
npm run lint
```

- **Antes del primer arranque:** si existe un `backend/MiniErp.Api/minierp.db` anterior, borralo
  junto con `-shm` y `-wal`. El esquema cambió (bugs 3 y 6) y `EnsureCreated` no lo actualiza.
- El cliente usa `http://localhost:5080` por defecto; para cambiarlo, copiá
  `frontend/.env.example` a `frontend/.env`.
- Vite está fijado al puerto 5173 (`strictPort`), el único que la API permite por CORS.
- Se necesita **Node 22.12 o superior** (probado con Node 24). El README del challenge dice 18+,
  pero Vitest 5 exige `^22.12.0 || ^24.0.0 || >=26.0.0`.

## Bugs encontrados (backend)

Cada bug se reprodujo sobre el código original antes de arreglarlo: con los tests que fallaban
en la línea base (3 de 6) o contra la API levantada con una base temporal.

### 1. El IVA se calculaba siempre al 21%

- **Qué pasaba:** en el flujo 2 del README (ART-001 + ART-004) el IVA daba 8.295,00 en lugar
  de 7.822,50.
- **Por qué:** `CalcularTotales` hacía `subtotal * 0.21m` e ignoraba la alícuota de cada línea.
  Además no redondeaba, y el controller repetía por su cuenta la cuenta del subtotal de línea.
- **Arreglo:** `CalculoLinea` concentra la regla (subtotal e IVA redondeados a 2 decimales por
  línea; después se suma). La usan el servicio y el DTO. El redondeo es
  `MidpointRounding.AwayFromZero`: el default de .NET es *banker's rounding* y no coincidiría
  con el cliente.
- **Verificación:** `CalcularTotales_ConAlicuotasMixtas_CalculaIvaPorLinea`.

### 4. Se facturaba sin stock y más de una vez

*(La numeración de esta lista sigue la del plan de acción; los bugs 1 y 4 se hicieron primero
porque tenían tests.)*

- **Qué pasaba:** el stock quedaba negativo y el mismo presupuesto se podía facturar varias
  veces, descontando stock en cada una.
- **Por qué:** `FacturarAsync` solo validaba el vencimiento y restaba stock sin comprobar nada.
- **Arreglo:** antes de modificar datos se valida, en este orden: estado (rechaza `Facturado`,
  `Rechazado` y presupuestos que ya tienen factura), vigencia y stock. El stock se valida
  sobre la suma por artículo, porque un artículo puede venir en dos líneas; el error nombra los
  artículos que no alcanzan. `Borrador` se puede facturar porque todo presupuesto nace así y no
  hay flujo de aprobación. Todo corre en una transacción abierta **antes de leer**: en SQLite
  (`BEGIN IMMEDIATE`) eso serializa las facturaciones simultáneas.
- **Verificación:** `Facturar_SinStockSuficiente_…` y `Facturar_DosVeces_…`. Además, contra la
  API: 8 `POST` simultáneos sobre el mismo presupuesto (entra uno solo) y 6 presupuestos
  compitiendo por 3 unidades (se facturan exactamente 3 y el stock queda en 0).

### 2. La lista no mostraba los presupuestos nuevos (sin test)

- **Qué pasaba:** después de un `POST`, `GET /api/presupuestos` devolvía `[]`.
- **Por qué:** `ListarAsync` filtraba `Estado != Borrador`, y todo presupuesto se crea como
  `Borrador`.
- **Arreglo:** se quitó el filtro.
- **Verificación:** flujo 1 del README.

### 3. La numeración se repetía después de borrar (sin test)

- **Qué pasaba:** creando el 1 y el 2, borrando el 1 y creando otro, volvía a salir el 2.
- **Por qué:** el próximo número era `Count() + 1`.
- **Arreglo:** tabla `Secuencias` (`Nombre`, `UltimoValor`) para presupuestos y facturas. Un
  número nunca se reutiliza, aunque se borre el último (con `Max + 1` sí se reutilizaría). El
  incremento es un único `INSERT … ON CONFLICT DO UPDATE … RETURNING`, dentro de la misma
  transacción que el alta, así que un alta fallida no consume número. Si ya hubiera documentos
  con un número mayor, la secuencia arranca desde el máximo. Además hay índices **UNIQUE** en
  `Presupuesto.Numero`, `Factura.Numero` y `Factura.PresupuestoId`.
- **⚠️ Esquema:** el proyecto usa `EnsureCreated`, que no actualiza una base existente. Si ya
  tenés un `backend/MiniErp.Api/minierp.db`, borralo (junto con `-shm` y `-wal`) antes de
  levantar la API.
- **Verificación:** flujo 3 del README.

### 5. No se validaban los datos de entrada (sin test)

- **Qué pasaba:** se aceptaban cantidades 0 o negativas, descuentos de 150%, validez negativa y
  presupuestos sin ítems; un cliente inexistente terminaba en un 500 (error de FK).
- **Por qué:** no había ninguna validación, ni en los DTOs ni en el servicio.
- **Arreglo, en dos capas:** DataAnnotations en los DTOs (`[ApiController]` responde 400 solo)
  y las mismas reglas en `CrearAsync`, más la existencia del cliente, para que el servicio no
  dependa de la API. Además: descuento con hasta 2 decimales (la columna es `decimal(5,2)` y el
  total en vivo del cliente trabaja en centésimos) y topes técnicos de cantidad (1.000.000) y
  validez (3650 días) para evitar desbordes. Estos topes no son una regla de negocio del
  README.
- **Verificación:** flujo 4 del README.

### 6. Borrar un presupuesto facturado borraba su factura (sin test, de datos)

- **Qué pasaba:** el `DELETE` respondía 204 y la factura desaparecía de `GET /api/facturas`,
  con el stock ya descontado. Además, borrar un id inexistente también daba 204.
- **Por qué:** `Factura.PresupuestoId` es una FK requerida, así que EF aplica borrado en
  cascada por defecto, y `EliminarAsync` no controlaba nada.
- **Arreglo:** el servicio rechaza los facturados (400 con `{ error }`), la relación usa
  `DeleteBehavior.Restrict` como defensa en la base (también requiere regenerar el `.db`) y un
  id inexistente da 404.
- **Verificación:** facturar, intentar borrar y comprobar que la factura sigue en la lista.

### 7. Las fechas salían sin zona horaria (menor, sin test)

- **Qué pasaba:** la respuesta del `POST` traía la fecha con `Z` (la entidad en memoria), pero
  un `GET` posterior la devolvía sin `Z`. El navegador la tomaba como hora local: 3 horas de
  corrimiento en Argentina.
- **Por qué:** SQLite no guarda el `Kind` y EF devuelve `DateTime` como `Unspecified`.
- **Arreglo:** `UtcDateTimeConverter` como convención global (`ConfigureConventions`), así
  cubre también las fechas futuras. Es válido porque los datos ya se guardaban en UTC (la API
  asigna `Fecha` con `DateTime.UtcNow` y no recibe fechas en los requests); no cubre una base
  editada a mano. Descarté un conversor de JSON: solo corregía la salida y la lógica del
  backend habría seguido recibiendo `Unspecified`.
- **Verificación:** `FechasUtcTests` y las fechas de los `GET`, que terminan en `Z`.

## Funcionalidades nuevas del backend

### Duplicar: `POST /api/presupuestos/{id}/duplicar`

Copia cliente, validez, artículos, cantidades y descuentos, y reutiliza `CrearAsync`: el número,
la fecha, el estado `Borrador` y el precio y la alícuota **actuales** salen de las mismas reglas
que un alta. 404 si el original no existe.

### Reporte: `GET /api/reportes/top-articulos?desde=&hasta=&top=5`

- **Monto = subtotal neto sin IVA**, con los precios guardados en el presupuesto facturado (no
  los actuales). También devuelve cantidad e IVA.
- **Zona horaria:** `desde` y `hasta` (`yyyy-MM-dd`) son días de **Argentina**, ambos
  inclusivos, y se convierten a un rango UTC semiabierto sobre `Factura.Fecha`. Una factura del
  30/11 a las 22:00 se guarda como 01/12 01:00 UTC y cuenta en noviembre.
- SQLite no puede hacer `Sum` sobre `decimal` desde EF: se filtra en SQL y se suma en memoria.
- `desde ≤ hasta` y `top` entre 1 y 100. Contempla los años con horario de verano en los que la
  medianoche no existió.

## Decisiones del cliente React

### Librerías

| Librería | Para qué y por qué |
|---|---|
| Vite + React + TypeScript (`strict`) | Arranque rápido; usa el puerto que la API ya permite |
| TanStack Query | Estado del servidor: caché, deduplicación, cancelación e invalidación tras cada acción |
| React Router | Rutas; las pantallas secundarias se cargan con `React.lazy` |
| Tailwind v4 | Estilos sin librería de componentes; las clases repetidas están en `index.css` |
| Vitest | Tests de la lógica pura, sin DOM |

No usé librerías de formularios ni de validación: hay un solo formulario.

### Estructura (`frontend/src`)

```
api/         client.ts (fetch + ApiError), errores.ts y un archivo por recurso
types/       DTOs espejo del backend
hooks/       una consulta o mutación por hook
lib/         lógica pura y testeada: totales, formulario, vencimiento, formato
components/  piezas reutilizables
pages/       Presupuestos, NuevoPresupuesto, PresupuestoDetalle, Facturas, Reporte
```

### Totales en vivo (`lib/totales.ts`)

- La misma regla que `CalculoLinea.cs`: redondeo del subtotal por línea, redondeo del IVA por
  línea (sobre el subtotal ya redondeado) y después la suma. Redondeo *half away from zero*.
- Se calcula en **centavos enteros** porque con `number` los casos `.5` fallan (1,005 redondea
  a 1,00). Con **`BigInt`**, porque cantidad × precio × descuento puede superar la precisión
  entera de `number`.
- Se derivan en cada render; no se guardan en estado. Las líneas con datos inválidos quedan
  afuera y la pantalla lo avisa.
- Es una ayuda visual: lo que vale es lo que calcula el backend, que es lo que muestran la
  grilla y el detalle.
- **Paridad comprobada:** el `totales.ts` real contra la API en 300 presupuestos aleatorios
  (1.310 líneas, descuentos con 2 decimales, cantidades de hasta 1.000.000): 0 diferencias.

### Errores y validaciones

- La API responde `{ error }` (reglas de negocio) o `ValidationProblem` (DTOs). Los dos, y la
  API caída, se convierten en `ApiError` con mensajes legibles.
- El alta valida lo mismo que el backend. Los campos se guardan como texto para no pisar lo que
  se está escribiendo, y el descuento admite coma o punto.
- Superar el stock **avisa pero no bloquea**: el stock se valida al facturar.
- En la grilla, "Facturar" se deshabilita solo por estado (`Facturado`, `Rechazado`). En un
  **vencido** queda habilitado, para mostrar el motivo que da el backend. "Ya facturado" se ve
  cuando la grilla está desactualizada (otra pestaña o usuario); ante cualquier error se
  vuelve a consultar.
- Facturar y eliminar piden confirmación, y las acciones se bloquean hasta que la grilla queda
  actualizada.

### Skill `react-best-practices` (Vercel)

Está versionada en `frontend/.agents/skills/` (`.claude/` es un enlace local, ignorado) y se
aplicó en todo el cliente. Apliqué: imports directos sin *barrels*, pantallas con carga
diferida y precarga al apuntar el enlace, valores derivados en el render, `setState`
funcional, acciones en los *event handlers*, ningún componente definido dentro de otro,
condicionales con ternarios y formateadores `Intl` creados una sola vez.

Ajustes:

- `client-swr-dedup` propone **SWR**. Mantuve **TanStack Query**, que es lo que decidía el
  plan: interpreto que el objetivo de la regla es no pedir datos a mano en efectos y
  deduplicar requests, y TanStack Query lo cumple. El plan decía que ante un choque ganaba la
  skill, así que dejo esta excepción explícita.
- Las reglas de Next.js se adaptaron (`React.lazy` en lugar de `next/dynamic`). Las de
  servidor e hidratación no aplican a una SPA.

Respecto de la estructura prevista en el plan, la tabla genérica quedó como una clase CSS
(`.tabla`): las grillas no comparten columnas ni comportamiento.

## Qué hice y qué dejé afuera

**Hecho:** las partes A (7 bugs), B (grilla, alta con totales en vivo y facturar) y C completa
(duplicar y reporte, con endpoint y UI, y tests propios). Además, detalle de presupuesto y
lista de facturas.

**Priorización:** primero los bugs con test, después el resto de los bugs (el cliente depende
de que la API sea correcta), el cliente obligatorio y al final los stretch. Hubo un cambio de
orden: el backend de la parte C se adelantó mientras estaba pendiente instalar la skill de
React, que hacía falta antes de escribir el cliente.

**Tiempo:** la consigna sugiere 3 horas. Según los commits, la ejecución con IA fue de 14:47
a 15:07 más la verificación final, con una pausa en el medio para instalar la skill. No
incluye la lectura previa del código ni el armado del plan.

**Quedó afuera:** lo listado en la última sección. No hay tests de componentes ni e2e en el
repo; el recorrido en navegador se hizo con un script descartable.

### Verificación

- Backend: 46 tests en verde (los 6 originales, sin modificar, y 40 nuevos).
- Cliente: 43 tests en verde; `build` y `lint` sin errores.
- Por HTTP, sobre una base nueva: los 5 flujos del README, los bugs sin test, duplicar,
  reporte, CORS y concurrencia. No usé la interfaz de Swagger; sí comprobé que publica los
  endpoints nuevos.
- En Chrome headless contra la API real: 33 controles (alta y totales, errores de stock,
  vencido y ya facturado, duplicar con precio actualizado, eliminar, facturas, reporte, 400 de
  la API y API caída), sin errores de consola. Para probar "vencido" y "precio actualizado"
  edité la base de prueba, porque la API no permite generar esos casos.

## Cómo usé IA

- **Para qué:** usé Claude Code para armar el plan (`PLAN-DE-ACCION.md`) a partir de leer el
  código, y después para implementarlo completo: arreglos, endpoints, cliente, tests y estas
  notas.
- **Qué le pedí:** ejecutar el plan en orden; más tarde, con la skill de React ya instalada,
  construir el cliente con sus tests, completar las notas y verificar de punta a punta.
- **Qué se decidió en el plan:** stack del cliente, tabla de secuencia (un número nunca se
  reutiliza, así que se descartó `Max + 1`), un commit por paso y tests propios en archivos
  nuevos. Para las fechas se descartó un conversor de JSON en favor del conversor del ORM.
- **Qué agregó por fuera del plan:** topes de cantidad y validez, el rechazo de descuentos con
  más de 2 decimales y de ítems nulos, confirmación antes de facturar o eliminar, tests de la
  validación del formulario y de los errores de la API, y las protecciones del reporte
  (horario de verano y fecha máxima).
- **Cómo se controló:** cada bug se reprodujo antes del arreglo; los tests originales no se
  tocaron; los flujos, la concurrencia, la paridad de totales y la UI se probaron contra la API
  real.
- **Límites de la herramienta:** el modo automático de Claude Code bloqueó el comando que iba
  a consultar las skills de Vercel; la skill la instalé yo y la IA revisó los archivos antes
  de usarla.
- **Dónde se equivocó:**
  - Intentó editar archivos con un script de Python, que no está instalado; el comando siguió
    de largo y corrió los tests sin los cambios.
  - Agregó a un comando una línea sobrante que creaba y borraba un archivo en `/tmp`. Un
    control de seguridad de Claude Code bloqueó el comando completo.
  - Escribió un test de redondeo cuyo caso daba el mismo resultado con las dos formas de
    calcular, así que no probaba nada. Se reemplazó por uno que sí las distingue.
  - Intentó crear muchos archivos del cliente en un solo comando de shell, que falló por las
    comillas sin ejecutar nada.
  - Puso mal el valor esperado de un test de importes grandes (cuenta mal hecha). El test
    falló, se rehízo la cuenta y se confirmó con el mismo caso en el backend, con `decimal`.

## Qué haría con más tiempo / qué falta para producción

- **Migraciones de EF** en lugar de `EnsureCreated`, para cambiar el esquema sin borrar datos.
- **Autenticación y autorización**, y auditoría de quién factura o elimina.
- **Concurrencia del stock** en un motor con escrituras simultáneas: la serialización actual
  depende del lock de SQLite. En otro motor haría falta un token de concurrencia o un `UPDATE`
  condicional, y revisar el SQL de la secuencia.
- **Errores:** un formato único (`ProblemDetails`), 404/409 donde hoy hay 400 (por ejemplo,
  facturar un presupuesto inexistente), manejo global de excepciones y mensajes con acentos.
- **Paginación y filtros** en las listas. Hoy todo se trae completo y el reporte suma en
  memoria.
- **Estados** `Aprobado` y `Rechazado`: existen, pero no hay flujo que los asigne.
- **Observabilidad:** logging estructurado, métricas y health checks.
- **Cliente:** tests de componentes y e2e en el repo, *error boundary* (por ejemplo, si falla
  la descarga de una pantalla), accesibilidad revisada y un diálogo propio en lugar de
  `window.confirm`.
- **CI** que corra tests, build y lint en cada PR.

