/**
 * SIDEBAR 2 — bilingual expanded rail (active).
 * Legacy Sidebar 1 is preserved in FloatingRail.sidebar1.tsx (not mounted).
 * Language switch + profile moved out to TopRightUserControls (top-right of the page,
 * not tied to the rail's hover/collapse state). Logout stays here in the sidebar.
 */
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useRouterState } from '@tanstack/react-router'
import {
  Building2,
  FileSearch,
  HelpCircle,
  Loader2,
  LogOut,
  Sparkles,
  Store,
} from 'lucide-react'
import { useState, type CSSProperties } from 'react'
import { toast } from 'sonner'
import { api } from '../api'
import { useT } from '../hooks/useT'
import { messages, type MessageKey } from '../i18n'
import { fromOperatorMe } from '../lib/operator'
import { useSession } from '../store'

type NavItem = {
  to: string
  search?: Record<string, undefined>
  hash?: string
  labelKey: MessageKey
  /** Short label for the mobile bottom bar (keeps 5 tabs from colliding). */
  mobileLabelKey?: MessageKey
  /** Optional Kannada override to match the Sidebar 2 mock copy. */
  knOverride?: string
  icon: typeof Sparkles
  match: (path: string, hash: string) => boolean
}

/** Citizen primary — AI discovery, Track, Service Catalog, Assisted Centre, Help. */
const CITIZEN_PRIMARY_NAV: NavItem[] = [
  {
    to: '/',
    search: { loginError: undefined },
    labelKey: 'aiDiscovery',
    mobileLabelKey: 'navAiShort',
    knOverride: 'ಸೇವೆ ಹುಡುಕಿ',
    icon: Sparkles,
    match: (p) => p === '/',
  },
  {
    to: '/track',
    labelKey: 'trackApp',
    mobileLabelKey: 'navTrackShort',
    knOverride: 'ಅರ್ಜಿ ಸ್ಥಿತಿ',
    icon: FileSearch,
    match: (p) => p.startsWith('/track'),
  },
  {
    to: '/catalog',
    labelKey: 'catalog',
    mobileLabelKey: 'navCatalogShort',
    icon: Building2,
    match: (p) => p === '/catalog' || p.startsWith('/catalog/') || p.startsWith('/services') || p.startsWith('/departments'),
  },
  {
    to: '/operator',
    labelKey: 'assistedCentre',
    mobileLabelKey: 'navAssistedShort',
    knOverride: 'ಕೌಂಟರ್ ಸೇವೆ',
    icon: Store,
    match: (p) => p.startsWith('/operator'),
  },
  {
    to: '/help',
    labelKey: 'help',
    icon: HelpCircle,
    match: (p) => p.startsWith('/help'),
  },
]

const MOBILE_NAV: NavItem[] = CITIZEN_PRIMARY_NAV

