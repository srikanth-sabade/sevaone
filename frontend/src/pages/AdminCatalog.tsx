import { useQueryClient } from '@tanstack/react-query'
import {
  CheckCircle2,
  Eye,
  FileWarning,
  History,
  LayoutGrid,
  Loader2,
  LogOut,
  ShieldCheck,
  UploadCloud,
  XCircle,
} from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { api, type AdminChangeLogEntry, type AdminImportRow } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Card } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { PasswordInput } from '../components/ui/password-input'
import { useAdminChangeLog, useAdminImportBatch, useAdminImportRows, useAdminMe, useAdminRoles, useAdminServiceVisibility } from '../hooks/useAdmin'
import { useCatalogDepartment, useCatalogDepartments } from '../hooks/useCatalog'
import { useT } from '../hooks/useT'

type Picked = { departmentCode: string; serviceCode: string; serviceName: string } | null

export function AdminCatalogPage() {
  const { t } = useT()
  const qc = useQueryClient()
  const meQ = useAdminMe()
  const admin = meQ.data ?? null

  if (meQ.isLoading) {
    return <div className="mx-auto max-w-xl px-4 py-20 text-center text-sm text-muted-foreground">{t('adminCheckingSession')}</div>
  }

  if (!admin) {
    return <AdminLoginView />
  }

  return <AdminConsole adminName={admin.adminName} onSignOut={() => void qc.invalidateQueries({ queryKey: ['admin', 'me'] })} />
}

