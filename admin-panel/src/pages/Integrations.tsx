import { useEffect, useMemo, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, ChevronDown, Copy, KeyRound, Link2, Lock, Plug, Save, Search, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { govAdminApi } from '../api'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Card } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import type { KaveriSettings, TokenIntegration, TokenIntegrationListItem, TokenIntegrationNewCredential, TokenIntegrationType, TokenProviderTestResult, TokenStatusTestResult } from '../types'

const CITIZEN_FIELDS: ReadonlyArray<readonly [string, string]> = [
  ['aadhaarHash', 'Aadhaar hash'], ['aadhaarMasked', 'Aadhaar (masked)'], ['name', 'Full name'], ['mobile', 'Mobile number'],
  ['email', 'Email'], ['dateOfBirth', 'Date of birth (yyyy-MM-dd)'], ['gender', 'Gender'], ['address', 'Address'],
  ['district', 'District'], ['taluk', 'Taluk'], ['state', 'State'], ['village', 'Village'], ['digilockerId', 'DigiLocker id'],
  ['sevaId', 'Seva ID'], ['username', 'Username'], ['sevaOneUserId', 'SevaOne user id'],
]
// Retired choices: still shown for a service already using one, but no longer offered for new setups.
const HIDDEN_TYPES = ['seva_sindhu_v3', 'edcs_api_form']
type KeySource = 'none' | 'aadhaar' | 'fixed' | 'citizen' | 'ask'

/** A key box holds plain text, "{citizen:field}" or "{ask:question}" -- the same text the server reads. */
function readKey(value: string): { source: KeySource; text: string } {
  const v = value.trim()
  if (!v) return { source: 'none', text: '' }
  const citizen = /^\{citizen:(.*)\}$/.exec(v)
  if (citizen) return citizen[1].trim() === 'aadhaarHash' ? { source: 'aadhaar', text: '' } : { source: 'citizen', text: citizen[1].trim() }
  const ask = /^\{ask:(.*)\}$/.exec(v)
  if (ask) return { source: 'ask', text: ask[1].trim() }
  return { source: 'fixed', text: v }
}
function writeKey(source: KeySource, text: string): string {
  if (source === 'none') return ''
  if (source === 'aadhaar') return '{citizen:aadhaarHash}'
  if (source === 'citizen') return `{citizen:${text || CITIZEN_FIELDS[1][0]}}`
  if (source === 'ask') return `{ask:${text}}`
  return text
}

function KeyRow({ label, value, onChange, hint }: { readonly label: string; readonly value: string; readonly onChange: (value: string) => void; readonly hint?: string }) {
  const { source, text } = readKey(value)
  return (
    <div className="grid gap-2 sm:grid-cols-[5rem_13rem_1fr] sm:items-center">
      <Label className="text-sm font-medium">{label}</Label>
      <select aria-label={`${label} source`} value={source} onChange={(event) => { const next = event.target.value as KeySource; onChange(writeKey(next, next === 'citizen' ? CITIZEN_FIELDS[1][0] : '')) }} className="h-9 w-full rounded-md border bg-background px-3 text-sm">
        <option value="none">Not used</option>
        <option value="aadhaar">Aadhaar hash</option>
        <option value="ask">Ask the citizen at Apply (you name it)</option>
        <option value="fixed">Same value every time</option>
        <option value="citizen">Another detail from the citizen's record</option>
      </select>
      {source === 'fixed' && <Input aria-label={`${label} value`} value={text} onChange={(event) => onChange(writeKey('fixed', event.target.value))} placeholder="Value to send" />}
      {source === 'citizen' && (
        <select aria-label={`${label} field`} value={text} onChange={(event) => onChange(writeKey('citizen', event.target.value))} className="h-9 w-full rounded-md border bg-background px-3 text-sm">
          {CITIZEN_FIELDS.slice(1).map(([field, name]) => <option key={field} value={field}>{name}</option>)}
        </select>
      )}
      {source === 'ask' && <Input aria-label={`${label} question`} value={text} onChange={(event) => onChange(writeKey('ask', event.target.value))} placeholder="Name shown to the citizen, e.g. FRUDS ID" />}
      {source === 'none' && <span className="text-xs text-muted-foreground">{hint ?? 'Nothing is sent for this key.'}</span>}
    </div>
  )
}

type Draft = {
  typeKey: string
  landingUrl: string
  statusUrl: string
  sendMobile: boolean
  sendDigilockerId: boolean
  allowedIps: string
  isEnabled: boolean
  statusAuthType: string
  statusAuthUser: string
  statusAuthHeader: string
  statusSecret: string
  clearStatusSecret: boolean
  statusPath: string
  messagePath: string
  statusMapText: string
  kaveriServiceId: string
  sevasindhuServiceId: string
  edcsKey1: string
  edcsKey2: string
  edcsKey3: string
  edcsKey4: string
  edcsKey5: string
  statusRequestField: string
  statusNumberSource: string
}

const KAVERI = 'kaveri_sso'
const EDCS_API = 'edcs_api'
const EDCS_API_FORM = 'edcs_api_form'
const OUR_STATUSES = ['Submitted', 'DocumentVerification', 'UnderReview', 'Approved', 'Rejected']

