import { Link, useSearch } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import {
  ArrowRight,
  Bell,
  Building2,
  CheckCheck,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  CircleAlert,
  ClipboardList,
  Clock3,
  FileSearch,
  LayoutGrid,
  Sparkles,
  X,
  XCircle,
} from 'lucide-react'
import { type FormEvent, type ReactNode, useEffect, useMemo, useState } from 'react'
import { api } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { Badge } from '../components/ui/badge'
import { useT } from '../hooks/useT'
import { friendlyAudit } from '../lib/auditLabels'
import { seededActivity, seededNotifications } from '../lib/dashboardSeed'
import { departmentName as deptNameKn, serviceName as svcNameKn } from '../lib/catalogKn'
import { statusBucket, statusTone } from '../lib/statusTone'
import { useSession } from '../store'

const READ_NOTIFICATIONS_KEY = 'seva.dashboard.readNotifications'

function loadReadIds(): Set<string> {
  try {
    const raw = localStorage.getItem(READ_NOTIFICATIONS_KEY)
    return raw ? new Set(JSON.parse(raw) as string[]) : new Set()
  } catch {
    return new Set()
  }
}

function ProfileFieldLabel({
  children,
  filled,
  required = false,
}: {
  readonly children: ReactNode
  readonly filled: boolean
  readonly required?: boolean
}) {
  const { t } = useT()
  return (
    <span className="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-[color:var(--gov-navy)]">
      {filled ? <CheckCircle2 className="h-4 w-4 text-emerald-600" aria-label={t('statusCompleted')} /> : <CircleAlert className="h-4 w-4 text-amber-600" aria-label={required ? 'Required field missing' : 'Field missing'} />}
      {children}
      {required ? <span className="text-red-500">*</span> : null}
    </span>
  )
}

function ProfileFieldHint({ filled, required = false }: { readonly filled: boolean; readonly required?: boolean }) {
  if (filled) return null

  return <p className="mt-1.5 text-xs font-medium text-amber-700">{required ? 'Required field' : 'Missing field'}</p>
}

