import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, ArrowRight, Megaphone, X } from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { api } from '../api'
import type { PublicServiceNotice } from '../types'
import { useT } from '../hooks/useT'

const DISMISSED_NOTICE_KEY = 'sevaone.dismissed-service-notice'

const styles: Record<PublicServiceNotice['severity'], { accent: string; iconSurface: string; icon: string; badge: string; button: string }> = {
  info: {
    accent: 'from-[color:var(--gov-blue)] via-cyan-500 to-[color:var(--gov-green)]',
    iconSurface: 'bg-[color:var(--gov-blue)]/10',
    icon: 'text-[color:var(--gov-blue)]',
    badge: 'bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]',
    button: 'bg-[color:var(--gov-navy)] text-white hover:bg-[color:var(--gov-blue-dark)]',
  },
  warning: {
    accent: 'from-[color:var(--saffron)] via-amber-400 to-orange-500',
    iconSurface: 'bg-amber-100',
    icon: 'text-amber-700',
    badge: 'bg-amber-100 text-amber-800',
    button: 'bg-[color:var(--gov-navy)] text-white hover:bg-[color:var(--gov-blue-dark)]',
  },
  critical: {
    accent: 'from-red-700 via-rose-600 to-orange-500',
    iconSurface: 'bg-red-100',
    icon: 'text-red-700',
    badge: 'bg-red-100 text-red-800',
    button: 'bg-red-700 text-white hover:bg-red-800',
  },
}

function readDismissedNotice() {
  try {
    return window.localStorage.getItem(DISMISSED_NOTICE_KEY)
  } catch {
    return null
  }
}

export function ServiceNoticeBanner() {
  const { t } = useT()
  const [dismissedNotice, setDismissedNotice] = useState<string | null>(readDismissedNotice)
  const dialogRef = useRef<HTMLDivElement>(null)
  const query = useQuery({
    queryKey: ['service-notice'],
    queryFn: api.serviceNotice,
    staleTime: 30_000,
    refetchInterval: 60_000,
    retry: false,
  })

  const notice = query.data
  const isBlocking = notice?.severity === 'warning' || notice?.severity === 'critical'
  const isVisible = !!notice && (isBlocking || notice.updatedAt !== dismissedNotice)

  const dismissNotice = useCallback(() => {
    if (!notice || isBlocking) return
    try {
      window.localStorage.setItem(DISMISSED_NOTICE_KEY, notice.updatedAt)
    } catch {
      // A private browsing context may reject localStorage; state still dismisses this view.
    }
    setDismissedNotice(notice.updatedAt)
  }, [isBlocking, notice])

  useEffect(() => {
    if (!isVisible) return

    const previousOverflow = document.body.style.overflow
    const previousActive = document.activeElement instanceof HTMLElement ? document.activeElement : null
    document.body.style.overflow = 'hidden'
    dialogRef.current?.focus()

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && !isBlocking) {
        dismissNotice()
      } else if (isBlocking) {
        event.preventDefault()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.body.style.overflow = previousOverflow
      document.removeEventListener('keydown', handleKeyDown)
      previousActive?.focus()
    }
  }, [dismissNotice, isBlocking, isVisible])

  if (!notice || !isVisible) return null

  const style = styles[notice.severity]
  const Icon = notice.severity === 'info' ? Megaphone : AlertTriangle

  return createPortal(
    <div
      role={isBlocking ? 'alertdialog' : 'dialog'}
      aria-modal="true"
      aria-labelledby="service-notice-title"
      aria-describedby="service-notice-message"
      className={`fixed inset-0 z-[100] flex min-h-screen items-center justify-center overflow-y-auto p-4 sm:p-6 ${isBlocking ? 'bg-[color:var(--gov-navy)]/80 backdrop-blur-md' : 'bg-[color:var(--gov-navy)]/45 backdrop-blur-sm'}`}
      onMouseDown={(event) => {
        if (!isBlocking && event.target === event.currentTarget) dismissNotice()
      }}
    >
      <div ref={dialogRef} tabIndex={-1} className="relative w-full max-w-xl overflow-hidden rounded-[2rem] border border-white/80 bg-white shadow-[0_28px_90px_rgba(8,35,65,0.3)] outline-none">
        <div className={`h-1.5 w-full bg-gradient-to-r ${style.accent}`} />
        <div className="p-6 sm:p-9">
          <div className="flex items-start justify-between gap-4">
            <div className={`flex h-14 w-14 items-center justify-center rounded-2xl ${style.iconSurface}`}>
              <Icon className={`h-7 w-7 ${style.icon}`} aria-hidden="true" />
            </div>
            {!isBlocking && (
              <button type="button" onClick={dismissNotice} aria-label={t('serviceNoticeClose')} className="rounded-full p-2 text-slate-400 transition hover:bg-slate-100 hover:text-[color:var(--gov-navy)]">
                <X className="h-5 w-5" />
              </button>
            )}
          </div>
          <div className="mt-7">
            <span className={`inline-flex rounded-full px-3 py-1 text-[10px] font-bold uppercase tracking-[0.18em] ${style.badge}`}>{t('serviceNotice')}</span>
            <h1 id="service-notice-title" className="mt-4 text-2xl font-bold tracking-tight text-[color:var(--gov-navy)] sm:text-3xl">{notice.title}</h1>
            <p id="service-notice-message" className="mt-4 whitespace-pre-line text-base leading-7 text-slate-600">{notice.message}</p>
          </div>
          {isBlocking ? (
            <div className="mt-8 rounded-2xl bg-slate-50 px-4 py-3 text-sm leading-6 text-slate-600">
              <span className="font-semibold text-[color:var(--gov-navy)]">{t('serviceNoticeBlockingTitle')}</span> {t('serviceNoticeBlockingBody')}
            </div>
          ) : (
            <button type="button" onClick={dismissNotice} className={`mt-8 inline-flex h-12 w-full items-center justify-center rounded-xl px-5 text-sm font-semibold transition ${style.button}`}>
              {t('serviceNoticeContinue')} <ArrowRight className="ml-2 h-4 w-4" />
            </button>
          )}
        </div>
      </div>
    </div>,
    document.body,
  )
}
