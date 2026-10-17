import { Loader2 } from 'lucide-react'
import { useSession } from '../store'

const copy = {
  'signing-in': { title: 'Opening sign in', detail: 'Taking you to the Seva login page…' },
  'signing-out': { title: 'Signing out', detail: 'Ending your Seva session…' },
} as const

/** Full-screen auth transition overlay (sidebar sign-in / logout). */
export function AuthBusyOverlay() {
  const authBusy = useSession((s) => s.authBusy)
  if (!authBusy) return null
  const text = copy[authBusy]

  return (
    <div
      className="fixed inset-0 z-[100] flex items-center justify-center bg-background/80 backdrop-blur-sm"
      role="status"
      aria-live="polite"
      aria-busy="true"
    >
      <div className="mx-4 flex max-w-sm flex-col items-center gap-3 rounded-2xl border border-border bg-card px-8 py-7 text-center">
        <Loader2 className="h-8 w-8 animate-spin text-[color:var(--gov-blue)]" aria-hidden />
        <div>
          <p className="text-base font-semibold text-[color:var(--gov-navy)]">{text.title}</p>
          <p className="mt-1 text-sm text-muted-foreground">{text.detail}</p>
        </div>
      </div>
    </div>
  )
}