export function DashboardPage() {
  const { me } = useSession()
  const { lang, t } = useT()
  const { ref } = useSearch({ from: '/dashboard' })

  const apps = useQuery({ queryKey: ['apps'], queryFn: () => api.applications(), enabled: !!me })
  const notifications = useQuery({
    queryKey: ['me', 'notifications'],
    queryFn: () => api.notifications(),
    enabled: !!me,
  })
  const audit = useQuery({
    queryKey: ['me', 'audit'],
    queryFn: () => api.audit(),
    enabled: !!me,
  })

  const mergedApps = apps.data ?? []

  const appStats = useMemo(() => {
    const stats = { total: mergedApps.length, pending: 0, completed: 0, rejected: 0 }
    for (const a of mergedApps) {
      stats[statusBucket(a.status)] += 1
    }
    return stats
  }, [mergedApps])

  const [readIds, setReadIds] = useState<Set<string>>(() => loadReadIds())
  const [showAllNotifications, setShowAllNotifications] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const [profileSaving, setProfileSaving] = useState(false)
  const [profileError, setProfileError] = useState<string | null>(null)
  const [profileForm, setProfileForm] = useState({
    name: '',
    mobile: '',
    email: '',
    address: '',
    district: '',
    taluk: '',
    stateName: '',
    villageName: '',
    dateOfBirth: '',
    gender: '',
    aadhaarLast4: '',
  })
  const visibleNotifications = seededNotifications(me, notifications.data)
  const visibleActivity = seededActivity(me, audit.data)
  const unreadCount = visibleNotifications.filter((n) => !readIds.has(n.id)).length
  const markAllRead = () => {
    const ids = new Set(readIds)
    for (const n of visibleNotifications) ids.add(n.id)
    setReadIds(ids)
    localStorage.setItem(READ_NOTIFICATIONS_KEY, JSON.stringify([...ids]))
  }

  const notifyCount = visibleNotifications.length
  const firstName = me?.name?.split(/\s+/)[0] ?? ''
  const profileFields = [
    ['Full name', me?.name],
    ['Seva ID', me?.sevaId],
    ['Mobile number', me?.mobile],
    ['Email address', me?.email],
    ['Date of birth', me?.dateOfBirth],
    ['Gender', me?.gender],
    ['Aadhaar', me?.aadhaarMasked],
    ['Address', me?.address],
    ['District', me?.district],
    ['Taluk', me?.taluk],
    ['Village', me?.villageName],
    ['State', me?.stateName],
  ] as const
  const completedProfileFields = profileFields.filter(([, value]) => Boolean(value?.trim())).length
  const profilePercent = Math.round((completedProfileFields / profileFields.length) * 100)

  useEffect(() => {
    if (!profileOpen) return
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setProfileOpen(false)
    }
    window.addEventListener('keydown', closeOnEscape)
    return () => window.removeEventListener('keydown', closeOnEscape)
  }, [profileOpen])

  const openProfileModal = () => {
    setProfileError(null)
    setProfileForm({
      name: me?.name ?? '',
      mobile: me?.mobile ?? '',
      email: me?.email ?? '',
      address: me?.address ?? '',
      district: me?.district ?? '',
      taluk: me?.taluk ?? '',
      stateName: me?.stateName ?? '',
      villageName: me?.villageName ?? '',
      dateOfBirth: me?.dateOfBirth?.slice(0, 10) ?? '',
      gender: me?.gender ?? '',
      aadhaarLast4: me?.aadhaarMasked?.slice(-4) ?? '',
    })
    setProfileOpen(true)
  }

  const saveProfile = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    try {
      setProfileSaving(true)
      setProfileError(null)
      await api.updateProfile({
        name: profileForm.name,
        mobile: profileForm.mobile,
        email: profileForm.email || undefined,
        address: profileForm.address,
        district: profileForm.district,
        taluk: profileForm.taluk,
        stateName: profileForm.stateName || undefined,
        villageName: profileForm.villageName || undefined,
        dateOfBirth: profileForm.dateOfBirth || undefined,
        gender: profileForm.gender || undefined,
        aadhaarLast4: profileForm.aadhaarLast4 || undefined,
      })
      await useSession.getState().refresh()
      setProfileOpen(false)
    } catch (err) {
      setProfileError(err instanceof Error ? err.message : 'Could not save profile')
    } finally {
      setProfileSaving(false)
    }
  }

  if (!me) {
    return (
      <div className="mx-auto max-w-6xl px-4 py-10">
        <div className="overflow-hidden rounded-3xl border border-border bg-card shadow-[var(--shadow-soft)]">
          <div className="gov-gradient px-8 py-10 text-white">
            <h1 className="text-2xl font-bold md:text-3xl">{t('dashTitle')}</h1>
            <Breadcrumbs />
            <p className="mt-3 max-w-md text-sm text-white/85">{t('dashSignInHint')}</p>
            <Link
              to="/login"
              search={{
                returnTo: ref ? `/dashboard?ref=${encodeURIComponent(ref)}` : '/dashboard',
                loginError: undefined,
                mode: undefined,
              }}
              className="mt-6 inline-flex items-center gap-2 rounded-xl bg-white px-4 py-2.5 text-sm font-semibold text-[color:var(--gov-navy)] transition hover:bg-white/95"
            >
              {t('signIn')} <ArrowRight className="h-4 w-4" />
            </Link>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="relative min-h-[70vh]">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-72 bg-[radial-gradient(90%_70%_at_10%_0%,color-mix(in_oklab,var(--gov-blue)_14%,transparent),transparent_70%),radial-gradient(70%_60%_at_90%_10%,color-mix(in_oklab,var(--gov-green)_10%,transparent),transparent_65%)]"
      />

      <div className="relative mx-auto max-w-6xl px-4 py-8 md:py-10">
        {/* Welcome */}
        <header className="flex flex-col gap-6 md:flex-row md:items-end md:justify-between">
          <div className="min-w-0">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[color:var(--gov-blue)]">
              {t('brandTwoWord')}
            </p>
            <h1 className="mt-1 text-3xl font-bold tracking-tight text-[color:var(--gov-navy)] md:text-4xl">
              {firstName ? `Welcome, ${firstName}` : t('dashTitle')}
            </h1>
            <Breadcrumbs />
            <p className="mt-2 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-muted-foreground">
              <span className="font-medium text-foreground">{me.name}</span>
              <span aria-hidden>·</span>
              <span className="font-mono text-xs">{me.sevaId}</span>
              {me.aadhaarMasked ? (
                <>
                  <span aria-hidden>·</span>
                  <span className="font-mono text-xs">{me.aadhaarMasked}</span>
                </>
              ) : null}
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <Link
              to="/"
              search={{ loginError: undefined }}
              className="inline-flex items-center gap-2 rounded-xl bg-[color:var(--gov-blue)] px-3.5 py-2 text-sm font-semibold text-white shadow-sm transition hover:opacity-95"
            >
              <Sparkles className="h-4 w-4" /> AI discovery
            </Link>
            <Link
              to="/track"
              className="inline-flex items-center gap-2 rounded-xl border border-border bg-card px-3.5 py-2 text-sm font-semibold text-[color:var(--gov-navy)] transition hover:border-[color:var(--gov-blue)]/35 hover:bg-[color:var(--gov-blue)]/5"
            >
              <FileSearch className="h-4 w-4" /> Track
            </Link>
            <Link
              to="/services"
              search={{ q: undefined, department: undefined }}
              className="inline-flex items-center gap-2 rounded-xl border border-border bg-card px-3.5 py-2 text-sm font-semibold text-[color:var(--gov-navy)] transition hover:border-[color:var(--gov-blue)]/35 hover:bg-[color:var(--gov-blue)]/5"
            >
              <LayoutGrid className="h-4 w-4" /> Services
            </Link>
          </div>
        </header>

        {/* Snapshot stats */}
        <section className="mt-8 grid gap-3 lg:grid-cols-2">
          <section className="grid grid-cols-2 gap-3">
          <div className="rounded-2xl border border-border/80 bg-card/90 p-4 shadow-[var(--shadow-soft)] backdrop-blur-sm">
            <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              <ClipboardList className="h-3.5 w-3.5 text-[color:var(--gov-blue)]" />
              {t('statusTotal')}
            </div>
            <p className="mt-2.5 text-3xl font-bold tabular-nums text-[color:var(--gov-navy)]">{appStats.total}</p>
          </div>
          <div className="rounded-2xl border border-border/80 bg-card/90 p-4 shadow-[var(--shadow-soft)] backdrop-blur-sm">
            <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              <Clock3 className="h-3.5 w-3.5 text-amber-600" />
              {t('statusPending')}
            </div>
            <p className="mt-2.5 text-3xl font-bold tabular-nums text-amber-600">{appStats.pending}</p>
          </div>
          <div className="rounded-2xl border border-border/80 bg-card/90 p-4 shadow-[var(--shadow-soft)] backdrop-blur-sm">
            <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              <CheckCircle2 className="h-3.5 w-3.5 text-emerald-600" />
              {t('statusCompleted')}
            </div>
            <p className="mt-2.5 text-3xl font-bold tabular-nums text-emerald-600">{appStats.completed}</p>
          </div>
          <div className="rounded-2xl border border-border/80 bg-card/90 p-4 shadow-[var(--shadow-soft)] backdrop-blur-sm">
            <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              <XCircle className="h-3.5 w-3.5 text-red-600" />
              {t('trackStatusRejected')}
            </div>
            <p className="mt-2.5 text-3xl font-bold tabular-nums text-red-600">{appStats.rejected}</p>
          </div>
          </section>

          <section className="grid gap-3">
          <div className="rounded-2xl border border-border/80 bg-card/90 p-4 shadow-[var(--shadow-soft)] backdrop-blur-sm">
            <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              <Bell className="h-3.5 w-3.5 text-[color:var(--gov-blue)]" />
              {t('dashNotifications')}
            </div>
            <p className="mt-2 text-3xl font-bold tabular-nums text-[color:var(--gov-navy)]">
              {notifyCount}
            </p>
          </div>
          <button
            type="button"
            onClick={openProfileModal}
            className="group relative rounded-2xl border border-border/80 bg-card/90 p-4 pr-10 text-left shadow-[var(--shadow-soft)] backdrop-blur-sm transition hover:-translate-y-0.5 hover:border-[color:var(--gov-blue)]/35 hover:shadow-[var(--shadow-lift)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[color:var(--gov-blue)]/40"
            aria-haspopup="dialog"
          >
            <ChevronRight className="absolute right-3 top-1/2 h-5 w-5 -translate-y-1/2 text-muted-foreground transition group-hover:text-[color:var(--gov-blue)]" aria-hidden />
            <div className="flex items-center gap-3">
              {(() => {
                const pct = profilePercent
                const size = 52
                const stroke = 5
                const r = (size - stroke) / 2
                const c = 2 * Math.PI * r
                const offset = c * (1 - pct / 100)
                return (
                  <div
                    className="relative shrink-0"
                    style={{ width: size, height: size }}
                    role="img"
                    aria-label={`Profile ${pct}% completed`}
                  >
                    <svg width={size} height={size} className="-rotate-90" aria-hidden>
                      <circle
                        cx={size / 2}
                        cy={size / 2}
                        r={r}
                        fill="none"
                        stroke="color-mix(in oklab, var(--gov-blue) 12%, transparent)"
                        strokeWidth={stroke}
                      />
                      <circle
                        cx={size / 2}
                        cy={size / 2}
                        r={r}
                        fill="none"
                        stroke="var(--gov-blue)"
                        strokeWidth={stroke}
                        strokeLinecap="round"
                        strokeDasharray={c}
                        strokeDashoffset={offset}
                      />
                    </svg>
                    <span className="absolute inset-0 flex items-center justify-center text-[11px] font-bold tabular-nums text-[color:var(--gov-navy)]">
                      {pct}%
                    </span>
                  </div>
                )
              })()}
              <div className="min-w-0">
                <div className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  <Building2 className="h-3.5 w-3.5 text-[color:var(--gov-blue)]" />
                  {t('dashProfile')}
                </div>
                <p className="mt-1 text-sm font-semibold text-[color:var(--gov-navy)]">{profilePercent}% completed</p>
                <p className="mt-0.5 truncate text-xs text-muted-foreground">
                  {me.district || me.taluk
                    ? [me.district, me.taluk].filter(Boolean).join(' · ')
                    : me.username}
                </p>
              </div>
            </div>
          </button>
          </section>
        </section>

        {/* Application tracking timeline moved to /track — see TrackPage. */}

        {/* Main grid: applications + feed */}
        <div className="mt-8 grid gap-6 lg:grid-cols-[1.35fr_1fr]">
          <section>
            <div className="mb-4">
              <h2 className="text-lg font-semibold text-[color:var(--gov-navy)]">{t('dashApps')}</h2>
              <p className="text-sm text-muted-foreground">{t('dashOpenAppHint')}</p>
            </div>

            {mergedApps.length === 0 ? (
              <div className="rounded-3xl border border-dashed border-border bg-card/60 px-6 py-12 text-center">
                <ClipboardList className="mx-auto h-8 w-8 text-muted-foreground/50" />
                <p className="mt-3 text-sm text-muted-foreground">{t('dashNoApps')}</p>
                <Link
                  to="/"
                  search={{ loginError: undefined }}
                  className="mt-4 inline-flex items-center gap-1.5 text-sm font-semibold text-[color:var(--gov-blue)]"
                >
                  Start with AI discovery <ArrowRight className="h-3.5 w-3.5" />
                </Link>
              </div>
            ) : (
              <ul className="space-y-3">
                {mergedApps.map((a) => {
                  const selected = ref === a.sevaReference
                  return (
                    <li key={a.sevaReference}>
                      <Link
                        to="/dashboard"
                        search={{ ref: a.sevaReference }}
                        className={`group block rounded-2xl border bg-card p-4 shadow-[var(--shadow-soft)] transition hover:-translate-y-0.5 hover:shadow-[var(--shadow-lift)] ${
                          selected
                            ? 'border-[color:var(--gov-blue)] ring-1 ring-[color:var(--gov-blue)]/25'
                            : 'border-border/80 hover:border-[color:var(--gov-blue)]/35'
                        }`}
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div className="min-w-0">
                            <div className="font-semibold text-[color:var(--gov-navy)] group-hover:text-[color:var(--gov-blue)]">
                              {svcNameKn(lang, a.serviceCode, a.serviceName)}
                            </div>
                            <div className="mt-0.5 font-mono text-xs text-muted-foreground">
                              {a.sevaReference}
                            </div>
                          </div>
                          <span
                            className={`shrink-0 rounded-full border px-2.5 py-0.5 text-[11px] font-semibold ${statusTone(a.status)}`}
                          >
                            {a.status}
                          </span>
                        </div>
                        <div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                          <Badge variant="secondary" className="font-normal">
                            {deptNameKn(lang, a.departmentCode, a.departmentName)}
                          </Badge>
                          <span>
                            Submitted{' '}
                            {new Date(a.submittedAt).toLocaleDateString(undefined, {
                              day: 'numeric',
                              month: 'short',
                              year: 'numeric',
                            })}
                          </span>
                          <span aria-hidden>·</span>
                          <span>
                            Due{' '}
                            {new Date(a.sakalaDueAt).toLocaleDateString(undefined, {
                              day: 'numeric',
                              month: 'short',
                            })}
                          </span>
                        </div>
                      </Link>
                    </li>
                  )
                })}
              </ul>
            )}
          </section>

          <aside>
            <div className="mb-4">
              <div className="flex items-center justify-between gap-2">
                <div className="flex items-center gap-2">
                  <h2 className="text-lg font-semibold text-[color:var(--gov-navy)]">{t('dashNotifications')}</h2>
                  {unreadCount > 0 ? <span className="rounded-full bg-[color:var(--gov-blue)]/10 px-2 py-0.5 text-[11px] font-semibold text-[color:var(--gov-blue)]">{unreadCount}</span> : null}
                </div>
                {unreadCount > 0 ? (
                  <button
                    type="button"
                    onClick={markAllRead}
                    className="flex items-center gap-1 text-xs font-medium text-[color:var(--gov-blue)] transition hover:underline"
                  >
                    <CheckCheck className="h-3.5 w-3.5" /> Mark all as read
                  </button>
                ) : null}
              </div>
              <p className="text-sm text-muted-foreground">{t('dashNotifyHint')}</p>
            </div>
            <section className="rounded-3xl border border-border/80 bg-card p-5 shadow-[var(--shadow-soft)]">
              <ul className="space-y-3">
                {visibleNotifications.length === 0 && (
                  <li className="text-sm text-muted-foreground">{t('dashNoNotify')}</li>
                )}
                {visibleNotifications
                  .slice(0, showAllNotifications ? undefined : 3)
                  .map((n) => {
                    const isRead = readIds.has(n.id)
                    return (
                      <li
                        key={n.id}
                        className={`relative rounded-xl border border-transparent px-3 py-2.5 transition hover:border-border ${
                          isRead ? 'bg-muted/20' : 'bg-muted/40'
                        }`}
                      >
                        {!isRead ? (
                          <span
                            className="absolute right-2.5 top-2.5 h-1.5 w-1.5 rounded-full bg-[color:var(--gov-blue)]"
                            aria-hidden
                          />
                        ) : null}
                        <div
                          className={`text-sm ${
                            isRead ? 'font-normal text-muted-foreground' : 'font-medium text-[color:var(--gov-navy)]'
                          }`}
                        >
                          {n.subject}
                        </div>
                        <div className="mt-0.5 text-xs leading-relaxed text-muted-foreground">{n.body}</div>
                        <div className="mt-1.5 text-[11px] text-muted-foreground">
                          {n.channel} · {new Date(n.createdAt).toLocaleString()}
                          {n.sentAt ? ' · sent' : ' · queued'}
                        </div>
                      </li>
                    )
                  })}
              </ul>
              {visibleNotifications.length > 3 ? (
                <button
                  type="button"
                  onClick={() => setShowAllNotifications((v) => !v)}
                  className="mt-3 flex w-full items-center justify-center gap-1 rounded-lg border border-border/70 py-1.5 text-xs font-medium text-muted-foreground transition hover:border-[color:var(--gov-blue)]/30 hover:text-[color:var(--gov-blue)]"
                >
                  {showAllNotifications ? 'Show less' : `View more (${visibleNotifications.length - 3})`}
                  <ChevronDown
                    className={`h-3.5 w-3.5 transition-transform ${showAllNotifications ? 'rotate-180' : ''}`}
                  />
                </button>
              ) : null}
            </section>

          </aside>
        </div>

        <section className="mt-6 rounded-3xl border border-border/80 bg-card p-5 shadow-[var(--shadow-soft)]">
          <div className="flex items-center gap-2">
            <h2 className="text-base font-semibold text-[color:var(--gov-navy)]">{t('dashActivity')}</h2>
            {visibleActivity.length > 0 ? (
              <span className="rounded-full bg-[color:var(--gov-blue)]/10 px-2 py-0.5 text-[11px] font-semibold text-[color:var(--gov-blue)]">
                {visibleActivity.length}
              </span>
            ) : null}
          </div>
          <div className="mt-4">
            {visibleActivity.length === 0 && <p className="text-sm text-muted-foreground">{t('dashNoActivity')}</p>}
            {(() => {
              const items = visibleActivity.slice(0, 8)
              const groups: { dateKey: string; items: typeof items }[] = []
              for (const a of items) {
                const dateKey = new Date(a.at).toLocaleDateString(undefined, {
                  day: 'numeric',
                  month: 'long',
                  year: 'numeric',
                })
                const existing = groups.find((g) => g.dateKey === dateKey)
                if (existing) existing.items.push(a)
                else groups.push({ dateKey, items: [a] })
              }
              return groups.map((g, gi) => (
                <section key={g.dateKey} className={`relative pl-8 ${gi > 0 ? 'mt-5' : ''}`}>
                  <div aria-hidden className="absolute inset-y-0 left-0 w-6">
                    <span className="absolute left-0 top-[7px] h-px w-3 bg-border" />
                    <span className="absolute left-[11px] top-[3px] h-2.5 w-2.5 rounded-full border-2 border-muted-foreground bg-card" />
                    <span className="absolute left-[21px] top-[7px] h-px w-3 bg-border" />
                    {gi < groups.length - 1 ? <span className="absolute bottom-[-20px] left-[12px] top-5 w-px bg-border" /> : null}
                  </div>
                  <h3 className="border-b border-border/80 pb-2 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
                    Activity on {g.dateKey}
                  </h3>
                  <ul className="mt-3 overflow-hidden rounded-xl border border-border/80 bg-card">
                    {g.items.map((a, i) => {
                      const { title, subtitle } = friendlyAudit(a, lang)
                      return (
                        <li key={a.id} className={`px-4 py-3.5 ${i > 0 ? 'border-t border-border/80' : ''}`}>
                          <div className="flex items-start justify-between gap-3">
                            <div className="min-w-0">
                              <div className="text-sm font-semibold text-[color:var(--gov-navy)]">{title}</div>
                              <div className="mt-0.5 text-xs text-muted-foreground">{subtitle}</div>
                            </div>
                            <time dateTime={a.at} className="shrink-0 whitespace-nowrap pt-0.5 text-[11px] text-muted-foreground">
                              {new Date(a.at).toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })}
                            </time>
                          </div>
                        </li>
                      )
                    })}
                  </ul>
                </section>
              ))
            })()}
          </div>
        </section>
      </div>

      {profileOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-[color:var(--gov-navy)]/45 p-4 backdrop-blur-sm"
          role="presentation"
          onMouseDown={() => setProfileOpen(false)}
        >
          <section
            role="dialog"
            aria-modal="true"
            aria-labelledby="profile-modal-title"
            className="max-h-[calc(100vh-2rem)] w-full max-w-3xl overflow-y-auto rounded-3xl border border-border bg-card shadow-2xl"
            onMouseDown={(event) => event.stopPropagation()}
          >
            <header className="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-border bg-card px-5 py-4 md:px-6">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[color:var(--gov-blue)]">{t('profileTitle')}</p>
                <h2 id="profile-modal-title" className="mt-1 text-xl font-semibold text-[color:var(--gov-navy)]">
                  {t('profileDetails')}
                </h2>
                <p className="mt-1 text-sm text-muted-foreground">
                  {completedProfileFields} of {profileFields.length} fields completed
                </p>
              </div>
              <button
                type="button"
                onClick={() => setProfileOpen(false)}
                className="rounded-lg p-2 text-muted-foreground transition hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[color:var(--gov-blue)]/40"
                aria-label={t('profileCloseAria')}
              >
                <X className="h-5 w-5" />
              </button>
            </header>

            <form className="grid gap-4 p-5 sm:grid-cols-2 md:p-6" onSubmit={(event) => void saveProfile(event)}>
              {profileError ? <p className="sm:col-span-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{profileError}</p> : null}

              <label className="block sm:col-span-2">
                <ProfileFieldLabel filled={Boolean(profileForm.name.trim())} required>{t('regFullName')}</ProfileFieldLabel>
                <input className="w-full rounded-lg border border-input bg-background px-3 py-2" value={profileForm.name} onChange={(event) => setProfileForm({ ...profileForm, name: event.target.value })} required />
                <ProfileFieldHint filled={Boolean(profileForm.name.trim())} required />
              </label>
              <label className="block">
                <ProfileFieldLabel filled={Boolean(profileForm.mobile.trim())} required>{t('regMobileSignIn')}</ProfileFieldLabel>
                <input className="w-full rounded-lg border border-input bg-background px-3 py-2" value={profileForm.mobile} onChange={(event) => setProfileForm({ ...profileForm, mobile: event.target.value.replace(/\D/g, '').slice(0, 15) })} inputMode="numeric" minLength={10} maxLength={15} pattern="\d{10,15}" title={t('regMobileHint')} placeholder={t('regMobilePlaceholder')} required />
                <ProfileFieldHint filled={Boolean(profileForm.mobile.trim())} required />
              </label>
              <label className="block">
                <ProfileFieldLabel filled={Boolean(profileForm.email.trim())}>{t('regEmail')}</ProfileFieldLabel>
                <input type="email" className="w-full rounded-lg border border-input bg-background px-3 py-2" value={profileForm.email} onChange={(event) => setProfileForm({ ...profileForm, email: event.target.value })} />
                <ProfileFieldHint filled={Boolean(profileForm.email.trim())} />
              </label>
              <label className="block sm:col-span-2">
                <ProfileFieldLabel filled={Boolean(profileForm.address.trim())} required>{t('regAddress')}</ProfileFieldLabel>
                <textarea className="min-h-20 w-full rounded-lg border border-input bg-background px-3 py-2" value={profileForm.address} onChange={(event) => setProfileForm({ ...profileForm, address: event.target.value })} required />
                <ProfileFieldHint filled={Boolean(profileForm.address.trim())} required />
              </label>
              <label className="block">
                <ProfileFieldLabel filled={Boolean(profileForm.district.trim())} required>{t('regDistrict')}</ProfileFieldLabel>
                <input className="w-full rounded-lg border border-input bg-background px-3 py-2" value={profileForm.district} onChange={(event) => setProfileForm({ ...profileForm, district: event.target.value })} required />
                <ProfileFieldHint filled={Boolean(profileForm.district.trim())} required />
              </label>
              <label className="block">
                <ProfileFieldLabel filled={Boolean(profileForm.taluk.trim())} required>{t('regTaluk')}</ProfileFieldLabel>
                <input className="w-full rounded-lg border border-input bg-background px-3 py-2" value={profileForm.taluk} onChange={(event) => setProfileForm({ ...profileForm, taluk: event.target.value })} required />
                <ProfileFieldHint filled={Boolean(profileForm.taluk.trim())} required />
              </label>
              <label className="block">
                <ProfileFieldLabel filled={Boolean(profileForm.stateName.trim())} required>{t('regState')}</ProfileFieldLabel>
                <input className="w-full rounded-lg border border-input bg-background px-3 py-2" value={profileForm.stateName} onChange={(event) => setProfileForm({ ...profileForm, stateName: event.target.value })} required />
                <ProfileFieldHint filled={Boolean(profileForm.stateName.trim())} required />
              </label>
              <label className="block">
                <ProfileFieldLabel filled={Boolean(profileForm.villageName.trim())} required>{t('regVillage')}</ProfileFieldLabel>
                <input className="w-full rounded-lg border border-input bg-background px-3 py-2" value={profileForm.villageName} onChange={(event) => setProfileForm({ ...profileForm, villageName: event.target.value })} required />
                <ProfileFieldHint filled={Boolean(profileForm.villageName.trim())} required />
              </label>
              <label className="block">
                <ProfileFieldLabel filled={Boolean(profileForm.dateOfBirth.trim())} required>{t('regDob')}</ProfileFieldLabel>
                <input type="date" className="w-full rounded-lg border border-input bg-background px-3 py-2" value={profileForm.dateOfBirth} onChange={(event) => setProfileForm({ ...profileForm, dateOfBirth: event.target.value })} required />
                <ProfileFieldHint filled={Boolean(profileForm.dateOfBirth.trim())} required />
              </label>
              <label className="block">
                <ProfileFieldLabel filled={Boolean(profileForm.gender.trim())} required>{t('regGender')}</ProfileFieldLabel>
                <select className="w-full rounded-lg border border-input bg-background px-3 py-2" value={profileForm.gender} onChange={(event) => setProfileForm({ ...profileForm, gender: event.target.value })} required>
                  <option value="" disabled>{t('selectPlaceholder')}</option>
                  <option value="Male">{t('genderMale')}</option>
                  <option value="Female">{t('genderFemale')}</option>
                  <option value="Other">{t('genderOther')}</option>
                  <option value="PreferNotToSay">{t('genderUnspecified')}</option>
                </select>
                <ProfileFieldHint filled={Boolean(profileForm.gender.trim())} required />
              </label>
              <label className="block">
                <ProfileFieldLabel filled={Boolean(profileForm.aadhaarLast4.trim())}>{t('regAadhaarLast4')}</ProfileFieldLabel>
                <input className="w-full rounded-lg border border-input bg-background px-3 py-2" value={profileForm.aadhaarLast4} onChange={(event) => setProfileForm({ ...profileForm, aadhaarLast4: event.target.value.replace(/\D/g, '').slice(0, 4) })} inputMode="numeric" maxLength={4} />
                <ProfileFieldHint filled={Boolean(profileForm.aadhaarLast4.trim())} />
              </label>
              {(me?.nameKannada || me?.addressKannada) ? (
                <div className="sm:col-span-2 rounded-lg border border-input bg-muted px-3 py-2 text-sm text-muted-foreground">
                  <p className="font-medium text-foreground">ಕನ್ನಡದಲ್ಲಿ (as per DigiLocker, in Kannada)</p>
                  {me.nameKannada ? <p className="mt-1">{me.nameKannada}</p> : null}
                  {me.addressKannada ? <p>{[me.addressKannada, me.villageKannada, me.districtKannada, me.stateKannada].filter(Boolean).join(', ')}</p> : null}
                </div>
              ) : null}
              <div className="flex gap-3 sm:col-span-2 sm:justify-end">
                <button type="button" onClick={() => setProfileOpen(false)} className="rounded-lg border border-border px-4 py-2.5 text-sm font-medium text-muted-foreground transition hover:bg-muted">{t('regCancel')}</button>
                <button type="submit" disabled={profileSaving} className="rounded-lg bg-[color:var(--gov-blue)] px-4 py-2.5 text-sm font-medium text-white transition hover:opacity-90 disabled:opacity-60">{profileSaving ? 'Saving…' : 'Save profile'}</button>
              </div>
            </form>
          </section>
        </div>
      ) : null}
    </div>
  )
}
