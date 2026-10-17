import { type FormEvent, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, Clock3, Megaphone, Plus, Save, SquareArrowOutUpRight } from 'lucide-react'
import { toast } from 'sonner'
import { govAdminApi } from '../api'
import { useGovServiceNotice, useGovServiceNoticeHistory } from '../hooks/useGovAdmin'
import type { ServiceNotice, ServiceNoticeSeverity } from '../types'
import { Card } from '../components/ui/card'
import { Button } from '../components/ui/button'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { Modal } from '../components/Modal'
import { PageIntro, QueryState } from '../components/AnalyticsPrimitives'

const SEVERITIES: { id: ServiceNoticeSeverity; label: string; description: string }[] = [
  { id: 'info', label: 'Info', description: 'Routine platform updates or guidance.' },
  { id: 'warning', label: 'Warning', description: 'Reduced availability or planned maintenance.' },
  { id: 'critical', label: 'Critical', description: 'An active outage or urgent service interruption.' },
]

function localInputValue(value: string | null) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function isoValue(value: string) {
  return value ? new Date(value).toISOString() : null
}

function historyDate(value: string) {
  return new Intl.DateTimeFormat('en-IN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

function previewClasses(severity: ServiceNoticeSeverity) {
  return {
    info: 'border-[color:var(--gov-blue)]/25 bg-[color:var(--gov-blue)]/8 text-[color:var(--gov-navy)]',
    warning: 'border-[color:var(--saffron)]/35 bg-[color:var(--saffron)]/10 text-[color:var(--gov-navy)]',
    critical: 'border-red-300 bg-red-50 text-red-950',
  }[severity]
}

export function ServiceNoticesPage() {
  const queryClient = useQueryClient()
  const query = useGovServiceNotice(true)
  const historyQuery = useGovServiceNoticeHistory(true)
  const activeNotice = query.data?.isActive ? query.data : null
  const [modalMode, setModalMode] = useState<'create' | 'edit' | null>(null)
  const [confirmStop, setConfirmStop] = useState(false)
  const [title, setTitle] = useState('')
  const [message, setMessage] = useState('')
  const [severity, setSeverity] = useState<ServiceNoticeSeverity>('info')
  const [isActive, setIsActive] = useState(false)
  const [startsAt, setStartsAt] = useState('')
  const [endsAt, setEndsAt] = useState('')
  const [busy, setBusy] = useState(false)

  function openCreate() {
    setTitle('')
    setMessage('')
    setSeverity('info')
    setIsActive(false)
    setStartsAt('')
    setEndsAt('')
    setModalMode('create')
  }

  function openEdit(notice: ServiceNotice) {
    setTitle(notice.title)
    setMessage(notice.message)
    setSeverity(notice.severity)
    setIsActive(notice.isActive)
    setStartsAt(localInputValue(notice.startsAt))
    setEndsAt(localInputValue(notice.endsAt))
    setModalMode('edit')
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!title.trim() || !message.trim()) {
      toast.error('Enter a notice title and message.')
      return
    }

    const start = isoValue(startsAt)
    const end = isoValue(endsAt)
    if (start && end && new Date(end) <= new Date(start)) {
      toast.error('The end time must be later than the start time.')
      return
    }

    setBusy(true)
    try {
      await govAdminApi.saveServiceNotice({ title: title.trim(), message: message.trim(), severity, isActive, startsAt: start, endsAt: end })
      setModalMode(null)
      await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'service-notice'] })
      await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'service-notice-history'] })
      toast.success(isActive ? 'Service notice published on SevaOne.' : 'Service notice saved as a draft.')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Could not save the service notice.')
    } finally {
      setBusy(false)
    }
  }

  async function stopNotice() {
    setBusy(true)
    try {
      await govAdminApi.stopServiceNotice()
      setConfirmStop(false)
      await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'service-notice'] })
      await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'service-notice-history'] })
      toast.success('Service notice stopped. Its record remains in history.')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Could not stop the service notice.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <PageIntro
        eyebrow="Administration"
        title="Service notices"
        description="Keep a clear operational record of maintenance, outages, and platform updates shared with SevaOne visitors."
        action={<Button type="button" onClick={openCreate} className="h-11 rounded-xl bg-[color:var(--gov-blue)] px-5 text-white shadow-sm hover:bg-[color:var(--gov-blue-dark)]"><Plus className="mr-2 h-4 w-4" /> Create new notice</Button>}
      />

      <QueryState isLoading={historyQuery.isLoading} isError={historyQuery.isError}>
        <Card className="rounded-2xl p-6">
          <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
            <div className="flex items-start gap-3">
              <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-[color:var(--gov-navy)]/8 text-[color:var(--gov-navy)]"><Clock3 className="h-5 w-5" /></div>
              <div><h2 className="text-lg font-semibold text-[color:var(--gov-navy)]">Previous notices</h2><p className="mt-1 text-sm leading-5 text-muted-foreground">A chronological record of every notice version that was saved or stopped.</p></div>
            </div>
            <div className="flex items-center gap-3">
              <span className="rounded-full bg-secondary px-3 py-1.5 text-xs font-semibold text-muted-foreground">{historyQuery.data?.length ?? 0} records</span>
              {activeNotice && <Button type="button" variant="outline" disabled={busy} onClick={() => setConfirmStop(true)} className="h-9 rounded-lg border-red-200 px-3 text-xs font-semibold text-red-700 hover:bg-red-50 hover:text-red-800">Stop active notice</Button>}
            </div>
          </div>
          {!historyQuery.data?.length ? (
            <div className="rounded-2xl border border-dashed border-border px-5 py-12 text-center"><Clock3 className="mx-auto h-7 w-7 text-muted-foreground/50" /><p className="mt-3 text-sm font-medium text-[color:var(--gov-navy)]">No previous notices yet</p><p className="mt-1 text-xs text-muted-foreground">Your saved notice versions will appear here for future reference.</p></div>
          ) : (
            <div className="divide-y divide-border overflow-hidden rounded-2xl border border-border">
              {historyQuery.data.map((notice) => (
                <article key={notice.id} className="grid gap-4 bg-white px-5 py-5 sm:grid-cols-[auto_1fr_auto] sm:items-start">
                  <span className={`mt-1.5 h-2.5 w-2.5 rounded-full ${notice.severity === 'critical' ? 'bg-red-600' : notice.severity === 'warning' ? 'bg-[color:var(--saffron)]' : 'bg-[color:var(--gov-blue)]'}`} aria-hidden="true" />
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2"><h3 className="truncate text-sm font-semibold text-[color:var(--gov-navy)]">{notice.title}</h3><span className="rounded-full bg-secondary px-2 py-0.5 text-[10px] font-bold uppercase tracking-[0.12em] text-muted-foreground">{notice.severity}</span><span className={`rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-[0.12em] ${notice.wasActive ? 'bg-[color:var(--gov-green)]/10 text-[color:var(--gov-green)]' : 'bg-secondary text-muted-foreground'}`}>{notice.wasActive ? 'Published' : 'Draft'}</span></div>
                    <p className="mt-1.5 line-clamp-2 text-sm leading-5 text-muted-foreground">{notice.message}</p>
                    {(notice.startsAt || notice.endsAt) && <p className="mt-2 text-xs text-muted-foreground">Scheduled {notice.startsAt ? historyDate(notice.startsAt) : 'immediately'}{notice.endsAt ? ` to ${historyDate(notice.endsAt)}` : ''}</p>}
                  </div>
                  <time className="text-xs font-medium text-muted-foreground sm:text-right" dateTime={notice.savedAt}>{historyDate(notice.savedAt)}</time>
                </article>
              ))}
            </div>
          )}
        </Card>
      </QueryState>

      <Card className="mt-5 rounded-2xl border-[color:var(--gov-blue)]/15 bg-gradient-to-br from-white to-[color:var(--gov-blue)]/5 p-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-start gap-3">
            <div className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl ${activeNotice ? 'bg-[color:var(--gov-green)]/10 text-[color:var(--gov-green)]' : 'bg-secondary text-muted-foreground'}`}><Megaphone className="h-5 w-5" /></div>
            <div><div className="flex flex-wrap items-center gap-2"><h2 className="text-base font-semibold text-[color:var(--gov-navy)]">Public notice</h2>{activeNotice && <span className="rounded-full bg-[color:var(--gov-green)]/10 px-2.5 py-1 text-[10px] font-bold uppercase tracking-[0.14em] text-[color:var(--gov-green)]">Live on SevaOne</span>}</div><p className="mt-1 text-sm text-muted-foreground">{activeNotice ? `${activeNotice.title} · ${activeNotice.severity}` : 'No notice is currently being shown to visitors.'}</p></div>
          </div>
          {activeNotice ? <Button type="button" variant="outline" onClick={() => openEdit(activeNotice)} className="h-9 rounded-lg"><SquareArrowOutUpRight className="mr-2 h-4 w-4" /> Edit current</Button> : <Button type="button" variant="outline" onClick={openCreate} className="h-9 rounded-lg"><Plus className="mr-2 h-4 w-4" /> Create notice</Button>}
        </div>
      </Card>

      <Modal open={modalMode !== null} title={modalMode === 'edit' ? 'Edit current notice' : 'Create a new service notice'} description="Choose the message and severity visitors should see. Warning and Critical notices block interaction with SevaOne until stopped or expired." size="xl" onClose={() => { if (!busy) setModalMode(null) }}>
        <form className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]" onSubmit={(event) => void save(event)}>
          <div className="space-y-5">
            <div className="space-y-2"><Label htmlFor="notice-title">Title</Label><Input id="notice-title" value={title} maxLength={120} onChange={(event) => setTitle(event.target.value)} placeholder="Scheduled maintenance" /></div>
            <div className="space-y-2"><div className="flex items-center justify-between gap-3"><Label htmlFor="notice-message">Message</Label><span className="text-[11px] text-muted-foreground">{message.length}/500</span></div><textarea id="notice-message" value={message} maxLength={500} onChange={(event) => setMessage(event.target.value)} placeholder="SevaOne services may be unavailable during scheduled maintenance." rows={5} className="w-full resize-y rounded-xl border border-border bg-secondary/30 px-3 py-2.5 text-sm outline-none ring-[color:var(--gov-blue)]/30 placeholder:text-muted-foreground/70 focus:ring-2" /></div>
            <div className="space-y-2"><Label htmlFor="notice-severity">Severity</Label><select id="notice-severity" value={severity} onChange={(event) => setSeverity(event.target.value as ServiceNoticeSeverity)} className="h-10 w-full rounded-xl border border-border bg-secondary/30 px-3 text-sm outline-none ring-[color:var(--gov-blue)]/30 focus:ring-2">{SEVERITIES.map((option) => <option key={option.id} value={option.id}>{option.label} - {option.description}</option>)}</select></div>
            <div className="grid gap-4 sm:grid-cols-2"><div className="space-y-2"><Label htmlFor="notice-start">Start time <span className="font-normal text-muted-foreground">(optional)</span></Label><Input id="notice-start" type="datetime-local" value={startsAt} onChange={(event) => setStartsAt(event.target.value)} /></div><div className="space-y-2"><Label htmlFor="notice-end">End time <span className="font-normal text-muted-foreground">(optional)</span></Label><Input id="notice-end" type="datetime-local" value={endsAt} onChange={(event) => setEndsAt(event.target.value)} /></div></div>
            <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-border bg-secondary/30 p-4"><input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} className="mt-0.5 h-4 w-4 accent-[color:var(--gov-blue)]" /><span><span className="block text-sm font-semibold text-[color:var(--gov-navy)]">Publish on the public site</span><span className="mt-1 block text-xs leading-5 text-muted-foreground">When disabled, this is saved as a draft and visitors will not see it.</span></span></label>
            <div className="flex justify-end gap-3 border-t border-border pt-5"><Button type="button" variant="outline" disabled={busy} onClick={() => setModalMode(null)} className="h-10 rounded-xl">Cancel</Button><Button type="submit" disabled={busy} className="h-10 rounded-xl bg-[color:var(--gov-blue)] px-5 text-white hover:bg-[color:var(--gov-blue-dark)]">{busy ? 'Saving...' : <><Save className="mr-2 h-4 w-4" /> Save notice</>}</Button></div>
          </div>
          <div className="space-y-5"><div className={`rounded-2xl border p-5 ${previewClasses(severity)}`}><div className="flex items-start gap-3">{severity === 'critical' ? <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0" /> : <Megaphone className="mt-0.5 h-5 w-5 shrink-0" />}<div className="min-w-0"><p className="text-[10px] font-bold uppercase tracking-[0.16em] opacity-70">Public preview</p><h3 className="mt-2 text-base font-bold">{title.trim() || 'Your notice title'}</h3><p className="mt-2 text-sm leading-6">{message.trim() || 'Your public message will appear here.'}</p></div></div></div><Card className="rounded-2xl border-[color:var(--gov-green)]/20 bg-[color:var(--gov-green)]/5 p-5"><p className="text-xs font-bold uppercase tracking-[0.16em] text-[color:var(--gov-green)]">Publishing guidance</p><p className="mt-2 text-sm leading-6 text-[color:var(--gov-navy)]">Use <strong>Warning</strong> for planned maintenance and reduced availability. Use <strong>Critical</strong> only for an active outage.</p></Card></div>
        </form>
      </Modal>

      <Modal open={confirmStop} title="Stop the active notice?" description="This will immediately stop showing it on SevaOne. The saved SQL record and its history will remain available." onClose={() => { if (!busy) setConfirmStop(false) }}>
        <div className="flex justify-end gap-3"><Button type="button" variant="outline" disabled={busy} onClick={() => setConfirmStop(false)} className="h-10 rounded-xl">Cancel</Button><Button type="button" disabled={busy} onClick={() => void stopNotice()} className="h-10 rounded-xl bg-red-700 text-white hover:bg-red-800">{busy ? 'Stopping...' : 'Stop notice'}</Button></div>
      </Modal>
    </>
  )
}
