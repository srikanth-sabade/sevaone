import { type FormEvent, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Archive, Bell, EyeOff, Plus, Send } from 'lucide-react'
import { toast } from 'sonner'
import { govAdminApi } from '../api'
import { useGovNotifications } from '../hooks/useGovAdmin'
import { Button } from '../components/ui/button'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { Modal } from '../components/Modal'
import { PageIntro, QueryState } from '../components/AnalyticsPrimitives'

function noticeDate(value: string) {
  return new Intl.DateTimeFormat('en-IN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

export function NotificationsPage() {
  const queryClient = useQueryClient()
  const query = useGovNotifications(true)
  const [createOpen, setCreateOpen] = useState(false)
  const [confirmId, setConfirmId] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [busy, setBusy] = useState(false)
  const allNotifications = query.data ?? []
  const currentNotifications = allNotifications.filter((notification) => notification.isActive)
  const storedNotifications = allNotifications.filter((notification) => !notification.isActive)

  function openCreate() {
    setTitle('')
    setBody('')
    setCreateOpen(true)
  }

  async function createNotification(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!title.trim() || !body.trim()) {
      toast.error('Enter a notification heading and message.')
      return
    }

    setBusy(true)
    try {
      await govAdminApi.createGovNotification({ title: title.trim(), body: body.trim() })
      setCreateOpen(false)
      await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'notifications'] })
      toast.success('Notification published on SevaOne.')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Could not publish the notification.')
    } finally {
      setBusy(false)
    }
  }

  async function deactivateNotification(id: string) {
    setBusy(true)
    try {
      await govAdminApi.setGovNotificationActive(id, false)
      setConfirmId(null)
      await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'notifications'] })
      toast.success('Notification deactivated. Its record remains stored.')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Could not deactivate the notification.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <PageIntro eyebrow="Administration" title="Notifications" description="Publish important government updates, guidance, and deadlines directly on the SevaOne citizen home page." action={<Button type="button" onClick={openCreate} className="h-11 rounded-xl bg-[color:var(--gov-blue)] px-5 text-white shadow-sm hover:bg-[color:var(--gov-blue-dark)]"><Plus className="mr-2 h-4 w-4" /> Create new notification</Button>} />

      <QueryState isLoading={query.isLoading} isError={query.isError}>
        <section className="rounded-2xl border border-border bg-card p-6">
          <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
            <div className="flex items-start gap-3"><div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]"><Bell className="h-5 w-5" /></div><div><h2 className="text-lg font-semibold text-[color:var(--gov-navy)]">Current notifications</h2><p className="mt-1 text-sm leading-5 text-muted-foreground">These notifications are visible to citizens below Frequently requested services.</p></div></div>
            <span className="rounded-full bg-[color:var(--gov-blue)]/10 px-3 py-1.5 text-xs font-semibold text-[color:var(--gov-blue)]">{currentNotifications.length} active</span>
          </div>
          {!currentNotifications.length ? (
            <div className="rounded-2xl border border-dashed border-border px-5 py-12 text-center"><Bell className="mx-auto h-7 w-7 text-muted-foreground/50" /><p className="mt-3 text-sm font-medium text-[color:var(--gov-navy)]">No current notifications</p><p className="mt-1 text-xs text-muted-foreground">Create a notification when citizens need an important update.</p></div>
          ) : (
            <div className="grid gap-4 lg:grid-cols-2">
              {currentNotifications.map((notification) => (
                <article key={notification.id} className="relative overflow-hidden rounded-2xl border border-border bg-white p-5 shadow-sm"><div className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-[color:var(--gov-blue)] via-cyan-500 to-[color:var(--gov-green)]" /><div className="flex items-start justify-between gap-4"><div className="min-w-0"><h3 className="text-base font-semibold text-[color:var(--gov-navy)]">{notification.title}</h3><p className="mt-2 line-clamp-4 whitespace-pre-line text-sm leading-6 text-muted-foreground">{notification.body}</p></div><span className="shrink-0 rounded-full bg-[color:var(--gov-green)]/10 px-2.5 py-1 text-[10px] font-bold uppercase tracking-[0.14em] text-[color:var(--gov-green)]">Live</span></div><div className="mt-5 flex flex-wrap items-center justify-between gap-3 border-t border-border pt-3"><time className="text-xs text-muted-foreground" dateTime={notification.createdAt}>Published {noticeDate(notification.createdAt)}</time>{confirmId === notification.id ? <div className="flex items-center gap-2"><Button type="button" variant="outline" disabled={busy} onClick={() => setConfirmId(null)} className="h-8 rounded-lg px-3 text-xs">Cancel</Button><Button type="button" disabled={busy} onClick={() => void deactivateNotification(notification.id)} className="h-8 rounded-lg bg-red-700 px-3 text-xs text-white hover:bg-red-800">{busy ? 'Stopping...' : 'Confirm stop'}</Button></div> : <Button type="button" variant="outline" disabled={busy} onClick={() => setConfirmId(notification.id)} className="h-8 rounded-lg border-red-200 px-3 text-xs text-red-700 hover:bg-red-50 hover:text-red-800"><EyeOff className="mr-1.5 h-3.5 w-3.5" /> Deactivate</Button>}</div></article>
              ))}
            </div>
          )}
        </section>

        <section className="mt-5 rounded-2xl border border-border bg-white/70 p-6">
          <div className="mb-5 flex items-center justify-between gap-3"><div className="flex items-center gap-2"><Archive className="h-4 w-4 text-[color:var(--gov-navy)]" /><h2 className="text-base font-semibold text-[color:var(--gov-navy)]">Stored notification records</h2></div><span className="rounded-full bg-secondary px-3 py-1 text-xs font-semibold text-muted-foreground">{storedNotifications.length} stored</span></div>
          {!storedNotifications.length ? <p className="rounded-xl border border-dashed border-border px-4 py-6 text-center text-sm text-muted-foreground">Deactivated notifications will remain available here.</p> : <div className="divide-y divide-border overflow-hidden rounded-xl border border-border">{storedNotifications.map((notification) => <article key={notification.id} className="flex flex-wrap items-start justify-between gap-3 bg-white px-4 py-4"><div className="min-w-0"><div className="flex flex-wrap items-center gap-2"><h3 className="text-sm font-semibold text-[color:var(--gov-navy)]">{notification.title}</h3><span className="rounded-full bg-secondary px-2 py-0.5 text-[10px] font-bold uppercase tracking-[0.12em] text-muted-foreground">Deactivated</span></div><p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{notification.body}</p></div><time className="text-xs text-muted-foreground" dateTime={notification.updatedAt}>Updated {noticeDate(notification.updatedAt)}</time></article>)}</div>}
        </section>
      </QueryState>

      <Modal open={createOpen} title="Create a new notification" description="Write a heading and the full government message citizens should be able to read." size="lg" onClose={() => { if (!busy) setCreateOpen(false) }}>
        <form className="space-y-5" onSubmit={(event) => void createNotification(event)}>
          <div className="space-y-2"><Label htmlFor="notification-title">Heading</Label><Input id="notification-title" value={title} maxLength={160} onChange={(event) => setTitle(event.target.value)} placeholder="Important update for citizens" /></div>
          <div className="space-y-2"><div className="flex items-center justify-between gap-3"><Label htmlFor="notification-body">Notification</Label><span className="text-[11px] text-muted-foreground">{body.length}/5000</span></div><textarea id="notification-body" value={body} maxLength={5000} onChange={(event) => setBody(event.target.value)} placeholder="Write the complete notification, including dates, instructions, and contact guidance..." rows={12} className="w-full resize-y rounded-xl border border-border bg-secondary/30 px-3 py-3 text-sm leading-6 outline-none ring-[color:var(--gov-blue)]/30 placeholder:text-muted-foreground/70 focus:ring-2" /></div>
          <div className="flex items-center justify-between gap-3 rounded-xl border border-[color:var(--gov-blue)]/15 bg-[color:var(--gov-blue)]/5 p-4 text-xs leading-5 text-muted-foreground"><span>New notifications publish immediately and can later be deactivated without deleting the stored record.</span><Send className="h-4 w-4 shrink-0 text-[color:var(--gov-blue)]" /></div>
          <div className="flex justify-end gap-3 border-t border-border pt-5"><Button type="button" variant="outline" disabled={busy} onClick={() => setCreateOpen(false)} className="h-10 rounded-xl">Cancel</Button><Button type="submit" disabled={busy} className="h-10 rounded-xl bg-[color:var(--gov-blue)] px-5 text-white hover:bg-[color:var(--gov-blue-dark)]">{busy ? 'Publishing...' : <><Send className="mr-2 h-4 w-4" /> Publish notification</>}</Button></div>
        </form>
      </Modal>
    </>
  )
}
