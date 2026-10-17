import { useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, Clock3, ExternalLink, FileText, Globe, Info, ListChecks, Loader2, Tag } from 'lucide-react'
import { useRef, useState } from 'react'
import { toast } from 'sonner'
import { api } from '../api'
import { applyToService, trackApplyClickBestEffort } from '../applyFlow'
import { useT } from '../hooks/useT'
import type { CatalogService } from '../lib/catalogHierarchy'
import { useSession } from '../store'
import { BindCitizenModal } from './BindCitizenModal'
import { Button } from './ui/button'

/** Detail view for a service from the Service Catalogue API. The source data is genuinely
 *  sparse -- most rows have a name and little else -- so every section below is rendered
 *  only when the underlying column actually had data. Nothing here is invented.
 *
 *  Apply behaviour differs for operator sessions: assisted applies always mint through
 *  /api/v1/operator/apply-token (binding a citizen first via BindCitizenModal if needed),
 *  while citizen apply still goes through the normal applyToService flow Chat.tsx uses
 *  (createApplyToken, resolved directly, no intermediate handoff page), instead of a plain
 *  applyUrl link: that link was never recorded anywhere (no audit entry,
 *  no discovery.service_apply_track row), which meant an ordinary citizen's apply click from
 *  the Service Catalogue was invisible to both their own activity history and the "how many
 *  people actually applied" count. */
