/**
 * SIDEBAR 1 (legacy) — hover-expand icon rail.
 * Not mounted. Restore by exporting FloatingRail from this file (or swapping the
 * import in Shell / Chat / Landing) instead of FloatingRail.tsx (Sidebar 2).
 */
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useRouterState } from '@tanstack/react-router'
import {
  Building2,
  ChevronRight,
  FileSearch,
  HelpCircle,
  Home,
  Languages,
  LayoutGrid,
  Loader2,
  LogIn,
  LogOut,
  MoreHorizontal,
  Receipt,
  Store,
  UserRound,
  X,
} from 'lucide-react'
import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { toast } from 'sonner'
import { api } from '../api'
import { sidebarLoginHref } from '../applyFlow'
import gokEmblem from '../assets/gok-emblem.webp'
import { useT } from '../hooks/useT'
import type { MessageKey } from '../i18n'
import { fromOperatorMe } from '../lib/operator'
import { useSession } from '../store'

type MoreItem = {
  to: string
  labelKey: MessageKey
  hash?: string
  search?: Record<string, undefined>
}

const CITIZEN_MORE_NAV: MoreItem[] = [
  { to: '/kpi', labelKey: 'kpi' },
  { to: '/operator', labelKey: 'assistedCentre' },
  { to: '/about', labelKey: 'about' },
  { to: '/grievance', labelKey: 'grievance' },
  { to: '/home', labelKey: 'serviceHome' },
]

const OPERATOR_MORE_NAV: MoreItem[] = [
  { to: '/operator', labelKey: 'counterDeptServices', hash: 'counter-services' },
  { to: '/operator', labelKey: 'counterShift', hash: 'counter-shift' },
  { to: '/', labelKey: 'citizenPortal', search: { loginError: undefined } },
  { to: '/about', labelKey: 'about' },
  { to: '/grievance', labelKey: 'grievance' },
]

type NavItem = {
  to: string
  search?: Record<string, undefined>
  hash?: string
  labelKey: MessageKey
  icon: typeof Home
  match: (path: string, hash: string) => boolean
}

const CITIZEN_PRIMARY_NAV: NavItem[] = [
  {
    to: '/',
    search: { loginError: undefined },
    labelKey: 'home',
    icon: Home,
    match: (p) => p === '/' || p === '/home',
  },
  {
    to: '/services',
    search: { q: undefined, department: undefined },
    labelKey: 'services',
    icon: LayoutGrid,
    match: (p) => p.startsWith('/services'),
  },
  {
    to: '/departments',
    labelKey: 'departments',
    icon: Building2,
    match: (p) => p.startsWith('/departments'),
  },
  {
    to: '/track',
    labelKey: 'track',
    icon: FileSearch,
    match: (p) => p.startsWith('/track'),
  },
  {
    to: '/help',
    labelKey: 'help',
    icon: HelpCircle,
    match: (p) => p.startsWith('/help'),
  },
]

const OPERATOR_PRIMARY_NAV: NavItem[] = [
  {
    to: '/operator',
    labelKey: 'counterConsole',
    icon: Store,
    match: (p, h) => p.startsWith('/operator') && !h,
  },
  {
    to: '/operator',
    hash: 'counter-bind',
    labelKey: 'counterBind',
    icon: UserRound,
    match: (p, h) => p.startsWith('/operator') && h === 'counter-bind',
  },
  {
    to: '/services',
    search: { q: undefined, department: undefined },
    labelKey: 'services',
    icon: LayoutGrid,
    match: (p) => p.startsWith('/services'),
  },
  {
    to: '/track',
    labelKey: 'track',
    icon: FileSearch,
    match: (p) => p.startsWith('/track'),
  },
  {
    to: '/help',
    labelKey: 'help',
    icon: HelpCircle,
    match: (p) => p.startsWith('/help'),
  },
]

