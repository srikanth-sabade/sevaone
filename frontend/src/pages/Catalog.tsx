import { Link, useSearch } from '@tanstack/react-router'
import { ChevronRight, Clock3, ExternalLink, Search } from 'lucide-react'
import { useState } from 'react'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { HierarchyServiceDetailPanel } from '../components/HierarchyServiceDetailPanel'
import { Input } from '../components/ui/input'
import { useCatalogDepartment, useCatalogDepartments, useCatalogSearch } from '../hooks/useCatalog'
import { useT } from '../hooks/useT'
import {
  departmentIcon,
  isMeaningfulText,
  matchesDepartmentQuery,
  type CatalogDepartment,
  type CatalogSearchServiceResult,
  type CatalogService,
} from '../lib/catalogHierarchy'

export function CatalogPage() {
  const { t, lang } = useT()
  const { dept: deptCode, sub: subName, service: serviceCode } = useSearch({ from: '/catalog' })
  const [query, setQuery] = useState('')
  const searchActive = query.trim().length > 0

  return (
    <div className="mx-auto max-w-[1400px] px-4 py-6 sm:px-6">
      <h1 className="text-2xl font-bold text-[color:var(--gov-navy)] sm:text-3xl">{t('catalog')}</h1>
      <Breadcrumbs />

      {deptCode ? (
        <DepartmentView deptCode={deptCode} subName={subName} serviceCode={serviceCode} lang={lang} t={t} />
      ) : (
        <LandingView query={query} setQuery={setQuery} searchActive={searchActive} lang={lang} t={t} />
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Screen 1: landing grid of department cards
// ---------------------------------------------------------------------------

function LandingView({
  query,
  setQuery,
  searchActive,
  lang,
  t,
}: {
  readonly query: string
  readonly setQuery: (q: string) => void
  readonly searchActive: boolean
  readonly lang: 'en' | 'kn'
  readonly t: ReturnType<typeof useT>['t']
}) {
  const departmentsQuery = useCatalogDepartments()
  const searchQuery = useCatalogSearch(query)

  return (
    <div>
      <p className="mt-2 text-sm text-muted-foreground">{t('catalogHint')}</p>

      <div className="relative mt-4 max-w-md">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder={t('catalogSearchPlaceholder')}
          aria-label={t('catalogSearchPlaceholder')}
          className="pl-9"
        />
      </div>

      {/* Live region so screen readers hear the result count as the filter narrows -- the
          visible count sits inside SearchResultsSection with no aria-live of its own. */}
      <div className="sr-only" role="status" aria-live="polite" aria-atomic="true">
        {searchActive ? searchResultsAnnouncement(searchQuery, query, t) : ''}
      </div>

      {searchActive ? (
        <SearchResultsSection searchQuery={searchQuery} query={query} lang={lang} t={t} />
      ) : (
        <DepartmentsListSection departmentsQuery={departmentsQuery} t={t} />
      )}
    </div>
  )
}

/** Concise text for the catalog-search live region -- mirrors what SearchResultsSection
 *  renders visually (loading / error / no-results / count) in one string for screen readers. */
function searchResultsAnnouncement(
  searchQuery: ReturnType<typeof useCatalogSearch>,
  query: string,
  t: ReturnType<typeof useT>['t'],
): string {
  if (searchQuery.isLoading) return t('catalogLoading')
  if (searchQuery.isError) return t('catalogLoadError')
  const count = searchQuery.data?.services.length ?? 0
  const noResults = count === 0 && (searchQuery.data?.departments.length ?? 0) === 0
  return noResults ? t('catalogSearchNoResults', { q: query }) : t('catalogSearchResultsCount', { count })
}

/** The search-results branch of the landing view, extracted out of what used to be a four-way
 *  nested JSX ternary alongside the departments-list branch above it. */
function SearchResultsSection({
  searchQuery,
  query,
  lang,
  t,
}: {
  readonly searchQuery: ReturnType<typeof useCatalogSearch>
  readonly query: string
  readonly lang: 'en' | 'kn'
  readonly t: ReturnType<typeof useT>['t']
}) {
  if (searchQuery.isLoading) {
    return <p className="mt-6 text-sm text-muted-foreground">{t('catalogLoading')}</p>
  }
  if (searchQuery.isError) {
    return <p className="mt-6 text-sm text-red-700">{t('catalogLoadError')}</p>
  }

  const noResults = (searchQuery.data?.services.length ?? 0) === 0 && (searchQuery.data?.departments.length ?? 0) === 0

  return (
    <div className="mt-6 space-y-6">
      {searchQuery.data && searchQuery.data.departments.length > 0 && (
        <div>
          <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{t('catalog')}</p>
          <div className="mt-2 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            {searchQuery.data.departments.map((dept) => (
              <DepartmentCard key={dept.code} dept={dept} />
            ))}
          </div>
        </div>
      )}
      <div>
        <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          {t('catalogSearchResultsCount', { count: searchQuery.data?.services.length ?? 0 })}
        </p>
        {noResults ? (
          <p className="mt-2 text-sm text-muted-foreground">{t('catalogSearchNoResults', { q: query })}</p>
        ) : (
          <div className="mt-2 grid gap-2 sm:grid-cols-2">
            {searchQuery.data?.services.map((row) => <SearchServiceRow key={row.compositeCode} row={row} lang={lang} />)}
          </div>
        )}
      </div>
    </div>
  )
}

/** The plain departments-grid branch of the landing view, extracted alongside
 *  SearchResultsSection so the two are simple sibling branches instead of a nested nested ternary. */
function DepartmentsListSection({
  departmentsQuery,
  t,
}: {
  readonly departmentsQuery: ReturnType<typeof useCatalogDepartments>
  readonly t: ReturnType<typeof useT>['t']
}) {
  if (departmentsQuery.isLoading) {
    return <p className="mt-6 text-sm text-muted-foreground">{t('catalogLoading')}</p>
  }
  if (departmentsQuery.isError) {
    return <p className="mt-6 text-sm text-red-700">{t('catalogLoadError')}</p>
  }
  return (
    <div className="mt-6 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
      {departmentsQuery.data?.map((dept) => <DepartmentCard key={dept.code} dept={dept} />)}
    </div>
  )
}

function DepartmentCard({ dept }: { readonly dept: CatalogDepartment }) {
  const { t } = useT()
  const Icon = departmentIcon(dept.code)
  return (
    <Link
      to="/catalog"
      search={{ dept: dept.code, sub: undefined, service: undefined }}
      className="group flex items-start gap-3 rounded-2xl border border-border bg-card p-4 text-left shadow-sm transition hover:border-[color:var(--gov-blue)] hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[color:var(--gov-blue)]"
    >
      <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]">
        <Icon className="h-6 w-6" aria-hidden />
      </span>
      <span className="min-w-0 flex-1">
        <span className="flex min-h-[2.75rem] items-center text-sm font-semibold leading-snug text-[color:var(--gov-navy)]">{dept.name}</span>
        <span className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs text-muted-foreground">
          <span>{t('deptsServices', { count: dept.serviceCount })}</span>
          {dept.subDepartmentCount > 0 && (
            <>
              <span aria-hidden>·</span>
              <span>{t('catalogSubDeptsCount', { count: dept.subDepartmentCount })}</span>
            </>
          )}
        </span>
      </span>
      <ChevronRight className="mt-1 h-4 w-4 shrink-0 text-muted-foreground/60 transition group-hover:translate-x-0.5 group-hover:text-[color:var(--gov-blue)]" aria-hidden />
    </Link>
  )
}

