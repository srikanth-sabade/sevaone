import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { BrainCircuit, CheckCircle2, CircleAlert, CircleX, Cpu, Database, Gauge, HardDrive, RefreshCw, ShieldAlert, Sparkles, Thermometer } from 'lucide-react'
import { toast } from 'sonner'
import { govAdminApi } from '../api'
import { Modal } from '../components/Modal'
import { QueryState, PageIntro } from '../components/AnalyticsPrimitives'
import { Button } from '../components/ui/button'
import { Card } from '../components/ui/card'
import { GovAiChatThread } from '../components/GovAiChatThread'
import { useGovAiStatus } from '../hooks/useGovAdmin'
import type { AiBackendStatus, GovAiStatus } from '../types'

function statusMeta(status: AiBackendStatus) {
  if (status === 'Healthy') return { label: 'Operational', icon: CheckCircle2, card: 'border-[color:var(--gov-green)]/20 bg-[color:var(--gov-green)]/5', tone: 'text-[color:var(--gov-green)]' }
  if (status === 'Degraded') return { label: 'Degraded', icon: CircleAlert, card: 'border-[color:var(--gov-gold)]/30 bg-[color:var(--gov-gold)]/8', tone: 'text-amber-700' }
  return { label: 'Unavailable', icon: CircleX, card: 'border-red-200 bg-red-50/70', tone: 'text-red-700' }
}

