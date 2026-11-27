import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  // La API solo permite CORS desde el 5173: mejor fallar que arrancar en otro puerto.
  server: { port: 5173, strictPort: true },
})
