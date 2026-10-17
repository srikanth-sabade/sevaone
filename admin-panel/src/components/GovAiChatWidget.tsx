import { useEffect, useState } from 'react'
import { BrainCircuit, MessageCircle, X } from 'lucide-react'
import { GovAiChatThread } from './GovAiChatThread'

/** Floating AI-assistant launcher, mounted once in Shell so it's available on every admin page.
 * Auto-opens shortly after login (Shell mounts once per session, so this effect fires once per
 * login, not on every route change) and collapses back to the floating button on close -- never
 * force-reopens itself afterward. */
export function GovAiChatWidget() {
  const [open, setOpen] = useState(false)
  const [hasOpenedOnce, setHasOpenedOnce] = useState(false)

  useEffect(() => {
    const timer = setTimeout(() => { setOpen(true); setHasOpenedOnce(true) }, 700)
    return () => clearTimeout(timer)
  }, [])

  return (
    <>
      <button
        type="button"
        aria-label="Open AI assistant"
        onClick={() => setOpen(true)}
        className={`fixed bottom-6 right-6 z-40 flex h-14 w-14 items-center justify-center rounded-full bg-[color:var(--gov-blue)] text-white shadow-[0_10px_30px_-8px_rgba(22,76,147,0.6)] transition-all duration-300 ease-out hover:scale-105 hover:bg-[color:var(--gov-blue-dark)] ${
          open ? 'pointer-events-none scale-75 opacity-0' : hasOpenedOnce ? 'scale-100 opacity-100' : 'scale-0 opacity-0'
        }`}
      >
        <MessageCircle className="h-6 w-6" />
      </button>

      <div
        aria-hidden={!open}
        className={`fixed bottom-24 left-4 right-4 z-40 flex h-[60vh] max-h-[600px] transform flex-col overflow-hidden rounded-3xl border border-border bg-white shadow-2xl transition-all duration-300 ease-out sm:left-auto sm:right-6 sm:w-96 ${
          open ? 'translate-y-0 opacity-100' : 'pointer-events-none translate-y-6 opacity-0'
        }`}
      >
        <div className="flex shrink-0 items-center justify-between border-b border-border bg-[color:var(--gov-navy)] px-5 py-4 text-white">
          <div className="flex items-center gap-2.5">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-white/10 text-[color:var(--gov-gold)]"><BrainCircuit className="h-5 w-5" /></div>
            <div>
              <p className="text-sm font-semibold leading-tight">Government AI assistant</p>
              <p className="text-[11px] leading-tight text-slate-300">Live reports only</p>
            </div>
          </div>
          <button type="button" aria-label="Close AI assistant" onClick={() => setOpen(false)} className="rounded-lg p-1.5 text-slate-300 transition hover:bg-white/10 hover:text-white">
            <X className="h-5 w-5" />
          </button>
        </div>
        <div className="min-h-0 flex-1 overflow-hidden px-4 py-3">
          <GovAiChatThread compact />
        </div>
      </div>
    </>
  )
}
