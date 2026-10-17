import { useQueryClient } from '@tanstack/react-query'
import { NavLink, useLocation } from 'react-router-dom'
import type { ReactNode } from 'react'
import { AlarmClockCheck, BarChart3, Bell, BookOpen, BrainCircuit, Building2, ChevronRight, CircleUserRound, Compass, FileChartColumn, HeartPulse, History, Hourglass, LogOut, MapPinned, Megaphone, MessageCircle, RefreshCw, ShieldCheck, Store, TrendingUp, UserCog } from 'lucide-react'
import { govAdminApi } from '../api'
import type { GovAdminMe } from '../types'
import { Button } from './ui/button'
import { tierLabel as roleLabel } from '../lib/tierLabels'
import { GovAiChatWidget } from './GovAiChatWidget'

const groups = [
  { label: 'Core metrics', links: [{ to: '/', label: 'Overview', icon: BarChart3 }, { to: '/departments', label: 'Department Usage', icon: Building2 }, { to: '/districts', label: 'District Usage', icon: MapPinned }, { to: '/service-usage', label: 'Service Usage', icon: TrendingUp }] },
  { label: 'Access and citizens', links: [{ to: '/discovery', label: 'AI vs Catalogue', icon: Compass }, { to: '/citizens', label: 'Citizens and DigiLocker', icon: CircleUserRound }, { to: '/lifecycle', label: 'Service Lifecycle', icon: FileChartColumn }, { to: '/sakala-sla', label: 'Sakala SLA', icon: AlarmClockCheck }, { to: '/application-aging', label: 'Application Aging', icon: Hourglass }, { to: '/application-funnel', label: 'Application Funnel', icon: TrendingUp }] },
]

function initials(name: string) {
  const parts = name.trim().split(/\s+/)
  return ((parts[0]?.[0] ?? '') + (parts[parts.length - 1]?.[0] ?? '')).toUpperCase()
}

