const KEY = 'seva.flow'
const STYLE = 'color:#0b3d91;font-weight:700'
const MUTED = 'color:#475569'

type Step = { t: string; msg: string; data?: unknown }

function load(): Step[] {
  try {
    const raw = sessionStorage.getItem(KEY)
    return raw ? (JSON.parse(raw) as Step[]) : []
  } catch {
    return []
  }
}

function now() {
  return new Date().toISOString().slice(11, 23)
}

/** Survives WSO2 redirects (different origin clears the live console). */
export function flow(msg: string, data?: unknown) {
  const step: Step = { t: now(), msg, ...(data !== undefined ? { data } : {}) }
  const next = [...load(), step].slice(-100)
  sessionStorage.setItem(KEY, JSON.stringify(next))
  if (data !== undefined) {
    console.log(`%c[SEVA] ${step.t}%c ${msg}`, STYLE, MUTED, data)
  } else {
    console.log(`%c[SEVA] ${step.t}%c ${msg}`, STYLE, MUTED)
  }
}

export function replayFlow() {
  const steps = load()
  console.log(
    '%c[SEVA] Citizen flow tracer — open this Console while you click Apply / Login.',
    STYLE,
  )
  console.log(
    '%cWSO2 (https://localhost:9443) is another origin, so its page cannot write here. Steps before and after that hop are stored in sessionStorage and replayed.',
    MUTED,
  )
  if (steps.length === 0) {
    console.log('%c[SEVA] no steps yet in this tab', MUTED)
    return
  }
  console.group(`%c[SEVA] replay (${steps.length} steps in this tab)`, STYLE)
  for (const step of steps) {
    if (step.data !== undefined) {
      console.log(`${step.t}  ${step.msg}`, step.data)
    } else {
      console.log(`${step.t}  ${step.msg}`)
    }
  }
  console.groupEnd()
}
