import { Outlet, useRouterState } from '@tanstack/react-router'
import { useEffect } from 'react'
import { Toaster } from 'sonner'
import { consumeLoginRedirectToast } from '../applyFlow'
import { useT } from '../hooks/useT'
import { useSession } from '../store'
import { AuthBusyOverlay } from './AuthBusyOverlay'
import { FloatingRail } from './FloatingRail'
import { SiteFooter } from './SiteFooter'
import { TopRightUserControls } from './TopRightUserControls'
import { ServiceNoticeBanner } from './ServiceNoticeBanner'

export function Shell() {
  const { refresh, setAuthBusy, lang } = useSession()
  const { t } = useT()
  const path = useRouterState({ select: (s) => s.location.pathname })
  const hideChrome =
    path === '/' ||
    path === '/oldhomepage' ||
    path === '/login' ||
    path === '/register' ||
    path.startsWith('/handoff') ||
    path.startsWith('/portal')

  useEffect(() => {
    void refresh()
  }, [refresh])

  useEffect(() => {
    document.documentElement.lang = lang === 'kn' ? 'kn' : 'en'
  }, [lang])

  // Clear "signing-in" overlay once login (or home) has mounted after the redirect.
  useEffect(() => {
    if (path === '/login' || path === '/') {
      setAuthBusy(null)
    }
  }, [path, setAuthBusy])

  // Show toasts queued across full-page redirects (e.g. OTP verified → dashboard/chat).
  useEffect(() => {
    consumeLoginRedirectToast()
  }, [path])

  if (hideChrome) {
    return (
      <div className="min-h-screen bg-background text-foreground">
        <a href="#main" className="skip-link">
          {t('skipToContent')}
        </a>
        <ServiceNoticeBanner />
        <main id="main">
          <Outlet />
        </main>
        <AuthBusyOverlay />
        <Toaster richColors position="top-center" />
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background text-foreground">
      <a href="#main" className="skip-link">
        {t('skipToContent')}
      </a>
      <ServiceNoticeBanner />
      <div className="gov-stripe h-1" />
      <FloatingRail />
      <div className="flex justify-end px-4 pt-3 md:pl-20 md:pr-6">
        <TopRightUserControls />
      </div>
      <main id="main" className="min-h-[60vh] pb-24 md:pb-0 md:pl-16">
        <Outlet />
      </main>
      <SiteFooter />
      <AuthBusyOverlay />
      <Toaster richColors position="top-center" />
    </div>
  )
}