export function Shell({ admin, children }: { readonly admin: GovAdminMe; readonly children: ReactNode }) {
  const queryClient = useQueryClient()
  const location = useLocation()
  const canManageAdmins = admin.tier === 'Superadmin' || admin.tier === 'ChiefMinister'
  const adminLinks = [
    ...(canManageAdmins ? [{ to: '/admins', label: 'Admin Accounts', icon: UserCog }] : []),
    ...(canManageAdmins ? [{ to: '/activity-log', label: 'Activity Log', icon: History }] : []),
    ...(canManageAdmins ? [{ to: '/operators', label: 'Operators', icon: Store }] : []),
    ...(admin.tier === 'Superadmin' ? [{ to: '/service-notices', label: 'Service Notices', icon: Megaphone }] : []),
    ...(admin.tier === 'Superadmin' ? [{ to: '/notifications', label: 'Notifications', icon: Bell }] : []),
    ...(admin.tier === 'Superadmin' ? [{ to: '/health', label: 'System Health', icon: HeartPulse }] : []),
    ...(admin.tier === 'Superadmin' ? [{ to: '/catalogue', label: 'Catalogue management', icon: BookOpen }] : []),
  ]
  const aiLinks = [{ to: '/ai/assistant', label: 'AI assistant', icon: MessageCircle }, ...(admin.tier === 'Superadmin' ? [{ to: '/ai', label: 'AI backend status', icon: BrainCircuit }, { to: '/ai/reingest', label: 'Reingest catalogue', icon: RefreshCw }] : [])]
  const visibleGroups = [...groups, ...(aiLinks.length ? [{ label: 'AI operations', links: aiLinks }] : []), ...(adminLinks.length ? [{ label: 'Administration', links: adminLinks }] : [])]
  const accessSummary = admin.assignedTaluk
    ? `${admin.assignedTaluk}, ${admin.assignedDistrict}`
    : (admin.assignedDistrict ?? admin.assignedDepartment ?? 'Karnataka statewide')

  async function signOut() {
    try { await govAdminApi.govAdminLogout() } finally { await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'me'] }) }
  }

  return (
    <div className="flex h-screen flex-col overflow-hidden bg-[color:var(--background)] text-[color:var(--foreground)]">
      <div className="h-2 shrink-0 gov-stripe" />
      <header className="relative z-30 shrink-0 overflow-hidden border-b border-white/10 bg-gradient-to-r from-[#0b1f3f] via-[color:var(--gov-navy)] to-[#0b1f3f] text-white shadow-lg">
        <div className="pointer-events-none absolute -left-16 -top-20 h-56 w-56 rounded-full bg-[color:var(--gov-blue)]/30 blur-3xl" />
        <div className="pointer-events-none absolute -right-10 -top-24 h-56 w-56 rounded-full bg-[color:var(--gov-green)]/15 blur-3xl" />
        <div className="relative mx-auto flex h-16 max-w-[1600px] items-center gap-4 px-4 sm:px-6">
          <div className="flex items-center gap-3">
            <div className="relative flex h-10 w-10 items-center justify-center overflow-hidden rounded-2xl bg-white/10 ring-1 ring-white/15">
              <img src="/branding/karnataka-emblem.webp" alt="" className="h-7 w-7 object-contain" />
            </div>
            <div>
              <p className="text-sm font-bold leading-tight tracking-wide">SevaOne</p>
              <p className="text-[10px] uppercase tracking-[0.18em] text-slate-400">Admin Console</p>
            </div>
          </div>
          <div className="hidden h-7 w-px bg-white/15 sm:block" />
          <div className="hidden items-center gap-2 rounded-full border border-white/10 bg-white/5 px-3 py-1.5 text-xs font-medium text-slate-200 sm:flex">
            <span className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-[color:var(--gov-green)] opacity-75" />
              <span className="relative inline-flex h-2 w-2 rounded-full bg-[color:var(--gov-green)]" />
            </span>
            All systems operational
          </div>
          <div className="hidden items-center gap-1.5 text-xs text-slate-400 md:flex">
            <ShieldCheck className="h-3.5 w-3.5 text-[color:var(--gov-green)]" /> WSO2-verified session
          </div>
          <div className="ml-auto flex items-center gap-3">
            <div className="flex items-center gap-2.5 rounded-full bg-white/5 py-1 pl-1 pr-2 ring-1 ring-white/10">
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-[color:var(--saffron)] to-[color:var(--gov-green)] text-xs font-bold text-white">
                {initials(admin.displayName)}
              </div>
              <div className="hidden text-right sm:block">
                <p className="text-xs font-semibold leading-tight text-white">{admin.displayName}</p>
                <p className="text-[10px] leading-tight text-slate-400">{roleLabel(admin.tier)}</p>
              </div>
            </div>
            <Button variant="ghost" size="sm" aria-label="Sign out" onClick={() => void signOut()} className="text-slate-300 hover:bg-white/10 hover:text-white">
              <LogOut className="h-4 w-4 sm:mr-1.5" /><span className="hidden sm:inline">Sign out</span>
            </Button>
          </div>
        </div>
      </header>
      <div className="mx-auto flex min-h-0 w-full max-w-[1600px] flex-1">
        <aside className="hidden h-full w-64 shrink-0 overflow-y-auto overscroll-contain border-r border-border bg-white/70 px-3 py-6 lg:block">
          <div className="mb-6 rounded-2xl bg-[color:var(--gov-blue)]/6 p-4"><p className="text-[10px] font-bold uppercase tracking-[0.16em] text-[color:var(--gov-blue)]">Your access</p><p className="mt-2 truncate text-sm font-semibold text-[color:var(--gov-navy)]" title={admin.displayName}>{admin.displayName}</p><p className="mt-1 text-xs leading-5 text-muted-foreground"><span className="font-semibold text-[color:var(--gov-navy)]">Scope:</span> {accessSummary}</p></div>
          <nav aria-label="Admin navigation" className="space-y-6">{visibleGroups.map((group) => <div key={group.label}><p className="px-3 pb-2 text-[10px] font-bold uppercase tracking-[0.15em] text-muted-foreground">{group.label}</p><div className="space-y-1">{group.links.map(({ to, label, icon: Icon }) => <NavLink key={to} to={to} end={to === '/' || to === '/ai'} className={({ isActive }) => `group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition ${isActive ? 'bg-[color:var(--gov-blue)] text-white shadow-sm' : 'text-muted-foreground hover:bg-[color:var(--gov-blue)]/7 hover:text-[color:var(--gov-navy)]'}`}><Icon className="h-4 w-4" /><span>{label}</span><ChevronRight className="ml-auto h-3.5 w-3.5 opacity-0 transition group-hover:opacity-60" /></NavLink>)}</div></div>)}</nav>
        </aside>
        <main className="flex min-h-0 min-w-0 flex-1 flex-col overflow-y-auto overscroll-contain px-4 py-6 sm:px-6 lg:px-10 lg:py-8">
          <div className="mb-5 flex shrink-0 items-center gap-2 text-xs text-muted-foreground"><span>Admin Console</span><ChevronRight className="h-3 w-3" /><span className="font-medium text-[color:var(--gov-blue)]">{location.pathname === '/' ? 'Overview' : location.pathname.slice(1).replace('-', ' ')}</span></div>
          <div className="mb-6 flex shrink-0 gap-2 overflow-x-auto pb-1 lg:hidden">{visibleGroups.flatMap((group) => group.links).map(({ to, label }) => <NavLink key={to} to={to} end={to === '/' || to === '/ai'} className={({ isActive }) => `whitespace-nowrap rounded-full px-3 py-1.5 text-xs font-semibold ${isActive ? 'bg-[color:var(--gov-blue)] text-white' : 'bg-secondary text-muted-foreground'}`}>{label}</NavLink>)}</div>
          <div className="min-h-0 flex-1">{children}</div>
        </main>
      </div>
      <GovAiChatWidget />
    </div>
  )
}
