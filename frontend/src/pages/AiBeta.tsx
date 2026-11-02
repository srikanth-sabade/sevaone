import { useEffect, useRef, useState } from 'react'
import { ArrowUp, Copy, FlaskConical, RotateCcw, Sparkles } from 'lucide-react'
import { sendAiBetaChatMessage, type AiBetaChatResponse, type AiBetaResponseType } from '../aiBetaChat'

/** Internal beta-testing page for the co-dev's new standalone AI Discovery backend
 *  (http://101.53.138.83:8001, proxied at /ai-beta-api/ — see nginx.conf / vite.config.ts).
 *  Deliberately separate from Chat.tsx / aiChat.ts (the citizen-facing "AI Discovery"
 *  flow, which talks to sevaone-rag on port 8000): this page is for testing the new
 *  backend itself, so every raw field it returns is shown rather than massaged into the
 *  polished citizen UI. Not linked from the citizen nav — reach it at /ai-beta. */

type Turn = { id: number; role: 'user' | 'assistant'; text: string; raw?: AiBetaChatResponse; error?: string }

let uid = 0
function nextId() {
  uid += 1
  return uid
}

const EXAMPLES = [
  'Renew my driving licence',
  'Apply for an income certificate',
  'I need a ration card',
  'Get my RTC land record',
]

const BADGE_STYLE: Record<AiBetaResponseType, string> = {
  Resolved: 'bg-emerald-100 text-emerald-700',
  Clarify: 'bg-amber-100 text-amber-700',
  Fallback: 'bg-amber-100 text-amber-700',
  List: 'bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]',
  Info: 'bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]',
  Greeting: 'bg-secondary text-muted-foreground',
  NoMatch: 'bg-red-100 text-red-700',
}

function AssistantAvatar() {
  return (
    <div className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-navy)]">
      <Sparkles className="h-4 w-4 text-white" />
    </div>
  )
}

