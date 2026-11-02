import type { LucideIcon } from 'lucide-react'
import {
  Activity,
  CheckCircle2,
  CircleAlert,
  CircleX,
  Clock3,
  Cpu,
  Database,
  Gauge,
  HardDrive,
  HeartPulse,
  MemoryStick,
  RefreshCw,
  Server,
  ShieldCheck,
  Wifi,
} from 'lucide-react'
import { useGovHealth } from '../hooks/useGovAdmin'
import type { GovHealthStatus } from '../types'
import { Button } from '../components/ui/button'
import { Card } from '../components/ui/card'
import { PageIntro, QueryState } from '../components/AnalyticsPrimitives'

const CHECK_META: Record<string, { label: string; detail: string; icon: LucideIcon }> = {
  bff: { label: 'BFF API', detail: 'SevaOne request process', icon: Server },
  postgres: { label: 'Postgres', detail: 'Primary application database', icon: Database },
  redis: { label: 'Redis', detail: 'Sessions and cache layer', icon: MemoryStick },
  kafka: { label: 'Kafka', detail: 'Event delivery backbone', icon: Activity },
  wso2is: { label: 'WSO2 IS', detail: 'Identity and access', icon: ShieldCheck },
  wso2apim: { label: 'WSO2 APIM', detail: 'API gateway and policies', icon: Wifi },
}

const STATUS_META: Record<GovHealthStatus, { label: string; icon: LucideIcon; card: string; iconClass: string }> = {
  Healthy: {
    label: 'Operational',
    icon: CheckCircle2,
    card: 'border-[color:var(--gov-green)]/20 bg-[color:var(--gov-green)]/5',
    iconClass: 'bg-[color:var(--gov-green)]/12 text-[color:var(--gov-green)]',
  },
  Degraded: {
    label: 'Degraded',
    icon: CircleAlert,
    card: 'border-[color:var(--gov-gold)]/35 bg-[color:var(--gov-gold)]/8',
    iconClass: 'bg-[color:var(--gov-gold)]/20 text-amber-700',
  },
  Unhealthy: {
    label: 'Unavailable',
    icon: CircleX,
    card: 'border-red-200 bg-red-50/70',
    iconClass: 'bg-red-100 text-red-700',
  },
}

function formatBytes(value: number) {
  if (!Number.isFinite(value) || value < 0) return 'Unknown'
  if (value < 1024 * 1024) return `${Math.round(value / 1024)} KB`
  if (value < 1024 * 1024 * 1024) return `${(value / (1024 * 1024)).toFixed(1)} MB`
  return `${(value / (1024 * 1024 * 1024)).toFixed(2)} GB`
}

