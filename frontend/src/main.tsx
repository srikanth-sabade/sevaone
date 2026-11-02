import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from '@tanstack/react-router'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { flow, replayFlow } from './flowLog'
import { router } from './router'
import './styles.css'

replayFlow()
flow('SPA booted', { href: window.location.href })

document.addEventListener(
  'click',
  (event) => {
    const target = event.target
    if (!(target instanceof Element)) return
    const anchor = target.closest('a')
    if (!anchor) return
    const href = anchor.getAttribute('href') ?? ''
    if (href.includes('/login')) {
      const returnTo = new URL(href, window.location.origin).searchParams.get('returnTo')
      flow('Guest → Seva custom login (WSO2 password grant via BFF API only)', {
        href,
        returnTo,
        from: window.location.pathname,
      })
    } else if (href.startsWith('/apply/')) {
      flow('Apply Now → SEVA apply form (already signed in, no WSO2 this click)', { href })
    }
  },
  true,
)

router.subscribe('onResolved', ({ toLocation, fromLocation }) => {
  const to = `${toLocation.pathname}${toLocation.searchStr}`
  const from = fromLocation ? `${fromLocation.pathname}${fromLocation.searchStr}` : ''
  if (to === from) return
  flow('SPA route changed', { from: from || '(boot)', to })
})

const queryClient = new QueryClient()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>,
)