export function FloatingRail() {
  const { me, loaded, logout, authBusy, setAuthBusy } = useSession()
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
  const [expandedItemKey, setExpandedItemKey] = useState<string | null>(null)
  /** After a nav click, keep the rail collapsed until the pointer leaves (hover would otherwise keep it open). */
  const [lockCollapsed, setLockCollapsed] = useState(false)

  const operatorQ = useQuery({
    queryKey: ['operator', 'me'],
    queryFn: async () => fromOperatorMe(await api.operatorMe()),
    retry: false,
  })
  const operator = operatorQ.data ?? null
  const signedIn = !!me || !!operator
  const busy = railBusy || !!authBusy

  const onLogout = async () => {
    if (busy) return
    setAuthBusy('signing-out')
    setRailBusy(true)
    try {
      if (operator) {
        await api.operatorLogout()
        // Set, not invalidate: invalidateQueries triggers a refetch, and that refetch now
        // 401s (the session is gone) -- React Query keeps the last successful `data` around
        // on a failed refetch by default, so the avatar/nav stayed showing the logged-out
        // operator until a hard page refresh started a fresh QueryClient. Writing null
        // directly updates every subscriber of this key (TopRightUserControls included)
        // immediately, with no network round trip that could fail this same way.
        queryClient.setQueryData(['operator', 'me'], null)
        queryClient.setQueryData(['operator', 'session'], null)
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

  const primaryNav = CITIZEN_PRIMARY_NAV

  // Exactly one primary item may be active (first match wins — keep matchers mutually exclusive).
  const activePrimaryKey = (() => {
    const hit = primaryNav.find((item) => item.match(path, hash))
    return hit ? `${hit.to}|${hit.hash ?? ''}|${hit.labelKey}` : null
  })()

  const collapseRail = () => {
    setLockCollapsed(true)
    setExpandedItemKey(null)
    const active = document.activeElement
    if (active instanceof HTMLElement) active.blur()
  }

  const goNav = (item: { to: string; search?: Record<string, undefined>; hash?: string }) => {
    collapseRail()
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
      case '/catalog':
        void navigate({ to: '/catalog', search: { dept: undefined, sub: undefined, service: undefined }, hash: item.hash }).then(afterNavigation)
        break
      case '/track':
        void navigate({ to: '/track', hash: item.hash }).then(afterNavigation)
        break
      case '/operator':
        void navigate({ to: '/operator', hash: item.hash }).then(afterNavigation)
        break
      case '/admin/catalog':
        void navigate({ to: '/admin/catalog', hash: item.hash }).then(afterNavigation)
        break
      case '/help':
        void navigate({ to: '/help', hash: item.hash }).then(afterNavigation)
        break
      default:
        toast.error('This navigation destination is unavailable')
    }
  }

  const enLabel = (key: MessageKey) => messages.en[key]
  const knLabelFor = (key: MessageKey, knOverride?: string) => knOverride ?? messages.kn[key]
  /** Active language on top; the other language as the small subtitle. */
  const bilingual = (key: MessageKey, knOverride?: string) => {
    const en = enLabel(key)
    const kn = knLabelFor(key, knOverride)
    return lang === 'kn' ? { primary: kn, secondary: en } : { primary: en, secondary: kn }
  }

  // One explicit key controls both the parent rail width and the visible label. This
  // avoids broad nav hover state accidentally expanding more than one menu item.
  const labelClass = (expanded: boolean) =>
    `min-w-0 overflow-hidden transition-all duration-150 ease-out ${
      expanded && !lockCollapsed
        ? 'max-h-12 max-w-[10rem] opacity-100'
        : 'max-h-0 max-w-0 opacity-0'
    }`

  const itemClass = (active: boolean, expanded: boolean) =>
    `relative flex rounded-xl border py-2 text-left transition-all duration-150 ease-out ${
      expanded && !lockCollapsed
        ? 'w-full items-start justify-start gap-2.5 px-2.5'
        : 'w-9 items-center justify-center gap-0 px-2'
    } ${
      active
        ? 'border-[color:var(--rail2-accent)]/40 bg-[color:var(--rail2-accent-soft)] text-[color:var(--rail2-accent)]'
        : 'border-transparent text-[#5b616e] hover:border-[#ebe9f0] hover:bg-[#fafafb] hover:text-[#2f3542]'
    }`

  const iconClass = (active?: boolean) =>
    `h-4 w-4 shrink-0 transition-colors ${
      active ? 'text-[color:var(--rail2-accent)]' : 'text-[#6b7280]'
    }`

  const renderPrimaryItem = (item: NavItem) => {
    const key = `${item.to}|${item.hash ?? ''}|${item.labelKey}`
    const active = activePrimaryKey === key
    const expanded = expandedItemKey === key
    const labels = bilingual(item.labelKey, item.knOverride)
    return (
      <li key={key}>
        <button
          type="button"
          title={lockCollapsed ? `${labels.primary} · ${labels.secondary}` : undefined}
          aria-label={labels.primary}
          aria-current={active ? 'page' : undefined}
          className={itemClass(active, expanded)}
          onPointerEnter={() => setExpandedItemKey(key)}
          onFocus={() => setExpandedItemKey(key)}
          onClick={() => goNav(item)}
        >
          <item.icon className={iconClass(active)} strokeWidth={active ? 2.1 : 1.75} />
          <span className={labelClass(expanded)}>
            <span
              className={`block truncate text-[0.8125rem] font-semibold leading-tight whitespace-nowrap ${
                active ? 'text-[color:var(--rail2-accent)]' : 'text-[#2f3542]'
              }`}
            >
              {labels.primary}
            </span>
            <span
              className={`mt-0.5 block truncate text-[0.65rem] leading-tight whitespace-nowrap ${
                active ? 'text-[color:var(--rail2-accent)]/65' : 'text-[#8b919c]'
              }`}
            >
              {labels.secondary}
            </span>
          </span>
        </button>
      </li>
    )
  }

  return (
    <>
      {/* Sidebar 2 — the rail itself widens on hover. Only the hovered row reveals its
          text, so surrounding rows remain icon-only within the expanded container. */}
      <nav
        aria-label={t('railNavAria')}
        onPointerLeave={() => {
          setExpandedItemKey(null)
          setLockCollapsed(false)
        }}
        onBlur={(event) => {
          if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setExpandedItemKey(null)
        }}
        className={`fixed left-3 top-1/2 z-40 hidden max-h-[min(92vh,40rem)] -translate-y-1/2 flex-col overflow-hidden p-1.5 transition-[width] duration-150 ease-out md:flex ${
          !lockCollapsed && expandedItemKey ? 'w-[13rem]' : 'w-12'
        }`}
        style={
          {
            ['--rail2-accent']: '#5b4a8a',
            ['--rail2-accent-soft']: '#f0ecf8',
          } as CSSProperties
        }
      >
        <ul className="flex flex-col gap-0.5">
          {CITIZEN_PRIMARY_NAV.map((item) => renderPrimaryItem(item))}

          {loaded && signedIn ? (
            <li>
              {(() => {
                const key = 'logout'
                const expanded = expandedItemKey === key
                return (
                  <button
                    type="button"
                    disabled={busy}
                    title={lockCollapsed ? t('logout') : undefined}
                    aria-label={t('logout')}
                    className={`${itemClass(false, expanded)} disabled:pointer-events-none disabled:opacity-60`}
                    onPointerEnter={() => setExpandedItemKey(key)}
                    onFocus={() => setExpandedItemKey(key)}
                    onClick={() => void onLogout()}
                  >
                    {busy && authBusy === 'signing-out' ? (
                      <Loader2 className="h-4 w-4 shrink-0 animate-spin text-[#6b7280]" />
                    ) : (
                      <LogOut className="h-4 w-4 shrink-0 text-[#6b7280]" strokeWidth={1.75} />
                    )}
                    <span className={labelClass(expanded)}>
                      <span className="block text-[0.8125rem] font-semibold leading-tight whitespace-nowrap text-[#2f3542]">
                        {busy && authBusy === 'signing-out' ? '…' : bilingual('logout').primary}
                      </span>
                      <span className="mt-0.5 block text-[0.65rem] leading-tight whitespace-nowrap text-[#8b919c]">
                        {bilingual('logout').secondary}
                      </span>
                    </span>
                  </button>
                )
              })()}
            </li>
          ) : null}
        </ul>
      </nav>

      <nav
        aria-label={t('railMobileNavAria')}
        className="fixed inset-x-0 bottom-0 z-40 border-t border-border/80 bg-card/95 px-1.5 pb-[max(0.5rem,env(safe-area-inset-bottom))] pt-1.5 shadow-[0_-8px_30px_-18px_rgba(26,35,50,0.35)] backdrop-blur-xl md:hidden"
      >
        <ul className="mx-auto flex max-w-lg items-stretch justify-between gap-0.5">
          {MOBILE_NAV.map((item) => {
            const key = `mbar-${item.to}|${item.hash ?? ''}|${item.labelKey}`
            const active = activePrimaryKey === `${item.to}|${item.hash ?? ''}|${item.labelKey}`
            const mobileKey = item.mobileLabelKey ?? item.labelKey
            const label = lang === 'kn' ? messages.kn[mobileKey] : messages.en[mobileKey]
            return (
              <li key={key} className="min-w-0 flex-1">
                <button
                  type="button"
                  title={bilingual(item.labelKey, item.knOverride).primary}
                  aria-label={bilingual(item.labelKey, item.knOverride).primary}
                  className={`flex w-full min-w-0 flex-col items-center gap-0.5 overflow-hidden rounded-xl border px-0.5 py-1.5 text-[10px] font-medium leading-tight ${
                    active
                      ? 'border-[#5b4a8a]/40 bg-[#f0ecf8] text-[#5b4a8a]'
                      : 'border-transparent text-muted-foreground'
                  }`}
                  onClick={() => goNav(item)}
                >
                  <item.icon className="h-4 w-4 shrink-0" strokeWidth={active ? 2.1 : 1.75} />
                  <span className="w-full truncate text-center">{label}</span>
                </button>
              </li>
            )
          })}
        </ul>
      </nav>
    </>
  )
}
