import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Where the ASP.NET Core API listens (see Properties/launchSettings.json).
const API_TARGET = process.env.VITE_API_TARGET || 'http://localhost:5176'

const proxy = {
  '/api': {
    target: API_TARGET,
    changeOrigin: true,
  },
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy,
  },
  // `npm run preview` (built files) also proxies so the production bundle works
  // without configuring CORS on the API.
  preview: {
    port: 4173,
    proxy,
  },
})
