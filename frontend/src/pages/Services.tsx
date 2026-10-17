import { Link, useSearch } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import { api } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { useT } from '../hooks/useT'
import { serviceName, departmentName } from '../lib/catalogKn'
import { formatFeeInr } from '../lib/fee'

export function ServicesPage() {
  const { q, department } = useSearch({ from: '/services' })
  const { lang, t } = useT()
  const schemes = useQuery({
    queryKey: ['schemes', q, department],
    queryFn: () => api.schemes(q, department),
  })

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <h1 className="text-2xl font-bold text-[color:var(--gov-navy)] md:text-3xl">{t('services')}</h1>
      <Breadcrumbs />
      <p className="mt-3 text-sm text-muted-foreground">
        {q ? t('resultsFor', { q }) : t('browseCatalogue')}
      </p>
      <div className="mt-6 grid gap-3 md:grid-cols-2">
        {schemes.data?.map((s) => (
          <Link
            key={s.serviceCode}
            to="/services/$code"
            params={{ code: s.serviceCode }}
            className="rounded-2xl border border-border bg-card p-4 shadow-sm transition hover:border-[color:var(--gov-blue)]"
          >
            <div className="flex items-start justify-between gap-2">
              <h2 className="text-sm font-semibold leading-tight text-[color:var(--gov-navy)]">{serviceName(lang, s.serviceCode, s.name)}</h2>
              <span className="whitespace-nowrap rounded-md bg-secondary px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">
                {t('sakala')} {s.sakalaDays}d
              </span>
            </div>
            <p className="mt-1 text-xs text-muted-foreground">{departmentName(lang, s.departmentCode, s.departmentName)}</p>
            <p className="mt-1.5 line-clamp-2 text-xs leading-relaxed text-foreground/70">{s.description}</p>
            <p className="mt-2 text-xs font-medium text-[color:var(--gov-navy)]">
              {t('fee')}: {formatFeeInr(s)}
            </p>
          </Link>
        ))}
      </div>
    </div>
  )
}
