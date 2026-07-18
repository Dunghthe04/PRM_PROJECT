import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      // Dev: tránh CORS khi gọi API cùng origin qua proxy
      '/api': {
        target: 'http://localhost:5177',
        changeOrigin: true,
      },
    },
  },
})