function checkedAt(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? 'Unknown' : new Intl.DateTimeFormat('en-IN', { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}

export function AIStatusPage() {
  const query = useGovAiStatus(true)
  const data = query.data
  const meta = statusMeta(data?.status ?? 'Unhealthy')
  const StatusIcon = meta.icon

  return <>
    <PageIntro eyebrow="AI operations" title="AI backend status" description="Read-only visibility into the AI discovery service running on the GPU host. No ingestion or index changes happen from this page." action={<Button type="button" variant="outline" onClick={() => void query.refetch()} disabled={query.isFetching} className="h-10 rounded-xl bg-white"><RefreshCw className={`mr-2 h-4 w-4 ${query.isFetching ? 'animate-spin' : ''}`} />{query.isFetching ? 'Checking...' : 'Check now'}</Button>} />
    <QueryState isLoading={query.isLoading} isError={query.isError}>
      {data && <div className="space-y-6">
        <Card className={`relative overflow-hidden rounded-[1.75rem] p-6 shadow-[var(--shadow-soft)] sm:p-8 ${meta.card}`}><div className="pointer-events-none absolute -right-16 -top-20 h-56 w-56 rounded-full bg-[color:var(--gov-blue)]/10 blur-3xl" /><div className="relative flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between"><div className="flex items-center gap-5"><div className={`flex h-16 w-16 shrink-0 items-center justify-center rounded-2xl bg-white shadow-sm ${meta.tone}`}><BrainCircuit className="h-8 w-8" /></div><div><div className={`flex items-center gap-2 text-xs font-bold uppercase tracking-[0.16em] ${meta.tone}`}><StatusIcon className="h-4 w-4" /> {meta.label}</div><h2 className="mt-2 text-2xl font-semibold tracking-tight text-[color:var(--gov-navy)]">AI discovery is {data.status.toLowerCase()}</h2><p className="mt-1 text-sm text-muted-foreground">Last checked {checkedAt(data.checkedAt)}</p></div></div><div className="rounded-2xl border border-white/70 bg-white/75 px-5 py-4 text-sm shadow-sm"><p className="text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground">Service host</p><p className="mt-1 font-semibold text-[color:var(--gov-navy)]">GPU server · port 8000</p></div></div></Card>
        <div className="grid gap-4 md:grid-cols-2">
          <SignalCard icon={Gauge} label="AI API" value={data.health?.status === 'ok' ? 'Healthy' : data.health?.status ?? 'Unavailable'} detail={data.health?.service ?? 'The AI health endpoint did not respond.'} healthy={data.health !== null} />
          <SignalCard icon={Database} label="Index readiness" value={data.readiness?.status ?? 'Unavailable'} detail={data.readiness ? `Source: ${data.readiness.activeKnowledgeSource}` : 'The readiness endpoint did not respond.'} healthy={data.readiness !== null} />
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <InfoCard icon={HardDrive} label="Services indexed" value={data.readiness ? data.readiness.totalServicesIndexed.toLocaleString('en-IN') : 'Unavailable'} detail={data.readiness?.serviceIndexPrefix ?? 'No index signal'} />
          <InfoCard icon={Sparkles} label="Embedding dimension" value={data.readiness ? String(data.readiness.embeddingDimension) : 'Unavailable'} detail="Reported by the AI readiness endpoint" />
        </div>
        {data.telemetry ? <TelemetryPanel telemetry={data.telemetry} /> : <Card className="rounded-2xl border-[color:var(--gov-gold)]/30 bg-[color:var(--gov-gold)]/8 p-5 sm:p-6"><div className="flex items-start gap-4"><ShieldAlert className="mt-0.5 h-5 w-5 shrink-0 text-amber-700" /><div><h2 className="text-base font-semibold text-[color:var(--gov-navy)]">Resource telemetry is unavailable</h2><p className="mt-1 max-w-3xl text-sm leading-6 text-muted-foreground">{data.telemetryNote}</p></div></div></Card>}
      </div>}
    </QueryState>
  </>
}

function TelemetryPanel({ telemetry }: { readonly telemetry: NonNullable<GovAiStatus['telemetry']> }) {
  const gpu = telemetry.gpus[0]
  return <Card className="overflow-hidden rounded-[1.75rem] border-0 bg-white p-6 shadow-[var(--shadow-soft)] sm:p-8">
    <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div><div className="flex items-center gap-2 text-xs font-bold uppercase tracking-[0.16em] text-[color:var(--gov-blue)]"><ActivityDot /> Live resources</div><h2 className="mt-2 text-xl font-semibold tracking-tight text-[color:var(--gov-navy)]">GPU host telemetry</h2><p className="mt-1 text-sm text-muted-foreground">Read-only signals from {telemetry.host}</p></div>
      <p className="text-xs text-muted-foreground">Collected {checkedAt(telemetry.collectedAt)}</p>
    </div>
    <div className="mt-6 grid gap-4 md:grid-cols-3">
      <TelemetryGauge icon={Cpu} label="CPU usage" value={telemetry.cpu.usagePercent} detail={`${telemetry.cpu.cores} logical cores${telemetry.cpu.load1 === null ? '' : ` · load ${telemetry.cpu.load1.toFixed(2)}`} `} />
      <TelemetryGauge icon={Database} label="Memory usage" value={telemetry.memory.usagePercent} detail={`${formatBytes(telemetry.memory.usedBytes)} used · ${formatBytes(telemetry.memory.availableBytes)} available`} />
      <TelemetryGauge icon={HardDrive} label={`Disk usage ${telemetry.disk.mount}`} value={telemetry.disk.usagePercent} detail={`${formatBytes(telemetry.disk.usedBytes)} used · ${formatBytes(telemetry.disk.availableBytes)} free`} />
    </div>
    {gpu ? <div className="mt-4 rounded-2xl border border-slate-200 bg-slate-50/80 p-5"><div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between"><div className="flex items-center gap-3"><div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white text-[color:var(--gov-blue)] shadow-sm"><Gauge className="h-5 w-5" /></div><div><p className="text-xs font-bold uppercase tracking-[0.14em] text-muted-foreground">GPU</p><p className="mt-1 font-semibold text-[color:var(--gov-navy)]">{gpu.name}</p></div></div><div className="flex items-center gap-2 text-sm font-semibold text-[color:var(--gov-navy)]"><Thermometer className="h-4 w-4 text-amber-600" />{gpu.temperatureCelsius === null ? 'Temperature unavailable' : `${gpu.temperatureCelsius.toFixed(0)}°C`}</div></div><div className="mt-5 grid gap-4 sm:grid-cols-2"><MetricBar label="Compute utilization" value={gpu.utilizationPercent} /><MetricBar label="VRAM utilization" value={gpu.memoryUsagePercent} detail={`${formatBytes(gpu.memoryUsedBytes)} / ${formatBytes(gpu.memoryTotalBytes)}`} /></div></div> : <div className="mt-4 rounded-2xl border border-slate-200 bg-slate-50/80 p-5 text-sm text-muted-foreground">No NVIDIA GPU was reported by the monitor sidecar.</div>}
  </Card>
}

function TelemetryGauge({ icon: Icon, label, value, detail }: { readonly icon: typeof Cpu; readonly label: string; readonly value: number; readonly detail: string }) {
  return <div className="rounded-2xl border border-slate-200 bg-slate-50/80 p-5"><div className="flex items-center justify-between gap-3"><div className="flex items-center gap-2 text-xs font-semibold text-muted-foreground"><Icon className="h-4 w-4 text-[color:var(--gov-blue)]" />{label}</div><span className="text-lg font-semibold text-[color:var(--gov-navy)]">{value.toFixed(1)}%</span></div><div className="mt-4 h-2 overflow-hidden rounded-full bg-slate-200"><div className="h-full rounded-full bg-[color:var(--gov-blue)] transition-[width]" style={{ width: `${Math.min(100, Math.max(0, value))}%` }} /></div><p className="mt-3 text-xs leading-5 text-muted-foreground">{detail}</p></div>
}

function MetricBar({ label, value, detail }: { readonly label: string; readonly value: number; readonly detail?: string }) {
  return <div><div className="flex items-center justify-between gap-3 text-xs"><span className="font-semibold text-[color:var(--gov-navy)]">{label}</span><span className="font-semibold text-muted-foreground">{value.toFixed(1)}%</span></div><div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-200"><div className="h-full rounded-full bg-[color:var(--gov-gold)]" style={{ width: `${Math.min(100, Math.max(0, value))}%` }} /></div>{detail && <p className="mt-2 text-xs text-muted-foreground">{detail}</p>}</div>
}

function ActivityDot() { return <span className="relative flex h-2.5 w-2.5"><span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-[color:var(--gov-green)] opacity-60" /><span className="relative inline-flex h-2.5 w-2.5 rounded-full bg-[color:var(--gov-green)]" /></span> }

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  const units = ['KB', 'MB', 'GB', 'TB']
  let value = bytes
  let unit = units[0]
  for (const nextUnit of units) { value /= 1024; unit = nextUnit; if (value < 1024 || nextUnit === units.at(-1)) break }
  return `${value.toFixed(value >= 10 ? 0 : 1)} ${unit}`
}

export function AIReingestPage() {
  const query = useGovAiStatus(true)
  const queryClient = useQueryClient()
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const data = query.data

  async function confirmReingest() {
    setBusy(true)
    try {
      const result = await govAdminApi.reingestAi(true)
      setConfirmOpen(false)
      await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'ai-status'] })
      toast.success(result.message || 'AI reingestion started.')
    } catch (error) { toast.error(error instanceof Error ? error.message : 'Could not start AI reingestion.') }
    finally { setBusy(false) }
  }

  return <>
    <PageIntro eyebrow="AI operations" title="Reingest catalogue" description="Rebuild the AI discovery index only after reviewing the source and confirming the operation." />
    <div className="space-y-6">
      <Card className="relative overflow-hidden rounded-[1.75rem] border-0 bg-[color:var(--gov-navy)] p-6 text-white shadow-[var(--shadow-lift)] sm:p-8"><div className="pointer-events-none absolute -right-20 -top-24 h-64 w-64 rounded-full bg-[color:var(--gov-blue)]/30 blur-3xl" /><div className="relative flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between"><div className="max-w-2xl"><div className="flex items-center gap-2 text-xs font-bold uppercase tracking-[0.16em] text-[color:var(--gov-gold)]"><BrainCircuit className="h-4 w-4" /> Controlled operation</div><h2 className="mt-3 text-2xl font-semibold tracking-tight sm:text-3xl">Refresh the AI knowledge index</h2><p className="mt-2 text-sm leading-6 text-slate-300">This action calls the existing AI refresh API. It may take time while source records are normalized and vector indexes are rebuilt.</p></div><div className="flex shrink-0 items-center gap-4 rounded-2xl border border-white/10 bg-white/5 px-5 py-4"><div><p className="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-400">Current index</p><p className="mt-1 text-xl font-semibold">{data?.readiness?.totalServicesIndexed.toLocaleString('en-IN') ?? '—'}</p></div><div className="h-10 w-px bg-white/10" /><div><p className="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-400">Source</p><p className="mt-1 max-w-[150px] truncate text-sm text-slate-200">{data?.readiness?.activeKnowledgeSource ?? 'Not available'}</p></div></div></div></Card>
      <div className="grid gap-4 md:grid-cols-3"><Step number="01" title="Review" detail="Check AI health, readiness, and the configured source." /><Step number="02" title="Confirm" detail="A second confirmation is required before any request is sent." /><Step number="03" title="Rebuild" detail="The existing AI service performs its configured refresh." /></div>
      <Card className="rounded-2xl border-[color:var(--gov-gold)]/30 bg-[color:var(--gov-gold)]/8 p-5 sm:p-6"><div className="flex items-start gap-4"><ShieldAlert className="mt-0.5 h-5 w-5 shrink-0 text-amber-700" /><div><h2 className="text-base font-semibold text-[color:var(--gov-navy)]">Important source note</h2><p className="mt-1 text-sm leading-6 text-muted-foreground">The current AI API refreshes its configured knowledge source. It does not yet accept a live Postgres catalogue payload, so this action is not silently presented as a direct catalogue export.</p></div></div></Card>
      <div className="flex justify-end"><Button type="button" onClick={() => setConfirmOpen(true)} disabled={busy} className="h-11 rounded-xl bg-[color:var(--gov-blue)] px-5 text-white hover:bg-[color:var(--gov-blue-dark)]"><RefreshCw className="mr-2 h-4 w-4" /> Review and reingest</Button></div>
    </div>
    <Modal open={confirmOpen} title="Confirm AI reingestion" description="No AI request has been sent yet. This confirmation is the final step before starting the existing refresh operation." onClose={() => { if (!busy) setConfirmOpen(false) }}>
      <div className="space-y-5"><div className="rounded-2xl border border-red-200 bg-red-50 p-5"><div className="flex items-start gap-3"><ShieldAlert className="mt-0.5 h-5 w-5 shrink-0 text-red-700" /><div><p className="font-semibold text-red-900">This may rebuild the AI index and use GPU resources.</p><p className="mt-2 text-sm leading-6 text-red-800">Confirm only if the configured source is ready and you want the AI service to refresh now. The admin panel will send `{`{ confirmed: true }`}` only after you click the button below.</p></div></div></div><div className="flex justify-end gap-3"><Button type="button" variant="outline" disabled={busy} onClick={() => setConfirmOpen(false)} className="rounded-xl">Cancel</Button><Button type="button" disabled={busy} onClick={() => void confirmReingest()} className="rounded-xl bg-red-700 text-white hover:bg-red-800">{busy ? 'Starting...' : 'Confirm and start'}</Button></div></div>
    </Modal>
  </>
}