/** @deprecated Sidebar 1 — not mounted. See FloatingRail.tsx (Sidebar 2). */
export function FloatingRailSidebar1() {
  const { me, loaded, logout, authBusy, setAuthBusy, setLang } = useSession()
  const { lang, t } = useT()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { path, hash } = useRouterState({
    select: (s) => ({
      path: s.location.pathname,
      hash: (s.location.hash || '').replace(/^#/, ''),
    }),
  })

  const [railBusy, setRailBusy] = useState(false)
  const [moreOpen, setMoreOpen] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)
  const [morePos, setMorePos] = useState<{ top: number; left: number } | null>(null)

  const moreBtnRef = useRef<HTMLButtonElement>(null)
  const moreMenuRef = useRef<HTMLDivElement>(null)

  const operatorQ = useQuery({
    queryKey: ['operator', 'me'],
    queryFn: async () => fromOperatorMe(await api.operatorMe()),
    retry: false,
  })
  const operator = operatorQ.data ?? null
  const isOperator = Boolean(operator)
  const primaryNav = isOperator ? OPERATOR_PRIMARY_NAV : CITIZEN_PRIMARY_NAV
  const moreNav = isOperator ? OPERATOR_MORE_NAV : CITIZEN_MORE_NAV
  const signedIn = Boolean(me || operator)
  const busy = railBusy || !!authBusy

  const goNav = (item: { to: string; search?: Record<string, undefined>; hash?: string }) => {
    const afterNavigation = () => {
      if (item.hash) {
        window.requestAnimationFrame(() => {
          document.getElementById(item.hash!)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
        })
      }
    }

    switch (item.to) {
      case '/':
        void navigate({ to: '/', search: { loginError: undefined }, hash: item.hash }).then(afterNavigation)
        break
      case '/services':
        void navigate({
          to: '/services',
          search: { q: undefined, department: undefined },
          hash: item.hash,
        }).then(afterNavigation)
        break
      case '/departments':
        void navigate({ to: '/departments', hash: item.hash }).then(afterNavigation)
        break
      case '/track':
        void navigate({ to: '/track', hash: item.hash }).then(afterNavigation)
        break
      case '/operator':
        void navigate({ to: '/operator', hash: item.hash }).then(afterNavigation)
        break
      case '/help':
        void navigate({ to: '/help', hash: item.hash }).then(afterNavigation)
        break
      case '/kpi':
        void navigate({ to: '/kpi', hash: item.hash }).then(afterNavigation)
        break
      case '/about':
        void navigate({ to: '/about', hash: item.hash }).then(afterNavigation)
        break
      case '/grievance':
        void navigate({ to: '/grievance', hash: item.hash }).then(afterNavigation)
        break
      case '/home':
        void navigate({ to: '/home', hash: item.hash }).then(afterNavigation)
        break
      default:
        toast.error('This navigation destination is unavailable')
    }
  }

  useEffect(() => {
    setMoreOpen(false)
    setMobileOpen(false)
  }, [path])

  useEffect(() => {
    document.documentElement.lang = lang === 'kn' ? 'kn' : 'en'
  }, [lang])

  useLayoutEffect(() => {
    if (!moreOpen || !moreBtnRef.current) {
      setMorePos(null)
      return
    }
    const place = () => {
      const r = moreBtnRef.current!.getBoundingClientRect()
      const menuW = 220
      const left = Math.min(r.right + 10, window.innerWidth - menuW - 12)
      const top = Math.min(Math.max(12, r.top), window.innerHeight - 240)
      setMorePos({ top, left })
    }
    place()
    window.addEventListener('resize', place)
    window.addEventListener('scroll', place, true)
    return () => {
      window.removeEventListener('resize', place)
      window.removeEventListener('scroll', place, true)
    }
  }, [moreOpen])

  useEffect(() => {
    if (!moreOpen) return
    const onPointer = (e: MouseEvent) => {
      const node = e.target as Node
      if (moreBtnRef.current?.contains(node) || moreMenuRef.current?.contains(node)) return
      setMoreOpen(false)
    }
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setMoreOpen(false)
    }
    document.addEventListener('mousedown', onPointer)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onPointer)
      document.removeEventListener('keydown', onKey)
    }
  }, [moreOpen])

  const onSignIn = (e: React.MouseEvent) => {
    e.preventDefault()
    if (busy) return
    setAuthBusy('signing-in')
    setRailBusy(true)
    window.location.assign(sidebarLoginHref())
  }

  const onLogout = async () => {
    if (busy) return
    setAuthBusy('signing-out')
    setRailBusy(true)
    try {
      if (operator) {
        await api.operatorLogout()
        await queryClient.invalidateQueries({ queryKey: ['operator', 'me'] })
      } else {
        await logout()
      }
      await navigate({ to: '/', search: { loginError: undefined } })
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Could not sign out')
    } finally {
      setAuthBusy(null)
      setRailBusy(false)
    }
  }

  const labelClass =
    'hidden max-w-0 overflow-hidden whitespace-nowrap opacity-0 transition-all duration-200 md:block group-hover/rail:md:max-w-44 group-hover/rail:md:opacity-100 group-focus-within/rail:md:max-w-44 group-focus-within/rail:md:opacity-100'

  const itemClass = (active?: boolean) =>
    `flex w-full items-center justify-center gap-0 rounded-xl px-2.5 py-2.5 transition group-hover/rail:md:justify-start group-hover/rail:md:gap-2.5 group-focus-within/rail:md:justify-start group-focus-within/rail:md:gap-2.5 ${
      active
        ? 'bg-[color:var(--gov-navy)]/10 text-[color:var(--gov-navy)]'
        : 'text-muted-foreground hover:bg-muted hover:text-[color:var(--gov-navy)]'
    }`

  const moreMenu =
    moreOpen &&
    morePos &&
    createPortal(
      <div
        ref={moreMenuRef}
        role="menu"
        className="fixed z-[60] min-w-[13.5rem] rounded-2xl border border-border/80 bg-card/95 p-1.5 shadow-[var(--shadow-lift)] backdrop-blur-xl animate-in fade-in zoom-in-95 duration-150"
        style={{ top: morePos.top, left: morePos.left }}
      >
        <div className="px-2.5 py-1.5 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
          {t('more')}
        </div>
        {moreNav.map((n) => (
          <button
            key={`${n.to}-${n.hash ?? ''}-${n.labelKey}`}
            type="button"
            role="menuitem"
            className="flex w-full items-center justify-between rounded-xl px-3 py-2.5 text-left text-sm hover:bg-muted"
            onClick={() => {
              setMoreOpen(false)
              setMobileOpen(false)
              goNav(n)
            }}
          >
            {t(n.labelKey)}
            <ChevronRight className="h-3.5 w-3.5 opacity-40" />
          </button>
        ))}
      </div>,
      document.body,
    )

  return (
    <>
      <nav
        aria-label="SevaOne navigation"
        className="group/rail fixed left-4 top-1/2 z-40 hidden max-h-[min(92vh,44rem)] -translate-y-1/2 overflow-visible rounded-3xl border border-border/80 bg-card/90 p-2 shadow-[var(--shadow-lift)] backdrop-blur-xl md:block"
      >
        <Link
          to={isOperator ? '/operator' : '/'}
          {...(isOperator ? {} : { search: { loginError: undefined } })}
          className="mb-1 flex items-center justify-center gap-0 rounded-xl px-2 py-2 transition group-hover/rail:md:justify-start group-hover/rail:md:gap-2.5 group-focus-within/rail:md:justify-start group-focus-within/rail:md:gap-2.5"
          title="SevaOne"
        >
          <img src={gokEmblem} alt="" width={32} height={32} className="h-8 w-8 shrink-0 object-contain" />
          <span className={labelClass}>
            <span className="block text-sm font-semibold tracking-tight text-foreground">
              Seva<span className="text-[color:var(--gov-blue)]">One</span>
            </span>
            <span className="block text-[11px] text-muted-foreground">
              {isOperator ? t('assistedCentre') : t('brandSub')}
            </span>
          </span>
        </Link>

        <div className="my-1 h-px bg-border" />

        <ul className="flex flex-col gap-0.5">
          {primaryNav.map((item) => {
            const active = item.match(path, hash)
            return (
              <li key={`${item.to}-${item.hash ?? ''}-${item.labelKey}`}>
                <button
                  type="button"
                  title={t(item.labelKey)}
                  aria-label={t(item.labelKey)}
                  aria-current={active ? 'page' : undefined}
                  className={itemClass(active)}
                  onClick={() => goNav(item)}
                >
                  <item.icon className="h-5 w-5 shrink-0" />
                  <span className={labelClass}>
                    <span className="block text-sm font-medium leading-tight text-foreground">
                      {t(item.labelKey)}
                    </span>
                  </span>
                </button>
              </li>
            )
          })}

          <li>
            <button
              ref={moreBtnRef}
              type="button"
              title={t('more')}
              aria-expanded={moreOpen}
              aria-haspopup="menu"
              className={itemClass(moreOpen || moreNav.some((n) => path.startsWith(n.to) && (!n.hash || hash === n.hash)))}
              onClick={() => setMoreOpen((v) => !v)}
            >
              <MoreHorizontal className="h-5 w-5 shrink-0" />
              <span className={labelClass}>
                <span className="flex items-center gap-1 text-sm font-medium leading-tight text-foreground">
                  {t('more')}
                  <ChevronRight className="h-3.5 w-3.5 opacity-60" />
                </span>
              </span>
            </button>
          </li>
        </ul>

        <div className="my-1.5 h-px bg-border" />

        <button
          type="button"
          title={lang === 'en' ? t('switchToKannada') : t('switchToEnglish')}
          aria-label={lang === 'en' ? t('switchToKannada') : t('switchToEnglish')}
          className={itemClass()}
          onClick={() => setLang(lang === 'en' ? 'kn' : 'en')}
        >
          <Languages className="h-5 w-5 shrink-0" />
          <span className={labelClass}>
            <span className="block text-sm font-medium leading-tight text-foreground">
              {lang === 'en' ? t('kannada') : t('english')}
            </span>
            <span className="block text-[11px] text-muted-foreground">
              {lang === 'en' ? t('langHintEn') : t('langHintKn')}
            </span>
          </span>
        </button>

        <div className="mt-0.5 border-t border-border pt-1">
          {loaded && signedIn ? (
            <>
              <button
                type="button"
                title={operator ? `${operator.channel} · ${operator.operatorId}` : me?.name}
                className={itemClass(path.startsWith('/dashboard') || path.startsWith('/operator'))}
                onClick={() =>
                  void navigate(
                    operator ? { to: '/operator' } : { to: '/dashboard', search: { ref: undefined } },
                  )
                }
              >
                <UserRound className="h-5 w-5 shrink-0" />
                <span className={labelClass}>
                  <span className="block truncate text-sm font-medium leading-tight text-foreground">
                    {operator ? operator.channel : me?.name}
                  </span>
                  <span className="block truncate text-[11px] text-muted-foreground">
                    {operator ? operator.operatorId : t('dashboard')}
                  </span>
                </span>
              </button>
              <button
                type="button"
                disabled={busy}
                title={t('logout')}
                aria-label={t('logout')}
                className={`${itemClass()} disabled:pointer-events-none disabled:opacity-60`}
                onClick={() => void onLogout()}
              >
                {busy && authBusy === 'signing-out' ? (
                  <Loader2 className="h-5 w-5 shrink-0 animate-spin" />
                ) : (
                  <LogOut className="h-5 w-5 shrink-0" />
                )}
                <span className={labelClass}>
                  <span className="block text-sm font-medium leading-tight text-foreground">
                    {busy && authBusy === 'signing-out' ? '…' : t('logout')}
                  </span>
                </span>
              </button>
            </>
          ) : (
            <a
              href={sidebarLoginHref()}
              title={t('login')}
              aria-label={t('login')}
              onClick={onSignIn}
              className={`${itemClass()} ${busy ? 'pointer-events-none opacity-60' : ''}`}
            >
              {busy && authBusy === 'signing-in' ? (
                <Loader2 className="h-5 w-5 shrink-0 animate-spin" />
              ) : (
                <LogIn className="h-5 w-5 shrink-0" />
              )}
              <span className={labelClass}>
                <span className="block text-sm font-medium leading-tight text-foreground">{t('login')}</span>
              </span>
            </a>
          )}
        </div>
      </nav>

      {moreMenu}

      <nav
        aria-label="SevaOne mobile navigation"
        className="fixed inset-x-0 bottom-0 z-40 border-t border-border/80 bg-card/95 px-2 pb-[max(0.5rem,env(safe-area-inset-bottom))] pt-1.5 shadow-[0_-8px_30px_-18px_rgba(26,35,50,0.35)] backdrop-blur-xl md:hidden"
      >
        <ul className="mx-auto flex max-w-lg items-stretch justify-between gap-0.5">
          {primaryNav.slice(0, 4).map((item) => {
            const active = item.match(path, hash)
            return (
              <li key={`mbar-${item.to}-${item.hash ?? ''}-${item.labelKey}`} className="flex-1">
                <button
                  type="button"
                  className={`flex w-full flex-col items-center gap-0.5 rounded-xl px-1 py-1.5 text-[10px] font-medium ${
                    active ? 'text-[color:var(--gov-navy)]' : 'text-muted-foreground'
                  }`}
                  onClick={() => goNav(item)}
                >
                  <item.icon className="h-5 w-5" />
                  <span className="truncate">{t(item.labelKey)}</span>
                </button>
              </li>
            )
          })}
          <li className="flex-1">
            <button
              type="button"
              className={`flex w-full flex-col items-center gap-0.5 rounded-xl px-1 py-1.5 text-[10px] font-medium ${
                mobileOpen ? 'text-[color:var(--gov-navy)]' : 'text-muted-foreground'
              }`}
              aria-expanded={mobileOpen}
              onClick={() => setMobileOpen((v) => !v)}
            >
              {mobileOpen ? <X className="h-5 w-5" /> : <MoreHorizontal className="h-5 w-5" />}
              <span>{t('more')}</span>
            </button>
          </li>
        </ul>
      </nav>

      {mobileOpen && (
        <div className="fixed inset-0 z-30 md:hidden">
          <button
            type="button"
            className="absolute inset-0 bg-foreground/20 backdrop-blur-[2px]"
            aria-label="Close menu"
            onClick={() => setMobileOpen(false)}
          />
          <div className="absolute inset-x-3 bottom-[4.5rem] max-h-[70vh] overflow-y-auto rounded-3xl border border-border bg-card p-3 shadow-lg">
            <ul className="space-y-0.5">
              {primaryNav.map((item) => (
                <li key={`m-${item.to}-${item.hash ?? ''}-${item.labelKey}`}>
                  <button
                    type="button"
                    className="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left text-sm hover:bg-muted"
                    onClick={() => {
                      setMobileOpen(false)
                      goNav(item)
                    }}
                  >
                    <item.icon className="h-4 w-4 text-muted-foreground" />
                    {t(item.labelKey)}
                  </button>
                </li>
              ))}
              {moreNav.map((n) => (
                <li key={`m-${n.to}-${n.hash ?? ''}-${n.labelKey}`}>
                  <button
                    type="button"
                    className="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left text-sm hover:bg-muted"
                    onClick={() => {
                      setMobileOpen(false)
                      goNav(n)
                    }}
                  >
                    {n.hash === 'counter-shift' ? (
                      <Receipt className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
                    )}
                    {t(n.labelKey)}
                  </button>
                </li>
              ))}
            </ul>
            <div className="mt-2 flex flex-wrap gap-2 border-t border-border pt-2">
              <button
                type="button"
                className="rounded-full border border-border px-3 py-1.5 text-xs font-medium"
                onClick={() => setLang(lang === 'en' ? 'kn' : 'en')}
              >
                {lang === 'en' ? t('kannada') : t('english')}
              </button>
              {loaded && signedIn ? (
                <button
                  type="button"
                  disabled={busy}
                  className="rounded-full border border-border px-3 py-1.5 text-xs font-medium"
                  onClick={() => void onLogout()}
                >
                  {t('logout')}
                </button>
              ) : (
                <a
                  href={sidebarLoginHref()}
                  onClick={onSignIn}
                  className="rounded-full bg-[color:var(--gov-navy)] px-3 py-1.5 text-xs font-medium text-white"
                >
                  {t('login')}
                </a>
              )}
            </div>
          </div>
        </div>
      )}
    </>
  )
}
