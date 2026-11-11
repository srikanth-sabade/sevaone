import { Link } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import { ChevronRight, Sparkles } from 'lucide-react'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { ScrollRow } from './ScrollRow'
import { useT } from '../hooks/useT'
import { departmentName, serviceName } from '../lib/catalogKn'
import { formatFeeInr } from '../lib/fee'
import { isOccupation, recommendFor } from '../lib/occupations'

/** Services and schemes suggested from the occupation saved on the citizen's profile. */
export function RecommendedServices({ occupation, onEdit }: { readonly occupation?: string | null; readonly onEdit: () => void }) {
  const { t, lang } = useT()
  const schemes = useQuery({ queryKey: ['schemes', undefined, undefined], queryFn: () => api.schemes(undefined, undefined) })
  const [all, setAll] = useState(false)
  const occ = isOccupation(occupation) ? occupation : null
  const picks = useMemo(() => (occ ? recommendFor(occ, schemes.data ?? [], 200) : []), [occ, schemes.data])

  const card = (s: (typeof picks)[number]) => (
    <Link
      key={s.serviceCode}
      to="/services/$code"
      params={{ code: s.serviceCode }}
      className={`group flex min-h-[6.5rem] flex-col rounded-2xl border border-border bg-card p-4 transition hover:border-[color:var(--gov-blue)] ${all ? '' : 'w-60 shrink-0 snap-start'}`}
    >
      <div className="line-clamp-2 text-sm font-semibold leading-snug text-[color:var(--gov-navy)]">{serviceName(lang, s.serviceCode, s.name)}</div>
      <div className="mt-1 line-clamp-1 text-xs text-muted-foreground">{departmentName(lang, s.departmentCode, s.departmentName)}</div>
      <div className="mt-auto flex items-center justify-between pt-3 text-xs">
        <span className="font-medium text-[color:var(--gov-navy)]">{formatFeeInr(s)}</span>
        <ChevronRight className="h-4 w-4 text-muted-foreground transition group-hover:translate-x-0.5" />
      </div>
    </Link>
  )

  return (
    <section className="mt-6 rounded-3xl border border-border/80 bg-card p-5 shadow-[var(--shadow-soft)]">
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-[color:var(--gov-blue)] to-[color:var(--gov-green)] text-white">
          <Sparkles className="h-5 w-5" />
        </span>
        <h2 className="t-card-title min-w-0 flex-1">{t('recommendedForYou')}</h2>
        {!occ && (
          <button
            type="button"
            onClick={onEdit}
            className="cursor-pointer rounded-full border border-border bg-card px-3 py-1 text-xs font-medium text-[color:var(--gov-blue)] transition hover:bg-secondary"
          >
            {t('addOccupation')}
          </button>
        )}
        {picks.length > 4 && (
          <button
            type="button"
            onClick={() => setAll((v) => !v)}
            className="cursor-pointer rounded-full border border-border bg-card px-3 py-1 text-xs font-medium text-[color:var(--gov-blue)] transition hover:bg-secondary"
          >
            {all ? t('showFewer') : t('viewAllN', { n: picks.length })}
          </button>
        )}
      </div>
      {!occ && <p className="text-sm text-muted-foreground">{t('addOccupationPrompt')}</p>}
      {occ && schemes.isSuccess && picks.length === 0 && <p className="text-sm text-muted-foreground">{t('noRecommendations')}</p>}
      {picks.length > 0 &&
        (all ? (
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">{picks.map(card)}</div>
        ) : (
          <ScrollRow className="pb-2">{picks.map(card)}</ScrollRow>
        ))}
    </section>
  )
}
