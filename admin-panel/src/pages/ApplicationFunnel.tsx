import { ArrowDown, CheckCircle2, ClipboardList, UserRound, UsersRound } from 'lucide-react'
import { formatNumber, MetricCard, PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { useGovApplicationDropoff, useGovApplicationFunnel } from '../hooks/useGovAdmin'

export function ApplicationFunnelPage() {
  const funnel = useGovApplicationFunnel(true)
  const dropoff = useGovApplicationDropoff(true)
  const data = funnel.data
  const stages = data ? [
    { label: 'Registered', value: data.registeredCitizens, icon: UsersRound, tone: 'blue' as const },
    { label: 'Profile completed', value: data.profileCompletedCitizens, icon: UserRound, tone: 'green' as const },
    { label: 'Citizens submitted', value: data.citizensSubmitted, icon: ClipboardList, tone: 'saffron' as const },
    { label: 'Citizens completed', value: data.citizensCompleted, icon: CheckCircle2, tone: 'navy' as const },
  ] : []

  return <>
    <PageIntro eyebrow="Citizen journey" title="Application funnel" description="See how citizens progress from registration to completed applications. Counts are distinct citizens and remain aggregate-only." />
    <QueryState isLoading={funnel.isLoading} isError={funnel.isError}>
      {data && <>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">{stages.map((stage, index) => <div key={stage.label} className="relative"><MetricCard label={stage.label} value={formatNumber(stage.value)} detail={index === 0 ? 'Citizens registered on SevaOne' : conversion(stages[index - 1].value, stage.value) + '% from previous stage'} tone={stage.tone} />{index < stages.length - 1 && <ArrowDown className="absolute -bottom-4 left-1/2 z-10 h-5 w-5 -translate-x-1/2 text-muted-foreground xl:hidden" />}</div>)}</div>
        <div className="mt-6 rounded-2xl border border-border bg-card p-5 sm:p-6"><div className="flex items-center justify-between gap-3"><div><h2 className="text-base font-semibold text-[color:var(--gov-navy)]">Journey progression</h2><p className="mt-1 text-xs text-muted-foreground">Conversion between each aggregate stage</p></div><span className="rounded-full bg-[color:var(--gov-blue)]/10 px-3 py-1 text-xs font-semibold text-[color:var(--gov-blue)]">{data.scope}</span></div><div className="mt-8 hidden items-center gap-2 xl:flex">{stages.map((stage, index) => <div key={stage.label} className="flex min-w-0 flex-1 items-center gap-2"><div className="min-w-0 flex-1 rounded-2xl bg-secondary/70 p-4"><p className="truncate text-xs font-semibold text-muted-foreground">{stage.label}</p><p className="mt-2 text-xl font-semibold text-[color:var(--gov-navy)]">{formatNumber(stage.value)}</p></div>{index < stages.length - 1 && <div className="w-20 shrink-0 text-center"><ArrowDown className="mx-auto h-4 w-4 rotate-[-90deg] text-[color:var(--gov-blue)]" /><span className="text-[10px] font-bold text-[color:var(--gov-blue)]">{conversion(stages[index].value, stages[index + 1].value)}%</span></div>}</div>)}</div></div>
      </>}
    </QueryState>
    <div className="mt-6"><QueryState isLoading={dropoff.isLoading} isError={dropoff.isError}>{dropoff.data && <div className="overflow-hidden rounded-2xl border border-border bg-card"><div className="border-b border-border p-5"><h2 className="text-base font-semibold text-[color:var(--gov-navy)]">Application drop-off</h2><p className="mt-1 text-xs text-muted-foreground">Open applications by workflow stage and their average time in that stage.</p></div><div className="overflow-x-auto"><table className="w-full min-w-[620px] text-left text-sm"><thead className="bg-secondary/60 text-xs uppercase tracking-wide text-muted-foreground"><tr><th className="px-5 py-3">Open stage</th><th className="px-5 py-3 text-right">Open applications</th><th className="px-5 py-3 text-right">Average time</th></tr></thead><tbody>{dropoff.data.byStatus.map((row) => <tr key={row.status} className="border-t border-border"><td className="px-5 py-4 font-semibold text-[color:var(--gov-navy)]">{row.status.replace(/([a-z])([A-Z])/g, '$1 $2')}</td><td className="px-5 py-4 text-right font-semibold">{formatNumber(row.openCount)}</td><td className="px-5 py-4 text-right text-muted-foreground">{formatHours(row.avgTimeInStatusHours)}</td></tr>)}{dropoff.data.byStatus.length === 0 && <tr><td colSpan={3} className="px-5 py-10 text-center text-sm text-muted-foreground">No open applications in the current scope.</td></tr>}</tbody></table></div></div>}</QueryState></div>
  </>
}

function conversion(previous: number, current: number) { return previous > 0 ? Math.min(100, Math.round((current / previous) * 1000) / 10) : 0 }
function formatHours(value: number) { return value < 24 ? value.toFixed(1) + ' hours' : (value / 24).toFixed(1) + ' days' }
