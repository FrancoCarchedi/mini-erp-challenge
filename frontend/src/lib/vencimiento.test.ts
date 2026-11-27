import { describe, expect, it } from 'vitest'
import { diaEnArgentina, formatearDia } from './format'
import { estaVencido, sumarDias } from './vencimiento'

describe('sumarDias', () => {
  it.each([
    ['2026-11-27', 15, '2026-12-12'],
    ['2026-12-31', 1, '2027-01-01'], // cambio de año
    ['2028-02-28', 1, '2028-02-29'], // año bisiesto
    ['2008-10-18', 1, '2008-10-19'], // día de cambio de horario en Argentina
  ])('%s + %i = %s', (dia, dias, esperado) => {
    expect(sumarDias(dia, dias)).toBe(esperado)
  })
})

describe('estaVencido', () => {
  it.each([
    ['2026-11-26', false],
    ['2026-11-27', false], // el día del vencimiento todavía vale
    ['2026-11-28', true],
  ])('si hoy es %s y vence el 27, vencido = %s', (hoy, esperado) => {
    expect(estaVencido('2026-11-27', hoy)).toBe(esperado)
  })
})

describe('días de Argentina', () => {
  it('a las 23:59 de Argentina sigue siendo el mismo día, aunque en UTC ya sea el siguiente', () => {
    expect(diaEnArgentina(new Date('2026-12-01T02:59:59Z'))).toBe('2026-11-30')
    expect(diaEnArgentina(new Date('2026-12-01T03:00:00Z'))).toBe('2026-12-01')
  })

  it('muestra el día sin correrlo por la zona horaria', () => {
    expect(formatearDia('2026-12-12')).toBe('12/12/2026')
  })
})
