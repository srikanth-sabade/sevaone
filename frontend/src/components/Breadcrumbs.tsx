import { useQuery } from '@tanstack/react-query'
import { Link, useParams, useRouterState } from '@tanstack/react-router'
import { ChevronRight, Home } from 'lucide-react'
import { api } from '../api'
import { useCatalogDepartment } from '../hooks/useCatalog'
import { useT } from '../hooks/useT'
import type { MessageKey } from '../i18n'
import type { CatalogDepartmentDetail } from '../lib/catalogHierarchy'
import { findMockService } from '../lib/catalogMock'

type Crumb = {
  label: string
  to?: string
  params?: Record<string, string>
  search?: Record<string, string | undefined>
}

/** Inline trail under the page title — not a site-wide header bar. */
export function Breadcrumbs() {
  const { t, lang } = useT()
  const pathname = useRouterState({ select: (s) => s.location.pathname })
  const params = useParams({ strict: false }) as { code?: string; dept?: string }
  const search = useRouterState({ select: (s) => s.location.search }) as Record<string, unknown>

  const schemeQ = useQuery({
    queryKey: ['scheme', params.code],
    queryFn: () => api.scheme(params.code!),
    enabled: Boolean(params.code && (pathname.startsWith('/services/') || pathname.startsWith('/apply/'))),
  })

  // Same query key Catalog.tsx's own department fetch uses -- React Query dedupes/shares the
  // request rather than firing a second network call, so this doesn't cost anything extra on
  // /catalog itself, only when Breadcrumbs alone needs the department (there isn't such a case
  // today, but keeps this component correct standalone).
  const catalogDeptCode = pathname.startsWith('/catalog') && typeof search.dept === 'string' ? search.dept : undefined
  const catalogDeptQ = useCatalogDepartment(catalogDeptCode)

  const crumbs = buildCrumbs(pathname, params, search, t, lang, schemeQ.data?.name, catalogDeptQ.data)
  if (!crumbs.length) return null

  return (
    <nav aria-label={lang === 'kn' ? 'ಬ್ರೆಡ್‌ಕ್ರಂಬ್' : 'Breadcrumb'} className="mt-2">
      <ol className="flex flex-wrap items-center gap-1 text-sm">
        {crumbs.map((crumb, i) => {
          const last = i === crumbs.length - 1
          const content =
            i === 0 ? (
              <span className="inline-flex items-center gap-1.5">
                <Home className="h-3.5 w-3.5 shrink-0" aria-hidden />
                {crumb.label}
              </span>
            ) : (
              crumb.label
            )

          const linkExtras = { ...(crumb.params ? { params: crumb.params } : {}), ...(crumb.search ? { search: crumb.search } : {}) }

          return (
            <li key={`${crumb.label}-${i}`} className="flex min-w-0 items-center gap-1">
              {i > 0 && <ChevronRight className="h-3.5 w-3.5 shrink-0 text-muted-foreground/70" aria-hidden />}
              {last || !crumb.to ? (
                <span className="truncate text-muted-foreground" aria-current={last ? 'page' : undefined}>
                  {content}
                </span>
              ) : (
                <Link
                  to={crumb.to}
                  {...linkExtras}
                  className="truncate text-muted-foreground transition hover:text-[color:var(--gov-blue)] hover:underline"
                >
                  {content}
                </Link>
              )}
            </li>
          )
        })}
      </ol>
    </nav>
  )
}

function buildCrumbs(
  pathname: string,
  params: { code?: string; dept?: string },
  search: Record<string, unknown>,
  t: (key: MessageKey, vars?: Record<string, string | number>) => string,
  lang: 'en' | 'kn',
  serviceName: string | undefined,
  catalogDept: CatalogDepartmentDetail | undefined,
): Crumb[] {
  const home: Crumb = {
    label: t('home'),
    to: '/',
    search: { loginError: undefined },
  }

  if (pathname === '/') {
    return [{ label: t('home') }]
  }

  const head = pathname.split('/').find(Boolean) ?? ''
  const crumbs: Crumb[] = [home]

  switch (head) {
    case 'home':
      crumbs.push({ label: t('serviceHome') })
      break
    case 'services':
      crumbs.push(...buildServicesCrumbs(params, t, serviceName))
      break
    case 'apply':
      crumbs.push(...buildApplyCrumbs(params, t, serviceName))
      break
    case 'dashboard':
      crumbs.push({ label: t('dashboard') })
      break
    case 'track':
      crumbs.push({ label: t('track') })
      break
    case 'departments':
      crumbs.push({ label: t('departments') })
      break
    case 'catalog':
      crumbs.push(...buildCatalogCrumbs(params, search, t, lang, catalogDept))
      break
    case 'help':
      crumbs.push({ label: t('help') })
      break
    case 'kpi':
      crumbs.push({ label: t('kpi') })
      break
    case 'about':
      crumbs.push({ label: t('about') })
      break
    case 'grievance':
      crumbs.push({ label: t('grievance') })
      break
    case 'operator':
      crumbs.push({ label: t('assistedCentre') })
      break
    case 'admin':
      crumbs.push({ label: t('catalogAdmin') })
      break
    case 'login':
      crumbs.push({ label: t('login') })
      break
    case 'register':
      crumbs.push(
        { label: t('login'), to: '/login', search: { loginError: undefined } },
        { label: lang === 'kn' ? 'ಪ್ರೊಫೈಲ್ ಪೂರ್ಣಗೊಳಿಸಿ' : 'Complete profile' },
      )
      break
    case 'handoff':
    case 'portal':
      crumbs.push({ label: t('departments'), to: '/departments' }, { label: handoffFallbackLabel(params.dept, lang) })
      break
    default:
      crumbs.push({ label: head })
  }

  const last = crumbs.at(-1)!
  crumbs[crumbs.length - 1] = { label: last.label }
  return crumbs
}

