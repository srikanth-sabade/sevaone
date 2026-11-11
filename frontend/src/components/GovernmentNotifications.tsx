import { useQuery } from '@tanstack/react-query'
import { Bell, CalendarDays, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'
import { api } from '../api'
import type { PublicGovNotification } from '../types'
import { useT } from '../hooks/useT'

function publishedDate(value: string) {
  return new Intl.DateTimeFormat('en-IN', { dateStyle: 'medium' }).format(new Date(value))
}

export function GovernmentNotifications() {
  const { t } = useT()
  const [selected, setSelected] = useState<PublicGovNotification | null>(null)
  const query = useQuery({
    queryKey: ['government-notifications'],
    queryFn: api.governmentNotifications,
    staleTime: 60_000,
    refetchInterval: 120_000,
    retry: false,
  })

  useEffect(() => {
    if (!selected) return
    const previousOverflow = document.body.style.overflow
    const previousActive = document.activeElement instanceof HTMLElement ? document.activeElement : null
    document.body.style.overflow = 'hidden'
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setSelected(null)
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.body.style.overflow = previousOverflow
      document.removeEventListener('keydown', handleKeyDown)
      previousActive?.focus()
    }
  }, [selected])

  if (!query.data?.length) return null

  const notificationContent = (
    <div className="grid gap-3 sm:grid-cols-2">
      {query.data.map((notification) => (
        <button
          key={notification.id}
          type="button"
          onClick={() => setSelected(notification)}
          className="group rounded-2xl border border-border bg-card p-4 text-left shadow-sm transition hover:-translate-y-0.5 hover:border-[color:var(--gov-blue)]/40 hover:shadow-[var(--shadow-soft)]"
        >
          <div className="flex items-start gap-3">
            <span className="mt-1 flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]">
              <Bell className="h-4 w-4" />
            </span>
            <span className="min-w-0">
              <span className="block text-sm font-semibold leading-5 text-[color:var(--gov-navy)]">
                {notification.title}
              </span>
              <span className="mt-2 block line-clamp-2 text-sm leading-5 text-muted-foreground">
                {notification.body}
              </span>
              <span className="mt-3 inline-flex items-center gap-1.5 text-xs font-semibold text-[color:var(--gov-blue)]">
                {t('notificationsReadMore')} <span aria-hidden="true">→</span>
              </span>
            </span>
          </div>
        </button>
      ))}
    </div>
  )

  return (
    <>
      <section className="mt-14 w-full">
        <div className="mb-4 flex items-end justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wider text-[color:var(--gov-blue)]">
              <Bell className="h-3.5 w-3.5" /> {t('notifications')}
            </div>
            <h2 className="mt-2 text-2xl font-bold tracking-tight text-[color:var(--gov-navy)] sm:text-3xl">
              {t('notificationsTitle')}
            </h2>
            <p className="mt-1 text-sm text-muted-foreground">{t('notificationsHint')}</p>
          </div>
        </div>
        {notificationContent}
      </section>

      {selected &&
        createPortal(
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="government-notification-title"
            aria-describedby="government-notification-body"
            className="fixed inset-0 z-[100] flex min-h-screen items-center justify-center overflow-y-auto bg-[color:var(--gov-navy)]/55 p-4 backdrop-blur-sm sm:p-6"
            onMouseDown={(event) => {
              if (event.target === event.currentTarget) setSelected(null)
            }}
          >
            <div className="w-full max-w-2xl overflow-hidden rounded-[2rem] border border-white/80 bg-white shadow-[0_28px_90px_rgba(8,35,65,0.3)]">
              <div className="h-1.5 bg-gradient-to-r from-[color:var(--gov-blue)] via-cyan-500 to-[color:var(--gov-green)]" />
              <div className="p-6 sm:p-9">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]">
                    <Bell className="h-6 w-6" />
                  </div>
                  <button
                    type="button"
                    onClick={() => setSelected(null)}
                    aria-label={t('notificationsClose')}
                    className="rounded-full p-2 text-slate-400 transition hover:bg-slate-100 hover:text-[color:var(--gov-navy)]"
                  >
                    <X className="h-5 w-5" />
                  </button>
                </div>
                <p className="mt-7 text-[10px] font-bold uppercase tracking-[0.18em] text-[color:var(--gov-blue)]">
                  {t('notifications')}
                </p>
                <h2 id="government-notification-title" className="mt-3 text-2xl font-bold tracking-tight text-[color:var(--gov-navy)] sm:text-3xl">
                  {selected.title}
                </h2>
                <div className="mt-4 flex items-center gap-2 text-xs text-muted-foreground">
                  <CalendarDays className="h-3.5 w-3.5" /> {t('notificationsPublished')} {publishedDate(selected.publishedAt)}
                </div>
                <p id="government-notification-body" className="mt-7 whitespace-pre-line text-base leading-8 text-slate-600">
                  {selected.body}
                </p>
              </div>
            </div>
          </div>,
          document.body,
        )}
    </>
  )
}
