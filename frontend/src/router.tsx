import { createRootRoute, createRoute, createRouter, redirect } from '@tanstack/react-router'
import { Shell } from './components/Shell'
import { isCategoryId } from './lib/serviceCategories'
import { AboutPage } from './pages/About'
import { AdminCatalogPage } from './pages/AdminCatalog'
import { AiBetaPage } from './pages/AiBeta'
import { ApplyPage } from './pages/Apply'
import { CatalogPage } from './pages/Catalog'
import { CatalogServiceDetailPage } from './pages/CatalogServiceDetail'
import { ChatPage } from './pages/Chat'
import { DashboardPage } from './pages/Dashboard'
import { DepartmentsPage } from './pages/Departments'
import { GrievancePage } from './pages/Grievance'
import { HelpPage } from './pages/Help'
import { NotFoundPage } from './pages/NotFound'
import { KpiPage } from './pages/Kpi'
import { LoginPage } from './pages/Login'
import { OperatorPage } from './pages/Operator'
import { RegisterPage } from './pages/Register'
import { ServiceCodePage } from './pages/ServiceCodePage'
import { ServiceDetailPage } from './pages/ServiceDetail'
import { ServicesPage } from './pages/Services'
import { CentresPage } from './pages/Centres'
import { DigiLockerPage } from './pages/DigiLocker'
import { TrackPage } from './pages/Track'

const rootRoute = createRootRoute({ component: Shell })

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  validateSearch: (s: Record<string, unknown>) => ({
    loginError: typeof s.loginError === 'string' ? s.loginError : undefined,
  }),
  component: ChatPage,
})
const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/home',
  beforeLoad: () => {
    throw redirect({ to: '/', search: { loginError: undefined } })
  },
})
const oldHomeRoute = createRoute({ getParentRoute: () => rootRoute, path: '/oldhomepage', component: () => <ChatPage legacy /> })
const operatorRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/operator',
  component: OperatorPage,
})
const adminCatalogRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/admin/catalog',
  component: AdminCatalogPage,
})
const aiBetaRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/ai-beta',
  component: AiBetaPage,
})
const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  validateSearch: (s: Record<string, unknown>) => ({
    returnTo: typeof s.returnTo === 'string' ? s.returnTo : undefined,
    loginError: typeof s.loginError === 'string' ? s.loginError : undefined,
    mode: s.mode === 'operator' ? 'operator' as const : undefined,
  }),
  component: LoginPage,
})
const registerRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/register',
  validateSearch: (s: Record<string, unknown>) => ({
    next: typeof s.next === 'string' ? s.next : undefined,
  }),
  component: RegisterPage,
})
const servicesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/services',
  validateSearch: (s: Record<string, unknown>) => ({
    q: typeof s.q === 'string' ? s.q : undefined,
    department: typeof s.department === 'string' ? s.department : undefined,
    category: isCategoryId(s.category) ? s.category : undefined,
  }),
  component: ServicesPage,
})
const serviceDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/services/$code',
  component: ServiceDetailPage,
})
const applyRoute = createRoute({ getParentRoute: () => rootRoute, path: '/apply/$code', component: ApplyPage })
const dashboardRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/dashboard',
  validateSearch: (s: Record<string, unknown>) => ({
    ref: typeof s.ref === 'string' ? s.ref : undefined,
  }),
  component: DashboardPage,
})
const trackRoute = createRoute({ getParentRoute: () => rootRoute, path: '/track', component: TrackPage })
const departmentsRoute = createRoute({ getParentRoute: () => rootRoute, path: '/departments', component: DepartmentsPage })
const catalogRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/catalog',
  validateSearch: (s: Record<string, unknown>) => ({
    dept: typeof s.dept === 'string' ? s.dept : undefined,
    sub: typeof s.sub === 'string' ? s.sub : undefined,
    service: typeof s.service === 'string' ? s.service : undefined,
  }),
  component: CatalogPage,
})
const catalogServiceDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/catalog/$code',
  component: CatalogServiceDetailPage,
})
const helpRoute = createRoute({ getParentRoute: () => rootRoute, path: '/help', component: HelpPage })
const kpiRoute = createRoute({ getParentRoute: () => rootRoute, path: '/kpi', component: KpiPage })
const aboutRoute = createRoute({ getParentRoute: () => rootRoute, path: '/about', component: AboutPage })
const grievanceRoute = createRoute({ getParentRoute: () => rootRoute, path: '/grievance', component: GrievancePage })
/** `/{serviceCode}` e.g. /111 — must stay last so named routes win first. */
const serviceCodeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/$serviceCode',
  component: ServiceCodePage,
})

const digilockerRoute = createRoute({ getParentRoute: () => rootRoute, path: '/digilocker', component: DigiLockerPage })

const centresRoute = createRoute({ getParentRoute: () => rootRoute, path: '/centres', component: CentresPage })

const routeTree = rootRoute.addChildren([
  indexRoute,
  homeRoute,
  oldHomeRoute,
  operatorRoute,
  adminCatalogRoute,
  aiBetaRoute,
  loginRoute,
  registerRoute,
  servicesRoute,
  serviceDetailRoute,
  applyRoute,
  dashboardRoute,
  trackRoute,
  departmentsRoute,
  catalogRoute,
  catalogServiceDetailRoute,
  helpRoute,
  kpiRoute,
  aboutRoute,
  grievanceRoute,
  digilockerRoute,
  centresRoute,
  serviceCodeRoute,
])

export const router = createRouter({ routeTree, defaultNotFoundComponent: NotFoundPage })

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
