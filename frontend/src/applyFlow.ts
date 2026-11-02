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

/** Token integration handoff: the department wants the user's details as a form POST (never in the
 *  address bar), so build a throwaway form and submit it into a new tab. */
function openPostForm(postForm: { url: string; fields: Record<string, string> }) {
  if (!/^https?:\/\//i.test(postForm.url)) throw new Error('Unsupported department address')
  const form = document.createElement('form')
  form.method = 'POST'
  form.action = postForm.url
  form.target = '_blank'
  form.rel = 'noopener noreferrer'
  form.style.display = 'none'
  for (const [name, value] of Object.entries(postForm.fields)) {
    const input = document.createElement('input')
    input.type = 'hidden'
    input.name = name
    input.value = value
    form.appendChild(input)
  }
  document.body.appendChild(form)
  form.submit()
  form.remove()
}

/** Some services need a detail only the citizen has (for example a ration card number). Ask for each, in a small
 *  dialog; resolves with the answers, or null if the citizen cancels. */
function askQuestions(serviceName: string, questions: { key: string; label: string }[]): Promise<Record<string, string> | null> {
  return new Promise((resolve) => {
    const overlay = document.createElement('div')
    overlay.setAttribute('role', 'dialog')
    overlay.setAttribute('aria-modal', 'true')
    overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,.45);display:flex;align-items:center;justify-content:center;z-index:9999;padding:16px'
    const box = document.createElement('form')
    box.style.cssText = 'background:#fff;color:#111;border-radius:12px;padding:20px;max-width:420px;width:100%;font:14px system-ui,sans-serif;display:flex;flex-direction:column;gap:12px'
    const title = document.createElement('strong')
    title.textContent = `A few details for ${serviceName}`
    box.appendChild(title)
    const inputs: Record<string, HTMLInputElement> = {}
    for (const q of questions) {
      const label = document.createElement('label')
      label.style.cssText = 'display:flex;flex-direction:column;gap:4px'
      label.append(q.label)
      const input = document.createElement('input')
      input.required = true
      if (q.key === 'Email') input.type = 'email'
      input.maxLength = 200
      input.style.cssText = 'padding:8px;border:1px solid #bbb;border-radius:6px;font:inherit'
      label.appendChild(input)
      box.appendChild(label)
      inputs[q.key] = input
    }
    const row = document.createElement('div')
    row.style.cssText = 'display:flex;gap:8px;justify-content:flex-end'
    const cancel = document.createElement('button')
    cancel.type = 'button'
    cancel.textContent = 'Cancel'
    const ok = document.createElement('button')
    ok.type = 'submit'
    ok.textContent = 'Continue'
    ok.style.cssText = 'background:#1d4ed8;color:#fff;border:0;border-radius:6px;padding:8px 14px'
    cancel.style.cssText = 'background:#eee;border:0;border-radius:6px;padding:8px 14px'
    row.append(cancel, ok)
    box.appendChild(row)
    overlay.appendChild(box)
    document.body.appendChild(overlay)
    const done = (value: Record<string, string> | null) => { overlay.remove(); resolve(value) }
    cancel.onclick = () => done(null)
    box.onsubmit = (e) => {
      e.preventDefault()
      done(Object.fromEntries(Object.entries(inputs).map(([k, i]) => [k, i.value.trim()])))
    }
    Object.values(inputs)[0]?.focus()
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
      let outcome = await api.createApplyToken(target.departmentCode, target.serviceCode)
      // Each round asks only what is still missing (email first, then the service's own questions).
      let given: Record<string, string> = {}
      for (let round = 0; outcome.needsInput?.length && round < 3; round++) {
        const answers = await askQuestions(target.name, outcome.needsInput)
        if (!answers) return
        given = { ...given, ...answers }
        outcome = await api.createApplyToken(target.departmentCode, target.serviceCode, given)
      }
      if (outcome.requiresLogin) {
        queueLoginRedirectToast(`Sign in to apply for ${target.name}`)
        window.location.assign(loginPath(DEFAULT_AFTER_AUTH))
        return
      }
      if (outcome.postForm) {
        openPostForm(outcome.postForm)
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
    } catch (error) {
      // A message from our server that the citizen can act on (for example "add your email to your profile") is shown as
      // it is; anything generic falls through to the usual wording.
      const message = error instanceof Error ? error.message : ''
      const generic = !message || /^(internal server error|bad gateway|service unavailable|gateway timeout|failed to fetch|load failed|networkerror)/i.test(message)
      if (!generic && message.length < 220) {
        toast.error(message)
        return
      }
    }
    toast.error('Could not open this service right now. Please try again.')
  } finally {
    applyInFlight = false
  }
}