export function HierarchyServiceDetailPanel({
  service,
  departmentCode,
}: {
  readonly service: CatalogService
  readonly departmentCode: string
}) {
  const { lang, t } = useT()
  const { me } = useSession()
  const queryClient = useQueryClient()
  const [busy, setBusy] = useState(false)
  const [showBindModal, setShowBindModal] = useState(false)
  const inFlight = useRef(false)
  const name = (lang === 'kn' && service.nameKannada) || service.name
  const description = (lang === 'kn' && service.descriptionKannada) || service.description
  const documents = (lang === 'kn' && service.documentsKn.length ? service.documentsKn : service.documentsEn) ?? []
  const eligibility = (lang === 'kn' && service.eligibilityKn.length ? service.eligibilityKn : service.eligibilityEn) ?? []
  const procedure = (lang === 'kn' && service.procedureKn.length ? service.procedureKn : service.procedureEn) ?? []
  const hasFee = service.departmentFee != null || service.serviceCharge != null
  const totalFee = (service.departmentFee ?? 0) + (service.serviceCharge ?? 0)
  // The source data's only real "Apply" link is the department's own homepage URL, captured
  // per service row (some departments run more than one portal, e.g. Revenue's AJSK
  // sub-department vs. its main services -- a single department-level URL would be wrong for
  // those). Falls back to English when the Kannada URL is missing rather than showing nothing.
  const applyUrl = ((lang === 'kn' && service.applyUrlKannada) || service.applyUrl)?.trim()

  // Silently absent (not an error toast) for the common case -- almost every visitor here is a
  // citizen, not an operator, and /operator/me 403s for them. Always queried (not just for
  // assisted-integration services) so an operator browsing a plain homepage-fallback service
  // is still correctly recognized as an operator, rather than falling through to the citizen
  // apply path and getting an incorrect login redirect.
  const operatorQ = useQuery({
    queryKey: ['operator', 'session'],
    queryFn: () => api.operatorSession(),
    retry: false,
    staleTime: 30_000,
  })
  const isOperator = !operatorQ.isError && !!operatorQ.data
  const boundCitizen = operatorQ.data?.boundCitizen ?? null

  const redirectAssisted = async (sevaOneUserId: number) => {
    inFlight.current = true
    setBusy(true)
    try {
      const { applyUrl: mintedUrl } = await api.operatorApplyToken(departmentCode, service.serviceCode, sevaOneUserId)
      // noopener may return null for a successfully opened tab.
      window.open(mintedUrl, '_blank', 'noopener,noreferrer')
    } catch {
      toast.error(t('catalog2ApplyError'))
    } finally {
      setBusy(false)
      inFlight.current = false
    }
  }

  const applyNowAssisted = async () => {
    if (inFlight.current) return
    trackApplyClickBestEffort(departmentCode, service.serviceCode, { assistedMode: true })
    if (!boundCitizen) {
      setShowBindModal(true)
      return
    }
    await redirectAssisted(boundCitizen.sevaOneUserId)
  }

  /** Citizen apply -- mirrors Chat.tsx's own handleApply exactly, so this goes through the
   *  same tracked mint (audit log + discovery.service_apply_track) instead of a plain,
   *  unrecorded link. */
  const applyNowCitizen = async () => {
    await applyToService({ serviceCode: service.serviceCode, departmentCode, name: service.name }, me)
  }

  const handleBound = async () => {
    setShowBindModal(false)
    const result = await queryClient.fetchQuery({ queryKey: ['operator', 'session'], queryFn: () => api.operatorSession() })
    if (result.boundCitizen) await redirectAssisted(result.boundCitizen.sevaOneUserId)
  }

  return (
    <div className="rounded-2xl border border-border bg-card p-6 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <h1 className="text-2xl font-bold text-[color:var(--gov-navy)]">{name}</h1>
          <p className="mt-1 text-xs text-muted-foreground">
            {t('serviceCode')}: <span className="font-medium text-foreground">{service.serviceCode}</span>
          </p>
        </div>
        <ApplyButton
          isOperator={isOperator}
          busy={busy}
          onApply={() => void (isOperator ? applyNowAssisted() : applyNowCitizen())}
          applyUrl={applyUrl}
          t={t}
        />
      </div>

      {showBindModal && <BindCitizenModal onBound={() => void handleBound()} onClose={() => setShowBindModal(false)} />}

      {description && <p className="mt-4 text-sm leading-relaxed text-foreground/80">{description}</p>}

      <div className="mt-4 flex flex-wrap gap-2 text-xs">
        {service.isSakalaEnabled != null && (
          <span className="inline-flex items-center gap-1 rounded-md bg-secondary px-2 py-1 font-medium text-foreground/80">
            <Clock3 className="h-3.5 w-3.5" />
            {service.isSakalaEnabled ? t('sakala') : service.sakalaLabelRaw || t('sakala')}
            {service.sakalaDays ? `: ${service.sakalaDays}d` : ''}
          </span>
        )}
        {service.category && (
          <span className="inline-flex items-center gap-1 rounded-md bg-secondary px-2 py-1 font-medium text-foreground/80">
            <Tag className="h-3.5 w-3.5" /> {service.category}
          </span>
        )}
        {service.modeOfService && (
          <span className="inline-flex items-center gap-1 rounded-md bg-secondary px-2 py-1 font-medium text-foreground/80">
            <Globe className="h-3.5 w-3.5" /> {service.modeOfService}
          </span>
        )}
      </div>

      <div className="mt-6 grid gap-6 sm:grid-cols-2">
        {hasFee && (
          <section>
            <h2 className="flex items-center gap-1.5 text-sm font-semibold text-[color:var(--gov-navy)]">
              <Tag className="h-4 w-4" /> {t('catalogFees')}
            </h2>
            <dl className="mt-2 space-y-1.5 text-sm">
              {service.departmentFee != null && (
                <div className="flex justify-between">
                  <dt className="text-muted-foreground">{t('catalogDeptFee')}</dt>
                  <dd className="font-medium text-foreground">₹{service.departmentFee}</dd>
                </div>
              )}
              {service.serviceCharge != null && (
                <div className="flex justify-between">
                  <dt className="text-muted-foreground">{t('catalogServiceCharge')}</dt>
                  <dd className="font-medium text-foreground">₹{service.serviceCharge}</dd>
                </div>
              )}
              {service.departmentFee != null && service.serviceCharge != null && (
                <div className="flex justify-between border-t border-border pt-1.5">
                  <dt className="font-medium text-foreground">{t('catalogTotalFee')}</dt>
                  <dd className="font-semibold text-[color:var(--gov-navy)]">₹{totalFee}</dd>
                </div>
              )}
            </dl>
          </section>
        )}

        {eligibility.length > 0 && (
          <section>
            <h2 className="flex items-center gap-1.5 text-sm font-semibold text-[color:var(--gov-navy)]">
              <CheckCircle2 className="h-4 w-4" /> {t('eligibility')}
            </h2>
            <ul className="mt-2 space-y-1.5 text-sm text-foreground/80">
              {eligibility.map((item) => (
                <li key={item} className="flex items-start gap-2">
                  <CheckCircle2 className="mt-0.5 h-3.5 w-3.5 shrink-0 text-emerald-600" />
                  {item}
                </li>
              ))}
            </ul>
          </section>
        )}

        {documents.length > 0 && (
          <section>
            <h2 className="flex items-center gap-1.5 text-sm font-semibold text-[color:var(--gov-navy)]">
              <FileText className="h-4 w-4" /> {t('documents')}
            </h2>
            <ul className="mt-2 space-y-1.5 text-sm text-foreground/80">
              {documents.map((item) => (
                <li key={item} className="flex items-start gap-2">
                  <FileText className="mt-0.5 h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                  {item}
                </li>
              ))}
            </ul>
          </section>
        )}

        {procedure.length > 0 && (
          <section className="sm:col-span-2">
            <h2 className="flex items-center gap-1.5 text-sm font-semibold text-[color:var(--gov-navy)]">
              <ListChecks className="h-4 w-4" /> {t('catalogHowToApply')}
            </h2>
            <ol className="mt-2 space-y-2 text-sm text-foreground/80">
              {procedure.map((step, i) => (
                <li key={step} className="flex items-start gap-2.5">
                  <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-blue)]/10 text-[11px] font-semibold text-[color:var(--gov-blue)]">
                    {i + 1}
                  </span>
                  {step}
                </li>
              ))}
            </ol>
          </section>
        )}

        {!description && eligibility.length === 0 && documents.length === 0 && procedure.length === 0 && !hasFee && (
          <section className="sm:col-span-2">
            <p className="flex items-center gap-1.5 text-sm text-muted-foreground">
              <Info className="h-4 w-4 shrink-0" /> {t('catalog2LimitedDataNote')}
            </p>
          </section>
        )}
      </div>
    </div>
  )
}

/** The top-right action button: operator-assisted apply, plain citizen apply link, or nothing
 *  when neither applies -- extracted out of the three-way JSX ternary it used to be. */
function ApplyButton({
  isOperator,
  busy,
  onApply,
  applyUrl,
  t,
}: {
  readonly isOperator: boolean
  readonly busy: boolean
  readonly onApply: () => void
  readonly applyUrl: string | undefined
  readonly t: ReturnType<typeof useT>['t']
}) {
  if (isOperator) {
    return (
      <Button disabled={busy} onClick={onApply} className="shrink-0 bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90">
        {busy ? (
          <>
            <Loader2 className="mr-1.5 h-4 w-4 animate-spin" /> {t('catalog2OpeningService')}
          </>
        ) : (
          t('apply')
        )}
      </Button>
    )
  }
  if (applyUrl) {
    // A button, not a plain <a href> anymore -- onApply goes through the same tracked
    // applyToService flow as Chat.tsx's own Apply button (mints via createApplyToken, which
    // records the click), instead of navigating straight to applyUrl with nothing recorded.
    return (
      <Button onClick={onApply} className="shrink-0 bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90">
        {t('apply')}
        <ExternalLink className="ml-1.5 h-4 w-4" />
      </Button>
    )
  }
  return null
}
