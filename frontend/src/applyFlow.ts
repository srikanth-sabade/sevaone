import { toast } from 'sonner'
import { api } from './api'
import type { Me, SchemeDetail, SchemeSummary } from './types'

const KEY = 'seva.pendingApply'
const LOGIN_TOAST_KEY = 'seva.loginToast'
const DEFAULT_AFTER_AUTH = '/'

export type PendingApply = {
  serviceCode: string
  departmentCode: string
  serviceName: string
}

export function savePendingApply(pending: PendingApply) {
  localStorage.setItem(KEY, JSON.stringify(pending))
}

export function readPendingApply(): PendingApply | null {
  try {
    const raw = localStorage.getItem(KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as PendingApply
    if (!parsed.serviceCode || !parsed.departmentCode) return null
    return parsed
  } catch {
    return null
  }
}

export function clearPendingApply() {
  localStorage.removeItem(KEY)
}

export function loginPath(returnTo: string) {
  return `/login?returnTo=${encodeURIComponent(returnTo)}`
}

export function registerPath(next: string) {
  return `/register?next=${encodeURIComponent(next)}`
}

/** Queue a toast then navigate to custom Seva login (WSO2 stays API-only). */
export function notifyAndGoLogin(returnTo: string, message = 'Sign in to continue') {
  queueLoginRedirectToast(message)
  window.location.assign(loginPath(returnTo))
}

export function queueLoginRedirectToast(message: string) {
  try {
    sessionStorage.setItem(LOGIN_TOAST_KEY, message)
  } catch {
    /* ignore */
  }
}

/** Show toast queued before a full-page redirect (login / register / OTP). */
export function consumeLoginRedirectToast() {
  try {
    const message = sessionStorage.getItem(LOGIN_TOAST_KEY)
    if (!message) return
    sessionStorage.removeItem(LOGIN_TOAST_KEY)
    if (/otp verified/i.test(message)) toast.success(message)
    else if (/not found|please register/i.test(message)) toast.warning(message)
    else toast.info(message)
  } catch {
    /* ignore */
  }
}

/** After login: land on the requested internal page, or home if none was given. */
export function resolveAfterAuth(me: Me | null, explicitNext?: string) {
  const next = safeInternalPath(explicitNext, DEFAULT_AFTER_AUTH)
  if (!me) {
    queueLoginRedirectToast('Sign in to continue')
    return loginPath(next === '/' ? DEFAULT_AFTER_AUTH : next)
  }
  if (!me.profileCompleted) return registerPath(DEFAULT_AFTER_AUTH)
  return next
}

/** Sidebar / casual Sign in — always return to dashboard (not a department service). */
export function sidebarLoginHref() {
  return loginPath(DEFAULT_AFTER_AUTH)
}

export function safeInternalPath(value: string | undefined, fallback = '/') {
  if (!value || !value.startsWith('/') || value.startsWith('//')) return fallback
  return value
}

/** Fire a "someone clicked Apply" count. Best-effort only — a miss here must never
 *  block navigation, and an AI-resolved service that isn't in the catalog yet is a
 *  silent no-op on the backend, not an error. */
export function trackApplyClickBestEffort(
  departmentCode: string,
  serviceCode: string,
  opts?: { assistedMode?: boolean; sevaOneUserId?: number },
) {
  void api.trackApplyClick(departmentCode, serviceCode, opts).catch(() => {
    /* best-effort only */
  })
}

export type ApplyTarget = Pick<SchemeSummary | SchemeDetail, 'serviceCode' | 'departmentCode' | 'name'>

// Prevent overlapping Apply requests while the service URL is being resolved.
let applyInFlight = false

/**
 * Apply to a service: count the click, then either land directly on the resolved
 * apply URL (works even for an anonymous citizen when the service is just a
 * redirect to the department's own homepage) or send the citizen to sign in first
 * when a real deep-linked token actually needs to be minted. There is no more
 * intermediate handoff page to auto-resume from after login — once signed in, the
 * citizen re-clicks Apply, which then resolves straight away.
 */
export async function applyToService(target: ApplyTarget, _me: Me | null, opts?: { assistedMode?: boolean }) {
  if (applyInFlight) return
  applyInFlight = true
  try {
    savePendingApply({ serviceCode: target.serviceCode, departmentCode: target.departmentCode, serviceName: target.name })
    trackApplyClickBestEffort(target.departmentCode, target.serviceCode, opts)

    // Let the backend decide whether this service needs identity. A plain
    // website redirect does not require the citizen to complete a profile.
    try {
      const outcome = await api.createApplyToken(target.departmentCode, target.serviceCode)
      if (outcome.requiresLogin) {
        queueLoginRedirectToast(`Sign in to apply for ${target.name}`)
        window.location.assign(loginPath(DEFAULT_AFTER_AUTH))
        return
      }
      if (outcome.applyUrl) {
        // Always a new tab -- the citizen leaves SevaOne for the department's own site, and
        // should be able to come straight back to the SevaOne tab they were already on.
        // noopener can return null even when the tab opens successfully. Never
        // interpret that as a reason to navigate the SevaOne tab as well.
        window.open(outcome.applyUrl, '_blank', 'noopener,noreferrer')
        return
      }
    } catch {
      /* fall through to the toast below */
    }
    toast.error('Could not open this service right now. Please try again.')
  } finally {
    applyInFlight = false
  }
}
