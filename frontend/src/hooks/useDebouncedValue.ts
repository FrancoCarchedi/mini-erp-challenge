import { useEffect, useState } from 'react'

export function useDebouncedValue<T>(valor: T, demoraMs: number): T {
  const [demorado, setDemorado] = useState(valor)

  useEffect(() => {
    const timer = setTimeout(() => setDemorado(valor), demoraMs)
    return () => clearTimeout(timer)
  }, [valor, demoraMs])

  return demorado
}