// ---------------------------------------------------------------------------
// Screen 2: department page -- sub-department rail (left) + services (right)
// ---------------------------------------------------------------------------

function DepartmentView({
  deptCode,
  subName,
  serviceCode,
  lang,
  t,
}: {
  readonly deptCode: string
  readonly subName: string | undefined
  readonly serviceCode: string | undefined
  readonly lang: 'en' | 'kn'
  readonly t: ReturnType<typeof useT>['t']
}) {
  const [query, setQuery] = useState('')
  const departmentQuery = useCatalogDepartment(deptCode)

  if (departmentQuery.isLoading) {
    return <p className="mt-6 text-sm text-muted-foreground">{t('catalogLoading')}</p>
  }
  if (departmentQuery.isError || !departmentQuery.data) {
    return (
      <p className="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">
        {t('catalogDeptNotFound')}
      </p>
    )
  }

  const dept = departmentQuery.data
  const selectedSub = dept.subDepartments.find((s) => s.name === subName)
  const selectedService = dept.services.find((s) => s.serviceCode === serviceCode)

  const visibleServices = (() => {
    const q = query.trim()
    const base = selectedSub ? dept.services.filter((s) => s.subDepartmentName === selectedSub.name) : dept.services
    if (!q) return base
    if (selectedSub) {
      return base.filter((s) => matchesDepartmentQuery(s, selectedSub.name, lang, q))
    }
    // No sub-department selected: a matching sub-department name pulls in every service under
    // it, in addition to any service whose own name/id matches directly.
    const matchedSubNames = new Set(
      dept.subDepartments.filter((s) => s.name.toLowerCase().includes(q.toLowerCase())).map((s) => s.name),
    )
    return base.filter((s) => (s.subDepartmentName && matchedSubNames.has(s.subDepartmentName)) || matchesDepartmentQuery(s, s.subDepartmentName ?? undefined, lang, q))
  })()

  return (
    <div>
      <div className="mt-4 grid gap-6 lg:grid-cols-[280px_1fr]">
        <nav aria-label={dept.name} className="space-y-1 lg:sticky lg:top-4 lg:max-h-[calc(100vh-2rem)] lg:self-start lg:overflow-y-auto">
          <div className="flex w-full items-center justify-between gap-2 px-2.5 py-2 text-sm">
            <span className="min-w-0 flex-1 whitespace-normal break-words font-bold leading-snug text-[color:var(--gov-navy)]">{dept.name}</span>
          </div>
          {dept.subDepartments.map((sub) => {
            const isActive = selectedSub?.name === sub.name
            return (
              <Link
                key={sub.name}
                to="/catalog"
                search={{ dept: dept.code, sub: sub.name, service: undefined }}
                className={`flex w-full items-center justify-between gap-2 rounded-lg px-2.5 py-2 text-left text-sm transition ${
                  isActive ? 'bg-[color:var(--gov-blue)]/10 font-semibold text-[color:var(--gov-blue)]' : 'text-foreground/80 hover:bg-secondary'
                }`}
                aria-current={isActive ? 'true' : undefined}
              >
                <span className="min-w-0 flex-1 whitespace-normal break-words leading-snug">{sub.name}</span>
                <span className="shrink-0 text-[10px] text-muted-foreground">{sub.serviceCount}</span>
              </Link>
            )
          })}
        </nav>

        <div className="min-w-0">
          {!selectedService && (dept.departmentUrl || dept.departmentUrlKannada) && (
            <div className="flex justify-end">
              <a
                href={(lang === 'kn' && dept.departmentUrlKannada) || dept.departmentUrl || dept.departmentUrlKannada || undefined}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex shrink-0 items-center gap-1 text-xs font-medium text-[color:var(--gov-blue)] hover:underline"
              >
                {t('catalogPortal')} <ExternalLink className="h-3 w-3" />
              </a>
            </div>
          )}

          {selectedService ? (
            <div className="mt-3">
              <HierarchyServiceDetailPanel service={selectedService} departmentCode={dept.code} />
            </div>
          ) : (
            <>
              <div className="relative mt-3 max-w-md">
                <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder={t('catalogDeptSearchPlaceholder')}
                  aria-label={t('catalogDeptSearchPlaceholder')}
                  className="pl-9"
                />
              </div>

              <div className="sr-only" role="status" aria-live="polite" aria-atomic="true">
                {query.trim()
                  ? visibleServices.length === 0
                    ? t('catalogSearchNoResults', { q: query })
                    : t('catalogSearchResultsCount', { count: visibleServices.length })
                  : ''}
              </div>

              {visibleServices.length === 0 ? (
                <p className="mt-6 text-sm text-muted-foreground">{t('catalogNoServicesHere')}</p>
              ) : (
                <div className="mt-4 grid gap-2 sm:grid-cols-2">
                  {visibleServices.map((service) => (
                    <ServiceRow key={service.compositeCode} service={service} deptCode={dept.code} lang={lang} />
                  ))}
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Shared service cards
// ---------------------------------------------------------------------------

function ServiceRow({
  service,
  deptCode,
  lang,
}: {
  readonly service: CatalogService
  readonly deptCode: string
  readonly lang: 'en' | 'kn'
}) {
  const name = (lang === 'kn' && service.nameKannada) || service.name
  return (
    <Link
      to="/catalog"
      search={{ dept: deptCode, sub: service.subDepartmentName ?? undefined, service: service.serviceCode }}
      className="flex items-start justify-between gap-3 rounded-xl border border-border bg-card px-3.5 py-3 transition hover:border-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/5"
    >
      <div className="min-w-0">
        <div className="text-sm font-semibold text-[color:var(--gov-navy)]">{name}</div>
        {isMeaningfulText(service.shortDescription) && <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">{service.shortDescription}</p>}
      </div>
      {service.sakalaDays > 0 && (
        <span className="inline-flex shrink-0 items-center gap-1 whitespace-nowrap rounded-full bg-secondary px-2 py-1 text-[11px] font-medium text-muted-foreground">
          <Clock3 className="h-3 w-3" /> {service.sakalaDays}d
        </span>
      )}
    </Link>
  )
}

function SearchServiceRow({ row, lang }: { readonly row: CatalogSearchServiceResult; readonly lang: 'en' | 'kn' }) {
  const name = (lang === 'kn' && row.nameKannada) || row.name
  const contextLabel = [row.departmentName, row.subDepartmentName].filter(Boolean).join(' · ')
  return (
    <Link
      to="/catalog"
      search={{ dept: row.departmentCode, sub: row.subDepartmentName ?? undefined, service: row.serviceCode }}
      className="flex items-start justify-between gap-3 rounded-xl border border-border bg-card px-3.5 py-3 transition hover:border-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/5"
    >
      <div className="min-w-0">
        <div className="text-sm font-semibold text-[color:var(--gov-navy)]">{name}</div>
        {isMeaningfulText(row.shortDescription) && <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">{row.shortDescription}</p>}
        <p className="mt-1 text-[10px] font-medium uppercase tracking-wide text-muted-foreground/70">{contextLabel}</p>
      </div>
      {row.sakalaDays > 0 && (
        <span className="inline-flex shrink-0 items-center gap-1 whitespace-nowrap rounded-full bg-secondary px-2 py-1 text-[11px] font-medium text-muted-foreground">
          <Clock3 className="h-3 w-3" /> {row.sakalaDays}d
        </span>
      )}
    </Link>
  )
}
