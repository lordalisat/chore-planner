import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.ico', 'favicon.svg', 'apple-touch-icon-180x180.png'],
      manifest: {
        name: 'Chores',
        short_name: 'Chores',
        description: 'What needs doing around the house',
        theme_color: '#2f6f4f',
        background_color: '#f4f1ea',
        display: 'standalone',
        icons: [
          { src: 'pwa-64x64.png', sizes: '64x64', type: 'image/png' },
          { src: 'pwa-192x192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512x512.png', sizes: '512x512', type: 'image/png' },
          { src: 'maskable-icon-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        // API calls always go to the network, never to the SPA shell
        navigateFallbackDenylist: [/^\/api\//, /^\/openapi\//],
      },
    }),
  ],
  // Served by Chores.Api from its web root
  build: { outDir: '../src/Chores.Api/wwwroot', emptyOutDir: true },
  server: {
    // Listen on 0.0.0.0 so the browser on the host reaches it through the devcontainer
    host: true,
    port: 5173,
    strictPort: true,
    proxy: { '/api': 'http://localhost:5080' },
  },
})
