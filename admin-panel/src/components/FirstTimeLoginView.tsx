import { FormEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { KeyRound, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import { govAdminApi } from '../api'
import { Button } from './ui/button'
import { Card } from './ui/card'
import { Input } from './ui/input'
import { Label } from './ui/label'
import { PasswordInput } from './ui/password-input'

type Step = 'details' | 'otp'

export function FirstTimeLoginView() {
  const navigate = useNavigate()
  const [step, setStep] = useState<Step>('details')
  const [username, setUsername] = useState('')
  const [mobile, setMobile] = useState('')
  const [password, setPassword] = useState('')
  const [otp, setOtp] = useState('')
  const [challengeId, setChallengeId] = useState('')
  const [busy, setBusy] = useState(false)

  async function submitDetails(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!username.trim() || !/^[0-9]{10,15}$/.test(mobile.trim())) {
      toast.error('Enter your username and a valid mobile number.')
      return
    }
    if (password.length < 8 || !/[A-Z]/.test(password) || !/[a-z]/.test(password) || !/[0-9]/.test(password)) {
      toast.error('Password must be at least 8 characters with an uppercase letter, a lowercase letter, and a number.')
      return
    }
    setBusy(true)
    try {
      const result = await govAdminApi.claimStart({ username: username.trim(), mobile: mobile.trim(), password })
      setChallengeId(result.challengeId)
      setStep('otp')
      toast.success('OTP sent to your mobile number.')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Could not start first-time login.')
    } finally {
      setBusy(false)
    }
  }

  async function submitOtp(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (otp.trim().length !== 6) {
      toast.error('Enter the 6-digit OTP.')
      return
    }
    setBusy(true)
    try {
      await govAdminApi.claimVerify({ challengeId, otp: otp.trim() })
      toast.success('Account set up. You can now sign in with your new password.')
      navigate('/login')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Could not verify the OTP.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-[color:var(--background)] px-4 py-8">
      <Card className="w-full max-w-md rounded-3xl p-8 shadow-[var(--shadow-lift)]">
        <div className="mb-6 flex h-12 w-12 items-center justify-center rounded-2xl bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]">
          <KeyRound className="h-6 w-6" />
        </div>
        <h1 className="text-2xl text-[color:var(--gov-navy)]">First time login</h1>
        <p className="mt-2 text-sm leading-6 text-muted-foreground">
          {step === 'details'
            ? 'Enter the username your administrator gave you, along with your mobile number and a new password.'
            : `Enter the 6-digit OTP sent to ${mobile}.`}
        </p>

        {step === 'details' ? (
          <form className="mt-6 space-y-5" onSubmit={(event) => void submitDetails(event)}>
            <div className="space-y-2">
              <Label htmlFor="claim-username">Username</Label>
              <Input id="claim-username" autoComplete="username" value={username} onChange={(event) => setUsername(event.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="claim-mobile">Mobile number</Label>
              <Input id="claim-mobile" inputMode="numeric" autoComplete="tel" value={mobile} onChange={(event) => setMobile(event.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="claim-password">Choose a password</Label>
              <PasswordInput id="claim-password" autoComplete="new-password" value={password} onChange={(event) => setPassword(event.target.value)} />
              <p className="text-xs text-muted-foreground">At least 8 characters, with an uppercase letter, a lowercase letter, and a number.</p>
            </div>
            <Button type="submit" disabled={busy} className="h-11 w-full rounded-xl bg-[color:var(--gov-blue)] text-white hover:bg-[color:var(--gov-blue-dark)]">
              {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Send OTP'}
            </Button>
          </form>
        ) : (
          <form className="mt-6 space-y-5" onSubmit={(event) => void submitOtp(event)}>
            <div className="space-y-2">
              <Label htmlFor="claim-otp">6-digit OTP</Label>
              <Input id="claim-otp" inputMode="numeric" maxLength={6} value={otp} onChange={(event) => setOtp(event.target.value)} />
            </div>
            <Button type="submit" disabled={busy} className="h-11 w-full rounded-xl bg-[color:var(--gov-blue)] text-white hover:bg-[color:var(--gov-blue-dark)]">
              {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Verify and finish'}
            </Button>
            <button type="button" onClick={() => setStep('details')} className="w-full text-center text-xs text-muted-foreground hover:text-[color:var(--gov-navy)]">
              Change details and resend
            </button>
          </form>
        )}

        <button type="button" onClick={() => navigate('/login')} className="mt-6 w-full text-center text-xs text-muted-foreground hover:text-[color:var(--gov-navy)]">
          Back to sign in
        </button>
      </Card>
    </main>
  )
}
