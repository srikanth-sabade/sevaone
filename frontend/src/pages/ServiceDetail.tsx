import { Link, useParams } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import { useEffect } from 'react'
import { api } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { applyToService } from '../applyFlow'
import { flow } from '../flowLog'
import { useSession } from '../store'
import { useT } from '../hooks/useT'
import { serviceName, departmentName } from '../lib/catalogKn'
import { formatFeeInr } from '../lib/fee'
import type { Me } from '../types'

/** Debug-log-only description of which apply path a citizen would take from here, given their sign-in/profile state. */
function applyFlowDescription(me: Me | null): string {
  if (me?.profileCompleted) return 'Apply Now → resolve apply URL directly'
  if (me) return 'Apply Now → resolve apply URL directly'
  return 'Apply Now → resolve apply URL, sign in only if a real token must be minted'
}

export function ServiceDetailPage() {
  const { lang, t } = useT()
  const { code } = useParams({ from: '/services/$code' })
  const { me, loaded } = useSession()
  const scheme = useQuery({ queryKey: ['scheme', code], queryFn: () => api.scheme(code) })
  const s = scheme.data

  useEffect(() => {
    if (!s || !loaded) return
    flow('service detail ready', {
      service: s.serviceCode,
      department: s.departmentCode,
      signedIn: Boolean(me),
      next: applyFlowDescription(me),
    })
  }, [s, loaded, me])

  if (!s) return <div className="mx-auto max-w-6xl px-4 py-8 text-sm text-muted-foreground">{t('svcLoading')}</div>

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <div className="rounded-2xl border border-border bg-card p-6 shadow-sm">
        <p className="text-xs text-muted-foreground">{departmentName(lang, s.departmentCode, s.departmentName)}</p>
        <h1 className="mt-1 text-2xl font-bold text-[color:var(--gov-navy)]">{serviceName(lang, s.serviceCode, s.name)}</h1>
        <Breadcrumbs />
        <div className="mt-3 flex flex-wrap gap-2 text-xs">
          <span className="rounded-md bg-secondary px-2 py-1 font-medium text-foreground/80">
            {t('sakala')}: {s.sakalaDays} days
          </span>
          <span className="rounded-md bg-secondary px-2 py-1 font-medium text-foreground/80">
            {t('fee')}: {formatFeeInr(s)}
          </span>
        </div>
        <p className="mt-4 text-sm leading-relaxed text-foreground/80">{s.description}</p>

        <div className="mt-4 text-sm text-muted-foreground">
          <p>{t('svcApplyPrompt')}</p>
        </div>

        <div className="mt-5 flex flex-wrap items-center gap-2">
          <button
            type="button"
            className="rounded-lg bg-[color:var(--gov-blue)] px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:opacity-90"
            onClick={() => void applyToService(s, me)}
          >
            {t('apply')}
          </button>
          <Link
            to="/services"
            search={{ q: undefined, department: s.departmentCode, category: undefined }}
            className="px-3 py-1.5 text-sm text-muted-foreground underline underline-offset-2 transition hover:text-foreground"
          >
            {t('svcMoreFromDept')}
          </Link>
        </div>
      </div>
    </div>
  )
}
