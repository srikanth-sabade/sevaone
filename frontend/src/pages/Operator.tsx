import { Link, useNavigate } from '@tanstack/react-router'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import {
  CheckCircle2,
  ClipboardList,
  IndianRupee,
  LogOut,
  Printer,
  Receipt,
  Search,
  ShieldCheck,
  Store,
  UserRound,
} from 'lucide-react'
import { toast } from 'sonner'
import { api } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { DeptServicesPanel } from '../components/DeptServicesPanel'
import { totalFeeInr } from '../lib/fee'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Card } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { fromOperatorMe, type OperatorSession } from '../lib/operator'
import { useT } from '../hooks/useT'
import { serviceName, departmentName } from '../lib/catalogKn'

type BoundCitizen = {
  citizenId: string
  sevaOneUserId: number
  name: string
  mobile?: string | null
  aadhaarMasked?: string | null
  email?: string | null
  profileCompleted: boolean
}

type Ack = {
  ack: string
  citizen: string
  mobile: string
  service: string
  fee: string
  at: string
  applyUrl?: string
}

const IDLE_MS = 15 * 60 * 1000

export function OperatorPage() {
  const { lang, t } = useT()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const operatorQ = useQuery({
    queryKey: ['operator', 'me'],
    queryFn: async () => fromOperatorMe(await api.operatorMe()),
    retry: false,
  })
  const operator: OperatorSession | null = operatorQ.data ?? null
  const schemes = useQuery({ queryKey: ['schemes', 'operator'], queryFn: () => api.schemes(), enabled: !!operator })
  const txnsQ = useQuery({
    queryKey: ['operator', 'txns'],
    queryFn: () => api.operatorTxns(),
    enabled: !!operator,
    refetchInterval: 30_000,
  })

  const [step, setStep] = useState<1 | 2 | 3 | 4>(1)
  const [mobile, setMobile] = useState('')
  /** Lookup preview only — not server-bound until OTP bind / register. */
  const [lookedUp, setLookedUp] = useState<BoundCitizen | null>(null)
  /** Server-bound citizen (Redis ActiveCitizenId). */
  const [citizen, setCitizen] = useState<BoundCitizen | null>(null)
  const [challengeId, setChallengeId] = useState<string | null>(null)
  const [otp, setOtp] = useState('')
  const [otpHint, setOtpHint] = useState<string | null>(null)
  const [registerName, setRegisterName] = useState('')
  const [profileAddress, setProfileAddress] = useState('')
  const [profileDistrict, setProfileDistrict] = useState('')
  const [profileTaluk, setProfileTaluk] = useState('')
  const [consent, setConsent] = useState(false)
  const [serviceCode, setServiceCode] = useState('')
  const [busy, setBusy] = useState(false)
  const [wallet, setWallet] = useState<number | null>(null)
  const [ack, setAck] = useState<Ack | null>(null)
  const [lastActivity, setLastActivity] = useState(() => Date.now())

  const selectedCode = serviceCode || schemes.data?.[0]?.serviceCode || ''
  const service = useMemo(() => schemes.data?.find((s) => s.serviceCode === selectedCode), [schemes.data, selectedCode])

  useEffect(() => {
    if (!operator) return
    const bump = () => setLastActivity(Date.now())
    window.addEventListener('pointerdown', bump)
    window.addEventListener('keydown', bump)
    const timer = window.setInterval(() => {
      if (Date.now() - lastActivity > IDLE_MS) {
        void api.operatorLogout().finally(() => {
          qc.setQueryData(['operator', 'me'], null)
          qc.setQueryData(['operator', 'session'], null)
          toast.message('Counter session ended after idle timeout')
          void navigate({ to: '/login', search: { returnTo: '/operator', loginError: undefined, mode: undefined } })
        })
      }
    }, 30_000)
    return () => {
      window.removeEventListener('pointerdown', bump)
      window.removeEventListener('keydown', bump)
      window.clearInterval(timer)
    }
  }, [operator, lastActivity, navigate])

  // Hydrate bound citizen from Redis when the console mounts / operator session refreshes.
  useEffect(() => {
    if (!operator) return
    let cancelled = false
    void api
      .operatorCitizenBound()
      .then((res) => {
        if (cancelled) return
        if (res.bound && res.citizen) {
          setCitizen(res.citizen)
          setLookedUp(null)
          setMobile(res.citizen.mobile?.replace(/\D/g, '').slice(-10) || '')
          setRegisterName(res.citizen.name)
          setStep(res.citizen.profileCompleted ? 2 : 1)
        }
      })
      .catch(() => {
        /* ignore — operator may not have a bound citizen yet */
      })
    return () => {
      cancelled = true
    }
  }, [operator?.operatorId])

  if (operatorQ.isLoading) {
    return <div className="mx-auto max-w-xl px-4 py-20 text-center text-sm text-muted-foreground">{t('opCheckingSession')}</div>
  }

  if (!operator) {
    return (
      <div className="mx-auto max-w-xl px-4 py-20 text-center">
        <Store className="mx-auto h-10 w-10 text-[color:var(--gov-blue)]" />
        <h1 className="mt-4 text-2xl font-bold text-[color:var(--gov-navy)]">{t('opConsoleTitle')}</h1>
        <div className="flex justify-center">
          <Breadcrumbs />
        </div>
        <p className="mt-3 text-sm text-muted-foreground">
          {t('opSignInIntro')}
        </p>
        <Button asChild className="mt-6 bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90">
          <Link to="/login" search={{ mode: 'operator', returnTo: '/operator', loginError: undefined }}>
            {t('opGoToSignIn')}
          </Link>
        </Button>
      </div>
    )
  }

  const balance = wallet ?? operator.walletBalance
  const serverTxns = txnsQ.data ?? []

  const lookupCitizen = async () => {
    if (!/^[6-9]\d{9}$/.test(mobile)) return toast.error('Enter a valid 10-digit mobile number')
    setBusy(true)
    try {
      const found = await api.operatorCitizenLookup(mobile)
      if (!found.found) {
        setLookedUp(null)
        setChallengeId(null)
        toast.message('Citizen not found — register them at the counter')
        return
      }
      // Preview only — fee / DigiLocker / apply require OTP bind or register.
      setLookedUp(found)
      setRegisterName(found.name)
      toast.success('Citizen found — send OTP and Bind to confirm presence at the counter')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Lookup failed')
    } finally {
      setBusy(false)
    }
  }

  const sendOtp = async () => {
    if (!/^[6-9]\d{9}$/.test(mobile)) return toast.error('Enter a valid 10-digit mobile number')
    setBusy(true)
    try {
      const ch = await api.operatorCitizenChallenge(mobile)
      setChallengeId(ch.challengeId)
      setOtpHint(ch.mobileHint)
      setOtp('')
      toast.success(`OTP sent (${ch.mobileHint}) — also logged on BFF for PoC`)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'OTP send failed')
    } finally {
      setBusy(false)
    }
  }

  const bindCitizen = async () => {
    if (!challengeId) return toast.error('Request an OTP first')
    if (!/^\d{6}$/.test(otp.trim())) return toast.error('Enter the 6-digit OTP')
    setBusy(true)
    try {
      const bound = await api.operatorCitizenBind({ challengeId, otp: otp.trim() })
      setCitizen(bound)
      setLookedUp(null)
      setChallengeId(null)
      setOtp('')
      setStep(bound.profileCompleted ? 2 : 1)
      void qc.invalidateQueries({ queryKey: ['operator', 'me'] })
      toast.success(`Bound ${bound.name}`)
      if (!bound.profileCompleted) toast.message('Complete the citizen profile before continuing')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Bind failed')
    } finally {
      setBusy(false)
    }
  }

  const registerCitizen = async () => {
    if (!/^[6-9]\d{9}$/.test(mobile)) return toast.error('Enter a valid 10-digit mobile number')
    if (registerName.trim().length < 2) return toast.error('Enter the citizen name')
    setBusy(true)
    try {
      const created = await api.operatorCitizenRegister({
        mobile,
        name: registerName.trim(),
        address: profileAddress || undefined,
        district: profileDistrict || undefined,
        taluk: profileTaluk || undefined,
      })
      setCitizen(created)
      setLookedUp(null)
      setStep(created.profileCompleted ? 2 : 1)
      void qc.invalidateQueries({ queryKey: ['operator', 'me'] })
      toast.success('Citizen registered and bound')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Register failed')
    } finally {
      setBusy(false)
    }
  }

  const completeProfile = async () => {
    if (!citizen) return
    setBusy(true)
    try {
      const updated = await api.operatorCitizenProfile({
        mobile: citizen.mobile || mobile,
        name: registerName.trim() || citizen.name,
        address: profileAddress || undefined,
        district: profileDistrict || undefined,
        taluk: profileTaluk || undefined,
      })
      setCitizen(updated)
      if (updated.profileCompleted) setStep(2)
      toast.success(updated.profileCompleted ? 'Profile completed' : 'Profile saved')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Profile update failed')
    } finally {
      setBusy(false)
    }
  }

  const unbind = async () => {
    setBusy(true)
    try {
      await api.operatorCitizenUnbind()
      setCitizen(null)
      setLookedUp(null)
      setConsent(false)
      setChallengeId(null)
      setOtp('')
      setStep(1)
      void qc.invalidateQueries({ queryKey: ['operator', 'me'] })
      toast.success('Citizen unbound')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unbind failed')
    } finally {
      setBusy(false)
    }
  }

  const submit = async () => {
    if (!citizen) return toast.error('Bind a citizen first')
    if (!consent) return toast.error("Record the citizen's Aadhaar e-KYC consent")
    if (!service) return toast.error('Select a service')
    setBusy(true)
    let collectedTxnId: string | null = null
    try {
      const amount = totalFeeInr(service)
      const collect = await api.operatorCollectFee({
        serviceCode: service.serviceCode,
        amountInr: amount,
        idempotencyKey: `fee-${service.serviceCode}-${citizen.citizenId}-${Date.now()}`,
        citizenId: citizen.citizenId,
      })
      collectedTxnId = collect.txnId
      setWallet(collect.walletBalance)

      let ackRef = collect.txnId
      let applyUrl: string | undefined
      if (service.departmentCode === 'revenue') {
        try {
          const outcome = await api.createApplyToken(service.departmentCode, service.serviceCode)
          if (outcome.requiresLogin || !outcome.applyUrl) throw new Error('Could not mint apply token')
          applyUrl = outcome.applyUrl
          ackRef = `ASSIST-${service.serviceCode}`
          window.open(outcome.applyUrl, '_blank', 'noopener,noreferrer')
          toast.success('Assisted apply URL opened')
        } catch (mintErr) {
          if (collectedTxnId) {
            const reversed = await api.operatorReverseFee({
              txnId: collectedTxnId,
              reason: 'apply-token mint failed',
            })
            setWallet(reversed.walletBalance)
          }
          throw mintErr
        }
      } else {
        try {
          const app = await api.operatorSubmit({
            departmentCode: service.departmentCode,
            serviceCode: service.serviceCode,
            applicantName: citizen.name,
            address: profileAddress || citizen.name,
            purpose: `Assisted via ${operator.channel}`,
            district: profileDistrict || undefined,
            taluk: profileTaluk || undefined,
          })
          ackRef = app.sevaReference
        } catch (submitErr) {
          if (collectedTxnId) {
            const reversed = await api.operatorReverseFee({
              txnId: collectedTxnId,
              reason: 'assisted submit failed',
            })
            setWallet(reversed.walletBalance)
          }
          throw submitErr
        }
      }

      const nextAck: Ack = {
        ack: ackRef,
        citizen: citizen.name,
        mobile: citizen.mobile || mobile,
        service: service.name,
        fee: `₹${amount}`,
        at: new Date().toLocaleTimeString(),
        applyUrl,
      }
      setAck(nextAck)
      setStep(4)
      void qc.invalidateQueries({ queryKey: ['operator', 'txns'] })
      toast.success(`Fee recorded · wallet ₹${collect.walletBalance}`)
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Submit failed'
      if (/insufficient/i.test(msg)) {
        toast.error('Insufficient wallet balance (409). Top up the centre wallet and retry.')
      } else {
        toast.error(msg)
      }
    } finally {
      setBusy(false)
    }
  }

  const printAck = () => {
    if (!ack) return
    const w = window.open('', '_blank', 'noopener,noreferrer,width=480,height=640')
    if (!w) {
      toast.error('Allow pop-ups to print the acknowledgement')
      return
    }
    w.document.write(`<!doctype html><html><head><title>Ack ${ack.ack}</title>
      <style>body{font-family:Georgia,serif;padding:24px;color:#0a1a3c} h1{font-size:18px} .row{margin:8px 0;font-size:14px}</style>
      </head><body>
      <h1>{t('opAckHeader')}</h1>
      <div class="row"><b>{t('opAckRef')}</b> ${ack.ack}</div>
      <div class="row"><b>{t('opCitizen')}</b> ${ack.citizen} · +91-${ack.mobile}</div>
      <div class="row"><b>{t('opService')}</b> ${ack.service}</div>
      <div class="row"><b>{t('opFee')}</b> ${ack.fee}</div>
      <div class="row"><b>{t('opCentre')}</b> ${operator.centreName} · ${operator.operatorId}</div>
      <div class="row"><b>{t('opTime')}</b> ${ack.at}</div>
      ${ack.applyUrl ? `<div class="row"><b>{t('opApplyUrl')}</b> ${ack.applyUrl}</div>` : ''}
      <script>window.print()</script>
      </body></html>`)
    w.document.close()
  }

  const endShift = async () => {
    try {
      const report = await api.operatorShiftReport()
      toast.message(
        `Shift report · ${report.collections} collections · ₹${report.collectedInr} · wallet ₹${report.walletBalance}`,
      )
    } catch {
      /* ignore */
    }
    await api.operatorLogout()
    qc.setQueryData(['operator', 'me'], null)
    qc.setQueryData(['operator', 'session'], null)
    toast.success('Counter shift ended')
    void navigate({ to: '/', search: { loginError: undefined } })
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <div className="flex flex-wrap items-center gap-3 rounded-2xl border border-border bg-card p-5 shadow-sm">
        <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-[color:var(--gov-blue)]/10">
          <Store className="h-5 w-5 text-[color:var(--gov-blue)]" />
        </div>
        <div>
          <h1 className="text-lg font-bold text-[color:var(--gov-navy)]">{operator.channelName}</h1>
          <Breadcrumbs />
          <p className="mt-1 text-xs text-muted-foreground">
            {operator.centreName} · Operator {operator.operatorId}
            {operator.email ? ` · ${operator.email}` : ''} · channel {operator.channel}
            {balance != null ? ` · wallet ₹${balance}` : ''}
          </p>
        </div>
        <div className="ml-auto flex items-center gap-2">
          <Badge variant="secondary" className="gap-1">
            <ShieldCheck className="h-3.5 w-3.5" /> seva.opr
          </Badge>
          <Button size="sm" variant="outline" className="rounded-lg" onClick={() => void endShift()}>
            <LogOut className="mr-1.5 h-4 w-4" /> End shift
          </Button>
        </div>
      </div>

      <div className="mt-4 flex flex-wrap gap-2 text-xs">
        {[
          { n: 1, label: 'Bind citizen' },
          { n: 2, label: 'Consent' },
          { n: 3, label: 'Service & fee' },
          { n: 4, label: 'Submit / ack' },
        ].map((s) => (
          <div
            key={s.n}
            className={`rounded-full px-3 py-1 ${step === s.n ? 'bg-[color:var(--gov-blue)] text-white' : 'bg-secondary text-muted-foreground'}`}
          >
            {s.n}. {s.label}
          </div>
        ))}
      </div>

      {citizen && (
        <div id="counter-services" className="mt-6 scroll-mt-24">
          <DeptServicesPanel
            departmentCode={service?.departmentCode || 'revenue'}
            serviceCode={selectedCode || 'SVC001'}
            signerName={citizen.name}
            assisted
          />
        </div>
      )}

      <div className="mt-6 grid gap-6 lg:grid-cols-[1.2fr_1fr]">
        <Card id="counter-bind" className="scroll-mt-24 rounded-2xl p-5">
          <h2 className="flex items-center gap-2 text-sm font-semibold text-[color:var(--gov-navy)]">
            <UserRound className="h-4 w-4 text-[color:var(--gov-blue)]" /> Apply on behalf of a citizen
          </h2>

          {!citizen && (
            <div className="mt-4 space-y-3">
              <div className="flex gap-2">
                <Input
                  inputMode="numeric"
                  maxLength={10}
                  value={mobile}
                  onChange={(e) => setMobile(e.target.value.replace(/\D/g, ''))}
                  placeholder="Citizen mobile (try 9876543210)"
                />
                <Button type="button" variant="outline" disabled={busy} onClick={() => void lookupCitizen()} className="shrink-0">
                  <Search className="mr-1.5 h-4 w-4" /> Lookup
                </Button>
              </div>
              {lookedUp && (
                <div className="rounded-xl border border-[color:var(--gov-blue)]/25 bg-[color:var(--gov-blue)]/5 p-3 text-sm">
                  <div className="font-semibold text-[color:var(--gov-navy)]">{lookedUp.name}</div>
                  <p className="mt-0.5 text-xs text-muted-foreground">
                    Found · +91-{lookedUp.mobile || mobile} · Aadhaar {lookedUp.aadhaarMasked || '—'}
                  </p>
                  <p className="mt-2 text-xs text-[color:var(--gov-navy)]">
                    {t('opBindHint')}
                  </p>
                </div>
              )}
              <div className="flex flex-wrap gap-2">
                <Button type="button" variant="outline" disabled={busy} onClick={() => void sendOtp()}>
                  {t('opSendPresenceOtp')}
                </Button>
                {challengeId && (
                  <>
                    <Input
                      className="max-w-[8rem] tracking-[0.3em]"
                      inputMode="numeric"
                      maxLength={6}
                      value={otp}
                      onChange={(e) => setOtp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                      placeholder="OTP"
                    />
                    <Button type="button" disabled={busy} onClick={() => void bindCitizen()}>
                      {t('opBind')}
                    </Button>
                  </>
                )}
              </div>
              {otpHint && <p className="text-[11px] text-muted-foreground">OTP destination hint: {otpHint}</p>}
              <div className="rounded-xl border border-dashed border-border p-3">
                <p className="text-xs font-medium text-[color:var(--gov-navy)]">{t('opRegisterCitizen')}</p>
                <div className="mt-2 grid gap-2 sm:grid-cols-2">
                  <Input value={registerName} onChange={(e) => setRegisterName(e.target.value)} placeholder={t('regFullName')} />
                  <Input value={profileAddress} onChange={(e) => setProfileAddress(e.target.value)} placeholder={t('regAddress')} />
                  <Input value={profileDistrict} onChange={(e) => setProfileDistrict(e.target.value)} placeholder={t('regDistrict')} />
                  <Input value={profileTaluk} onChange={(e) => setProfileTaluk(e.target.value)} placeholder={t('regTaluk')} />
                </div>
                <Button type="button" className="mt-2" variant="secondary" disabled={busy} onClick={() => void registerCitizen()}>
                  {t('opRegisterBind')}
                </Button>
              </div>
            </div>
          )}

          {citizen && (
            <div className="mt-4 space-y-4">
              <div className="rounded-2xl border border-border bg-secondary/50 p-3 text-sm">
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <div className="font-semibold">{citizen.name}</div>
                    <div className="text-xs text-muted-foreground">
                      +91-{citizen.mobile || mobile} · Aadhaar {citizen.aadhaarMasked || '—'} · SevaOneUserID {citizen.sevaOneUserId}
                      {!citizen.profileCompleted ? ' · profile incomplete' : ''}
                    </div>
                  </div>
                  <Button type="button" size="sm" variant="ghost" disabled={busy} onClick={() => void unbind()}>
                    {t('opUnbind')}
                  </Button>
                </div>
              </div>

              {!citizen.profileCompleted && (
                <div className="rounded-xl border border-amber-200 bg-amber-50 p-3">
                  <p className="text-xs font-medium text-amber-950">{t('opAssistedProfile')}</p>
                  <div className="mt-2 grid gap-2 sm:grid-cols-2">
                    <Input value={registerName || citizen.name} onChange={(e) => setRegisterName(e.target.value)} placeholder={t('regFullName')} />
                    <Input value={profileAddress} onChange={(e) => setProfileAddress(e.target.value)} placeholder={t('regAddress')} />
                    <Input value={profileDistrict} onChange={(e) => setProfileDistrict(e.target.value)} placeholder={t('regDistrict')} />
                    <Input value={profileTaluk} onChange={(e) => setProfileTaluk(e.target.value)} placeholder={t('regTaluk')} />
                  </div>
                  <Button type="button" className="mt-2" size="sm" disabled={busy} onClick={() => void completeProfile()}>
                    {t('opSaveProfile')}
                  </Button>
                </div>
              )}

              <label className="flex items-start gap-2 text-xs text-muted-foreground">
                <input
                  type="checkbox"
                  checked={consent}
                  onChange={(e) => {
                    setConsent(e.target.checked)
                    if (e.target.checked) setStep(3)
                    else setStep(2)
                  }}
                  className="mt-0.5"
                />
                {t('opConsentNote')}
              </label>

              <div>
                <Label htmlFor="svc">{t('opServiceMulti')}</Label>
                <select
                  id="svc"
                  value={selectedCode}
                  onChange={(e) => setServiceCode(e.target.value)}
                  className="mt-1.5 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
                >
                  {(schemes.data ?? []).map((s) => (
                    <option key={s.serviceCode} value={s.serviceCode}>
                      {serviceName(lang, s.serviceCode, s.name)} — {departmentName(lang, s.departmentCode, s.departmentName)}
                    </option>
                  ))}
                </select>
              </div>

              {service && (
                <div className="grid grid-cols-2 gap-3 text-xs">
                  <div className="rounded-2xl border border-border p-3">
                    <div className="text-muted-foreground">{t('opSakalaTimeline')}</div>
                    <div className="mt-0.5 text-sm font-semibold">{service.sakalaDays} days</div>
                  </div>
                  <div className="rounded-2xl border border-border p-3">
                    <div className="text-muted-foreground">{t('opCounterCollection')}</div>
                    <div className="mt-0.5 flex items-center gap-1 text-sm font-semibold">
                      <IndianRupee className="h-3.5 w-3.5" />
                      {totalFeeInr(service)}
                    </div>
                  </div>
                </div>
              )}

              <Button
                disabled={busy || !consent}
                onClick={() => void submit()}
                className="w-full rounded-lg bg-[color:var(--gov-blue)] text-white hover:bg-[color:var(--gov-blue)]/90"
              >
                {t('opCollectSubmit')}
              </Button>
            </div>
          )}
        </Card>

        <Card id="counter-shift" className="scroll-mt-24 rounded-2xl p-5">
          <h2 className="flex items-center gap-2 text-sm font-semibold text-[color:var(--gov-navy)]">
            <Receipt className="h-4 w-4 text-[color:var(--gov-blue)]" /> Today at this counter
          </h2>
          <div className="mt-3 grid grid-cols-3 gap-2 text-center">
            {[
              { k: 'Collections', v: serverTxns.filter((t) => t.status === 'Success').length },
              { k: 'Reversals', v: serverTxns.filter((t) => t.status === 'Reversed').length },
              { k: 'Wallet', v: balance != null ? `₹${balance}` : '—' },
            ].map((s) => (
              <div key={s.k} className="rounded-2xl border border-border p-3">
                <div className="text-lg font-bold text-[color:var(--gov-navy)]">{s.v}</div>
                <div className="text-[11px] text-muted-foreground">{s.k}</div>
              </div>
            ))}
          </div>

          <ul className="mt-4 space-y-2">
            {serverTxns.length === 0 && (
              <li className="rounded-2xl border border-dashed border-border p-6 text-center text-xs text-muted-foreground">
                {t('opNoWalletTxn')}
              </li>
            )}
            {serverTxns.map((t) => (
              <li key={t.id} className="rounded-2xl border border-border p-3 text-xs">
                <div className="flex items-center justify-between">
                  <span className="font-semibold">{t.serviceCode || 'Fee'}</span>
                  <span className="flex items-center gap-1 text-[color:var(--gov-blue)]">
                    <CheckCircle2 className="h-3.5 w-3.5" /> {t.status} · ₹{t.amount}
                  </span>
                </div>
                <div className="mt-1 text-muted-foreground">{new Date(t.createdAt).toLocaleTimeString()}</div>
              </li>
            ))}
          </ul>

          {ack && (
            <div className="mt-4 rounded-2xl border border-[color:var(--gov-blue)]/30 bg-[color:var(--gov-blue)]/5 p-3 text-xs">
              <div className="flex items-center gap-2 font-semibold text-[color:var(--gov-navy)]">
                <ClipboardList className="h-4 w-4" /> Latest acknowledgement
              </div>
              <p className="mt-2">
                {ack.service} · {ack.ack} · {ack.citizen}
              </p>
              <div className="mt-2 flex gap-2">
                <Button type="button" size="sm" variant="outline" onClick={printAck}>
                  <Printer className="mr-1.5 h-3.5 w-3.5" /> Print ack
                </Button>
                {ack.applyUrl && (
                  <Button type="button" size="sm" variant="outline" onClick={() => window.open(ack.applyUrl, '_blank', 'noopener,noreferrer')}>
                    {t('opReopenApplyUrl')}
                  </Button>
                )}
              </div>
            </div>
          )}
        </Card>
      </div>
    </div>
  )
}