function mapToText(map: Record<string, string> | null) {
  return Object.entries(map ?? {}).map(([word, ours]) => `${word} = ${ours}`).join('\n')
}

function textToMap(text: string): Record<string, string> {
  const map: Record<string, string> = {}
  for (const line of text.split('\n')) {
    const at = line.lastIndexOf('=')
    if (at > 0 && line.slice(0, at).trim()) map[line.slice(0, at).trim()] = line.slice(at + 1).trim()
  }
  return map
}

const operatorLabels: Record<string, string> = {
  ajsk: 'AJSK',
  sakala_token: 'Sakala token',
  sakala_token_escom: 'Sakala token (ESCOM)',
}

function toDraft(item: TokenIntegration, defaultType: string): Draft {
  return {
    typeKey: item.typeKey ?? defaultType,
    landingUrl: item.landingUrl ?? '',
    statusUrl: item.statusUrl ?? '',
    sendMobile: item.sendMobile,
    sendDigilockerId: item.sendDigilockerId,
    allowedIps: item.allowedIps.join('\n'),
    isEnabled: item.isEnabled,
    statusAuthType: item.statusConfig.authType,
    statusAuthUser: item.statusConfig.authUser ?? '',
    statusAuthHeader: item.statusConfig.authHeaderName ?? '',
    statusSecret: '',
    clearStatusSecret: false,
    statusPath: item.statusConfig.statusPath,
    messagePath: item.statusConfig.messagePath ?? '',
    statusMapText: mapToText(item.statusConfig.statusMap),
    kaveriServiceId: item.kaveriServiceId?.toString() ?? '',
    sevasindhuServiceId: item.operatorSevasindhuId ?? '',
    edcsKey1: item.edcsKey1 ?? '',
    edcsKey2: item.edcsKey2 ?? '',
    edcsKey3: item.edcsKey3 ?? '',
    edcsKey4: item.edcsKey4 ?? '',
    edcsKey5: item.edcsKey5 ?? '',
    statusRequestField: item.statusConfig.requestField || 'GscNumber',
    statusNumberSource: item.statusConfig.numberSource || 'department',
  }
}

function serviceKey(item: { departmentCode: string; serviceCode: string }) {
  return `${item.departmentCode}/${item.serviceCode}`
}