function AdminLoginView() {
  const { t } = useT()
  const qc = useQueryClient()
  const [adminCode, setAdminCode] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = async () => {
    if (!adminCode.trim() || !password) {
      toast.error(t('adminMissingCreds'))
      return
    }
    setBusy(true)
    try {
      await api.adminLogin({ adminCode: adminCode.trim(), password })
      await qc.invalidateQueries({ queryKey: ['admin', 'me'] })
      toast.success(t('adminSignedIn'))
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('adminSignInFailed'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="mx-auto max-w-md px-4 py-20 text-center">
      <ShieldCheck className="mx-auto h-10 w-10 text-[color:var(--gov-blue)]" />
      <h1 className="mt-4 text-2xl font-bold text-[color:var(--gov-navy)]">{t('adminConsoleTitle')}</h1>
      <div className="flex justify-center">
        <Breadcrumbs />
      </div>
      <p className="mt-3 text-sm text-muted-foreground">{t('adminSignInIntro')}</p>

      <form
        className="mt-6 space-y-3 text-left"
        onSubmit={(e) => {
          e.preventDefault()
          void submit()
        }}
      >
        <div>
          <Label htmlFor="admin-code">{t('adminCode')}</Label>
          <Input id="admin-code" className="mt-1.5" autoComplete="username" value={adminCode} onChange={(e) => setAdminCode(e.target.value)} />
        </div>
        <div>
          <Label htmlFor="admin-password">{t('loginPassword')}</Label>
          <PasswordInput id="admin-password" containerClassName="mt-1.5" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
        </div>
        <Button type="submit" disabled={busy} className="w-full rounded-lg bg-[color:var(--gov-blue)] text-white hover:bg-[color:var(--gov-blue)]/90">
          {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : t('adminSignIn')}
        </Button>
      </form>
    </div>
  )
}

type TabKey = 'import' | 'browse' | 'visibility' | 'audit'

function AdminConsole({ adminName, onSignOut }: { readonly adminName: string; readonly onSignOut: () => void }) {
  const { t } = useT()
  const [tab, setTab] = useState<TabKey>('import')
  const [picked, setPicked] = useState<Picked>(null)
  const [busy, setBusy] = useState(false)

  const signOut = async () => {
    setBusy(true)
    try {
      await api.adminLogout()
    } catch {
      /* ignore -- the session cookie is cleared server-side regardless */
    } finally {
      setBusy(false)
      onSignOut()
    }
  }

  const tabs: { key: TabKey; label: string; icon: typeof UploadCloud }[] = [
    { key: 'import', label: t('adminTabImport'), icon: UploadCloud },
    { key: 'browse', label: t('adminTabBrowse'), icon: LayoutGrid },
    { key: 'visibility', label: t('adminTabVisibility'), icon: Eye },
    { key: 'audit', label: t('adminTabAuditLog'), icon: History },
  ]

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <div className="flex flex-wrap items-center gap-3 rounded-2xl border border-border bg-card p-5 shadow-sm">
        <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-[color:var(--gov-blue)]/10">
          <ShieldCheck className="h-5 w-5 text-[color:var(--gov-blue)]" />
        </div>
        <div>
          <h1 className="text-lg font-bold text-[color:var(--gov-navy)]">{t('adminConsoleTitle')}</h1>
          <Breadcrumbs />
          <p className="mt-1 text-xs text-muted-foreground">{t('adminSignedInAs', { name: adminName })}</p>
        </div>
        <div className="ml-auto">
          <Button size="sm" variant="outline" className="rounded-lg" disabled={busy} onClick={() => void signOut()}>
            <LogOut className="mr-1.5 h-4 w-4" /> {t('adminSignOut')}
          </Button>
        </div>
      </div>

      <div className="mt-4 flex flex-wrap gap-2">
        {tabs.map((tb) => (
          <button
            key={tb.key}
            type="button"
            onClick={() => setTab(tb.key)}
            className={`inline-flex items-center gap-1.5 rounded-full px-3.5 py-1.5 text-xs font-semibold transition ${
              tab === tb.key ? 'bg-[color:var(--gov-blue)] text-white' : 'bg-secondary text-muted-foreground hover:text-foreground'
            }`}
          >
            <tb.icon className="h-3.5 w-3.5" /> {tb.label}
          </button>
        ))}
      </div>

      <div className="mt-6">
        {tab === 'import' && <ImportTab />}
        {tab === 'browse' && (
          <BrowseTab
            onManageVisibility={(p) => {
              setPicked(p)
              setTab('visibility')
            }}
          />
        )}
        {tab === 'visibility' && <VisibilityTab picked={picked} onPickedChange={setPicked} />}
        {tab === 'audit' && <AuditTab picked={picked} />}
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Import
// ---------------------------------------------------------------------------

function ImportTab() {
  const { t } = useT()
  const [batchId, setBatchId] = useState<string | undefined>(undefined)
  const [rowFilter, setRowFilter] = useState<string | undefined>(undefined)
  const [uploading, setUploading] = useState(false)
  const [publishing, setPublishing] = useState(false)

  const batchQ = useAdminImportBatch(batchId)
  const rowsQ = useAdminImportRows(batchId, rowFilter)

  const onFile = async (file: File) => {
    if (!file.name.toLowerCase().endsWith('.json')) {
      toast.error(t('adminImportJsonOnly'))
      return
    }
    setUploading(true)
    try {
      const res = await api.adminImportUpload(file)
      setBatchId(res.batchId)
      toast.success(t('adminImportStaged'))
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('adminImportFailed'))
    } finally {
      setUploading(false)
    }
  }

  const publish = async () => {
    if (!batchId) return
    setPublishing(true)
    try {
      const res = await api.adminImportPublish(batchId)
      toast.success(t('adminImportPublished', { published: res.published, skipped: res.skipped }))
      await batchQ.refetch()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('adminImportFailed'))
    } finally {
      setPublishing(false)
    }
  }

  const discard = async () => {
    if (!batchId) return
    setPublishing(true)
    try {
      await api.adminImportDiscard(batchId)
      toast.success(t('adminImportDiscarded'))
      await batchQ.refetch()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('adminImportFailed'))
    } finally {
      setPublishing(false)
    }
  }

  const batch = batchQ.data
  const rows = rowsQ.data ?? []

  return (
    <div className="space-y-6">
      <Card className="rounded-2xl p-5">
        <h2 className="flex items-center gap-2 text-sm font-semibold text-[color:var(--gov-navy)]">
          <UploadCloud className="h-4 w-4 text-[color:var(--gov-blue)]" /> {t('adminTabImport')}
        </h2>
        <p className="mt-2 text-xs text-muted-foreground">{t('adminImportHint')}</p>
        <label className="mt-4 flex cursor-pointer flex-col items-center justify-center gap-2 rounded-2xl border-2 border-dashed border-border p-8 text-center transition hover:border-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/5">
          <UploadCloud className="h-6 w-6 text-muted-foreground" />
          <span className="text-sm font-medium text-[color:var(--gov-navy)]">{uploading ? t('adminImportUploading') : t('adminImportChooseFile')}</span>
          <input
            type="file"
            accept=".json"
            className="hidden"
            disabled={uploading}
            onChange={(e) => {
              const file = e.target.files?.[0]
              e.target.value = ''
              if (file) void onFile(file)
            }}
          />
        </label>
      </Card>

      {batch && (
        <Card className="rounded-2xl p-5">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="text-sm font-semibold text-[color:var(--gov-navy)]">{t('adminImportBatchStatus')}</h2>
              <p className="mt-0.5 text-xs text-muted-foreground">
                {batch.sourceFilename} · {batch.uploadedBy}
              </p>
            </div>
            <BatchStatusBadge status={batch.status} />
          </div>

          <div className="mt-4 grid grid-cols-3 gap-2 text-center">
            {[
              { k: t('adminImportRowCount', { count: batch.rowCount }), v: batch.rowCount },
              { k: t('adminImportErrorCount', { count: batch.errorCount }), v: batch.errorCount },
              { k: t('adminImportWarningCount', { count: batch.warningCount }), v: batch.warningCount },
            ].map((s) => (
              <div key={s.k} className="rounded-2xl border border-border p-3">
                <div className="text-lg font-bold text-[color:var(--gov-navy)]">{s.v}</div>
                <div className="text-[11px] text-muted-foreground">{s.k}</div>
              </div>
            ))}
          </div>

          {batch.status === 'ready_to_publish' && (
            <div className="mt-4 flex gap-2">
              <Button disabled={publishing} onClick={() => void publish()} className="bg-[color:var(--gov-blue)] text-white hover:bg-[color:var(--gov-blue)]/90">
                {t('adminImportPublish')}
              </Button>
              <Button disabled={publishing} variant="outline" onClick={() => void discard()}>
                {t('adminImportDiscard')}
              </Button>
            </div>
          )}

          <div className="mt-5">
            <div className="flex items-center justify-between">
              <h3 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{t('adminImportRowsTitle')}</h3>
              <div className="flex gap-1">
                {(['all', 'insert', 'update', 'conflict'] as const).map((f) => (
                  <button
                    key={f}
                    type="button"
                    onClick={() => setRowFilter(f === 'all' ? undefined : f)}
                    className={`rounded-full px-2.5 py-1 text-[11px] font-medium ${
                      (rowFilter ?? 'all') === f ? 'bg-[color:var(--gov-blue)] text-white' : 'bg-secondary text-muted-foreground'
                    }`}
                  >
                    {f === 'all' ? t('adminImportFilterAll') : f}
                  </button>
                ))}
              </div>
            </div>
            <ul className="mt-2 space-y-1.5">
              {rows.length === 0 && <li className="rounded-xl border border-dashed border-border p-4 text-center text-xs text-muted-foreground">{t('adminImportNoRows')}</li>}
              {rows.map((row) => (
                <ImportRowItem key={row.id} row={row} />
              ))}
            </ul>
          </div>
        </Card>
      )}
    </div>
  )
}

