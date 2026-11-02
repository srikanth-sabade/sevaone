import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'
import path from 'node:path'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    // Offline shell for Grama One village kiosks / flaky KSWAN uplinks (DEF-008). The SW
    // precaches the built app shell + self-hosted fonts and, at runtime, only ever caches
    // the PUBLIC service catalogue -- every other /api/* path (auth, applications, track,
    // grievances, operator, me) has no runtimeCaching rule, so the SW always hits the
    // network for it and no per-citizen or authenticated response is ever stored.
    VitePWA({
      registerType: 'autoUpdate',
      // External /registerSW.js, not an inline <script> -- the citizen CSP has script-src
      // 'self' with no 'unsafe-inline' (Docker-deploy/nginx/nginx.conf), which would block
      // an inline registration snippet.
      injectRegister: 'script',
      devOptions: { enabled: false },
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2}'],
        navigateFallback: '/index.html',
        // Never answer an API or RAG request with the SPA shell -- let those 404/502 for real.
        navigateFallbackDenylist: [/^\/api\//, /^\/rag-api\//, /^\/ai-api\//, /^\/dashboard\//],
        runtimeCaching: [
          {
            urlPattern: ({ url }) => url.pathname.startsWith('/api/v1/catalog/'),
            handler: 'StaleWhileRevalidate',
            options: {
              cacheName: 'sevaone-catalog',
              expiration: { maxEntries: 200, maxAgeSeconds: 60 * 60 * 24 },
              cacheableResponse: { statuses: [200] },
            },
          },
        ],
      },
      manifest: {
        name: 'SevaOne — One Portal, Every Service',
        short_name: 'SevaOne',
        description: 'One place to ask for any Government of Karnataka citizen service.',
        theme_color: '#164c93',
        background_color: '#fafafa',
        display: 'standalone',
        start_url: '/',
        scope: '/',
        lang: 'en',
        icons: [
          { src: '/icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: '/icon-512.png', sizes: '512x512', type: 'image/png' },
        ],
      },
    }),
  ],
  resolve: {
    alias: { '@': path.resolve(import.meta.dirname, 'src') },
  },
  server: {
    port: 5174,
    proxy: {
      // AiDiscoveryBetaPage -> sevaone-rag directly (its own local dev process, port 8009 -
      // see sevaone-rag/run.py), not the bff or ai_backend. nginx.conf's own /rag-api/ rule
      // targets a Docker Compose service name (sevaone-rag-service:8008) that only resolves
      // inside that network, so local `npm run dev` needs its own separate mapping here.
      // Path rewrite: sevaone-rag's real route is /api/v1/chat, not bare /chat.
      '/rag-api': {
        target: 'http://localhost:8009',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/rag-api/, '/api/v1'),
      },
      // AiBeta.tsx (/ai-beta route) -- the co-dev's new standalone AI backend, same mapping
      // as nginx.conf's /ai-beta-api/ location for local `npm run dev` usage.
      '/ai-beta-api': {
        target: 'http://101.53.138.83:8001',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/ai-beta-api/, ''),
      },
      // AI chat/voice, proxied through the bff (backend/SevaOne.Services/Controllers/
      // AiChatController.cs), not ai_backend directly -- must be declared BEFORE the generic
      // '/api' entry below: Vite matches proxy keys in object-definition order, first match
      // wins, and the generic entry's rewrite (-> /sevaone-citizen/1.0.0$path against the
      // local APIM gateway) has no ai-chat/language resource published and never will, since
      // CitizenGatewayAuthMiddleware's own allowlist deliberately excludes these paths too.
      '/api/v1/ai-chat': {
        target: 'http://localhost:5080',
        changeOrigin: true,
      },
      '/api/v1/language': {
        target: 'http://localhost:5080',
        changeOrigin: true,
      },
      '/api': {
        // Real citizen APIM gateway, not the backend port directly -- the backend rejects
        // direct :5080 traffic outright (CitizenGatewayAuthMiddleware), same as production.
        target: 'https://localhost:8244',
        changeOrigin: true,
        secure: false, // local APIM's cert is self-signed
        rewrite: (path) => `/sevaone-citizen/1.0.0${path}`,
        configure(proxy) {
          proxy.on('proxyRes', (proxyRes) => {
            const setCookie = proxyRes.headers['set-cookie']
            if (!setCookie) {
              return
            }
            proxyRes.headers['set-cookie'] = setCookie.map((cookie) =>
              cookie.replace(/;\s*Secure/gi, '').replace(/;\s*Domain=[^;]+/gi, ''),
            )
          })
        },
      },
    },
  },
})