function when(value: string | null) {
  return value ? new Intl.DateTimeFormat('en-IN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : ''
}

/** Calls the saved status API once with a sample GSC number, to check the settings above before real applications use them. */
function StatusTest({ departmentCode, serviceCode, saved }: { departmentCode: string; serviceCode: string; saved: boolean }) {
  const [gsc, setGsc] = useState('')
  const [busy, setBusy] = useState(false)
  const [result, setResult] = useState<TokenStatusTestResult | null>(null)

  async function run() {
    setBusy(true)
    try {
      setResult(await govAdminApi.testIntegrationStatus(departmentCode, serviceCode, gsc.trim()))
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'The test could not run.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="space-y-2 border-t pt-3">
      <p className="text-sm font-medium text-[color:var(--gov-navy)]">Try it</p>
      <div className="flex flex-wrap items-center gap-2">
        <Input value={gsc} onChange={(event) => setGsc(event.target.value)} placeholder="A real GSC number from the department" className="max-w-xs" aria-label="GSC number" />
        <Button type="button" size="sm" variant="outline" disabled={busy || !saved || !gsc.trim()} onClick={() => void run()}>Test the status API</Button>
      </div>
      {!saved && <p className="text-xs text-muted-foreground">Save your changes first. The test uses what is saved.</p>}
      {result && (
        <div className="space-y-1 rounded-lg border bg-muted/40 p-3 text-xs">
          <p>{result.error ? <span className="font-medium text-red-700">{result.error}</span> : <span className="font-medium text-[color:var(--gov-green)]">The department answered.</span>}{result.httpStatus ? ` HTTP ${result.httpStatus}.` : ''}</p>
          {result.departmentStatus && <p>Status found: <b>{result.departmentStatus}</b>{result.mappedStatus ? <> → our status <b>{result.mappedStatus}</b></> : ' (not mapped, so it would only appear on the timeline)'}</p>}
          {result.message && <p>Message: {result.message}</p>}
          {result.rawBody && <pre className="max-h-40 overflow-auto whitespace-pre-wrap break-all rounded bg-background p-2 font-mono">{result.rawBody}</pre>}
        </div>
      )}
    </div>
  )
}

const emptyKaveri: KaveriSettings = { loginUrl: '', refreshUrl: '', ssoUrl: '', statusUrl: '', username: '', assumeEmailVerified: false }

/** The addresses and technical login SevaOne uses with Kaveri. Shared by every Kaveri service; the password is write-only. */
function KaveriProviderPanel({ typeKey }: { typeKey: string }) {
  const queryClient = useQueryClient()
  const provider = useQuery({ queryKey: ['integration-provider', typeKey], queryFn: () => govAdminApi.integrationProvider(typeKey) })
  const [form, setForm] = useState<KaveriSettings>(emptyKaveri)
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [test, setTest] = useState<TokenProviderTestResult | null>(null)

  useEffect(() => { if (provider.data) setForm(provider.data.kaveri ?? emptyKaveri) }, [provider.data])

  async function save() {
    setBusy(true)
    try {
      await govAdminApi.saveIntegrationProvider(typeKey, form, password || undefined)
      setPassword('')
      setTest(null)
      toast.success('Kaveri settings saved.')
      await queryClient.invalidateQueries({ queryKey: ['integration-provider', typeKey] })
      await queryClient.invalidateQueries({ queryKey: ['integration'] })
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Could not save the Kaveri settings.')
    } finally {
      setBusy(false)
    }
  }

  async function signIn() {
    setBusy(true)
    try {
      setTest(await govAdminApi.testIntegrationProviderLogin(typeKey))
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'The test could not run.')
    } finally {
      setBusy(false)
    }
  }

  const field = (id: string, label: string, key: keyof KaveriSettings, placeholder: string) => (
    <div className="space-y-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} value={String(form[key] ?? '')} onChange={(event) => setForm({ ...form, [key]: event.target.value })} placeholder={placeholder} />
    </div>
  )

  return (
    <div className="space-y-3 rounded-xl border p-3">
      <div>
        <p className="text-sm font-medium text-[color:var(--gov-navy)]">Kaveri settings (the same for every Kaveri service)</p>
        <p className="text-xs text-muted-foreground">One technical login is used for the whole platform. {provider.data?.isReady ? 'These settings are complete.' : 'Fill all of these in before any Kaveri service can be switched on.'}</p>
      </div>
      <QueryState isLoading={provider.isLoading} isError={provider.isError}>
        <div className="grid gap-3 sm:grid-cols-2">
          {field('kv-login', 'Sign-in address', 'loginUrl', 'https://…/api/ThirdPartyLoginSevaOne')}
          {field('kv-refresh', 'Refresh address', 'refreshUrl', 'https://…/api/ThirdPartyLoginSevaOneRefreshToken')}
          {field('kv-sso', 'Single sign-on address', 'ssoUrl', 'https://…/api/SevaOne_SSO_Request')}
          {field('kv-status', 'Status address', 'statusUrl', 'https://…/api/SevaOne_ApplicationStatus')}
          {field('kv-user', 'Technical username', 'username', '')}
          <div className="space-y-1.5">
            <Label htmlFor="kv-pass">Password</Label>
            <Input id="kv-pass" type="password" autoComplete="new-password" value={password} onChange={(event) => setPassword(event.target.value)} placeholder={provider.data?.hasSecret ? 'Saved. Type to replace it.' : 'Not set'} />
          </div>
        </div>
        <label className="flex items-start gap-2 text-sm">
          <input type="checkbox" className="mt-1" checked={form.assumeEmailVerified} onChange={(event) => setForm({ ...form, assumeEmailVerified: event.target.checked })} />
          <span>Tell Kaveri the citizen's email is verified<span className="block text-xs text-muted-foreground">SevaOne does not record whether an email was verified. Leave this off unless Kaveri says it accepts that.</span></span>
        </label>
        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" size="sm" disabled={busy} onClick={() => void save()}><Save /> Save Kaveri settings</Button>
          <Button type="button" size="sm" variant="outline" disabled={busy || !provider.data?.isReady} onClick={() => void signIn()}>Test sign-in</Button>
          {test && <span className={`text-xs font-medium ${test.ok ? 'text-[color:var(--gov-green)]' : 'text-red-700'}`}>{test.message}</span>}
        </div>
      </QueryState>
    </div>
  )
}

const ACK_PATH = '/api/v1/dept/token-integration/department-update'

/** Logins the department uses to call our acknowledgement API. A password is shown once, when it is made. */
function AcknowledgementPanel({ departmentCode, departmentName }: { departmentCode: string; departmentName: string }) {
  const queryClient = useQueryClient()
  const [busy, setBusy] = useState(false)
  const [fresh, setFresh] = useState<TokenIntegrationNewCredential | null>(null)
  const logins = useQuery({
    queryKey: ['integration-credentials', departmentCode],
    queryFn: () => govAdminApi.integrationCredentials(departmentCode),
  })

  useEffect(() => { setFresh(null) }, [departmentCode])

  async function run(action: () => Promise<TokenIntegrationNewCredential | void>, success: string) {
    setBusy(true)
    try {
      const result = await action()
      if (result) setFresh(result)
      toast.success(success)
      await queryClient.invalidateQueries({ queryKey: ['integration-credentials', departmentCode] })
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Something went wrong.')
    } finally {
      setBusy(false)
    }
  }

  async function copy(text: string) {
    try { await navigator.clipboard.writeText(text); toast.success('Copied.') } catch { toast.error('Could not copy. Select the text and copy it.') }
  }

  const rows = logins.data ?? []

  return (
    <div className="mt-6 space-y-3 border-t pt-5">
      <div>
        <h3 className="flex items-center gap-2 text-sm font-semibold text-[color:var(--gov-navy)]"><KeyRound className="h-4 w-4" /> Acknowledgement API for {departmentName}</h3>
        <p className="mt-1 text-xs text-muted-foreground">After the department has the application, it posts the result to our address below with one of these logins (Basic authentication). It can only acknowledge its own applications.</p>
      </div>
      <div className="rounded-lg border bg-muted/40 px-3 py-2 font-mono text-xs">POST {ACK_PATH}</div>

      {fresh && (
        <div className="space-y-2 rounded-xl border border-[color:var(--saffron)]/50 bg-[color:var(--saffron)]/10 p-3 text-sm">
          <p className="font-medium">Give these to the department now. The password cannot be shown again.</p>
          <div className="flex items-center justify-between gap-2 font-mono text-xs"><span>Username: {fresh.username}</span><Button type="button" size="sm" variant="outline" onClick={() => void copy(fresh.username)}><Copy /></Button></div>
          <div className="flex items-center justify-between gap-2 font-mono text-xs"><span className="break-all">Password: {fresh.password}</span><Button type="button" size="sm" variant="outline" onClick={() => void copy(fresh.password)}><Copy /></Button></div>
          <Button type="button" size="sm" variant="outline" onClick={() => setFresh(null)}>I have copied it</Button>
        </div>
      )}

      <QueryState isLoading={logins.isLoading} isError={logins.isError}>
        {rows.length === 0 ? (
          <p className="text-xs text-muted-foreground">No login yet. The department cannot call this API until you create one.</p>
        ) : (
          <ul className="divide-y rounded-xl border text-sm">
            {rows.map((row) => (
              <li key={row.id} className="flex flex-wrap items-center gap-2 px-3 py-2">
                <div className="min-w-0 flex-1">
                  <p className="break-all font-mono text-xs">{row.username}</p>
                  <p className="text-xs text-muted-foreground">
                    {row.isActive ? 'Active' : 'Disabled'} · {row.lastUsedAt ? `last used ${when(row.lastUsedAt)}` : 'never used'}
                  </p>
                </div>
                <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void run(() => govAdminApi.resetIntegrationCredential(departmentCode, row.id), 'New password made.')}>New password</Button>
                <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void run(() => govAdminApi.setIntegrationCredentialActive(departmentCode, row.id, !row.isActive), row.isActive ? 'Login disabled.' : 'Login enabled.')}>{row.isActive ? 'Disable' : 'Enable'}</Button>
              </li>
            ))}
          </ul>
        )}
      </QueryState>
      <Button type="button" size="sm" disabled={busy} onClick={() => void run(() => govAdminApi.createIntegrationCredential(departmentCode), 'Login created.')}><KeyRound /> Create login</Button>
    </div>
  )
}