function formatUptime(value: number) {
  const seconds = Math.max(0, Math.floor(value))
  const days = Math.floor(seconds / 86400)
  const hours = Math.floor((seconds % 86400) / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  if (days) return `${days}d ${hours}h`
  if (hours) return `${hours}h ${minutes}m`
  return `${minutes}m`
}

function formatCheckedAt(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? 'Unknown' : new Intl.DateTimeFormat('en-IN', { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}

function statusHeadline(status: GovHealthStatus) {
  if (status === 'Healthy') return 'All systems operational'
  if (status === 'Degraded') return 'Attention needed'
  return 'Service interruption detected'
}

function metricBar(value: number, maximum: number) {
  if (!Number.isFinite(value) || !Number.isFinite(maximum) || maximum <= 0) return 0
  return Math.min(100, Math.max(4, (value / maximum) * 100))
}

export function SystemHealthPage() {
  const query = useGovHealth(true)
  const data = query.data

  return (
    <>
      <PageIntro
        eyebrow="Administration"
        title="System health"
        description="A live operational view of the services that keep SevaOne available to citizens and administrators."
        action={(
          <Button type="button" variant="outline" onClick={() => void query.refetch()} disabled={query.isFetching} className="h-10 rounded-xl bg-white">
            <RefreshCw className={`mr-2 h-4 w-4 ${query.isFetching ? 'animate-spin' : ''}`} />
            {query.isFetching ? 'Checking...' : 'Check now'}
          </Button>
        )}
      />

      <QueryState isLoading={query.isLoading} isError={query.isError}>
        {data && (
          <div className="space-y-6">
            <Card className="relative overflow-hidden rounded-[1.75rem] border-0 bg-[color:var(--gov-navy)] p-6 text-white shadow-[var(--shadow-lift)] sm:p-8">
              <div className="pointer-events-none absolute -right-24 -top-28 h-72 w-72 rounded-full bg-[color:var(--gov-blue)]/35 blur-3xl" />
              <div className="pointer-events-none absolute -bottom-28 left-1/3 h-64 w-64 rounded-full bg-[color:var(--gov-green)]/20 blur-3xl" />
              <div className="relative flex flex-col gap-8 lg:flex-row lg:items-center lg:justify-between">
                <div className="flex items-center gap-5 sm:gap-7">
                  <div className="relative flex h-24 w-24 shrink-0 items-center justify-center rounded-full border border-white/15 bg-white/10">
                    <div className="absolute inset-2 rounded-full border border-[color:var(--gov-green)]/45" />
                    <HeartPulse className="h-9 w-9 text-[color:var(--gov-green)]" />
                  </div>
                  <div>
                    <div className="mb-2 flex flex-wrap items-center gap-2">
                      <span className="rounded-full bg-white/10 px-2.5 py-1 text-[10px] font-bold uppercase tracking-[0.16em] text-slate-300">Live pulse</span>
                      <span className="inline-flex items-center gap-1.5 text-xs text-slate-300"><span className="h-1.5 w-1.5 rounded-full bg-[color:var(--gov-green)]" /> Auto-checks every 30 seconds</span>
                    </div>
                    <h2 className="text-2xl font-semibold tracking-tight sm:text-3xl">{statusHeadline(data.status)}</h2>
                    <p className="mt-2 max-w-xl text-sm leading-6 text-slate-300">{data.status === 'Healthy' ? 'Core infrastructure is responding within the latest check window.' : 'One or more dependencies need attention. Open the service cards below for the exact signal.'}</p>
                  </div>
                </div>
                <div className="flex shrink-0 items-center gap-6 rounded-2xl border border-white/10 bg-white/5 px-5 py-4">
                  <div><p className="text-[10px] font-bold uppercase tracking-[0.15em] text-slate-400">Checks</p><p className="mt-1 text-2xl font-semibold">{data.checks.length}</p></div>
                  <div className="h-10 w-px bg-white/10" />
                  <div><p className="text-[10px] font-bold uppercase tracking-[0.15em] text-slate-400">Last checked</p><p className="mt-1 text-sm font-medium text-slate-200">{formatCheckedAt(data.checkedAt)}</p></div>
                </div>
              </div>
            </Card>

            <section>
              <div className="mb-4 flex items-end justify-between gap-4">
                <div><p className="text-xs font-bold uppercase tracking-[0.16em] text-[color:var(--gov-blue)]">Dependency pulse</p><h2 className="mt-1 text-xl text-[color:var(--gov-navy)]">Service checkpoints</h2></div>
                <div className="hidden items-center gap-1.5 text-xs text-muted-foreground sm:flex"><Clock3 className="h-3.5 w-3.5" /> Measured from the BFF</div>
              </div>
              <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
                {data.checks.map((check) => {
                  const meta = CHECK_META[check.name] ?? { label: check.name, detail: 'Registered health check', icon: Gauge }
                  const status = STATUS_META[check.status]
                  const StatusIcon = status.icon
                  const CheckIcon = meta.icon
                  return (
                    <Card key={check.name} className={`rounded-2xl p-5 transition hover:-translate-y-0.5 hover:shadow-[var(--shadow-soft)] ${status.card}`}>
                      <div className="flex items-start justify-between gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/80 text-[color:var(--gov-navy)] shadow-sm"><CheckIcon className="h-5 w-5" /></div>
                        <div className="flex items-center gap-1.5 text-xs font-semibold"><StatusIcon className="h-4 w-4" /> {status.label}</div>
                      </div>
                      <h3 className="mt-5 text-base font-semibold text-[color:var(--gov-navy)]">{meta.label}</h3>
                      <p className="mt-1 text-xs text-muted-foreground">{meta.detail}</p>
                      <div className="mt-5 flex items-center justify-between gap-3 border-t border-black/5 pt-3 text-xs">
                        <span className="truncate text-muted-foreground">{check.description ?? 'No additional signal'}</span>
                        <span className="shrink-0 font-semibold text-[color:var(--gov-navy)]">{check.durationMs.toFixed(1)} ms</span>
                      </div>
                    </Card>
                  )
                })}
              </div>
            </section>

            <div className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
              <Card className="rounded-2xl p-5 sm:p-6">
                <div className="flex items-start justify-between gap-4">
                  <div><p className="text-xs font-bold uppercase tracking-[0.16em] text-[color:var(--gov-blue)]">Node runtime</p><h2 className="mt-1 text-xl text-[color:var(--gov-navy)]">BFF resource footprint</h2><p className="mt-1 text-sm text-muted-foreground">Safe process-level usage from the serving node.</p></div>
                  <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]"><Server className="h-5 w-5" /></div>
                </div>
                <div className="mt-6 grid gap-4 sm:grid-cols-2">
                  <div className="rounded-xl bg-secondary/60 p-4"><div className="flex items-center gap-2 text-xs text-muted-foreground"><HardDrive className="h-3.5 w-3.5" /> Working set</div><p className="mt-2 text-xl font-semibold text-[color:var(--gov-navy)]">{formatBytes(data.node.workingSetBytes)}</p><div className="mt-3 h-1.5 overflow-hidden rounded-full bg-white"><div className="h-full rounded-full bg-[color:var(--gov-blue)]" style={{ width: `${metricBar(data.node.workingSetBytes, data.node.privateMemoryBytes)}%` }} /></div></div>
                  <div className="rounded-xl bg-secondary/60 p-4"><div className="flex items-center gap-2 text-xs text-muted-foreground"><MemoryStick className="h-3.5 w-3.5" /> Managed heap</div><p className="mt-2 text-xl font-semibold text-[color:var(--gov-navy)]">{formatBytes(data.node.managedHeapBytes)}</p><div className="mt-3 h-1.5 overflow-hidden rounded-full bg-white"><div className="h-full rounded-full bg-[color:var(--gov-green)]" style={{ width: `${metricBar(data.node.managedHeapBytes, data.node.privateMemoryBytes)}%` }} /></div></div>
                </div>
                <div className="mt-4 grid gap-3 sm:grid-cols-3">
                  <div className="rounded-xl border border-border p-4"><p className="text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground">Private memory</p><p className="mt-2 text-sm font-semibold text-[color:var(--gov-navy)]">{formatBytes(data.node.privateMemoryBytes)}</p></div>
                  <div className="rounded-xl border border-border p-4"><p className="text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground">Uptime</p><p className="mt-2 text-sm font-semibold text-[color:var(--gov-navy)]">{formatUptime(data.node.uptimeSeconds)}</p></div>
                  <div className="rounded-xl border border-border p-4"><p className="text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground">Processors</p><p className="mt-2 text-sm font-semibold text-[color:var(--gov-navy)]">{data.node.processorCount} vCPU</p></div>
                </div>
              </Card>

              <Card className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-[color:var(--gov-blue)]/8 via-white to-[color:var(--gov-green)]/8 p-5 sm:p-6">
                <div className="pointer-events-none absolute -right-12 -top-14 h-40 w-40 rounded-full bg-[color:var(--gov-gold)]/15 blur-2xl" />
                <div className="relative"><div className="flex items-center gap-2 text-xs font-bold uppercase tracking-[0.16em] text-[color:var(--gov-blue)]"><Cpu className="h-4 w-4" /> Runtime context</div><h2 className="mt-2 text-xl text-[color:var(--gov-navy)]">Where this signal comes from</h2><p className="mt-2 text-sm leading-6 text-muted-foreground">The BFF performs the dependency probes, so these timings represent the path used by live application traffic.</p></div>
                <div className="relative mt-6 space-y-3">
                  <div className="flex items-center justify-between gap-4 rounded-xl border border-white/80 bg-white/75 px-4 py-3"><span className="text-xs text-muted-foreground">Node</span><span className="max-w-[65%] truncate text-right text-sm font-semibold text-[color:var(--gov-navy)]">{data.node.machineName}</span></div>
                  <div className="flex items-center justify-between gap-4 rounded-xl border border-white/80 bg-white/75 px-4 py-3"><span className="text-xs text-muted-foreground">Runtime</span><span className="text-sm font-semibold text-[color:var(--gov-navy)]">{data.node.runtime}</span></div>
                  <div className="flex items-center justify-between gap-4 rounded-xl border border-white/80 bg-white/75 px-4 py-3"><span className="text-xs text-muted-foreground">Process ID</span><span className="text-sm font-semibold text-[color:var(--gov-navy)]">{data.node.processId}</span></div>
                </div>
                <div className="relative mt-6 flex items-start gap-3 rounded-xl border border-[color:var(--gov-green)]/20 bg-[color:var(--gov-green)]/8 p-4"><ShieldCheck className="mt-0.5 h-4 w-4 shrink-0 text-[color:var(--gov-green)]" /><p className="text-xs leading-5 text-[color:var(--gov-navy)]">Telemetry is limited to infrastructure health and process usage. No citizen or credential data is included.</p></div>
              </Card>
            </div>
          </div>
        )}
      </QueryState>
    </>
  )
}