function batchStatusVariant(status: string): 'default' | 'destructive' | 'secondary' {
  if (status === 'published') return 'default'
  if (status === 'failed') return 'destructive'
  return 'secondary'
}

function BatchStatusBadge({ status }: { readonly status: string }) {
  const variant = batchStatusVariant(status)
  return (
    <Badge variant={variant === 'destructive' ? 'default' : (variant as 'default' | 'secondary')} className={variant === 'destructive' ? 'bg-red-600 text-white' : undefined}>
      {status}
    </Badge>
  )
}

function ImportRowItem({ row }: { readonly row: AdminImportRow }) {
  const hasErrors = row.errors.length > 0
  return (
    <li className="rounded-xl border border-border p-3 text-xs">
      <div className="flex items-center justify-between gap-2">
        <span className="font-semibold text-[color:var(--gov-navy)]">
          #{row.rowNumber} · {row.departmentCode}-{row.serviceCode} — {row.serviceName}
        </span>
        <span className="flex items-center gap-1">
          {hasErrors ? <XCircle className="h-3.5 w-3.5 text-red-600" /> : <CheckCircle2 className="h-3.5 w-3.5 text-emerald-600" />}
          {row.action ?? '—'}
        </span>
      </div>
      {row.errors.length > 0 && (
        <ul className="mt-1.5 space-y-0.5 text-red-700">
          {row.errors.map((e) => (
            <li key={`${e.field}-${e.message}`} className="flex items-center gap-1">
              <FileWarning className="h-3 w-3 shrink-0" /> {e.field}: {e.message}
            </li>
          ))}
        </ul>
      )}
      {row.warnings.length > 0 && (
        <ul className="mt-1.5 space-y-0.5 text-amber-700">
          {row.warnings.map((w) => (
            <li key={`${w.field}-${w.message}`} className="flex items-center gap-1">
              <FileWarning className="h-3 w-3 shrink-0" /> {w.field}: {w.message}
            </li>
          ))}
        </ul>
      )}
    </li>
  )
}

