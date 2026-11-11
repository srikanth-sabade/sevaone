import { Link, useNavigate, useSearch } from '@tanstack/react-router'
import { useQueries, useQuery } from '@tanstack/react-query'
import { ChevronDown, ChevronRight, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { ScrollRow } from '../components/ScrollRow'
import { useT } from '../hooks/useT'
import { deptLogoOrEmblem } from '../lib/deptLogos'
import { serviceName, departmentName } from '../lib/catalogKn'
import { formatFeeInr } from '../lib/fee'
import { SERVICE_CATEGORIES, categoryOf } from '../lib/serviceCategories'
import type { SchemeSummary } from '../types'

export function ServicesPage() {
  const { q, department, category } = useSearch({ from: '/services' })
  const { lang, t } = useT()
  const navigate = useNavigate()
  const [filter, setFilter] = useState('')
  const [gq, setGq] = useState('')
  const [viewAll, setViewAll] = useState<Record<string, boolean>>({})
  const [openGroup, setOpenGroup] = useState<Record<string, string | undefined>>({})
  const schemes = useQuery({
    queryKey: ['schemes', q, department],
    queryFn: () => api.schemes(q, department),
  })

  const active = SERVICE_CATEGORIES.find((c) => c.id === category)
  const list = useMemo(
    () => (schemes.data ?? []).filter((s) => !category || categoryOf(s.serviceCode) === category),
    [schemes.data, category],
  )

  const row = (s: SchemeSummary, showDept = false) => (
    <Link
      key={s.serviceCode}
      to="/services/$code"
      params={{ code: s.serviceCode }}
      className="group flex items-center gap-3 px-4 py-3 transition hover:bg-secondary/60"
    >
      <div className="min-w-0 flex-1">
        <div className="truncate text-sm font-medium text-[color:var(--gov-navy)]">{serviceName(lang, s.serviceCode, s.name)}</div>
        {showDept && <div className="truncate text-xs text-muted-foreground">{departmentName(lang, s.departmentCode, s.departmentName)}</div>}
      </div>
      <div className="hidden shrink-0 text-right text-xs sm:block">
        <div className="font-medium text-[color:var(--gov-navy)]">{formatFeeInr(s)}</div>
        {s.sakalaDays > 0 && (
          <div className="text-muted-foreground">
            {t('sakala')} {s.sakalaDays}d
          </div>
        )}
      </div>
      <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground transition group-hover:translate-x-0.5 group-hover:text-[color:var(--gov-blue)]" />
    </Link>
  )

  const groups = useMemo(() => {
    const f = filter.trim().toLowerCase()
    const m = new Map<string, { code: string; name: string; items: SchemeSummary[] }>()
    for (const s of list) {
      if (f && !s.name.toLowerCase().includes(f)) continue
      const g = m.get(s.departmentCode) ?? { code: s.departmentCode, name: departmentName(lang, s.departmentCode, s.departmentName), items: [] }
      g.items.push(s)
      m.set(s.departmentCode, g)
    }
    return [...m.values()].sort((a, b) => b.items.length - a.items.length || a.name.localeCompare(b.name))
  }, [list, filter, lang])

  // Overview shown on "All categories": per-category and per-department tallies.
  const overview = useMemo(() => {
    const cats = SERVICE_CATEGORIES.map((cat) => {
      const items = list.filter((s) => categoryOf(s.serviceCode) === cat.id)
      const byDept = new Map<string, { code: string; name: string; n: number }>()
      for (const s of items) {
        const d = byDept.get(s.departmentCode) ?? { code: s.departmentCode, name: departmentName(lang, s.departmentCode, s.departmentName), n: 0 }
        d.n++
        byDept.set(s.departmentCode, d)
      }
      return { cat, count: items.length, top: [...byDept.values()].sort((a, b) => b.n - a.n).slice(0, 3) }
    }).filter((x) => x.count > 0)
    const depts = new Map<string, { code: string; name: string; n: number }>()
    for (const s of list) {
      const d = depts.get(s.departmentCode) ?? { code: s.departmentCode, name: departmentName(lang, s.departmentCode, s.departmentName), n: 0 }
      d.n++
      depts.set(s.departmentCode, d)
    }
    return { cats, depts: [...depts.values()].sort((a, b) => b.n - a.n || a.name.localeCompare(b.name)) }
  }, [list, lang])
  const gHits = useMemo(() => {
    const f = gq.trim().toLowerCase()
    return f ? list.filter((s) => s.name.toLowerCase().includes(f)) : []
  }, [list, gq])

  // The scheme list does not say which corporation/sub-department a service belongs to; the catalogue endpoint does.
  const deptCodes = useMemo(() => [...new Set(list.map((s) => s.departmentCode))], [list])
  const deptDetails = useQueries({
    queries: deptCodes.map((code) => ({
      queryKey: ['catalog', 'department', code],
      queryFn: () => api.catalogDepartment(code),
      enabled: !!category,
      staleTime: 5 * 60_000,
    })),
  })
  const subOf = useMemo(() => {
    const m = new Map<string, string>()
    for (const d of deptDetails) for (const sv of d.data?.services ?? []) if (sv.subDepartmentName) m.set(sv.serviceCode, sv.subDepartmentName)
    return m
  }, [deptDetails])

  // Same-named services inside a department (one per corporation) collapse into a single card.
  const cardsFor = (items: SchemeSummary[]) => {
    const byName = new Map<string, SchemeSummary[]>()
    for (const s of items) {
      const k = s.name.replace(/\s+/g, ' ').trim().toLowerCase()
      byName.set(k, [...(byName.get(k) ?? []), s])
    }
    return [...byName.entries()]
      .map(([key, g]) => ({ key, items: g }))
      .sort((a, b) => a.items[0].name.localeCompare(b.items[0].name))
  }

  const cardBody = (s: SchemeSummary) => (
    <>
      <div className="line-clamp-2 text-sm font-semibold leading-snug text-[color:var(--gov-navy)]">{serviceName(lang, s.serviceCode, s.name)}</div>
    </>
  )
  const cardMeta = (s: SchemeSummary) => (
    <div className="mt-auto flex items-center justify-between pt-3 text-xs">
      <span className="font-medium text-[color:var(--gov-navy)]">{formatFeeInr(s)}</span>
      <ChevronRight className="h-4 w-4 text-muted-foreground" />
    </div>
  )

  const chips = (
    <label className="relative inline-block w-full sm:w-72">
      <select
        value={category ?? ''}
        onChange={(e) =>
          void navigate({
            to: '/services',
            search: { q: undefined, department: undefined, category: SERVICE_CATEGORIES.find((c) => c.id === e.target.value)?.id },
          })
        }
        className="w-full cursor-pointer appearance-none rounded-full border border-border bg-card py-2 pl-4 pr-10 text-sm font-medium text-[color:var(--gov-navy)] outline-none focus:border-[color:var(--gov-blue)]"
      >
        <option value="">{t('allCategories')}</option>
        {SERVICE_CATEGORIES.map((c) => (
          <option key={c.id} value={c.id}>
            {t(c.nameKey)}
          </option>
        ))}
      </select>
      <ChevronDown className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
    </label>
  )

  if (!category) {
    const overviewMode = !q && !department
    return (
      <div className="mx-auto max-w-6xl px-4 py-8">
        <h1 className="t-section-title">{t('services')}</h1>
        <Breadcrumbs />
        {overviewMode ? (
          <>
            <p className="mt-3 text-sm text-muted-foreground">
              {t('allServicesSubtitle', { s: list.length, d: overview.depts.length })}
            </p>
            <label className="relative mt-4 block sm:w-96">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <input
                value={gq}
                onChange={(e) => setGq(e.target.value)}
                placeholder={t('searchAllServices')}
                className="w-full rounded-full border border-border bg-card py-2 pl-9 pr-3 text-sm outline-none focus:border-[color:var(--gov-blue)]"
              />
            </label>
            {gq.trim() ? (
              <div className="mt-5">
                <p className="mb-2 text-sm text-muted-foreground">{t('servicesCount', { n: gHits.length })}</p>
                <div className="divide-y divide-border overflow-hidden rounded-2xl border border-border bg-card">
                  {gHits.slice(0, 50).map((s) => row(s, true))}
                  {gHits.length === 0 && <p className="p-4 text-sm text-muted-foreground">{t('noMatches')}</p>}
                </div>
              </div>
            ) : (
              <>
                <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                  {overview.cats.map(({ cat, count, top }) => (
                    <Link
                      key={cat.id}
                      to="/services"
                      search={{ q: undefined, department: undefined, category: cat.id }}
                      className="group flex flex-col rounded-2xl border border-border bg-card p-5 transition hover:border-[color:var(--gov-blue)]"
                    >
                      <div className="flex items-center gap-3">
                        <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-[color:var(--gov-blue)] to-[color:var(--gov-green)] text-white">
                          <cat.icon className="h-5 w-5" />
                        </span>
                        <div className="min-w-0 flex-1">
                          <div className="text-base font-semibold text-[color:var(--gov-navy)]">{t(cat.nameKey)}</div>
                          <div className="text-xs text-muted-foreground">{t('servicesCount', { n: count })}</div>
                        </div>
                        <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground transition group-hover:translate-x-0.5" />
                      </div>
                      <p className="mt-3 line-clamp-2 text-xs text-muted-foreground">{top.map((d) => d.name).join(' · ')}</p>
                    </Link>
                  ))}
                </div>
                <h2 className="mb-3 mt-10 t-section-title">{t('browseByDepartment')}</h2>
                <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                  {overview.depts.map((d) => (
                    <Link
                      key={d.code}
                      to="/services"
                      search={{ q: undefined, department: d.code, category: undefined }}
                      className="flex items-center gap-3 rounded-2xl border border-border bg-card p-3 transition hover:border-[color:var(--gov-blue)]"
                    >
                      <img src={deptLogoOrEmblem(d.name)} alt="" className="h-9 w-9 shrink-0 object-contain" />
                      <div className="min-w-0 flex-1">
                        <div className="line-clamp-2 text-sm font-medium leading-snug text-[color:var(--gov-navy)]">{d.name}</div>
                        <div className="text-xs text-muted-foreground">{t('servicesCount', { n: d.n })}</div>
                      </div>
                    </Link>
                  ))}
                </div>
              </>
            )}
          </>
        ) : (
          <>
            <div className="mt-4">{chips}</div>
            <p className="mt-3 text-sm text-muted-foreground">{q ? t('resultsFor', { q }) : t('browseCatalogue')}</p>
            <div className="mt-4 divide-y divide-border overflow-hidden rounded-2xl border border-border bg-card">
              {list.map((s) => row(s, true))}
            </div>
          </>
        )}
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <Breadcrumbs />
      <header className="mt-3 flex flex-wrap items-center gap-4">
        {active && (
          <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-gradient-to-br from-[color:var(--gov-blue)] to-[color:var(--gov-green)] text-white shadow-sm">
            <active.icon className="h-7 w-7" />
          </span>
        )}
        <div className="min-w-0 flex-1">
          <h1 className="t-section-title">{active ? t(active.nameKey) : t('services')}</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {t('servicesCount', { n: list.length })} · {t('departmentsCount', { n: new Set(list.map((s) => s.departmentCode)).size })}
          </p>
        </div>
        <label className="relative w-full sm:w-72">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            placeholder={t('searchInCategory')}
            className="w-full rounded-full border border-border bg-card py-2 pl-9 pr-3 text-sm outline-none focus:border-[color:var(--gov-blue)]"
          />
        </label>
      </header>
      <div className="mt-5">{chips}</div>

      <div className="mt-6">
        <div className="space-y-5">
          {groups.length === 0 && <p className="text-sm text-muted-foreground">{t('noMatches')}</p>}
          {groups.map((g) => {
            const cards = cardsFor(g.items)
            const all = viewAll[g.code] || filter.trim() !== ''
            const selKey = openGroup[g.code]
            const sel = cards.find((c) => c.key === selKey)
            const subCount = new Set(g.items.map((x) => subOf.get(x.serviceCode)).filter(Boolean)).size

            const cardEls = cards.map((c) => {
                    const first = c.items[0]
                    const multi = c.items.length > 1
                    const subs = c.items.map((x) => subOf.get(x.serviceCode)).filter(Boolean)
                    const cls = `flex min-h-[6.5rem] snap-start flex-col rounded-2xl border bg-card p-4 text-left shadow-sm transition hover:border-[color:var(--gov-blue)] ${
                      all ? '' : 'w-60 shrink-0'
                    } ${selKey === c.key ? 'border-[color:var(--gov-blue)] ring-1 ring-[color:var(--gov-blue)]/30' : 'border-border'}`
                    return multi ? (
                      <button
                        key={c.key}
                        type="button"
                        onClick={() => {
                          setOpenGroup((p) => ({ ...p, [g.code]: p[g.code] === c.key ? undefined : c.key }))
                          // In View all mode the corporations row sits below the whole grid; bring it into view.
                          setTimeout(() => document.getElementById(`sub-${g.code}`)?.scrollIntoView({ block: 'nearest', behavior: 'smooth' }), 50)
                        }}
                        className={`${cls} cursor-pointer`}
                      >
                        {cardBody(first)}
                        <div className="mt-auto flex items-center justify-between pt-3">
                          <span className="rounded-full bg-[color:var(--gov-blue)]/10 px-2 py-0.5 text-[11px] font-medium text-[color:var(--gov-blue)]">
                            {t(subs.length === c.items.length ? 'corporationsCount' : 'versionsCount', { n: c.items.length })}
                          </span>
                          <ChevronRight className={`h-4 w-4 text-muted-foreground transition ${selKey === c.key ? 'rotate-90' : ''}`} />
                        </div>
                      </button>
                    ) : (
                      <Link key={c.key} to="/services/$code" params={{ code: first.serviceCode }} className={cls}>
                        {cardBody(first)}
                        {cardMeta(first)}
                      </Link>
                    )
                  })
            return (
              <section
                key={g.code}
                id={`dept-${g.code}`}
                className="scroll-mt-6 rounded-3xl border border-border bg-card p-5"
              >
                <div className="mb-4 flex items-center gap-3">
                  <img
                    src={deptLogoOrEmblem(g.name)}
                    alt=""
                    className="h-11 w-11 shrink-0 rounded-xl bg-white object-contain p-1 shadow-sm"
                  />
                  <div className="min-w-0 flex-1">
                    <h2 className="truncate text-base font-semibold text-[color:var(--gov-navy)] sm:text-lg">{g.name}</h2>
                    <p className="text-xs text-muted-foreground">
                      {t('servicesCount', { n: g.items.length })}
                      {subCount > 0 && ` · ${t('subDepartmentsCount', { n: subCount })}`}
                    </p>
                  </div>
                  {cards.length > 3 && !filter.trim() && (
                    <button
                      type="button"
                      onClick={() => setViewAll((p) => ({ ...p, [g.code]: !p[g.code] }))}
                      className="shrink-0 cursor-pointer rounded-full border border-border bg-white px-3 py-1 text-xs font-medium text-[color:var(--gov-blue)] transition hover:bg-secondary"
                    >
                      {all ? t('showFewer') : t('viewAllN', { n: cards.length })}
                    </button>
                  )}
                </div>
                {all ? (
                  <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">{cardEls}</div>
                ) : (
                  <ScrollRow className="pb-2">{cardEls}</ScrollRow>
                )}
                {sel && (
                  <div id={`sub-${g.code}`} className="mt-3 rounded-2xl border border-[color:var(--gov-blue)]/25 bg-secondary/40 p-3">
                    <div className="mb-3">
                      <div className="text-sm font-semibold text-[color:var(--gov-navy)]">
                        {serviceName(lang, sel.items[0].serviceCode, sel.items[0].name)}
                      </div>
                      <p className="mt-0.5 text-xs text-muted-foreground">{t('offeredByN', { n: sel.items.length })}</p>
                    </div>
                    <ScrollRow className="!gap-2">
                      {sel.items.map((x) => (
                        <Link
                          key={x.serviceCode}
                          to="/services/$code"
                          params={{ code: x.serviceCode }}
                          className="flex w-64 shrink-0 snap-start items-center gap-2 rounded-xl border border-border bg-card px-3 py-2 transition hover:border-[color:var(--gov-blue)]"
                        >
                          <img src={deptLogoOrEmblem(subOf.get(x.serviceCode) ?? '')} alt="" className="h-8 w-8 shrink-0 object-contain" />
                          <div className="min-w-0 flex-1">
                            <div className="line-clamp-2 text-xs font-semibold text-[color:var(--gov-navy)]">
                              {subOf.get(x.serviceCode) ?? x.serviceCode}
                            </div>
                          </div>
                          <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                        </Link>
                      ))}
                    </ScrollRow>
                  </div>
                )}
              </section>
            )
          })}
        </div>
      </div>
    </div>
  )
}
