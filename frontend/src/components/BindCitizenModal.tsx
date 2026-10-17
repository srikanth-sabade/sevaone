import { Loader2, X } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { api } from '../api'
import { useT } from '../hooks/useT'
import { Button } from './ui/button'
import { Input } from './ui/input'
import { Label } from './ui/label'

/**
 * Assisted-mode "bind a citizen" popup -- shown from HierarchyServiceDetailPanel's Apply button
 * when an operator session has no bound citizen yet. A separate OTP mechanism from the citizen's
 * own login (see OperatorCitizenBindService on the backend): the operator sends the OTP, the
 * citizen standing at the counter reads it out, the operator enters it here. Only on success does
 * the caller proceed to the Seva Sindhu redirect.
 */
export function BindCitizenModal({
  onBound,
  onClose,
}: {
  readonly onBound: (citizen: { sevaOneUserId: number; name: string; mobile: string | null }) => void
  readonly onClose: () => void
}) {
  const { t } = useT()
  const [step, setStep] = useState<'mobile' | 'otp'>('mobile')
  const [mobile, setMobile] = useState('')
  const [otp, setOtp] = useState('')
  const [challengeId, setChallengeId] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const sendOtp = async (e: React.FormEvent) => {
    e.preventDefault()
    const m = mobile.trim()
    if (!/^[0-9]{10,15}$/.test(m)) {
      toast.error('Enter a valid mobile number')
      return
    }
    setBusy(true)
    try {
      const result = await api.operatorSendCitizenOtp(m)
      setChallengeId(result.challengeId)
      setStep('otp')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Could not send OTP')
    } finally {
      setBusy(false)
    }
  }

  const verify = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!challengeId) {
      toast.error('OTP challenge expired. Start again.')
      setStep('mobile')
      return
    }
    const code = otp.trim()
    if (!/^\d{6}$/.test(code)) {
      toast.error('Enter the 6-digit OTP')
      return
    }
    setBusy(true)
    try {
      const citizen = await api.operatorVerifyCitizenOtp(challengeId, code)
      toast.success(t('bindCitizenSuccess'))
      onBound(citizen)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'OTP verification failed')
      setStep('mobile')
      setChallengeId(null)
      setOtp('')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" role="dialog" aria-modal="true">
      <div className="w-full max-w-sm rounded-2xl border border-border bg-card p-6 shadow-lg">
        <div className="mb-4 flex items-start justify-between gap-3">
          <h2 className="text-lg font-bold text-[color:var(--gov-navy)]">{t('bindCitizenTitle')}</h2>
          <button type="button" onClick={onClose} disabled={busy} className="text-muted-foreground hover:text-foreground">
            <X className="h-5 w-5" />
          </button>
        </div>
        <p className="mb-4 text-sm text-muted-foreground">{t('bindCitizenIntro')}</p>

        {step === 'mobile' && (
          <form onSubmit={(e) => void sendOtp(e)} className="space-y-4">
            <div>
              <Label htmlFor="bindMobile">{t('bindCitizenMobile')}</Label>
              <Input
                id="bindMobile"
                inputMode="numeric"
                autoComplete="off"
                value={mobile}
                onChange={(e) => setMobile(e.target.value.replace(/\D/g, '').slice(0, 15))}
                placeholder={t('regMobilePlaceholder')}
                className="mt-1.5"
                disabled={busy}
                autoFocus
              />
            </div>
            <div className="flex gap-2">
              <Button type="button" variant="outline" className="flex-1" disabled={busy} onClick={onClose}>
                {t('bindCitizenCancel')}
              </Button>
              <Button
                type="submit"
                disabled={busy}
                className="flex-1 bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90"
              >
                {busy ? (
                  <>
                    <Loader2 className="mr-1.5 h-4 w-4 animate-spin" /> {t('bindCitizenSendingOtp')}
                  </>
                ) : (
                  t('bindCitizenSendOtp')
                )}
              </Button>
            </div>
          </form>
        )}

        {step === 'otp' && (
          <form onSubmit={(e) => void verify(e)} className="space-y-4">
            <p className="text-sm text-muted-foreground">{t('bindCitizenOtpSent', { mobile })}</p>
            <div>
              <Label htmlFor="bindOtp">{t('bindCitizenOtp')}</Label>
              <Input
                id="bindOtp"
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={6}
                value={otp}
                onChange={(e) => setOtp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                placeholder="••••••"
                className="mt-1.5 tracking-[0.35em]"
                disabled={busy}
                autoFocus
              />
            </div>
            <Button
              type="submit"
              disabled={busy}
              className="w-full bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90"
            >
              {busy ? (
                <>
                  <Loader2 className="mr-1.5 h-4 w-4 animate-spin" /> {t('bindCitizenVerifying')}
                </>
              ) : (
                t('bindCitizenVerify')
              )}
            </Button>
            <button
              type="button"
              className="w-full text-center text-sm text-muted-foreground underline"
              disabled={busy}
              onClick={() => {
                setStep('mobile')
                setChallengeId(null)
                setOtp('')
              }}
            >
              {t('bindCitizenBack')}
            </button>
          </form>
        )}
      </div>
    </div>
  )
}