// ---------------------------------------------------------------------------
// Browse
// ---------------------------------------------------------------------------

function BrowseTab({ onManageVisibility }: { readonly onManageVisibility: (p: Picked) => void }) {
  const { t } = useT()
  const [deptCode, setDeptCode] = useState<string | undefined>(undefined)
  const departmentsQ = useCatalogDepartments()
  const departmentQ = useCatalogDepartment(deptCode)

  return (
    <Card className="rounded-2xl p-5">
      <h2 className="flex items-center gap-2 text-sm font-semibold text-[color:var(--gov-navy)]">
        <LayoutGrid className="h-4 w-4 text-[color:var(--gov-blue)]" /> {t('adminTabBrowse')}
      </h2>
      <p className="mt-2 text-xs text-muted-foreground">{t('adminBrowseHint')}</p>

      <div className="mt-4 grid gap-4 sm:grid-cols-[220px_1fr]">
        <ul className="space-y-1">
          {(departmentsQ.data ?? []).map((d) => (
            <li key={d.code}>
              <button
                type="button"
                onClick={() => setDeptCode(d.code)}
                className={`flex w-full items-center justify-between rounded-lg px-2.5 py-1.5 text-left text-xs ${
                  deptCode === d.code ? 'bg-[color:var(--gov-blue)]/10 font-semibold text-[color:var(--gov-blue)]' : 'text-foreground/80 hover:bg-secondary'
                }`}
              >
                <span className="truncate">{d.name}</span>
                <span className="shrink-0 text-muted-foreground">{d.serviceCount}</span>
              </button>
            </li>
          ))}
        </ul>

        <div>
          {!deptCode && <p className="text-xs text-muted-foreground">{t('catalogPickDept')}</p>}
          {departmentQ.data && (
            <ul className="space-y-1.5">
              {departmentQ.data.services.map((s) => (
                <li key={s.compositeCode} className="flex items-center justify-between gap-2 rounded-lg border border-border px-3 py-2 text-xs">
                  <div className="min-w-0">
                    <div className="truncate font-medium text-[color:var(--gov-navy)]">{s.name}</div>
                    <div className="text-[10px] text-muted-foreground">{s.compositeCode}</div>
                  </div>
                  <Button
                    size="sm"
                    variant="outline"
                    className="shrink-0"
                    onClick={() => onManageVisibility({ departmentCode: departmentQ.data.code, serviceCode: s.serviceCode, serviceName: s.name })}
                  >
                    <Eye className="mr-1 h-3.5 w-3.5" /> {t('adminBrowseManageVisibility')}
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </Card>
  )
}

// ---------------------------------------------------------------------------
// Visibility
// ---------------------------------------------------------------------------

function VisibilityTab({ picked, onPickedChange }: { readonly picked: Picked; readonly onPickedChange: (p: Picked) => void }) {
  const { t } = useT()
  const [deptCode, setDeptCode] = useState(picked?.departmentCode ?? '')
  const [serviceCode, setServiceCode] = useState(picked?.serviceCode ?? '')
  const [lookedUp, setLookedUp] = useState<Picked>(picked)
  const [busyRole, setBusyRole] = useState<string | null>(null)

  const rolesQ = useAdminRoles(true)
  const visibilityQ = useAdminServiceVisibility(lookedUp?.departmentCode, lookedUp?.serviceCode)

  const lookup = () => {
    if (!deptCode.trim() || !serviceCode.trim()) return
    const next = { departmentCode: deptCode.trim(), serviceCode: serviceCode.trim(), serviceName: '' }
    setLookedUp(next)
    onPickedChange(next)
  }

  const toggle = async (roleKey: string, grant: boolean) => {
    if (!lookedUp) return
    setBusyRole(roleKey)
    try {
      await api.adminSetServiceVisibility(lookedUp.departmentCode, lookedUp.serviceCode, roleKey, grant)
      toast.success(grant ? t('adminVisibilityGranted', { role: roleKey }) : t('adminVisibilityRevoked', { role: roleKey }))
      await visibilityQ.refetch()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('adminImportFailed'))
    } finally {
      setBusyRole(null)
    }
  }

  const grantedKeys = new Set((visibilityQ.data ?? []).map((v) => v.roleKey))

  return (
    <div className="space-y-6">
      <Card className="rounded-2xl p-5">
        <h2 className="flex items-center gap-2 text-sm font-semibold text-[color:var(--gov-navy)]">
          <Eye className="h-4 w-4 text-[color:var(--gov-blue)]" /> {t('adminTabVisibility')}
        </h2>
        <p className="mt-2 text-xs text-muted-foreground">{t('adminVisibilityHint')}</p>

        <div className="mt-4 flex flex-wrap items-end gap-2">
          <div>
            <Label htmlFor="vis-dept">{t('adminVisibilityDeptCode')}</Label>
            <Input id="vis-dept" className="mt-1.5 w-40" value={deptCode} onChange={(e) => setDeptCode(e.target.value)} />
          </div>
          <div>
            <Label htmlFor="vis-svc">{t('adminVisibilityServiceCode')}</Label>
            <Input id="vis-svc" className="mt-1.5 w-40" value={serviceCode} onChange={(e) => setServiceCode(e.target.value)} />
          </div>
          <Button variant="outline" onClick={lookup}>
            {t('adminVisibilityLookup')}
          </Button>
        </div>

        {!lookedUp && <p className="mt-4 text-xs text-muted-foreground">{t('adminVisibilityNoService')}</p>}

        {lookedUp && (
          <div className="mt-5">
            <p className="text-xs font-semibold text-[color:var(--gov-navy)]">
              {lookedUp.departmentCode}-{lookedUp.serviceCode}
              {lookedUp.serviceName ? ` — ${lookedUp.serviceName}` : ''}
            </p>
            <div className="mt-3 flex flex-wrap gap-2">
              {(rolesQ.data ?? []).map((role) => {
                const granted = grantedKeys.has(role.roleKey)
                return (
                  <button
                    key={role.roleKey}
                    type="button"
                    disabled={busyRole === role.roleKey}
                    onClick={() => void toggle(role.roleKey, !granted)}
                    className={`inline-flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs font-medium transition disabled:opacity-60 ${
                      granted
                        ? 'border-emerald-300 bg-emerald-50 text-emerald-800'
                        : 'border-border bg-secondary text-muted-foreground hover:text-foreground'
                    }`}
                  >
                    {granted ? <CheckCircle2 className="h-3.5 w-3.5" /> : <XCircle className="h-3.5 w-3.5" />}
                    {role.displayName}
                  </button>
                )
              })}
            </div>
            {grantedKeys.size === 0 && <p className="mt-2 text-xs text-muted-foreground">{t('adminVisibilityNotGranted')}</p>}
          </div>
        )}
      </Card>

      <BulkVisibilityCard defaultDeptCode={lookedUp?.departmentCode} roles={rolesQ.data ?? []} />
    </div>
  )
}

function BulkVisibilityCard({
  defaultDeptCode,
  roles,
}: {
  readonly defaultDeptCode: string | undefined
  readonly roles: { roleKey: string; displayName: string }[]
}) {
  const { t } = useT()
  const [deptCode, setDeptCode] = useState(defaultDeptCode ?? '')
  const [subDept, setSubDept] = useState('')
  const [selectedRoles, setSelectedRoles] = useState<Set<string>>(new Set())
  const [busy, setBusy] = useState(false)
  const [result, setResult] = useState<{ matchedServices: number; applied: number } | null>(null)

  const toggleRole = (key: string) => {
    setSelectedRoles((prev) => {
      const next = new Set(prev)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })
  }

  const apply = async (grant: boolean) => {
    if (!deptCode.trim() || selectedRoles.size === 0) {
      toast.error(t('adminVisibilityBulkMissing'))
      return
    }
    setBusy(true)
    try {
      const res = await api.adminBulkVisibility({
        departmentCode: deptCode.trim(),
        subDepartmentName: subDept.trim() || undefined,
        roleKeys: [...selectedRoles],
        grant,
      })
      setResult(res)
      toast.success(t('adminVisibilityBulkResult', { applied: res.applied, matched: res.matchedServices }))
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('adminImportFailed'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Card className="rounded-2xl p-5">
      <h2 className="text-sm font-semibold text-[color:var(--gov-navy)]">{t('adminVisibilityBulkTitle')}</h2>
      <p className="mt-2 text-xs text-muted-foreground">{t('adminVisibilityBulkHint')}</p>

      <div className="mt-4 grid gap-2 sm:grid-cols-2">
        <div>
          <Label htmlFor="bulk-dept">{t('adminVisibilityDeptCode')}</Label>
          <Input id="bulk-dept" className="mt-1.5" value={deptCode} onChange={(e) => setDeptCode(e.target.value)} />
        </div>
        <div>
          <Label htmlFor="bulk-sub">{t('adminVisibilityBulkSubDept')}</Label>
          <Input id="bulk-sub" className="mt-1.5" value={subDept} onChange={(e) => setSubDept(e.target.value)} />
        </div>
      </div>

      <div className="mt-3">
        <Label>{t('adminVisibilityBulkRoles')}</Label>
        <div className="mt-1.5 flex flex-wrap gap-2">
          {roles.map((role) => (
            <button
              key={role.roleKey}
              type="button"
              onClick={() => toggleRole(role.roleKey)}
              className={`rounded-full border px-3 py-1.5 text-xs font-medium ${
                selectedRoles.has(role.roleKey) ? 'border-[color:var(--gov-blue)] bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]' : 'border-border text-muted-foreground'
              }`}
            >
              {role.displayName}
            </button>
          ))}
        </div>
      </div>

      <div className="mt-4 flex gap-2">
        <Button disabled={busy} onClick={() => void apply(true)} className="bg-[color:var(--gov-blue)] text-white hover:bg-[color:var(--gov-blue)]/90">
          {t('adminVisibilityGrant')}
        </Button>
        <Button disabled={busy} variant="outline" onClick={() => void apply(false)}>
          {t('adminVisibilityRevoke')}
        </Button>
      </div>

      {result && <p className="mt-3 text-xs text-muted-foreground">{t('adminVisibilityBulkResult', { applied: result.applied, matched: result.matchedServices })}</p>}
    </Card>
  )
}

// ---------------------------------------------------------------------------
// Audit log
// ---------------------------------------------------------------------------

function AuditTab({ picked }: { readonly picked: Picked }) {
  const { t } = useT()
  const [deptCode, setDeptCode] = useState(picked?.departmentCode ?? '')
  const [serviceCode, setServiceCode] = useState(picked?.serviceCode ?? '')
  const [applied, setApplied] = useState<{ dept?: string; svc?: string }>({ dept: picked?.departmentCode, svc: picked?.serviceCode })

  const logQ = useAdminChangeLog(applied.dept || undefined, applied.svc || undefined, true)

  return (
    <Card className="rounded-2xl p-5">
      <h2 className="flex items-center gap-2 text-sm font-semibold text-[color:var(--gov-navy)]">
        <History className="h-4 w-4 text-[color:var(--gov-blue)]" /> {t('adminTabAuditLog')}
      </h2>
      <p className="mt-2 text-xs text-muted-foreground">{t('adminAuditHint')}</p>

      <div className="mt-4 flex flex-wrap items-end gap-2">
        <div>
          <Label htmlFor="audit-dept">{t('adminAuditFilterDept')}</Label>
          <Input id="audit-dept" className="mt-1.5 w-44" value={deptCode} onChange={(e) => setDeptCode(e.target.value)} />
        </div>
        <div>
          <Label htmlFor="audit-svc">{t('adminAuditFilterService')}</Label>
          <Input id="audit-svc" className="mt-1.5 w-44" value={serviceCode} onChange={(e) => setServiceCode(e.target.value)} />
        </div>
        <Button variant="outline" onClick={() => setApplied({ dept: deptCode || undefined, svc: serviceCode || undefined })}>
          {t('search')}
        </Button>
      </div>

      <ul className="mt-4 space-y-1.5">
        {(logQ.data ?? []).length === 0 && <li className="rounded-xl border border-dashed border-border p-4 text-center text-xs text-muted-foreground">{t('adminAuditNoEntries')}</li>}
        {(logQ.data ?? []).map((entry) => (
          <AuditRow key={entry.id} entry={entry} />
        ))}
      </ul>
    </Card>
  )
}

function AuditRow({ entry }: { readonly entry: AdminChangeLogEntry }) {
  return (
    <li className="rounded-xl border border-border p-3 text-xs">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="font-semibold text-[color:var(--gov-navy)]">
          {entry.serviceName ?? entry.serviceCode ?? '—'} · {entry.changeType}
        </span>
        <span className="text-muted-foreground">
          {entry.changedBy} · {new Date(entry.changedAt).toLocaleString()}
        </span>
      </div>
      {entry.diff && <p className="mt-1 truncate text-muted-foreground">{entry.diff}</p>}
    </li>
  )
}
