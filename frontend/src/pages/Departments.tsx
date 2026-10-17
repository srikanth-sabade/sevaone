import { Link } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import { Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { Input } from '../components/ui/input'
import { useT } from '../hooks/useT'
import { departmentName } from '../lib/catalogKn'

export function DepartmentsPage() {
  const { lang, t } = useT()
  const depts = useQuery({ queryKey: ['depts'], queryFn: api.departments })
  const [q, setQ] = useState('')

  const filtered = useMemo(() => {
    const all = depts.data ?? []
    const query = q.trim().toLowerCase()
    if (!query) return all
    return all.filter((d) => {
      const localized = departmentName(lang, d.code, d.name)
      return (
        d.name.toLowerCase().includes(query) ||
        localized.toLowerCase().includes(query) ||
        d.portalName.toLowerCase().includes(query) ||
        d.code.toLowerCase().includes(query)
      )
    })
  }, [depts.data, lang, q])

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <h1 className="text-3xl font-bold text-[color:var(--gov-navy)]">{t('deptsTitle')}</h1>
      <Breadcrumbs />
      <p className="mt-3 text-sm text-muted-foreground">{t('deptsHint')}</p>

      <div className="relative mt-6 max-w-md">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder={t('deptsSearchPlaceholder')}
          aria-label={t('deptsSearchPlaceholder')}
          className="pl-9"
        />
      </div>

      {depts.data && filtered.length === 0 ? (
        <p className="mt-8 text-sm text-muted-foreground">{t('deptsSearchNoResults', { q })}</p>
      ) : (
        <div className="mt-6 grid gap-4 md:grid-cols-3">
          {filtered.map((d) => (
            <div key={d.code} className="rounded-2xl border border-border bg-card p-4 shadow-sm">
              <h2 className="font-semibold text-[color:var(--gov-navy)]">{departmentName(lang, d.code, d.name)}</h2>
              <p className="text-sm text-muted-foreground">{d.portalName}</p>
              <p className="mt-2 text-sm">{t('deptsServices', { count: d.schemeCount })}</p>
              <p className="text-xs text-muted-foreground">{d.isIntegrated ? t('deptsLive') : t('deptsStub')}</p>
              <Link
                className="mt-3 inline-block text-sm font-medium text-[color:var(--gov-blue)] hover:underline"
                to="/services"
                search={{ q: undefined, department: d.code }}
              >
                {t('deptsView')}
              </Link>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
