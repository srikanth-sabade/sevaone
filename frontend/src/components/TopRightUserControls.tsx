/**
 * Language switch + profile, shown top-right on every page. Previously lived in
 * FloatingRail's sidebar; moved here so it's not tied to the rail's hover/collapse state.
 * Logout stays in the sidebar (FloatingRail) — this is identity/language only.
 */
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { Loader2, LogIn } from 'lucide-react'
import { useState } from 'react'
import { api } from '../api'
import { sidebarLoginHref } from '../applyFlow'
import { useT } from '../hooks/useT'
import { fromOperatorMe } from '../lib/operator'
import { useSession } from '../store'

export function TopRightUserControls() {
  const { me, loaded, authBusy, setAuthBusy, setLang } = useSession()
  const { lang, t } = useT()
  const navigate = useNavigate()
  const [busyLocal, setBusyLocal] = useState(false)

  const operatorQ = useQuery({
    queryKey: ['operator', 'me'],
    queryFn: async () => fromOperatorMe(await api.operatorMe()),
    retry: false,
  })
  const operator = operatorQ.data ?? null
  const signedIn = Boolean(me || operator)
  const busy = busyLocal || !!authBusy
  const displayName = operator ? operator.channel : (me?.name ?? '')
  const initial = displayName.trim().charAt(0).toUpperCase() || '?'

  const onSignIn = (e: React.MouseEvent) => {
    e.preventDefault()
    if (busy) return
    setAuthBusy('signing-in')
    setBusyLocal(true)
    window.location.assign(sidebarLoginHref())
  }

  return (
    <div className="flex shrink-0 items-center gap-2">
      <button
        type="button"
        aria-label={lang === 'en' ? t('switchToKannada') : t('switchToEnglish')}
        title={lang === 'en' ? t('switchToKannada') : t('switchToEnglish')}
        onClick={() => setLang(lang === 'en' ? 'kn' : 'en')}
        className="rounded-full border border-border bg-card px-3 py-1.5 text-xs font-medium text-[#5b616e] transition hover:border-[#ebe9f0] hover:bg-[#fafafb] hover:text-[#2f3542]"
      >
        {lang === 'en' ? t('kannada') : t('english')}
      </button>

      {loaded && signedIn ? (
        <button
          type="button"
          title={operator ? `${operator.channel} · ${operator.operatorId}` : me?.name}
          aria-label={operator ? `${operator.channel} · ${operator.operatorId}` : me?.name}
          onClick={() =>
            void navigate(
              operator ? { to: '/operator' } : { to: '/dashboard', search: { ref: undefined } },
            )
          }
          className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-[#5b4a8a] text-xs font-semibold text-white transition hover:bg-[#4d3f75]"
        >
          {initial}
        </button>
      ) : (
        <a
          href={sidebarLoginHref()}
          title={t('login')}
          aria-label={t('login')}
          onClick={onSignIn}
          className={`flex items-center gap-1.5 rounded-full bg-[#5b4a8a] px-3 py-1.5 text-xs font-medium text-white transition hover:bg-[#4d3f75] ${
            busy ? 'pointer-events-none opacity-60' : ''
          }`}
        >
          {busy && authBusy === 'signing-in' ? (
            <Loader2 className="h-3.5 w-3.5 animate-spin" />
          ) : (
            <LogIn className="h-3.5 w-3.5" strokeWidth={1.75} />
          )}
          {t('login')}
        </a>
      )}
    </div>
  )
}