export function AiBetaPage() {
  const [q, setQ] = useState('')
  const [turns, setTurns] = useState<Turn[]>([])
  const [pending, setPending] = useState(false)
  const [language, setLanguage] = useState<'EN' | 'KN'>('EN')
  const [copiedId, setCopiedId] = useState<number | null>(null)
  const sessionIdRef = useRef<string | null>(null)
  const [sessionId, setSessionId] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' })
  }, [turns, pending])

  async function ask(raw: string) {
    const query = raw.trim()
    if (!query || pending) return
    setQ('')
    setPending(true)
    setTurns((prev) => [...prev, { id: nextId(), role: 'user', text: query }])
    try {
      const resp = await sendAiBetaChatMessage({
        sessionId: sessionIdRef.current,
        queryText: query,
        language,
      })
      sessionIdRef.current = resp.sessionId
      setSessionId(resp.sessionId)
      setTurns((prev) => [...prev, { id: nextId(), role: 'assistant', text: resp.message, raw: resp }])
    } catch (err) {
      setTurns((prev) => [
        ...prev,
        { id: nextId(), role: 'assistant', text: '', error: err instanceof Error ? err.message : String(err) },
      ])
    } finally {
      setPending(false)
      inputRef.current?.focus()
    }
  }

  function resetSession() {
    sessionIdRef.current = null
    setSessionId(null)
    setTurns([])
    setQ('')
    inputRef.current?.focus()
  }

  function copyRaw(turn: Turn) {
    if (!turn.raw) return
    void navigator.clipboard.writeText(JSON.stringify(turn.raw, null, 2)).then(() => {
      setCopiedId(turn.id)
      window.setTimeout(() => setCopiedId((id) => (id === turn.id ? null : id)), 1200)
    })
  }

  return (
    <div className="flex h-dvh flex-col bg-background">
      {/* Header */}
      <header className="shrink-0 border-b border-border bg-card/80 backdrop-blur">
        <div className="mx-auto flex w-full max-w-3xl items-center justify-between gap-3 px-4 py-3">
          <div className="flex items-center gap-2.5">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-amber-100">
              <FlaskConical className="h-4.5 w-4.5 text-amber-600" />
            </div>
            <div className="min-w-0">
              <div className="flex items-center gap-2 text-sm font-semibold leading-tight">
                AI Discovery <span className="rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-amber-700">Beta</span>
              </div>
              <div className="truncate text-xs text-muted-foreground">
                New backend · <code className="rounded bg-secondary px-1 py-0.5">101.53.138.83:8001</code>
              </div>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <div className="flex rounded-full border border-border bg-secondary/50 p-0.5 text-xs font-medium">
              {(['EN', 'KN'] as const).map((lng) => (
                <button
                  key={lng}
                  type="button"
                  onClick={() => setLanguage(lng)}
                  className={`rounded-full px-2.5 py-1 transition ${
                    language === lng ? 'bg-[color:var(--gov-navy)] text-white' : 'text-muted-foreground hover:text-foreground'
                  }`}
                >
                  {lng}
                </button>
              ))}
            </div>
            <button
              type="button"
              onClick={resetSession}
              title="Start a new session"
              className="inline-flex items-center gap-1.5 rounded-full border border-border px-3 py-1.5 text-xs font-medium text-muted-foreground transition hover:bg-secondary hover:text-foreground"
            >
              <RotateCcw className="h-3.5 w-3.5" /> New session
            </button>
          </div>
        </div>
      </header>

      {/* Thread */}
      <div className="flex-1 overflow-y-auto">
        <div className="mx-auto w-full max-w-3xl space-y-4 px-4 py-5">
          {turns.length === 0 ? (
            <div className="flex flex-col items-center gap-4 pt-16 text-center">
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-[color:var(--gov-blue)]/10">
                <Sparkles className="h-6 w-6 text-[color:var(--gov-blue)]" />
              </div>
              <div>
                <p className="text-sm font-medium">Try the new AI Discovery backend</p>
                <p className="mt-1 text-xs text-muted-foreground">
                  Ask it something a citizen might ask. Every field it returns is shown below the reply for debugging.
                </p>
              </div>
              <div className="flex flex-wrap justify-center gap-2">
                {EXAMPLES.map((ex) => (
                  <button
                    key={ex}
                    type="button"
                    onClick={() => void ask(ex)}
                    className="rounded-full border border-border bg-card px-3.5 py-1.5 text-xs text-foreground/80 transition hover:border-[color:var(--gov-blue)]/40 hover:bg-secondary"
                  >
                    {ex}
                  </button>
                ))}
              </div>
            </div>
          ) : null}

          {turns.map((turn) => {
            if (turn.role === 'user') {
              return (
                <div key={turn.id} className="flex justify-end">
                  <div className="max-w-[80%] rounded-2xl rounded-br-md bg-[color:var(--gov-navy)] px-4 py-2.5 text-sm leading-relaxed text-white shadow-sm">
                    {turn.text}
                  </div>
                </div>
              )
            }
            if (turn.error) {
              return (
                <div key={turn.id} className="flex gap-3">
                  <AssistantAvatar />
                  <div className="max-w-[85%] rounded-2xl rounded-bl-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
                    <span className="font-medium">Request failed.</span> {turn.error}
                  </div>
                </div>
              )
            }
            const d = turn.raw!
            return (
              <div key={turn.id} className="flex gap-3">
                <AssistantAvatar />
                <div className="w-full max-w-[85%] space-y-3 rounded-2xl rounded-bl-md border border-border bg-card px-4 py-3 text-sm shadow-sm">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className={`rounded-full px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide ${BADGE_STYLE[d.responseType]}`}>
                      {d.responseType}
                    </span>
                    {d.catalogVersion ? (
                      <span className="text-[11px] text-muted-foreground">catalog {d.catalogVersion}</span>
                    ) : null}
                  </div>

                  <p className="whitespace-pre-wrap leading-relaxed text-foreground">{turn.text}</p>

                  {d.resolvedService ? (
                    <div className="rounded-xl border border-emerald-200 bg-emerald-50/60 p-3 text-xs">
                      <div className="text-[10px] font-semibold uppercase tracking-wide text-emerald-700">Resolved service</div>
                      <div className="mt-1 text-sm font-semibold text-foreground">{d.resolvedService.serviceName}</div>
                      {d.resolvedService.kannadaServiceName ? (
                        <div className="text-muted-foreground">{d.resolvedService.kannadaServiceName}</div>
                      ) : null}
                      <div className="mt-1.5 flex flex-wrap gap-x-3 gap-y-0.5 text-muted-foreground">
                        <span>id {d.resolvedService.serviceId}</span>
                        <span>code {d.resolvedService.serviceCode ?? '—'}</span>
                        <span>dept {d.resolvedService.departmentId ?? d.resolvedService.departmentCode ?? '—'}</span>
                      </div>
                    </div>
                  ) : null}

                  {d.candidates.length > 0 ? (
                    <div className="space-y-1.5">
                      {d.candidates.map((c) => (
                        <button
                          key={c.serviceId}
                          type="button"
                          onClick={() => void ask(c.serviceName)}
                          className="block w-full rounded-xl border border-border bg-secondary/40 px-3 py-2 text-left text-xs transition hover:border-[color:var(--gov-blue)]/40 hover:bg-secondary"
                        >
                          <span className="font-medium text-foreground">{c.serviceName}</span>
                          {c.shortDescription ? <span className="text-muted-foreground"> — {c.shortDescription}</span> : null}
                        </button>
                      ))}
                    </div>
                  ) : null}

                  {d.references.length > 0 ? (
                    <div className="truncate text-[11px] text-muted-foreground">refs: {d.references.join(', ')}</div>
                  ) : null}

                  {d.suggestedQuestions.length > 0 ? (
                    <div className="flex flex-wrap gap-1.5 pt-0.5">
                      {d.suggestedQuestions.map((sq) => (
                        <button
                          key={sq}
                          type="button"
                          onClick={() => void ask(sq)}
                          className="rounded-full border border-border bg-card px-2.5 py-1 text-xs transition hover:border-[color:var(--gov-blue)]/40 hover:bg-secondary"
                        >
                          {sq}
                        </button>
                      ))}
                    </div>
                  ) : null}

                  <div className="flex items-center justify-between border-t border-border pt-2">
                    <details className="group flex-1">
                      <summary className="cursor-pointer text-[11px] font-medium text-muted-foreground transition hover:text-foreground">
                        Raw response
                      </summary>
                      <pre className="mt-1.5 max-h-64 overflow-auto rounded-lg bg-black/5 p-2 text-[11px] leading-relaxed">
                        {JSON.stringify(d, null, 2)}
                      </pre>
                    </details>
                    <button
                      type="button"
                      onClick={() => copyRaw(turn)}
                      title="Copy raw JSON"
                      className="ml-2 inline-flex shrink-0 items-center gap-1 rounded-md px-1.5 py-1 text-[11px] text-muted-foreground transition hover:bg-secondary hover:text-foreground"
                    >
                      <Copy className="h-3 w-3" /> {copiedId === turn.id ? 'Copied' : 'Copy'}
                    </button>
                  </div>
                </div>
              </div>
            )
          })}

          {pending ? (
            <div className="flex gap-3">
              <AssistantAvatar />
              <div className="flex items-center gap-2 rounded-2xl rounded-bl-md border border-border bg-card px-4 py-3 text-sm text-muted-foreground shadow-sm">
                <span className="relative flex h-2 w-2">
                  <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-[color:var(--gov-blue)] opacity-60" />
                  <span className="relative inline-flex h-2 w-2 rounded-full bg-[color:var(--gov-blue)]" />
                </span>
                Asking the beta backend…
              </div>
            </div>
          ) : null}

          <div ref={bottomRef} />
        </div>
      </div>

      {/* Composer */}
      <div className="shrink-0 border-t border-border bg-card/80 backdrop-blur">
        <div className="mx-auto w-full max-w-3xl px-4 py-3">
          {sessionId ? (
            <div className="mb-2 truncate text-[10px] text-muted-foreground">
              session <code className="rounded bg-secondary px-1 py-0.5">{sessionId}</code>
            </div>
          ) : null}
          <form
            onSubmit={(e) => {
              e.preventDefault()
              void ask(q)
            }}
            className="flex gap-2"
          >
            <div className="flex flex-1 items-center rounded-2xl border border-border bg-background px-3 shadow-sm transition-colors focus-within:border-[color:var(--gov-blue)]/40">
              <Sparkles className="h-4 w-4 shrink-0 text-[color:var(--gov-blue)]" />
              <input
                ref={inputRef}
                autoFocus
                value={q}
                onChange={(e) => setQ(e.target.value)}
                placeholder="Type a citizen-style question…"
                disabled={pending}
                className="min-w-0 flex-1 bg-transparent px-2 py-3 text-sm outline-none disabled:opacity-60"
              />
            </div>
            <button
              type="submit"
              disabled={pending || !q.trim()}
              className="inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-[color:var(--gov-navy)] text-white transition hover:opacity-90 disabled:opacity-40"
            >
              <ArrowUp className="h-4 w-4" />
            </button>
          </form>
          <p className="mt-1.5 text-center text-[10px] text-muted-foreground">
            Internal testing only — talks directly to the new backend, bypasses the citizen app and the bff.
          </p>
        </div>
      </div>
    </div>
  )
}
