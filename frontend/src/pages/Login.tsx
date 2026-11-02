import { Link, useNavigate, useSearch } from '@tanstack/react-router'
import { Fingerprint, KeyRound, ShieldCheck, Smartphone } from 'lucide-react'
import { useEffect, useState } from 'react'
import { api } from '../api'
import emblem from '../assets/karnataka-emblem.webp'
import logo from '../assets/sevasindhu-logo.webp'
import {
  consumeLoginRedirectToast,
  resolveAfterAuth,
  safeInternalPath,
} from '../applyFlow'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { Button } from '../components/ui/button'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { useSession } from '../store'
import { toast } from 'sonner'
import { useT } from '../hooks/useT'

export function LoginPage() {
  const { t } = useT()
  const { me, loaded, refresh } = useSession()
  const navigate = useNavigate()
  const { returnTo, loginError, mode: modeParam } = useSearch({ from: '/login' })
  // Casual login and Apply both land on chat (/); operator keeps /operator.
  const next = (() => {
    const requested = safeInternalPath(returnTo, '/')
    if (requested === '/operator') return requested
    return '/'
  })()

  const [mode, setMode] = useState<'citizen' | 'operator'>(modeParam === 'operator' ? 'operator' : 'citizen')

  const [step, setStep] = useState<'mobile' | 'otp'>('mobile')
  const [mobile, setMobile] = useState('')
  const [otp, setOtp] = useState('')
  const [challengeId, setChallengeId] = useState<string | null>(null)
  const [debugOtp, setDebugOtp] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const [operatorCode, setOperatorCode] = useState('')
  const [operatorPassword, setOperatorPassword] = useState('')
  const [operatorBusy, setOperatorBusy] = useState(false)

  // Self-claim: an operator on the whitelist roster sets their own credentials the first time
  // (see OperatorClaimService) -- no operator has a password otherwise except the one seeded
  // test account, so this is how a real operator actually gets one.
  const [operatorView, setOperatorView] = useState<'login' | 'claimStart' | 'claimOtp'>('login')
  const [claimUserId, setClaimUserId] = useState('')
  const [claimMobile, setClaimMobile] = useState('')
  const [claimPassword, setClaimPassword] = useState('')
  const [claimOtp, setClaimOtp] = useState('')
  const [claimChallengeId, setClaimChallengeId] = useState<string | null>(null)
  const [claimBusy, setClaimBusy] = useState(false)

  useEffect(() => {
    consumeLoginRedirectToast()
  }, [])

  useEffect(() => {
    if (!loaded || !me) return
    window.location.assign(resolveAfterAuth(me, next))
  }, [loaded, me, next])

  const goToDigiLocker = async (pendingId?: string) => {
    const started = await api.digilockerStart({
      flow: 'signup',
      returnTo: next,
      pendingId,
    })
    toast.success('Opening DigiLocker…')
    window.location.assign(started.redirectUrl)
  }

  const citizenMobileSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const m = mobile.trim()
    if (!/^[0-9]{10,15}$/.test(m)) return toast.error('Enter a valid 10-digit mobile number')
    setBusy(true)
    try {
      const result = await api.mobileStart(m)
      setChallengeId(result.challengeId)
      setDebugOtp(result.debugOtp ?? null)
      setStep('otp')
      toast.success('OTP sent. Check the SevaOne API log if SMS is not wired yet.')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Could not send OTP')
    } finally {
      setBusy(false)
    }
  }

  const citizenOtpSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!challengeId) return toast.error('OTP challenge expired. Start again.')
    const code = otp.trim()
    if (!/^\d{6}$/.test(code)) return toast.error('Enter the 6-digit mobile OTP')
    setBusy(true)
    try {
      const result = await api.mobileVerify({ challengeId, otp: code })
      if (result.found) {
        await refresh()
        toast.success('Signed in')
        window.location.assign(result.returnTo || next)
        return
      }
      await goToDigiLocker(result.digiLockerPendingId ?? undefined)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'OTP verification failed')
      setStep('mobile')
      setChallengeId(null)
      setDebugOtp(null)
      setOtp('')
    } finally {
      setBusy(false)
    }
  }

  const operatorSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const code = operatorCode.trim()
    if (code.length < 3 || operatorPassword.length < 6) {
      toast.error('Enter a valid operator code and password')
      return
    }
    setOperatorBusy(true)
    try {
      await api.operatorLogin({ operatorCode: code, password: operatorPassword })
      toast.success('Signed in')
      // Assisted-mode operators land on the same home page as a citizen -- the only
      // difference downstream is Catalog 2.0's Apply flow (bind-then-apply), never a
      // separate console page.
      window.location.assign('/')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Invalid operator code or password')
    } finally {
      setOperatorBusy(false)
    }
  }

  const claimStartSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const userId = Number(claimUserId.trim())
    if (!Number.isInteger(userId) || userId <= 0) {
      toast.error('Enter your operator ID as it appears on your roster entry')
      return
    }
    if (!/^[0-9]{10,15}$/.test(claimMobile.trim())) {
      toast.error('Enter a valid mobile number')
      return
    }
    // Must match WSO2 IS's own password policy (queried live: 8-64 chars, >=1 digit, >=1
    // uppercase, >=1 lowercase) -- see OperatorClaimService.StartAsync's matching backend check.
    if (
      claimPassword.length < 8 ||
      claimPassword.length > 64 ||
      !/\d/.test(claimPassword) ||
      !/[A-Z]/.test(claimPassword) ||
      !/[a-z]/.test(claimPassword)
    ) {
      toast.error('Password must be 8-64 characters with at least one uppercase letter, one lowercase letter, and one number')
      return
    }
    setClaimBusy(true)
    try {
      const result = await api.operatorClaimStart({ userId, mobile: claimMobile.trim(), password: claimPassword })
      setClaimChallengeId(result.challengeId)
      setOperatorView('claimOtp')
      toast.success(`OTP sent to ${claimMobile.trim()}`)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Could not start claim')
    } finally {
      setClaimBusy(false)
    }
  }

  const claimVerifySubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!claimChallengeId) return
    if (!/^\d{6}$/.test(claimOtp.trim())) {
      toast.error('Enter the 6-digit OTP')
      return
    }
    setClaimBusy(true)
    try {
      const result = await api.operatorClaimVerify({ challengeId: claimChallengeId, otp: claimOtp.trim() })
      toast.success('Account created — sign in below')
      setOperatorCode(result.operatorCode)
      setOperatorPassword('')
      setClaimUserId('')
      setClaimMobile('')
      setClaimPassword('')
      setClaimOtp('')
      setClaimChallengeId(null)
      setOperatorView('login')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Incorrect OTP')
    } finally {
      setClaimBusy(false)
    }
  }

  return (
    <div className="mx-auto flex min-h-[calc(100vh-2rem)] max-w-5xl items-stretch px-4 py-10">
      <div className="grid w-full gap-8 md:grid-cols-2 md:items-stretch">
      <div className="hidden md:flex md:min-h-full">
        <div className="flex h-full w-full flex-col justify-between rounded-2xl gov-gradient p-8 text-white shadow-lg">
          <div>
            <div className="flex items-center gap-3">
              <img src={emblem} alt="" decoding="async" className="h-14 w-14 object-contain" />
              <img src={logo} alt={t('sevaSindhu')} decoding="async" className="h-14 w-14 rounded-lg bg-white object-contain p-1" />
            </div>
            <h2 className="mt-8 text-2xl font-bold leading-snug">{t('loginSsoHeadline')}</h2>
            <p className="mt-4 text-sm leading-relaxed text-white/85">
              {t('loginIntro')}
            </p>
          </div>
          <ul className="mt-10 space-y-4 text-sm">
            <li className="flex items-start gap-3">
              <ShieldCheck className="mt-0.5 h-5 w-5 shrink-0 text-[color:var(--gov-gold)]" />
              <span>{t('loginMobileOtpHint')}</span>
            </li>
            <li className="flex items-start gap-3">
              <Fingerprint className="mt-0.5 h-5 w-5 shrink-0 text-[color:var(--gov-gold)]" />
              <span>{t('loginFeature2')}</span>
            </li>
            <li className="flex items-start gap-3">
              <KeyRound className="mt-0.5 h-5 w-5 shrink-0 text-[color:var(--gov-gold)]" />
              <span>{t('loginFeature3')}</span>
            </li>
          </ul>
          <p className="mt-10 text-xs text-white/70">
            {t('loginFooterNote')}
          </p>
        </div>
      </div>

      <div className="flex h-full flex-col rounded-2xl border bg-card p-6 shadow-sm md:p-8">
        <div className="mb-6 flex items-center gap-2">
          <Smartphone className="h-5 w-5 text-[color:var(--gov-blue)]" />
          <h1 className="text-xl font-bold text-[color:var(--gov-navy)]">{t('loginCitizenTitle')}</h1>
        </div>
        <div className="mb-4 -mt-2">
          <Breadcrumbs />
        </div>

        <div className="mb-5 inline-flex w-fit rounded-lg border border-border bg-secondary/40 p-1 text-sm">
          <button
            type="button"
            onClick={() => setMode('citizen')}
            className={`rounded-md px-3 py-1.5 font-medium transition-colors ${
              mode === 'citizen' ? 'bg-card shadow-sm text-[color:var(--gov-navy)]' : 'text-muted-foreground'
            }`}
          >
            {t('loginTabCitizen')}
          </button>
          <button
            type="button"
            onClick={() => setMode('operator')}
            className={`rounded-md px-3 py-1.5 font-medium transition-colors ${
              mode === 'operator' ? 'bg-card shadow-sm text-[color:var(--gov-navy)]' : 'text-muted-foreground'
            }`}
          >
            {t('loginTabOperator')}
          </button>
        </div>

        {mode === 'operator' && operatorView === 'login' && (
          <form onSubmit={(e) => void operatorSubmit(e)} className="space-y-4">
            <div>
              <Label htmlFor="operatorCode">{t('loginOperatorCode')}</Label>
              <Input
                id="operatorCode"
                autoComplete="username"
                value={operatorCode}
                onChange={(e) => setOperatorCode(e.target.value.toUpperCase().slice(0, 20))}
                placeholder={t('loginOperatorCodePlaceholder')}
                className="mt-1.5"
                disabled={operatorBusy}
              />
            </div>
            <div>
              <Label htmlFor="operatorPassword">{t('loginOperatorPassword')}</Label>
              <Input
                id="operatorPassword"
                type="password"
                autoComplete="current-password"
                value={operatorPassword}
                onChange={(e) => setOperatorPassword(e.target.value)}
                className="mt-1.5"
                disabled={operatorBusy}
              />
            </div>
            <Button
              type="submit"
              disabled={operatorBusy}
              className="w-full bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90"
            >
              {operatorBusy ? t('loginOperatorSigningIn') : t('loginOperatorSubmit')}
            </Button>
            <button
              type="button"
              className="w-full text-center text-sm text-muted-foreground underline"
              onClick={() => setOperatorView('claimStart')}
            >
              {t('opClaimFirstTimeLink')}
            </button>
          </form>
        )}

        {mode === 'operator' && operatorView === 'claimStart' && (
          <form onSubmit={(e) => void claimStartSubmit(e)} className="space-y-4">
            <p className="text-sm text-muted-foreground">{t('opClaimIntro')}</p>
            <div>
              <Label htmlFor="claimUserId">{t('opClaimUserId')}</Label>
              <Input
                id="claimUserId"
                inputMode="numeric"
                autoComplete="off"
                value={claimUserId}
                onChange={(e) => setClaimUserId(e.target.value.replace(/\D/g, '').slice(0, 15))}
                placeholder={t('opClaimUserIdPlaceholder')}
                className="mt-1.5"
                disabled={claimBusy}
              />
            </div>
            <div>
              <Label htmlFor="claimMobile">{t('loginMobileNumber')}</Label>
              <Input
                id="claimMobile"
                inputMode="numeric"
                autoComplete="tel"
                value={claimMobile}
                onChange={(e) => setClaimMobile(e.target.value.replace(/\D/g, '').slice(0, 15))}
                placeholder={t('regMobilePlaceholder')}
                className="mt-1.5"
                disabled={claimBusy}
              />
            </div>
            <div>
              <Label htmlFor="claimPassword">{t('opClaimPassword')}</Label>
              <Input
                id="claimPassword"
                type="password"
                autoComplete="new-password"
                value={claimPassword}
                onChange={(e) => setClaimPassword(e.target.value)}
                className="mt-1.5"
                disabled={claimBusy}
              />
              <p className="mt-1 text-[11px] text-muted-foreground">{t('opClaimPasswordHint')}</p>
            </div>
            <Button
              type="submit"
              disabled={claimBusy}
              className="w-full bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90"
            >
              {claimBusy ? t('opClaimSendingOtp') : t('opClaimSendOtp')}
            </Button>
            <button
              type="button"
              className="w-full text-center text-sm text-muted-foreground underline"
              disabled={claimBusy}
              onClick={() => setOperatorView('login')}
            >
              {t('loginBackToMobile')}
            </button>
          </form>
        )}

        {mode === 'operator' && operatorView === 'claimOtp' && (
          <form onSubmit={(e) => void claimVerifySubmit(e)} className="space-y-4">
            <p className="text-sm text-muted-foreground">{t('loginOtpSent', { mobile: claimMobile })}</p>
            <div>
              <Label htmlFor="claimOtp">{t('loginMobileOtp')}</Label>
              <Input
                id="claimOtp"
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={6}
                value={claimOtp}
                onChange={(e) => setClaimOtp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                placeholder="••••••"
                className="mt-1.5 tracking-[0.35em]"
                disabled={claimBusy}
              />
            </div>
            <Button
              type="submit"
              disabled={claimBusy}
              className="w-full bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90"
            >
              {claimBusy ? t('opClaimVerifying') : t('opClaimVerifyAndCreate')}
            </Button>
            <button
              type="button"
              className="w-full text-center text-sm text-muted-foreground underline"
              disabled={claimBusy}
              onClick={() => {
                setOperatorView('claimStart')
                setClaimChallengeId(null)
                setClaimOtp('')
              }}
            >
              {t('loginBackToMobile')}
            </button>
          </form>
        )}

        {mode === 'citizen' && loginError && (
          <p className="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
            Sign-in did not complete ({loginError}). Try mobile OTP again, or open DigiLocker from this page.
          </p>
        )}

        {mode === 'citizen' && step === 'mobile' && (
          <form onSubmit={(e) => void citizenMobileSubmit(e)} className="space-y-4">
            <div>
              <Label htmlFor="mobile">{t('loginMobileNumber')}</Label>
              <Input
                id="mobile"
                inputMode="numeric"
                autoComplete="tel"
                value={mobile}
                onChange={(e) => setMobile(e.target.value.replace(/\D/g, '').slice(0, 15))}
                placeholder={t('regMobilePlaceholder')}
                className="mt-1.5"
                disabled={busy}
              />
            </div>
            <Button
              type="submit"
              disabled={busy}
              className="w-full bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90"
            >
              {busy ? 'Sending OTP…' : 'Send OTP'}
            </Button>
          </form>
        )}

        {mode === 'citizen' && step === 'otp' && (
          <form onSubmit={(e) => void citizenOtpSubmit(e)} className="space-y-4">
            <p className="text-sm text-muted-foreground">
              {t('loginOtpSent', { mobile })}
            </p>
            <div>
              <Label htmlFor="otp">{t('loginMobileOtp')}</Label>
              <Input
                id="otp"
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={6}
                value={otp}
                onChange={(e) => setOtp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                placeholder="••••••"
                className="mt-1.5 tracking-[0.35em]"
                disabled={busy}
              />
              {debugOtp && (
                <p className="mt-1 text-[11px] text-muted-foreground">
                  Dev OTP: <b>{debugOtp}</b> (no real SMS provider wired up)
                </p>
              )}
            </div>
            <Button
              type="submit"
              disabled={busy}
              className="w-full bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90"
            >
              {busy ? 'Verifying…' : 'Verify OTP'}
            </Button>
            <button
              type="button"
              className="w-full text-center text-sm text-muted-foreground underline"
              disabled={busy}
              onClick={() => {
                setStep('mobile')
                setChallengeId(null)
                setDebugOtp(null)
                setOtp('')
              }}
            >
              {t('loginBackToMobile')}
            </button>
          </form>
        )}

        <div className="mt-auto pt-8">
          {mode === 'citizen' && (
            <p className="text-center text-xs text-muted-foreground">
              First-time citizens complete{' '}
              <Link to="/register" search={{ next }} className="font-medium text-[color:var(--gov-blue)]">
                {t('registrationLink')}
              </Link>{' '}
              after sign-in.
            </p>
          )}
          <button
            type="button"
            className="mt-3 w-full text-center text-sm text-muted-foreground underline"
            onClick={() => void navigate({ to: '/', search: { loginError: undefined } })}
          >
            {t('backToHome')}
          </button>
        </div>
      </div>
      </div>
    </div>
  )
}