function buildServicesCrumbs(
  params: { code?: string },
  t: (key: MessageKey, vars?: Record<string, string | number>) => string,
  serviceName: string | undefined,
): Crumb[] {
  const crumbs: Crumb[] = [{ label: t('services'), to: '/services', search: { q: undefined, department: undefined } }]
  if (params.code) crumbs.push({ label: serviceName || params.code })
  return crumbs
}

function buildApplyCrumbs(
  params: { code?: string },
  t: (key: MessageKey, vars?: Record<string, string | number>) => string,
  serviceName: string | undefined,
): Crumb[] {
  const crumbs: Crumb[] = [{ label: t('services'), to: '/services', search: { q: undefined, department: undefined } }]
  if (params.code) {
    crumbs.push({ label: serviceName || params.code, to: '/services/$code', params: { code: params.code } })
  }
  crumbs.push({ label: t('applyCta') })
  return crumbs
}

/** Department name from the URL param, title-cased -- or a locale-appropriate fallback when there isn't one. */
function handoffFallbackLabel(deptParam: string | undefined, lang: 'en' | 'kn'): string {
  if (deptParam) {
    return deptParam.charAt(0).toUpperCase() + deptParam.slice(1)
  }
  return lang === 'kn' ? 'ಹ್ಯಾಂಡ್‌ಆಫ್' : 'Handoff'
}

/** Extracted from buildCrumbs' 'catalog' case -- the only case with meaningfully nested logic
 *  (sub-department/service lookups feeding each other), kept separate to hold buildCrumbs'
 *  overall cognitive complexity down to the size of a flat switch dispatcher. */
function buildCatalogCrumbs(
  params: { code?: string },
  search: Record<string, unknown>,
  t: (key: MessageKey, vars?: Record<string, string | number>) => string,
  lang: 'en' | 'kn',
  catalogDept: CatalogDepartmentDetail | undefined,
): Crumb[] {
  const subName = typeof search.sub === 'string' ? search.sub : undefined
  const serviceCode = typeof search.service === 'string' ? search.service : undefined

  const sub = catalogDept?.subDepartments.find((s) => s.name === subName)
  const service = catalogDept?.services.find((s) => s.serviceCode === serviceCode)

  const catalogCrumb: Crumb = {
    label: t('catalog'),
    ...(catalogDept ? { to: '/catalog', search: { dept: undefined, sub: undefined, service: undefined } } : {}),
  }

  const deptCrumb: Crumb | null = catalogDept
    ? {
        label: catalogDept.name,
        ...(sub || service ? { to: '/catalog', search: { dept: catalogDept.code, sub: undefined, service: undefined } } : {}),
      }
    : null

  const subCrumb: Crumb | null = sub
    ? {
        label: sub.name,
        ...(service ? { to: '/catalog', search: { dept: catalogDept!.code, sub: sub.name, service: undefined } } : {}),
      }
    : null

  const serviceCrumb: Crumb | null = service ? { label: (lang === 'kn' && service.nameKannada) || service.name } : null

  // Legacy deep-link shim (/catalog/$code) — separate, older dataset, untouched by the API-backed rewrite.
  const legacyServiceCrumb = legacyCatalogCrumb(params.code, lang)

  return [catalogCrumb, deptCrumb, subCrumb, serviceCrumb, legacyServiceCrumb].filter((c): c is Crumb => Boolean(c))
}

function legacyCatalogCrumb(code: string | undefined, lang: 'en' | 'kn'): Crumb | null {
  if (!code) return null
  const svc = findMockService(code)
  return svc ? { label: lang === 'kn' ? svc.nameKannada : svc.name } : null
}