export function AIAssistantPage() {
  return <div className="flex h-full min-h-0 flex-col">
    <div className="shrink-0"><PageIntro eyebrow="AI operations" title="Government AI assistant" description="Ask questions about live SevaOne aggregate reports. Answers are scoped to your signed-in government access and never use the catalogue index." /></div>
    <Card className="flex min-h-[420px] flex-1 flex-col overflow-hidden rounded-[1.75rem] p-4 shadow-[var(--shadow-soft)] sm:p-6">
      <GovAiChatThread />
    </Card>
  </div>
}

function SignalCard({ icon: Icon, label, value, detail, healthy }: { readonly icon: typeof Gauge; readonly label: string; readonly value: string; readonly detail: string; readonly healthy: boolean }) { return <Card className={`rounded-2xl p-5 ${healthy ? 'border-[color:var(--gov-green)]/20 bg-[color:var(--gov-green)]/5' : 'border-red-200 bg-red-50/70'}`}><div className="flex items-start justify-between gap-3"><div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white text-[color:var(--gov-navy)] shadow-sm"><Icon className="h-5 w-5" /></div><span className={`text-xs font-semibold ${healthy ? 'text-[color:var(--gov-green)]' : 'text-red-700'}`}>{healthy ? 'Available' : 'Unavailable'}</span></div><h3 className="mt-5 text-base font-semibold text-[color:var(--gov-navy)]">{label}</h3><p className="mt-1 text-lg font-semibold text-[color:var(--gov-navy)]">{value}</p><p className="mt-1 text-xs leading-5 text-muted-foreground">{detail}</p></Card> }
function InfoCard({ icon: Icon, label, value, detail }: { readonly icon: typeof HardDrive; readonly label: string; readonly value: string; readonly detail: string }) { return <Card className="rounded-2xl p-5"><div className="flex items-center gap-2 text-xs font-semibold text-muted-foreground"><Icon className="h-4 w-4 text-[color:var(--gov-blue)]" /> {label}</div><p className="mt-3 text-2xl font-semibold text-[color:var(--gov-navy)]">{value}</p><p className="mt-1 truncate text-xs text-muted-foreground">{detail}</p></Card> }
function Step({ number, title, detail }: { readonly number: string; readonly title: string; readonly detail: string }) { return <Card className="rounded-2xl p-5"><p className="font-mono text-xs font-bold text-[color:var(--gov-blue)]">{number}</p><h2 className="mt-4 text-base font-semibold text-[color:var(--gov-navy)]">{title}</h2><p className="mt-1 text-sm leading-6 text-muted-foreground">{detail}</p></Card> }
