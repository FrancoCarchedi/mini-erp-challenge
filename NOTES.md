# NOTES

> Completá este archivo a medida que avanzás. Es parte de la entrega.

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

<!--
Estructura del proyecto, librerías que usaste y por qué, y cómo resolvés los
totales en vivo (subtotal, IVA, total) para que coincidan con el backend.
-->

## Qué hice y qué dejé afuera

<!-- Qué completaste, qué priorizaste y por qué, qué quedó pendiente. -->

## Cómo usé IA

<!--
Para qué partes la usaste, qué le pediste, qué aceptaste y qué descartaste,
y dónde se equivocó o te llevó por mal camino.
-->

## Qué haría con más tiempo / qué falta para producción

