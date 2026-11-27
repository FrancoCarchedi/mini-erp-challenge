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

