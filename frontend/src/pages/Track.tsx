import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { api } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { useT } from '../hooks/useT'
import { serviceName } from '../lib/catalogKn'
import { statusTone } from '../lib/statusTone'
import { useSession } from '../store'
import type { SakalaComplaint, TrackApiResult } from '../types'

function milestoneDotClass(complete: boolean, isSecond: boolean): string {
  if (complete) return 'bg-emerald-500 ring-4 ring-emerald-500/15'
  if (isSecond) return 'bg-[color:var(--gov-blue)] ring-4 ring-[color:var(--gov-blue)]/15'
  return 'bg-card ring-2 ring-border'
}

function formatProcessTime(startAt: string, endAt?: string): string {
  const elapsedMs = Math.max(0, new Date(endAt ?? Date.now()).getTime() - new Date(startAt).getTime())
  const totalHours = Math.floor(elapsedMs / (1000 * 60 * 60))
  const days = Math.floor(totalHours / 24)
  const hours = totalHours % 24
  const hourWord = hours === 1 ? 'hour' : 'hours'
  if (days === 0) return `${hours} ${hourWord}`
  const hoursPart = hours ? ` ${hours} ${hourWord}` : ''
  const dayWord = days === 1 ? 'day' : 'days'
  return `${days} ${dayWord}${hoursPart}`
}

/** Sakala's dates come back as dd/MM/yyyy strings already -- just display as-is rather than
 *  round-tripping through Date parsing, which assumes an unambiguous format. */
function SakalaComplaintRow({ label, complaint }: { readonly label: string; readonly complaint: SakalaComplaint }) {
  return (
    <div className="rounded-2xl border border-border bg-secondary/30 p-4">
      <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-1 font-mono text-sm text-[color:var(--gov-navy)]">{complaint.complaintNo}</p>
      <dl className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-xs text-muted-foreground">
        {complaint.complaintDate && (
          <>
            <dt>Filed</dt>
            <dd>{complaint.complaintDate}</dd>
          </>
        )}
        {complaint.dueDate && (
          <>
            <dt>Due</dt>
            <dd>{complaint.dueDate}</dd>
          </>
        )}
        {complaint.closedDate && (
          <>
            <dt>Closed</dt>
            <dd>{complaint.closedDate}</dd>
          </>
        )}
        {complaint.daysPending != null && (
          <>
            <dt>Days pending</dt>
            <dd>{complaint.daysPending}</dd>
          </>
        )}
      </dl>
      {complaint.statusDescription && (
        <span className={`mt-3 inline-flex rounded-full border px-2.5 py-0.5 text-xs font-semibold ${statusTone(complaint.statusDescription)}`}>
          {complaint.statusDescription}
        </span>
      )}
    </div>
  )
}

