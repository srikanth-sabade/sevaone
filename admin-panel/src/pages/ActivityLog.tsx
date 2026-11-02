import { useState } from 'react'
import { CheckCircle2, History, LogIn, LogOut, Megaphone, ShieldPlus, UserCog } from 'lucide-react'
import { PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { Card } from '../components/ui/card'
import { useGovAdminActivityLog } from '../hooks/useGovAdmin'
import type { GovAdminActivityLogEntry } from '../types'

const ACTIONS = [
  { value: '', label: 'All actions' },
  { value: 'login', label: 'Login' },
  { value: 'logout', label: 'Logout' },
  { value: 'admin.create', label: 'Admin account created' },
  { value: 'admin.deactivate', label: 'Admin account deactivated' },
  { value: 'admin.reactivate', label: 'Admin account reactivated' },
  { value: 'notice.save', label: 'Service notice saved' },
  { value: 'notice.stop', label: 'Service notice stopped' },
  { value: 'notification.create', label: 'Notification created' },
  { value: 'notification.deactivate', label: 'Notification deactivated' },
]

function actionLabel(action: string) {
  return ACTIONS.find((a) => a.value === action)?.label ?? action
}

function actionIcon(action: string) {
  if (action === 'login') return LogIn
  if (action === 'logout') return LogOut
  if (action.startsWith('admin.')) return UserCog
  if (action.startsWith('notice.')) return Megaphone
  if (action.startsWith('notification.')) return ShieldPlus
  return History
}

function dateLabel(value: string) {
  return new Intl.DateTimeFormat('en-IN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

export function ActivityLogPage() {
  const [action, setAction] = useState('')
  const query = useGovAdminActivityLog({ limit: 200, action: action || undefined }, true)

  return (
    <>
      <PageIntro eyebrow="Administration" title="Activity log" description="Who did what across logins, admin accounts, service notices, and notifications -- an append-only record, not editable from here." />
      <div className="mb-5">
        <select value={action} onChange={(event) => setAction(event.target.value)} className="h-11 rounded-xl border border-border bg-white px-3 text-sm text-[color:var(--gov-navy)]">
          {ACTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
        </select>
      </div>
      <QueryState isLoading={query.isLoading} isError={query.isError}>
        {query.data && <ActivityTable entries={query.data} />}
      </QueryState>
    </>
  )
}

function ActivityTable({ entries }: { readonly entries: GovAdminActivityLogEntry[] }) {
  return (
    <Card className="overflow-hidden rounded-2xl">
      <div className="border-b border-border bg-secondary/40 px-5 py-3 text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground">Append-only record</div>
      {!entries.length ? (
        <div className="px-5 py-14 text-center text-sm text-muted-foreground">
          <History className="mx-auto h-7 w-7 text-muted-foreground/40" />
          <p className="mt-3">No activity recorded yet for this filter.</p>
        </div>
      ) : (
        <div className="divide-y divide-border">
          {entries.map((entry) => {
            const Icon = actionIcon(entry.action)
            return (
              <div key={entry.id} className="flex flex-wrap items-center gap-3 px-5 py-4">
                <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]"><Icon className="h-4 w-4" /></div>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="text-sm font-semibold text-[color:var(--gov-navy)]">{actionLabel(entry.action)}</p>
                    {entry.target && <span className="rounded-full bg-secondary px-2.5 py-0.5 font-mono text-[10px] text-muted-foreground">{entry.target}</span>}
                  </div>
                  <p className="mt-1 text-xs text-muted-foreground">by {entry.actorDisplayName} ({entry.actorUsername}) · {dateLabel(entry.at)}</p>
                </div>
                <CheckCircle2 className="h-4 w-4 text-[color:var(--gov-green)]" />
              </div>
            )
          })}
        </div>
      )}
    </Card>
  )
}
