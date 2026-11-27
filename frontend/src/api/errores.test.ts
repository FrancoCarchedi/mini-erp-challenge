import { describe, expect, it } from 'vitest'
import { mensajesDeError } from './errores'

describe('mensajesDeError', () => {
  it('lee el formato { error } de las reglas de negocio', () => {
    expect(mensajesDeError(400, { error: 'El presupuesto ya fue facturado.' })).toEqual([
      'El presupuesto ya fue facturado.',
    ])
  })

  it('lee un ValidationProblem y no repite mensajes', () => {
    const cuerpo = { title: 'x', errors: { 'Items[0].Cantidad': ['Mal.'], 'Items[1].Cantidad': ['Mal.'] } }

    expect(mensajesDeError(400, cuerpo)).toEqual(['Mal.'])
  })

  it.each([null, 'texto', {}, { errors: {} }])('con un cuerpo desconocido (%j) informa el status', (cuerpo) => {
    expect(mensajesDeError(500, cuerpo)).toEqual(['La API respondió con un error (HTTP 500).'])
  })

  it('tiene un mensaje propio para el 404', () => {
    expect(mensajesDeError(404, null)).toEqual(['No se encontró el recurso solicitado.'])
  })
})