export function TrackPage() {
  const { lang, t } = useT()
  const { me } = useSession()
  const [ref, setRef] = useState('')
  const [result, setResult] = useState<TrackApiResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  /** Only set once a public lookup resolves to a SevaOne-owned application — avoids firing the
   *  authenticated detail call for a Sakala-sourced result (which was never a SevaOne
   *  application to begin with) or on every keystroke. */
  const [trackedRef, setTrackedRef] = useState<string | null>(null)

  // Public track() only ever returns a flat status for a SevaOne-owned reference. The full
  // step-by-step timeline is citizen-only data — moved here from the Dashboard, so it's fetched
  // the same way that page fetched it (api.application), just gated on a successful public
  // lookup first rather than a route search param. Silently falls back to the flat card below
  // when signed out, or when this reference isn't the signed-in citizen's own application.
  const detail = useQuery({
    queryKey: ['app', trackedRef],
    queryFn: () => api.application(trackedRef!),
    enabled: !!trackedRef && !!me,
    retry: false,
  })

  // Applications handed to a department's own portal: ask the department where it stands, once when the page
  // opens and again on request. The server limits how often the department is really asked.
  const queryClient = useQueryClient()
  const [refreshing, setRefreshing] = useState(false)
  const autoRefreshed = useRef<string | null>(null)
  const refreshStatus = async (reference: string) => {
    setRefreshing(true)
    try {
      queryClient.setQueryData(['app', reference], await api.refreshApplicationStatus(reference))
    } catch {
      /* keep showing what we already have */
    } finally {
      setRefreshing(false)
    }
  }
  useEffect(() => {
    const reference = detail.data?.sevaReference
    if (reference && detail.data?.statusCheckAvailable && autoRefreshed.current !== reference) {
      autoRefreshed.current = reference
      void refreshStatus(reference)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [detail.data?.sevaReference, detail.data?.statusCheckAvailable])

  return (
    <div className="mx-auto max-w-7xl px-4 py-10">
      <h1 className="text-3xl font-bold text-[color:var(--gov-navy)]">{t('trackTitle')}</h1>
      <Breadcrumbs />
      <p className="mt-3 text-sm text-muted-foreground">{t('trackHint')}</p>
      <form
        className="mt-6 flex gap-2"
        onSubmit={(e) => {
          e.preventDefault()
          void (async () => {
            try {
              setError(null)
              const trackResult = await api.track(ref)
              setResult(trackResult)
              setTrackedRef(trackResult.source === 'sevaone' ? ref : null)
            } catch {
              setResult(null)
              setTrackedRef(null)
              setError('Not submitted')
            }
          })()
        }}
      >
        <input
          className="flex-1 rounded-lg border border-input bg-background px-3 py-2 text-sm"
          value={ref}
          onChange={(e) => setRef(e.target.value)}
          placeholder="SVK2026… or your Sakala GSC number"
        />
        <button className="rounded-lg bg-[color:var(--gov-blue)] px-4 py-2 text-sm font-medium text-white" type="submit">
          {t('trackBtn')}
        </button>
      </form>
      {error && <p className="mt-3 text-sm text-red-700">{error}</p>}

      {result?.source === 'sakala' && result.sakala && (
        <div className="mt-6 overflow-hidden rounded-3xl border border-[color:var(--gov-blue)]/25 bg-card shadow-[var(--shadow-soft)]">
          <div className="flex flex-col gap-4 border-b border-border/70 bg-[linear-gradient(135deg,color-mix(in_oklab,var(--gov-blue)_8%,white),white_55%)] px-5 py-5">
            <div className="min-w-0">
              <p className="text-[11px] font-semibold uppercase tracking-[0.14em] text-[color:var(--gov-blue)]">Sakala acknowledgement</p>
              <h2 className="mt-1 text-xl font-semibold text-[color:var(--gov-navy)]">{result.sakala.serviceName}</h2>
              <p className="mt-1 font-mono text-xs text-muted-foreground">
                {result.sakala.gscNo} · {result.sakala.institutionName}
              </p>
              <p className="mt-2 text-xs text-muted-foreground">
                Applicant {result.sakala.applicantName}
                {result.sakala.sakalaDate ? ` · Applied ${result.sakala.sakalaDate}` : ''}
                {result.sakala.dueDate ? ` · Due ${result.sakala.dueDate}` : ''}
              </p>
            </div>
            <span className={`inline-flex w-fit shrink-0 rounded-full border px-3 py-1 text-xs font-semibold ${statusTone(result.sakala.statusDescription)}`}>
              {result.sakala.statusDescription}
            </span>
          </div>
          {(result.sakala.firstComplaint || result.sakala.secondComplaint) && (
            <div className="grid gap-4 p-5 sm:grid-cols-2">
              {result.sakala.firstComplaint && <SakalaComplaintRow label="First complaint" complaint={result.sakala.firstComplaint} />}
              {result.sakala.secondComplaint && <SakalaComplaintRow label="Second complaint" complaint={result.sakala.secondComplaint} />}
            </div>
          )}
          <p className="border-t border-border/70 px-5 py-3 text-xs text-muted-foreground">
            Live status from Karnataka's Sakala tracking system, looked up by your acknowledgement number.
          </p>
        </div>
      )}

      {result?.source === 'sevaone' &&
        (() => {
          if (!(result.sevaOne && detail.data)) {
            return result.sevaOne ? (
              <div className="mt-6 rounded-2xl border border-emerald-200 bg-emerald-50 p-4 shadow-sm">
                <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">{t('trackUpdate')}</p>
                <p className="mt-1 text-lg font-semibold text-emerald-900">{t('trackStatusSubmitted')}</p>
                <p className="mt-1 text-sm text-emerald-800">{t('trackSignInHint')}</p>
              </div>
            ) : null
          }
          return (
        <div
          className="mt-6 overflow-hidden rounded-3xl border border-[color:var(--gov-blue)]/25 bg-card shadow-[var(--shadow-soft)]"
          role="region"
          aria-label={t('dashTracking')}
        >
          <div className="flex flex-col gap-4 border-b border-border/70 bg-[linear-gradient(135deg,color-mix(in_oklab,var(--gov-blue)_8%,white),white_55%)] px-5 py-5">
            <div className="min-w-0">
              <p className="text-[11px] font-semibold uppercase tracking-[0.14em] text-[color:var(--gov-blue)]">
                {t('dashTracking')}
              </p>
              <h2 className="mt-1 text-xl font-semibold text-[color:var(--gov-navy)]">
                {serviceName(lang, detail.data.serviceCode, detail.data.serviceName)}
              </h2>
              <p className="mt-1 font-mono text-xs text-muted-foreground">
                {detail.data.sevaReference}
                {detail.data.departmentReference
                  ? ` · ${t('dashDeptAck')} ${detail.data.departmentReference}`
                  : ''}
              </p>
              <p className="mt-2 text-xs text-muted-foreground">
                Submitted {new Date(detail.data.submittedAt).toLocaleString()} · Sakala due{' '}
                {new Date(detail.data.sakalaDueAt).toLocaleDateString()}
              </p>
              {detail.data.statusCheckAvailable && (
                <button
                  type="button"
                  disabled={refreshing}
                  onClick={() => void refreshStatus(detail.data!.sevaReference)}
                  className="mt-3 mr-2 inline-flex items-center rounded-md border border-border px-3 py-1.5 text-xs font-semibold text-[color:var(--gov-navy)] hover:bg-muted disabled:opacity-60"
                >
                  {refreshing ? 'Checking with the department…' : 'Refresh status'}
                </button>
              )}
              {detail.data.acknowledgementAvailable && (
                <a
                  href={`/api/v1/applications/${encodeURIComponent(detail.data.sevaReference)}/acknowledgement`}
                  download
                  className="mt-3 inline-flex items-center rounded-md border border-[color:var(--gov-blue)]/40 px-3 py-1.5 text-xs font-semibold text-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/8"
                >
                  Download acknowledgement
                </a>
              )}
            </div>
            <span
              className={`inline-flex w-fit shrink-0 rounded-full border px-3 py-1 text-xs font-semibold ${statusTone(detail.data.status)}`}
            >
              {detail.data.status}
            </span>
          </div>
          {(() => {
            const approval = detail.data.timeline.find((step) => step.status.toLowerCase().includes('approved'))
            const completion = detail.data.timeline.find((step) => {
              const status = step.status.toLowerCase()
              return status.includes('completed') || status.includes('issued')
            })
            const milestones = [
              {
                label: 'Date of application',
                value: new Date(detail.data.submittedAt).toLocaleDateString(),
                note: 'Application submitted successfully',
                complete: true,
              },
              {
                label: 'Process time',
                value: formatProcessTime(detail.data.submittedAt, completion?.at),
                note: completion ? 'Application processing completed' : `Currently ${detail.data.status.toLowerCase()}`,
                complete: Boolean(completion),
              },
              {
                label: 'Approval date',
                value: approval ? new Date(approval.at).toLocaleDateString() : 'Awaiting approval',
                note: approval?.note ?? 'The department has not approved this application yet',
                complete: Boolean(approval),
              },
              {
                label: 'Completion date',
                value: completion ? new Date(completion.at).toLocaleDateString() : 'Not completed',
                note: completion?.note ?? 'The final document or service is still pending',
                complete: Boolean(completion),
              },
            ]
            return (
              <ol className="relative space-y-0 px-5 py-6">
                {milestones.map((milestone, index) => (
                  <li key={milestone.label} className="relative flex gap-4 pb-7 last:pb-0">
                    <div className="relative flex w-4 shrink-0 flex-col items-center pt-1">
                      <span className={`h-2.5 w-2.5 rounded-full ${milestoneDotClass(milestone.complete, index === 1)}`} />
                      {index < milestones.length - 1 ? <span className="mt-1 w-px flex-1 bg-border" /> : null}
                    </div>
                    <div className="min-w-0 pb-1">
                      <div className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">{milestone.label}</div>
                      <div className="mt-1 text-base font-semibold text-[color:var(--gov-navy)]">{milestone.value}</div>
                      <div className="mt-1 text-sm text-muted-foreground">{milestone.note}</div>
                    </div>
                  </li>
                ))}
              </ol>
            )
          })()}
        </div>
          )
        })()}
    </div>
  )
}
