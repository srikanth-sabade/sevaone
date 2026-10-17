import { ShieldCheck, Loader2 } from 'lucide-react'
import { FormEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { govAdminApi } from '../api'
import { Button } from './ui/button'
import { Card } from './ui/card'
import { Input } from './ui/input'
import { Label } from './ui/label'
import { PasswordInput } from './ui/password-input'

export function AdminLoginView() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!username.trim() || !password) {
      toast.error('Enter your username and password.')
      return
    }
    setBusy(true)
    try {
      await govAdminApi.govAdminLogin(username.trim(), password)
      await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'me'] })
      toast.success('Signed in to the government admin panel.')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Invalid username or password.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-[color:var(--background)] px-4 py-8">
      <div className="mx-auto grid min-h-[620px] w-full max-w-5xl overflow-hidden rounded-3xl border border-border bg-card shadow-[var(--shadow-lift)] lg:grid-cols-[1.05fr_0.95fr]">
        <section className="relative hidden overflow-hidden bg-[color:var(--gov-navy)] p-10 text-white lg:flex lg:flex-col lg:justify-between">
          <div className="absolute -right-28 -top-24 h-80 w-80 rounded-full bg-[color:var(--gov-blue)]/30 blur-3xl" />
          <div className="absolute -bottom-28 -left-20 h-72 w-72 rounded-full bg-[color:var(--gov-green)]/20 blur-3xl" />
          <div className="relative">
            <div className="flex items-center gap-3">
              <img src="/branding/karnataka-emblem.webp" alt="" className="h-14 w-14 object-contain" />
              <img src="/branding/sevasindhu-logo.webp" alt="" className="h-14 w-14 rounded-xl bg-white object-contain p-1" />
            </div>
            <p className="mt-10 text-xs font-bold uppercase tracking-[0.2em] text-[color:var(--saffron)]">Government of Karnataka</p>
            <h1 className="mt-3 text-4xl font-semibold leading-tight">SevaOne<br />Admin Console</h1>
            <p className="mt-5 max-w-sm text-sm leading-6 text-slate-300">
              Statewide service intelligence for responsible, evidence-led administration.
            </p>
          </div>
          <div className="relative space-y-3 text-sm text-slate-300">
            <div className="flex items-center gap-3 border-t border-white/10 pt-4"><span className="h-2 w-2 rounded-full bg-[color:var(--saffron)]" /> Live service delivery analytics</div>
            <div className="flex items-center gap-3"><span className="h-2 w-2 rounded-full bg-[color:var(--gov-green)]" /> District and taluk-aware access</div>
            <div className="flex items-center gap-3"><span className="h-2 w-2 rounded-full bg-sky-300" /> Protected by WSO2 identity</div>
          </div>
        </section>

        <section className="flex items-center justify-center p-6 sm:p-10">
          <Card className="w-full max-w-md border-0 p-1 shadow-none">
            <div className="mb-8 flex items-center gap-3 lg:hidden">
              <img src="/branding/karnataka-emblem.webp" alt="" className="h-12 w-12 object-contain" />
              <div><p className="text-xs font-bold uppercase tracking-[0.16em] text-[color:var(--gov-blue)]">SevaOne</p><p className="text-sm font-semibold text-[color:var(--gov-navy)]">Government Admin Console</p></div>
            </div>
            <div className="mb-8">
              <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]"><ShieldCheck className="h-6 w-6" /></div>
              <h2 className="text-2xl text-[color:var(--gov-navy)]">Secure sign in</h2>
              <p className="mt-2 text-sm leading-6 text-muted-foreground">Use your WSO2 government administrator account to continue.</p>
            </div>
            <form className="space-y-5" onSubmit={(event) => void submit(event)}>
              <div className="space-y-2"><Label htmlFor="gov-username">Username</Label><Input id="gov-username" autoComplete="username" value={username} onChange={(event) => setUsername(event.target.value)} /></div>
              <div className="space-y-2"><Label htmlFor="gov-password">Password</Label><PasswordInput id="gov-password" autoComplete="current-password" value={password} onChange={(event) => setPassword(event.target.value)} /></div>
              <Button type="submit" disabled={busy} className="h-11 w-full rounded-xl bg-[color:var(--gov-blue)] text-white hover:bg-[color:var(--gov-blue-dark)]">
                {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Sign in'}
              </Button>
              <button
                type="button"
                onClick={() => navigate('/first-time-login')}
                className="w-full text-center text-xs font-medium text-[color:var(--gov-blue)] hover:underline"
              >
                First time login
              </button>
            </form>
            <p className="mt-8 text-center text-xs text-muted-foreground">Access is provisioned by the SevaOne administration team.</p>
          </Card>
        </section>
      </div>
    </main>
  )
}
