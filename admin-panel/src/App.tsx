import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { Toaster } from 'sonner'
import { AdminLoginView } from './components/AdminLoginView'
import { FirstTimeLoginView } from './components/FirstTimeLoginView'
import { Shell } from './components/Shell'
import { useGovAdminMe } from './hooks/useGovAdmin'
import { AdminManagementPage } from './pages/AdminManagement'
import { ActivityLogPage } from './pages/ActivityLog'
import { OperatorsPage } from './pages/Operators'
import { ServiceNoticesPage } from './pages/ServiceNotices'
import { CitizensPage } from './pages/Citizens'
import { DepartmentsPage } from './pages/Departments'
import { DiscoveryPage } from './pages/Discovery'
import { DistrictsPage } from './pages/Districts'
import { LifecyclePage } from './pages/Lifecycle'
import { OverviewPage } from './pages/Overview'
import { SakalaSlaPage } from './pages/SakalaSla'
import { ApplicationAgingPage } from './pages/ApplicationAging'
import { ApplicationFunnelPage } from './pages/ApplicationFunnel'
import { ServiceUsagePage } from './pages/ServiceUsage'
import { NotificationsPage } from './pages/Notifications'
import { SystemHealthPage } from './pages/SystemHealth'
import { CataloguePage } from './pages/Catalogue'
import { IntegrationsPage } from './pages/Integrations'
import { AIAssistantPage, AIReingestPage, AIStatusPage } from './pages/AIOperations'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 60_000, refetchOnWindowFocus: false },
  },
})

function LoadingView() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-[color:var(--background)] px-4">
      <div className="text-center">
        <div className="mx-auto mb-4 h-10 w-10 animate-spin rounded-full border-4 border-[color:var(--gov-blue)]/20 border-t-[color:var(--gov-blue)]" />
        <p className="text-sm font-medium text-[color:var(--gov-navy)]">Checking government access</p>
      </div>
    </main>
  )
}

function ProtectedApp() {
  const me = useGovAdminMe()
  const location = useLocation()

  if (me.isLoading) return <LoadingView />
  if (me.isError || !me.data) {
    return location.pathname === '/first-time-login' ? <FirstTimeLoginView /> : <AdminLoginView />
  }

  const canManageAdmins = me.data.tier === 'Superadmin' || me.data.tier === 'ChiefMinister'

  return (
    <Shell admin={me.data}>
      <Routes>
        <Route path="/login" element={<Navigate to="/" replace />} />
        <Route path="/first-time-login" element={<Navigate to="/" replace />} />
        <Route path="/" element={<OverviewPage />} />
        <Route path="/departments" element={<DepartmentsPage tier={me.data.tier} />} />
        <Route path="/districts" element={<DistrictsPage admin={me.data} />} />
        <Route path="/discovery" element={<DiscoveryPage />} />
        <Route path="/citizens" element={<CitizensPage tier={me.data.tier} />} />
        <Route path="/lifecycle" element={<LifecyclePage />} />
        <Route path="/sakala-sla" element={<SakalaSlaPage />} />
        <Route path="/application-aging" element={<ApplicationAgingPage />} />
        <Route path="/service-usage" element={<ServiceUsagePage />} />
        <Route path="/application-funnel" element={<ApplicationFunnelPage />} />
        {canManageAdmins && <Route path="/admins" element={<AdminManagementPage tier={me.data.tier} />} />}
        {canManageAdmins && <Route path="/activity-log" element={<ActivityLogPage />} />}
        {canManageAdmins && <Route path="/operators" element={<OperatorsPage tier={me.data.tier} />} />}
        {me.data.tier === 'Superadmin' && <Route path="/service-notices" element={<ServiceNoticesPage />} />}
        {me.data.tier === 'Superadmin' && <Route path="/notifications" element={<NotificationsPage />} />}
        {me.data.tier === 'Superadmin' && <Route path="/health" element={<SystemHealthPage />} />}
        {me.data.tier === 'Superadmin' && <Route path="/catalogue" element={<CataloguePage />} />}
        {me.data.tier === 'Superadmin' && <Route path="/integrations" element={<IntegrationsPage />} />}
        {me.data.tier === 'Superadmin' && <Route path="/ai" element={<AIStatusPage />} />}
        {me.data.tier === 'Superadmin' && <Route path="/ai/reingest" element={<AIReingestPage />} />}
        <Route path="/ai/assistant" element={<AIAssistantPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Shell>
  )
}

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter basename={import.meta.env.BASE_URL.replace(/\/$/, '')}>
        <ProtectedApp />
        <Toaster position="top-right" richColors closeButton />
      </BrowserRouter>
    </QueryClientProvider>
  )
}
