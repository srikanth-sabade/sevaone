import { useMemo, useState } from 'react'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { useT } from '../hooks/useT'
import { selectHelpFaqs } from '../lib/helpFaqs'
import type { Faq } from '../types'

const HELPLINES = [
  { number: '1902', labelKey: 'helpHelpline1902', hoursKey: 'helpHelplineHours247' },
  { number: '112', labelKey: 'helpHelpline112', hoursKey: null },
  { number: '108', labelKey: 'helpHelpline108', hoursKey: null },
] as const

export function HelpPage() {
  const { t, lang } = useT()
  const [q, setQ] = useState('')

  const faqs = useMemo(() => selectHelpFaqs(lang, q), [lang, q])
  const grouped = useMemo(() => groupByCategory(faqs), [faqs])

  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="text-3xl font-bold text-[color:var(--gov-navy)]">{t('helpTitle')}</h1>
      <Breadcrumbs />
      <p className="mt-3 text-sm text-muted-foreground">{t('helpHint')}</p>

      <section className="mt-6">
        <h2 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          {t('helpHelplinesTitle')}
        </h2>
        <div className="mt-2 grid gap-3 sm:grid-cols-3">
          {HELPLINES.map((line) => (
            <a
              key={line.number}
              href={`tel:${line.number}`}
              className="rounded-2xl border border-border bg-card p-4 shadow-sm transition hover:border-[color:var(--gov-blue)]"
            >
              <div className="text-lg font-bold text-[color:var(--gov-navy)]">{line.number}</div>
              <div className="mt-0.5 text-sm text-muted-foreground">{t(line.labelKey)}</div>
              {line.hoursKey && (
                <div className="mt-1 text-xs text-[color:var(--gov-green)]">{t(line.hoursKey)}</div>
              )}
            </a>
          ))}
        </div>
        <p className="mt-3 rounded-xl border border-border bg-secondary/30 p-3 text-sm text-muted-foreground">
          <span className="font-medium text-[color:var(--gov-navy)]">{t('helpCountersTitle')}: </span>
          {t('helpCountersNote')}
        </p>
      </section>

      <section className="mt-10">
        <h2 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          {t('helpFaqsTitle')}
        </h2>
        <input
          className="mt-3 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
          placeholder={t('helpSearch')}
          aria-label={t('helpSearch')}
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />

        <div className="sr-only" role="status" aria-live="polite" aria-atomic="true">
          {q.trim() && faqs.length === 0 ? t('helpNoResults', { q }) : ''}
        </div>

        {faqs.length === 0 ? (
          <p className="mt-4 text-sm text-muted-foreground">{t('helpNoResults', { q })}</p>
        ) : (
          <div className="mt-4 space-y-6">
            {grouped.map(([category, items]) => (
              <div key={category}>
                <h3 className="text-sm font-semibold text-[color:var(--gov-navy)]">{category}</h3>
                <div className="mt-2 space-y-2">
                  {items.map((f) => (
                    <details key={f.id} className="rounded-2xl border border-border bg-card p-4 shadow-sm">
                      <summary className="cursor-pointer font-medium text-[color:var(--gov-navy)]">
                        {f.question}
                      </summary>
                      <p className="mt-2 text-sm text-muted-foreground">{f.answer}</p>
                      {f.relatedServices.length > 0 && (
                        <p className="mt-2 text-xs text-muted-foreground">
                          {t('helpRelatedServices')}: {f.relatedServices.join(', ')}
                        </p>
                      )}
                    </details>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}

/** Preserves the category order selectHelpFaqs already sorted by. */
function groupByCategory(faqs: Faq[]): [string, Faq[]][] {
  const order: string[] = []
  const map = new Map<string, Faq[]>()
  for (const f of faqs) {
    if (!map.has(f.category)) {
      map.set(f.category, [])
      order.push(f.category)
    }
    map.get(f.category)!.push(f)
  }
  return order.map((c) => [c, map.get(c)!])
}
