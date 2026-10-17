import { useNavigate, useSearch } from '@tanstack/react-router'
import { useEffect, useState } from 'react'
import { api } from '../api'
import {
  applyToService,
  consumeLoginRedirectToast,
  notifyAndGoLogin,
  type PendingApply,
  readPendingApply,
  safeInternalPath,
} from '../applyFlow'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { useSession } from '../store'
import { useT } from '../hooks/useT'

function registerIntro(pending: PendingApply | null): string {
  if (!pending) {
    return 'Your identity is verified. Complete your Seva profile to use the portal.'
  }
  return `Your identity is verified. Complete your Seva profile to continue with ${pending.serviceName}.`
}

export function RegisterPage() {
  const { t } = useT()
  const { me, loaded, refresh } = useSession()
  const navigate = useNavigate()
  const { next } = useSearch({ from: '/register' })
  const pending = readPendingApply()
  const destination = safeInternalPath(next, '/')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState({
    name: '',
    mobile: '',
    email: '',
    address: '',
    district: '',
    taluk: '',
    stateName: '',
    villageName: '',
    dateOfBirth: '',
    gender: '',
    aadhaarLast4: '',
  })

  useEffect(() => {
    consumeLoginRedirectToast()
  }, [])

  useEffect(() => {
    if (!loaded) return
    if (!me) {
      notifyAndGoLogin(destination, 'Sign in to complete registration')
      return
    }
    if (me.profileCompleted) {
      window.location.assign(destination)
      return
    }
    setForm((current) => ({
      ...current,
      name: me.name || current.name,
      mobile: me.mobile || current.mobile,
      email: me.email || current.email,
      address: me.address || current.address,
      district: me.district || current.district,
      taluk: me.taluk || current.taluk,
      stateName: me.stateName || current.stateName,
      villageName: me.villageName || current.villageName,
      dateOfBirth: me.dateOfBirth || current.dateOfBirth,
      gender: me.gender || current.gender,
      aadhaarLast4: me.aadhaarMasked?.slice(-4) || current.aadhaarLast4,
    }))
  }, [loaded, me, destination])

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4 py-10">
      <form
        className="w-full max-w-4xl rounded-2xl border border-border bg-card p-6 shadow-sm md:p-10"
        onSubmit={(e) => {
          e.preventDefault()
          void (async () => {
            try {
              setSaving(true)
              setError(null)
              await api.updateProfile({
                name: form.name,
                mobile: form.mobile,
                email: form.email || undefined,
                address: form.address,
                district: form.district,
                taluk: form.taluk,
                stateName: form.stateName || undefined,
                villageName: form.villageName || undefined,
                dateOfBirth: form.dateOfBirth || undefined,
                gender: form.gender || undefined,
                aadhaarLast4: form.aadhaarLast4 || undefined,
              })
              await refresh()
              const updated = useSession.getState().me ?? {
                ...me!,
                name: form.name,
                mobile: form.mobile,
                email: form.email || null,
                profileCompleted: true,
              }
              if (pending) {
                await applyToService({ serviceCode: pending.serviceCode, departmentCode: pending.departmentCode, name: pending.serviceName }, updated)
                return
              }
              window.location.assign(destination)
            } catch (err) {
              setError(err instanceof Error ? err.message : 'Could not save profile')
            } finally {
              setSaving(false)
            }
          })()
        }}
      >
        <p className="text-sm font-semibold text-[color:var(--gov-blue)]">
          Seva<span className="text-[color:var(--gov-navy)]">One</span>
        </p>
        <h1 className="mt-2 text-2xl font-bold text-[color:var(--gov-navy)]">{t('regTitle')}</h1>
        <Breadcrumbs />
        <p className="mt-3 text-sm text-muted-foreground">{registerIntro(pending)}</p>
        {error && <p className="mt-3 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="mt-4 grid gap-3">
          <label className="text-sm">
            {t('regFullName')} <span className="text-red-500">*</span>
            <input
              className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              required
            />
          </label>
          <label className="text-sm">
            {t('regMobileSignIn')} <span className="text-red-500">*</span>
            <input
              className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
              value={form.mobile}
              onChange={(e) => setForm({ ...form, mobile: e.target.value.replace(/\D/g, '').slice(0, 15) })}
              inputMode="numeric"
              minLength={10}
              maxLength={15}
              pattern="\d{10,15}"
              title={t('regMobileHint')}
              placeholder={t('regMobilePlaceholder')}
              required
            />
          </label>
          <label className="text-sm">
            {t('regEmail')}
            <input
              type="email"
              className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
            />
          </label>
          <label className="text-sm">
            {t('regAddress')} <span className="text-red-500">*</span>
            <textarea
              className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
              value={form.address}
              onChange={(e) => setForm({ ...form, address: e.target.value })}
              required
            />
          </label>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <label className="text-sm">
              {t('regDistrict')} <span className="text-red-500">*</span>
              <input
                className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
                value={form.district}
                onChange={(e) => setForm({ ...form, district: e.target.value })}
                required
              />
            </label>
            <label className="text-sm">
              {t('regTaluk')} <span className="text-red-500">*</span>
              <input
                className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
                value={form.taluk}
                onChange={(e) => setForm({ ...form, taluk: e.target.value })}
                required
              />
            </label>
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <label className="text-sm">
              {t('regState')} <span className="text-red-500">*</span>
              <input
                className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
                value={form.stateName}
                onChange={(e) => setForm({ ...form, stateName: e.target.value })}
                required
              />
            </label>
            <label className="text-sm">
              {t('regVillage')} <span className="text-red-500">*</span>
              <input
                className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
                value={form.villageName}
                onChange={(e) => setForm({ ...form, villageName: e.target.value })}
                required
              />
            </label>
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <label className="text-sm">
              {t('regDob')} <span className="text-red-500">*</span>
              <input
                type="date"
                className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
                value={form.dateOfBirth}
                onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
                required
              />
            </label>
            <label className="text-sm">
              {t('regGender')} <span className="text-red-500">*</span>
              <select
                className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
                value={form.gender}
                onChange={(e) => setForm({ ...form, gender: e.target.value })}
                required
              >
                <option value="" disabled>
                  {t('selectPlaceholder')}
                </option>
                <option value="Male">{t('genderMale')}</option>
                <option value="Female">{t('genderFemale')}</option>
                <option value="Other">{t('genderOther')}</option>
                <option value="PreferNotToSay">{t('genderUnspecified')}</option>
              </select>
            </label>
          </div>
          {(me?.nameKannada || me?.addressKannada) && (
            <div className="rounded-lg border border-input bg-muted px-3 py-2 text-sm text-muted-foreground">
              <p className="font-medium text-foreground">ಕನ್ನಡದಲ್ಲಿ (as per DigiLocker, in Kannada)</p>
              {me.nameKannada && <p className="mt-1">{me.nameKannada}</p>}
              {me.addressKannada && (
                <p>
                  {[me.addressKannada, me.villageKannada, me.districtKannada, me.stateKannada]
                    .filter(Boolean)
                    .join(', ')}
                </p>
              )}
            </div>
          )}
          <label className="text-sm">
            {t('regAadhaarLast4')}
            <input
              className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
              value={form.aadhaarLast4}
              onChange={(e) => setForm({ ...form, aadhaarLast4: e.target.value })}
              maxLength={4}
            />
          </label>
        </div>
        <button
          type="submit"
          disabled={saving}
          className="mt-5 w-full rounded-lg bg-[color:var(--gov-blue)] px-4 py-2.5 text-sm font-medium text-white hover:opacity-90 disabled:opacity-60"
        >
          {saving ? 'Saving…' : 'Save and continue'}
        </button>
        <button
          type="button"
          className="mt-3 w-full text-sm text-muted-foreground underline"
          onClick={() => void navigate({ to: '/', search: { loginError: undefined } })}
        >
          {t('regCancel')}
        </button>
      </form>
    </div>
  )
}