export function IntegrationsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [debounced, setDebounced] = useState('')
  const [configuredOnly, setConfiguredOnly] = useState(false)
  const [citizenVisibleOnly, setCitizenVisibleOnly] = useState(true)
  const [selected, setSelected] = useState<{ departmentCode: string; serviceCode: string } | null>(null)
  const [draft, setDraft] = useState<Draft | null>(null)
  const [busy, setBusy] = useState(false)
  const [advancedOpen, setAdvancedOpen] = useState(false)

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(search.trim()), 250)
    return () => window.clearTimeout(timer)
  }, [search])

  const types = useQuery({ queryKey: ['integration-types'], queryFn: govAdminApi.integrationTypes })
  const list = useQuery({
    queryKey: ['integration-services', debounced, configuredOnly, citizenVisibleOnly],
    queryFn: () => govAdminApi.integrationServices({ search: debounced, configuredOnly, citizenVisibleOnly }),
  })
  const detail = useQuery({
    queryKey: ['integration', selected?.departmentCode, selected?.serviceCode],
    queryFn: () => govAdminApi.integration(selected!.departmentCode, selected!.serviceCode),
    enabled: selected !== null,
  })

  const typeList: TokenIntegrationType[] = types.data ?? []
  const defaultType = typeList[0]?.typeKey ?? 'seva_sindhu_v3'
  const currentType = typeList.find((item) => item.typeKey === draft?.typeKey)

  useEffect(() => {
    if (detail.data) {
      setDraft(toDraft(detail.data, defaultType))
      setAdvancedOpen(Boolean(detail.data.statusUrl || detail.data.allowedIps.length > 0
        || detail.data.edcsKey5))
    }
  }, [detail.data, defaultType])

  const isKaveri = draft?.typeKey === KAVERI
  function changeType(typeKey: string) {
    if (!draft) return
    if (typeKey === KAVERI) {
      setDraft({ ...draft, typeKey, statusAuthType: 'provider', statusPath: 'data.Status', statusRequestField: 'ApplicationNumber', statusNumberSource: 'edcs' })
    } else {
      setDraft({
        ...draft, typeKey,
        statusAuthType: draft.statusAuthType === 'provider' ? 'none' : draft.statusAuthType,
        statusPath: draft.statusPath === 'data.Status' ? 'status' : draft.statusPath,
        statusRequestField: draft.statusRequestField === 'ApplicationNumber' ? 'GscNumber' : draft.statusRequestField,
        statusNumberSource: 'department',
      })
    }
  }

  const dirty = useMemo(() => {
    if (!detail.data || !draft) return false
    return JSON.stringify(toDraft(detail.data, defaultType)) !== JSON.stringify(draft)
  }, [detail.data, draft, defaultType])

  function refresh() {
    void queryClient.invalidateQueries({ queryKey: ['integration-services'] })
    void queryClient.invalidateQueries({ queryKey: ['integration'] })
  }

  async function save() {
    if (!selected || !draft) return
    setBusy(true)
    try {
      await govAdminApi.saveIntegration(selected.departmentCode, selected.serviceCode, {
        typeKey: draft.typeKey,
        landingUrl: draft.typeKey === KAVERI ? null : draft.landingUrl.trim() || null,
        sendMobile: draft.sendMobile,
        sendDigilockerId: draft.sendDigilockerId,
        statusUrl: draft.typeKey === KAVERI ? null : draft.statusUrl.trim() || null,
        ...(draft.typeKey === KAVERI && draft.kaveriServiceId.trim() ? { kaveriServiceId: Number(draft.kaveriServiceId.trim()) } : {}),
        ...(draft.typeKey === EDCS_API || draft.typeKey === EDCS_API_FORM ? {
          sevasindhuServiceId: draft.sevasindhuServiceId.trim() || null,
          edcsKey1: draft.edcsKey1.trim() || null, edcsKey2: draft.edcsKey2.trim() || null, edcsKey3: draft.edcsKey3.trim() || null,
          edcsKey4: draft.edcsKey4.trim() || null, edcsKey5: draft.edcsKey5.trim() || null,
        } : {}),
        allowedIps: draft.allowedIps.split(/[\n,]+/).map((ip) => ip.trim()).filter(Boolean),
        isEnabled: draft.isEnabled,
        statusConfig: {
          authType: draft.statusAuthType,
          authUser: draft.statusAuthType === 'basic' ? draft.statusAuthUser.trim() || null : null,
          authHeaderName: draft.statusAuthType === 'header' ? draft.statusAuthHeader.trim() || null : null,
          statusPath: draft.statusPath.trim() || 'status',
          messagePath: draft.messagePath.trim() || null,
          statusMap: textToMap(draft.statusMapText),
          requestField: draft.statusRequestField.trim() || 'GscNumber',
          numberSource: draft.statusNumberSource,
        },
        ...(draft.clearStatusSecret || draft.statusAuthType === 'none' ? (detail.data?.hasStatusSecret ? { statusSecret: '' } : {}) : draft.statusSecret ? { statusSecret: draft.statusSecret } : {}),
      })
      toast.success('Integration saved.')
      refresh()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Could not save the integration.')
    } finally {
      setBusy(false)
    }
  }

  async function remove() {
    if (!selected) return
    if (!window.confirm('Remove the token integration for this service? Citizens will get the normal apply link again.')) return
    setBusy(true)
    try {
      await govAdminApi.removeIntegration(selected.departmentCode, selected.serviceCode)
      toast.success('Integration removed.')
      refresh()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Could not remove the integration.')
    } finally {
      setBusy(false)
    }
  }

  const rows: TokenIntegrationListItem[] = list.data ?? []

  return (
    <>
      <PageIntro
        eyebrow="Integrations"
        title="Token integrations"
        description="Search a service and set up where its users are sent. This is separate from the operator-only Sakala setup, which is shown read-only and is not changed here."
      />

      <div className="grid gap-5 lg:grid-cols-[minmax(0,5fr)_minmax(0,7fr)]">
        <Card className="rounded-2xl p-4">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search by service, code or department"
              className="pl-9"
              aria-label="Search services"
            />
          </div>
          <div className="mt-3 flex flex-wrap gap-x-5 gap-y-2 text-sm">
            <label className="flex items-center gap-2"><input type="checkbox" checked={citizenVisibleOnly} onChange={(event) => setCitizenVisibleOnly(event.target.checked)} /> Live services only</label>
            <label className="flex items-center gap-2"><input type="checkbox" checked={configuredOnly} onChange={(event) => setConfiguredOnly(event.target.checked)} /> Already set up</label>
          </div>

          <div className="mt-4">
            <QueryState isLoading={list.isLoading} isError={list.isError}>
              {rows.length === 0 ? (
                <p className="py-10 text-center text-sm text-muted-foreground">No services match.</p>
              ) : (
                <ul className="max-h-[34rem] divide-y overflow-y-auto rounded-xl border">
                  {rows.map((row) => {
                    const active = selected && serviceKey(selected) === serviceKey(row)
                    return (
                      <li key={row.serviceId}>
                        <button
                          type="button"
                          onClick={() => setSelected({ departmentCode: row.departmentCode, serviceCode: row.serviceCode })}
                          className={`flex w-full flex-col gap-1 px-3 py-2.5 text-left transition-colors hover:bg-muted/60 ${active ? 'bg-[color:var(--gov-blue)]/8' : ''}`}
                        >
                          <span className="text-sm font-medium text-[color:var(--gov-navy)]">{row.name}</span>
                          <span className="text-xs text-muted-foreground">{row.departmentName} · {row.serviceCode}</span>
                          <span className="flex flex-wrap gap-1.5">
                            {row.isConfigured
                              ? <Badge variant={row.isEnabled ? 'default' : 'secondary'}>{row.isEnabled ? 'Token on' : 'Token set up, off'}</Badge>
                              : <Badge variant="outline">Not set up</Badge>}
                            {row.operatorMethod && <Badge variant="outline">Operator: {operatorLabels[row.operatorMethod] ?? row.operatorMethod}</Badge>}
                            {!row.citizenVisible && <Badge variant="outline">Hidden from citizens</Badge>}
                          </span>
                        </button>
                      </li>
                    )
                  })}
                </ul>
              )}
            </QueryState>
            {rows.length >= 50 && <p className="mt-2 text-xs text-muted-foreground">Showing the first 50. Type more to narrow the list.</p>}
          </div>
        </Card>

        <Card className="rounded-2xl p-5">
          {!selected ? (
            <div className="flex min-h-72 flex-col items-center justify-center text-center text-sm text-muted-foreground">
              <Plug className="mb-3 h-8 w-8 text-[color:var(--gov-blue)]" />
              Pick a service on the left to set up its token integration.
            </div>
          ) : (
            <QueryState isLoading={detail.isLoading || !draft} isError={detail.isError}>
              {detail.data && draft && (
                <form
                  className="space-y-5"
                  onSubmit={(event) => { event.preventDefault(); void save() }}
                >
                  <div>
                    <h2 className="text-lg font-semibold text-[color:var(--gov-navy)]">{detail.data.name}</h2>
                    <p className="text-xs text-muted-foreground">{detail.data.departmentName} · {detail.data.serviceCode}</p>
                    {detail.data.updatedAt && <p className="mt-1 text-xs text-muted-foreground">Last changed {when(detail.data.updatedAt)} by {detail.data.updatedBy}</p>}
                  </div>

                  {detail.data.operatorMethod && (
                    <div className="flex items-start gap-2 rounded-xl border bg-muted/40 p-3 text-sm">
                      <Lock className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
                      <div>
                        <p className="font-medium text-[color:var(--gov-navy)]">Operator setup (Sakala): {operatorLabels[detail.data.operatorMethod] ?? detail.data.operatorMethod}</p>
                        <p className="text-xs text-muted-foreground">Used only when an operator applies. It is not changed here and does not affect the setup below{detail.data.operatorSevasindhuId ? ` (Sakala id ${detail.data.operatorSevasindhuId})` : ''}.</p>
                      </div>
                    </div>
                  )}

                  <div className="space-y-1.5">
                    <Label htmlFor="int-type">Integration type</Label>
                    <select
                      id="int-type"
                      value={draft.typeKey}
                      onChange={(event) => changeType(event.target.value)}
                      className="h-9 w-full rounded-md border bg-background px-3 text-sm"
                    >
                      {typeList.filter((item) => !HIDDEN_TYPES.includes(item.typeKey) || item.typeKey === draft.typeKey).map((item) => <option key={item.typeKey} value={item.typeKey}>{item.displayName}</option>)}
                    </select>
                    {currentType?.description && <p className="text-xs text-muted-foreground">{currentType.description}</p>}
                  </div>

                  {isKaveri ? (
                    <>
                      <div className="space-y-1.5">
                        <Label htmlFor="kv-service">Kaveri service id</Label>
                        <Input id="kv-service" inputMode="numeric" value={draft.kaveriServiceId} onChange={(event) => setDraft({ ...draft, kaveriServiceId: event.target.value.replace(/[^0-9]/g, '') })} placeholder="The id of this service in Kaveri" />
                        <p className="text-xs text-muted-foreground">Sent to Kaveri as EdcsserviceId with the citizen's details and our application number. Kaveri gives back the single sign-on address.</p>
                      </div>
                      <KaveriProviderPanel typeKey={KAVERI} />
                    </>
                  ) : (
                    <>
                  {(draft.typeKey === EDCS_API || draft.typeKey === EDCS_API_FORM) && (
                    <div className="space-y-1.5">
                      <Label htmlFor="int-sevasindhu-id">Seva Sindhu service id</Label>
                      <Input id="int-sevasindhu-id" inputMode="numeric" value={draft.sevasindhuServiceId} onChange={(event) => setDraft({ ...draft, sevasindhuServiceId: event.target.value.replace(/[^0-9]/g, '') })} placeholder="e.g. 2952" />
                      <p className="text-xs text-muted-foreground">The id Seva Sindhu uses for this service, sent as ServiceId to its generate API.</p>
                    </div>
                  )}
                  {draft.typeKey === EDCS_API ? (
                    <p className="rounded-lg border bg-muted/40 px-3 py-2 text-xs text-muted-foreground">That's all this type needs {'—'} Seva Sindhu's own redirect sends the citizen on to the right department page itself, so no department URL is entered here.</p>
                  ) : (
                  <div className="space-y-1.5">
                    <Label htmlFor="int-landing">Department URL (URL 1)</Label>
                    <Input id="int-landing" value={draft.landingUrl} onChange={(event) => setDraft({ ...draft, landingUrl: event.target.value })} placeholder="https://department.gov.in/apply" />
                    <p className="text-xs text-muted-foreground">
                      {draft.typeKey === EDCS_API_FORM
                        ? "The department's own entry page. We post UserId, edcsApplicationRefNo, Token, assistedmode and ServiceId to it ourselves — one less hop than the redirect type, but keep this URL correct."
                        : 'The page that receives the user. We send a form POST with UserId, edcsApplicationRefNo, Token, assistedmode and ServiceId. It must answer HTTP 200.'}
                    </p>
                  </div>
                  )}

                  {!['edcs_api', 'edcs_api_form'].includes(draft.typeKey) && (
                  <fieldset className="space-y-2">
                    <legend className="text-sm font-medium">Extra details this department asks for</legend>
                    <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={draft.sendMobile} onChange={(event) => setDraft({ ...draft, sendMobile: event.target.checked })} /> Mobile number</label>
                    <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={draft.sendDigilockerId} onChange={(event) => setDraft({ ...draft, sendDigilockerId: event.target.checked })} /> DigiLocker id</label>
                  </fieldset>
                  )}
                  {(draft.typeKey === EDCS_API || draft.typeKey === EDCS_API_FORM) && (
                    <fieldset className="space-y-3 rounded-xl border p-3">
                      <legend className="px-1 text-sm font-medium text-[color:var(--gov-navy)]">Values sent to the department (Key1 to Key4)</legend>
                      <p className="text-xs text-muted-foreground">Mobile, email, DigiLocker id and the user id are always sent when we have them. Use the keys for anything extra this department needs. Choose where each value comes from.</p>
                      {(['edcsKey1', 'edcsKey2', 'edcsKey3', 'edcsKey4'] as const).map((key, i) => (
                        <KeyRow key={key} label={`Key${i + 1}`} value={draft[key]} onChange={(value) => setDraft({ ...draft, [key]: value })} />
                      ))}
                      <p className="text-xs text-muted-foreground">"Ask the citizen" shows the citizen your question when they press Apply and sends their answer. Key5, if a department ever needs it, is under Advanced.</p>
                    </fieldset>
                  )}
                    </>
                  )}

                  <div className="rounded-xl border">
                    <button
                      type="button"
                      onClick={() => setAdvancedOpen(!advancedOpen)}
                      className="flex w-full items-center justify-between gap-2 px-3 py-2.5 text-left text-sm font-medium text-[color:var(--gov-navy)]"
                    >
                      <span>Advanced (optional): {draft.typeKey === EDCS_API || draft.typeKey === EDCS_API_FORM ? 'Key5, status check' : 'status check, IP allow-list'}</span>
                      <ChevronDown className={`h-4 w-4 shrink-0 text-muted-foreground transition-transform ${advancedOpen ? 'rotate-180' : ''}`} />
                    </button>
                    {advancedOpen && (
                      <div className="space-y-4 border-t p-3">
                        {(draft.typeKey === EDCS_API || draft.typeKey === EDCS_API_FORM) && (
                          <fieldset className="space-y-2">
                            <legend className="text-sm font-medium">Key5 (rarely needed)</legend>
                            <KeyRow label="Key5" value={draft.edcsKey5} onChange={(value) => setDraft({ ...draft, edcsKey5: value })} />
                          </fieldset>
                        )}
                        {!isKaveri && (
                          <div className="space-y-1.5">
                            <Label htmlFor="int-status">Status check API (URL 3)</Label>
                            <Input id="int-status" value={draft.statusUrl} onChange={(event) => setDraft({ ...draft, statusUrl: event.target.value })} placeholder="https://department.gov.in/api/status" />
                            <p className="text-xs text-muted-foreground">Optional. The department API we ask for an application's status. It receives {'{"GscNumber": "..."}'}.</p>
                          </div>
                        )}

                        <div className="space-y-3 rounded-xl border p-3">
                          <p className="text-sm font-medium text-[color:var(--gov-navy)]">How to call the status API and read its answer</p>
                          <p className="text-xs text-muted-foreground">The contract does not say what the status API answers, so tell us where the status is. We send {'{"GscNumber": "<the department application number>"}'}.</p>
                          <div className="grid gap-3 sm:grid-cols-2">
                            <div className="space-y-1.5">
                              <Label htmlFor="st-auth">Authentication</Label>
                              <select id="st-auth" value={draft.statusAuthType} disabled={isKaveri} onChange={(event) => setDraft({ ...draft, statusAuthType: event.target.value })} className="h-9 w-full rounded-md border bg-background px-3 text-sm">
                                {isKaveri && <option value="provider">The Kaveri sign-in (shared settings)</option>}
                                <option value="none">None</option>
                                <option value="basic">Basic (username and password)</option>
                                <option value="bearer">Bearer token</option>
                                <option value="header">A header, such as X-Api-Key</option>
                              </select>
                            </div>
                            {draft.statusAuthType === 'basic' && (
                              <div className="space-y-1.5"><Label htmlFor="st-user">Username</Label><Input id="st-user" value={draft.statusAuthUser} onChange={(event) => setDraft({ ...draft, statusAuthUser: event.target.value })} /></div>
                            )}
                            {draft.statusAuthType === 'header' && (
                              <div className="space-y-1.5"><Label htmlFor="st-header">Header name</Label><Input id="st-header" value={draft.statusAuthHeader} onChange={(event) => setDraft({ ...draft, statusAuthHeader: event.target.value })} placeholder="X-Api-Key" /></div>
                            )}
                          </div>
                          <div className="grid gap-3 sm:grid-cols-2">
                            <div className="space-y-1.5"><Label htmlFor="st-field">Name of the field we send</Label><Input id="st-field" value={draft.statusRequestField} onChange={(event) => setDraft({ ...draft, statusRequestField: event.target.value })} placeholder="GscNumber  or  ApplicationNumber" /></div>
                            <div className="space-y-1.5">
                              <Label htmlFor="st-source">Which number we send</Label>
                              <select id="st-source" value={draft.statusNumberSource} onChange={(event) => setDraft({ ...draft, statusNumberSource: event.target.value })} className="h-9 w-full rounded-md border bg-background px-3 text-sm">
                                <option value="department">The department's own application number</option>
                                <option value="edcs">Our application number</option>
                              </select>
                            </div>
                          </div>
                          {draft.statusAuthType !== 'none' && draft.statusAuthType !== 'provider' && (
                            <div className="space-y-1.5">
                              <Label htmlFor="st-secret">{draft.statusAuthType === 'basic' ? 'Password' : 'Secret'}</Label>
                              <Input id="st-secret" type="password" autoComplete="new-password" value={draft.statusSecret} onChange={(event) => setDraft({ ...draft, statusSecret: event.target.value, clearStatusSecret: false })} placeholder={detail.data.hasStatusSecret ? 'Saved. Type to replace it.' : 'Not set'} />
                              <p className="text-xs text-muted-foreground">Stored encrypted and never shown again.</p>
                            </div>
                          )}
                          <div className="grid gap-3 sm:grid-cols-2">
                            <div className="space-y-1.5"><Label htmlFor="st-path">Where the status is in the reply</Label><Input id="st-path" value={draft.statusPath} onChange={(event) => setDraft({ ...draft, statusPath: event.target.value })} placeholder="status  or  data.state  or  result[0].status" /></div>
                            <div className="space-y-1.5"><Label htmlFor="st-msg">Where a message is (optional)</Label><Input id="st-msg" value={draft.messagePath} onChange={(event) => setDraft({ ...draft, messagePath: event.target.value })} placeholder="message" /></div>
                          </div>
                          <div className="space-y-1.5">
                            <Label htmlFor="st-map">The department's words, and our status for each</Label>
                            <textarea id="st-map" rows={4} value={draft.statusMapText} onChange={(event) => setDraft({ ...draft, statusMapText: event.target.value })} placeholder={'Pending = UnderReview\nApproved = Approved\nRejected = Rejected'} className="w-full rounded-md border bg-background px-3 py-2 font-mono text-sm" />
                            <p className="text-xs text-muted-foreground">Our statuses: {OUR_STATUSES.join(', ')}. A word that is not listed is shown on the timeline but does not change the application.</p>
                          </div>
                          <StatusTest departmentCode={detail.data.departmentCode} serviceCode={detail.data.serviceCode} saved={!dirty && (!!detail.data.statusUrl || (isKaveri && detail.data.providerReady))} />
                        </div>

                        {!isKaveri && (
                          <div className="space-y-1.5">
                            <Label htmlFor="int-ips">Department server IPs</Label>
                            <textarea
                              id="int-ips"
                              value={draft.allowedIps}
                              onChange={(event) => setDraft({ ...draft, allowedIps: event.target.value })}
                              rows={3}
                              placeholder={'One per line, for example\n103.21.4.10\n10.20.0.0/24'}
                              className="w-full rounded-md border bg-background px-3 py-2 font-mono text-sm"
                            />
                            <p className="text-xs text-muted-foreground">The addresses the department calls us from. Used to restrict our acknowledgement API to this department.</p>
                          </div>
                        )}
                      </div>
                    )}
                  </div>

                  <label className="flex items-start gap-2 rounded-xl border p-3 text-sm">
                    <input type="checkbox" className="mt-1" checked={draft.isEnabled} onChange={(event) => setDraft({ ...draft, isEnabled: event.target.checked })} />
                    <span>
                      <span className="font-medium text-[color:var(--gov-navy)]">Switch this integration on</span>
                      <span className="block text-xs text-muted-foreground">When off, citizens keep getting the normal apply link. Needs a department URL.</span>
                    </span>
                  </label>

                  {detail.data.warnings.length > 0 && (
                    <ul className="space-y-1.5 rounded-xl border border-[color:var(--saffron)]/40 bg-[color:var(--saffron)]/10 p-3 text-xs">
                      {detail.data.warnings.map((warning) => (
                        <li key={warning} className="flex items-start gap-2"><AlertTriangle className="mt-0.5 h-3.5 w-3.5 shrink-0 text-[color:var(--saffron)]" />{warning}</li>
                      ))}
                    </ul>
                  )}

                  <div className="flex flex-wrap items-center gap-3 border-t pt-4">
                    <Button type="submit" disabled={busy || !dirty}><Save /> Save</Button>
                    {detail.data.isConfigured && (
                      <Button type="button" variant="outline" disabled={busy} onClick={() => void remove()}><Trash2 /> Remove setup</Button>
                    )}
                    <span className="ml-auto flex items-center gap-1.5 text-xs text-muted-foreground"><Link2 className="h-3.5 w-3.5" /> Every change is recorded in the service change log.</span>
                  </div>
                </form>
              )}
              {detail.data && !isKaveri && <AcknowledgementPanel departmentCode={detail.data.departmentCode} departmentName={detail.data.departmentName} />}
            </QueryState>
          )}
        </Card>
      </div>
    </>
  )
}
