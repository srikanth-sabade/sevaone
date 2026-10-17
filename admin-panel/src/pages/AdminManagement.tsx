import { ChangeEvent, FormEvent, useMemo, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { CheckCircle2, CircleSlash, FileUp, LogOut, Plus, ShieldPlus, UserCog } from 'lucide-react'
import { formatNumber, PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { Button } from '../components/ui/button'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { govAdminApi } from '../api'
import { useGovAdminList, useGovAdminSessions, useReferenceDepartments, useReferenceDistricts } from '../hooks/useGovAdmin'
import type { GovAdminImportRow, GovAdminMe, GovAdminTier } from '../types'
import { Modal } from '../components/Modal'
import { TIER_LABELS, tierLabel } from '../lib/tierLabels'

const TIERS = Object.keys(TIER_LABELS) as GovAdminTier[]

export function AdminManagementPage({ tier }: { readonly tier: GovAdminMe['tier'] }) {
  const isSuperadmin = tier === 'Superadmin'
  const queryClient = useQueryClient()
  const query = useGovAdminList(true)
  const departmentsQuery = useReferenceDepartments(isSuperadmin)
  const districtsQuery = useReferenceDistricts(isSuperadmin)

  const [username, setUsername] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [role, setRole] = useState<GovAdminTier>('DistrictHead')
  const [district, setDistrict] = useState('')
  const [taluk, setTaluk] = useState('')
  const [department, setDepartment] = useState('')
  const [busy, setBusy] = useState(false)
  const [createOpen, setCreateOpen] = useState(false)
  const [sessionsAdmin, setSessionsAdmin] = useState<{ id: string; displayName: string } | null>(null)
  const sessionsQuery = useGovAdminSessions(sessionsAdmin?.id ?? null, isSuperadmin)
  const [importOpen, setImportOpen] = useState(false)
  const [importRows, setImportRows] = useState<GovAdminImportRow[]>([])
  const [importName, setImportName] = useState('')
  const [importBatchId, setImportBatchId] = useState<string | null>(null)
  const [importError, setImportError] = useState('')

  const taluksForDistrict = useMemo(
    () => districtsQuery.data?.find((d) => d.district === district)?.taluks ?? [],
    [districtsQuery.data, district],
  )

  async function createAdmin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!username.trim() || !displayName.trim()) {
      toast.error('Enter a username and display name.')
      return
    }
    if (role === 'DistrictHead' && !district) {
      toast.error('Select a district.')
      return
    }
    if (role === 'TalukHead' && (!district || !taluk)) {
      toast.error('Select a district and taluk.')
      return
    }
    if (role === 'Department' && !department) {
      toast.error('Select a department.')
      return
    }

    setBusy(true)
    try {
      await govAdminApi.createAdmin({
        username: username.trim(),
        displayName: displayName.trim(),
        tier: role,
        assignedDistrict: role === 'DistrictHead' || role === 'TalukHead' ? district : null,
        assignedTaluk: role === 'TalukHead' ? taluk : null,
        assignedDepartment: role === 'Department' ? department : null,
      })
      toast.success(`Account "${username.trim()}" created. Share the username with them so they can complete first-time login.`)
      setUsername('')
      setDisplayName('')
      setDistrict('')
      setTaluk('')
      setDepartment('')
      setCreateOpen(false)
      await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'admin-list'] })
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Could not create the account.')
    } finally {
      setBusy(false)
    }
  }

  async function toggleActive(id: string, next: boolean) {
    try {
      await govAdminApi.setAdminActive(id, next)
      toast.success(next ? 'Account reactivated.' : 'Account deactivated.')
      await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'admin-list'] })
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Could not update the account.')
    }
  }

  async function readImport(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (!file) return
    setImportError('')
    setImportName(file.name)
    try {
      const text = await file.text()
      const rows = parseAdminCsv(text)
      if (!rows.length) throw new Error('The CSV has no data rows.')
      setImportRows(rows)
    } catch (error) { setImportRows([]); setImportError(error instanceof Error ? error.message : 'Could not read this CSV.') }
    event.target.value = ''
  }

  async function stageImport() {
    if (!importRows.length) return
    setBusy(true)
    try {
      const result = await govAdminApi.startAdminImport({ sourceFilename: importName, sourceFormat: 'csv', rows: importRows })
      setImportBatchId(result.batchId)
      toast.success('Import staged. Review the rows before publishing.')
    } catch (error) { toast.error(error instanceof Error ? error.message : 'Could not stage this import.') }
    finally { setBusy(false) }
  }

  async function publishImport() {
    if (!importBatchId) return
    setBusy(true)
    try { const result = await govAdminApi.publishAdminImport(importBatchId); toast.success(`${result.created} admin account(s) created.`); setImportOpen(false); setImportRows([]); setImportBatchId(null); await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'admin-list'] }) }
    catch (error) { toast.error(error instanceof Error ? error.message : 'Could not publish this import.') }
    finally { setBusy(false) }
  }

  async function forceLogout(sessionId: string) {
    if (!sessionsAdmin) return
    try { await govAdminApi.forceLogoutAdminSession(sessionsAdmin.id, sessionId); toast.success('Session ended.'); await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'admin-sessions', sessionsAdmin.id] }) }
    catch (error) { toast.error(error instanceof Error ? error.message : 'Could not end this session.') }
  }

  return (
    <>
      <PageIntro
        eyebrow="Access control"
        title="Admin accounts"
        description={
          isSuperadmin
            ? 'Create government admin accounts and assign what each one can see.'
            : 'View every government admin account. Creating and deactivating accounts is limited to Superadmin.'
        }
        action={isSuperadmin && <div className="flex flex-wrap gap-2"><Button type="button" variant="outline" onClick={() => setImportOpen(true)} className="h-11 rounded-xl"><FileUp className="mr-2 h-4 w-4" /> Import CSV</Button><Button type="button" onClick={() => setCreateOpen(true)} className="h-11 rounded-xl bg-[color:var(--gov-blue)] px-5 text-white shadow-sm hover:bg-[color:var(--gov-blue-dark)]"><Plus className="mr-2 h-4 w-4" /> Create new admin</Button></div>}
      />

      <QueryState isLoading={query.isLoading} isError={query.isError}>
        {query.data && (
          <div className="overflow-hidden rounded-2xl border border-border bg-card">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border p-5">
              <div className="flex items-center gap-2">
                <UserCog className="h-4 w-4 text-[color:var(--gov-blue)]" />
                <h2 className="text-base font-semibold text-[color:var(--gov-navy)]">All admin accounts</h2>
              </div>
              <span className="rounded-full bg-[color:var(--gov-blue)]/10 px-3 py-1 text-xs font-semibold text-[color:var(--gov-blue)]">{formatNumber(query.data.length)} accounts</span>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[760px] text-left text-sm">
                <thead className="bg-secondary/60 text-xs uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="px-5 py-3 font-semibold">Username</th>
                    <th className="px-5 py-3 font-semibold">Name</th>
                    <th className="px-5 py-3 font-semibold">Role</th>
                    <th className="hidden px-5 py-3 font-semibold md:table-cell">Scope</th>
                    <th className="px-5 py-3 font-semibold">Status</th>
                    {isSuperadmin && <th className="px-5 py-3 text-right font-semibold">Actions</th>}
                  </tr>
                </thead>
                <tbody>
                  {query.data.map((admin) => (
                    <tr key={admin.id} className="border-t border-border">
                      <td className="px-5 py-3 font-mono text-xs text-[color:var(--gov-blue)]">{admin.username}</td>
                      <td className="px-5 py-3 font-medium text-[color:var(--gov-navy)]"><p>{admin.displayName}</p><p className="mt-1 truncate text-xs font-normal text-muted-foreground md:hidden" title={adminScope(admin)}>Scope: {adminScope(admin)}</p></td>
                      <td className="px-5 py-3">{tierLabel(admin.tier)}</td>
                      <td className="hidden px-5 py-3 text-muted-foreground md:table-cell">
                        {admin.assignedTaluk ? `${admin.assignedTaluk}, ${admin.assignedDistrict}` : (admin.assignedDistrict ?? admin.assignedDepartment ?? 'Statewide')}
                      </td>
                      <td className="px-5 py-3">
                        <div className="flex items-center gap-2">
                          {admin.isActive ? (
                            <span className="inline-flex items-center gap-1 rounded-full bg-[color:var(--gov-green)]/10 px-2.5 py-1 text-xs font-semibold text-[color:var(--gov-green)]">
                              <CheckCircle2 className="h-3 w-3" /> Active
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1 rounded-full bg-secondary px-2.5 py-1 text-xs font-semibold text-muted-foreground">
                              <CircleSlash className="h-3 w-3" /> Deactivated
                            </span>
                          )}
                          {!admin.claimed && (
                            <span className="rounded-full bg-[color:var(--saffron)]/15 px-2.5 py-1 text-xs font-semibold text-[color:var(--saffron)]">Awaiting first login</span>
                          )}
                        </div>
                      </td>
                      {isSuperadmin && (
                        <td className="px-5 py-3 text-right">
                          <div className="flex justify-end gap-1"><Button variant="ghost" size="sm" onClick={() => setSessionsAdmin({ id: admin.id, displayName: admin.displayName })}>Sessions</Button><Button variant="ghost" size="sm" onClick={() => void toggleActive(admin.id, !admin.isActive)}>{admin.isActive ? 'Deactivate' : 'Reactivate'}</Button></div>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </QueryState>
      <Modal open={Boolean(sessionsAdmin)} title={`Active sessions: ${sessionsAdmin?.displayName ?? ''}`} description="End a session if an account is signed in on a device that should no longer have access." onClose={() => setSessionsAdmin(null)}>
        {sessionsQuery.isLoading && <p className="text-sm text-muted-foreground">Loading sessions...</p>}
        {sessionsQuery.isError && <p className="text-sm text-red-700">Could not load sessions.</p>}
        {sessionsQuery.data && <div className="space-y-3">{sessionsQuery.data.length === 0 && <p className="rounded-xl bg-secondary/50 p-4 text-sm text-muted-foreground">No active sessions.</p>}{sessionsQuery.data.map((session) => <div key={session.sessionId} className="flex flex-wrap items-center gap-3 rounded-xl border border-border p-4"><div className="min-w-0 flex-1"><p className="text-sm font-semibold text-[color:var(--gov-navy)]">{session.isCurrent ? 'Current browser session' : 'Signed-in session'}</p><p className="mt-1 text-xs text-muted-foreground">Expires {new Date(session.expiresAt).toLocaleString()}</p></div><Button type="button" variant="outline" size="sm" disabled={session.isCurrent} onClick={() => void forceLogout(session.sessionId)}><LogOut className="mr-1.5 h-3.5 w-3.5" /> Force logout</Button></div>)}</div>}
      </Modal>
      <Modal open={importOpen} title="Import admin accounts" description="Upload the CSV, review the parsed rows, then publish. No account is created during staging." onClose={() => { if (!busy) { setImportOpen(false); setImportRows([]); setImportBatchId(null) } }}>
        {!importBatchId && <div className="space-y-4"><div className="rounded-2xl border border-dashed border-[color:var(--gov-blue)]/35 bg-[color:var(--gov-blue)]/5 p-6 text-center"><FileUp className="mx-auto h-9 w-9 text-[color:var(--gov-blue)]" /><p className="mt-2 text-sm font-semibold text-[color:var(--gov-navy)]">Choose a CSV file</p><p className="mt-1 text-xs text-muted-foreground">Columns: username, displayName, tier, assignedDistrict, assignedTaluk, assignedDepartment</p><input type="file" accept=".csv,text/csv" onChange={(event) => void readImport(event)} className="mt-4 block w-full text-sm" /><a href="data:text/csv;charset=utf-8,username%2CdisplayName%2Ctier%2CassignedDistrict%2CassignedTaluk%2CassignedDepartment%0Aexample-admin%2CExample%20Admin%2CDistrictHead%2CBengaluru%20Urban%2CBengaluru%2C" download="gov-admin-import-template.csv" className="mt-3 inline-block text-xs font-semibold text-[color:var(--gov-blue)]">Download CSV template</a></div>{importError && <p className="rounded-xl bg-red-50 p-3 text-sm text-red-700">{importError}</p>}{importRows.length > 0 && <div className="rounded-xl border border-border p-4"><p className="text-sm font-semibold">{importRows.length} row(s) ready</p><div className="mt-3 max-h-48 overflow-auto text-xs">{importRows.slice(0, 12).map((row, index) => <p key={`${row.username}-${index}`} className="border-t border-border py-2"><span className="font-mono text-[color:var(--gov-blue)]">{row.username}</span> · {row.displayName} · {row.tier}</p>)}</div></div>}<div className="flex justify-end"><Button type="button" disabled={!importRows.length || busy} onClick={() => void stageImport()} className="rounded-xl bg-[color:var(--gov-blue)] text-white">{busy ? 'Staging...' : 'Stage import'}</Button></div></div>}
        {importBatchId && <div className="space-y-4"><div className="rounded-xl bg-[color:var(--gov-green)]/10 p-4 text-sm text-[color:var(--gov-navy)]">The batch is staged and ready. Publish only after reviewing the source rows above.</div><div className="flex justify-end gap-2"><Button type="button" variant="outline" disabled={busy} onClick={() => { setImportBatchId(null); setImportRows([]) }}>Choose another file</Button><Button type="button" disabled={busy} onClick={() => void publishImport()} className="rounded-xl bg-[color:var(--gov-green)] text-white">{busy ? 'Publishing...' : 'Publish accounts'}</Button></div></div>}
      </Modal>
      <Modal
        open={createOpen}
        title="Create an admin account"
        description="Set the administrator's identity and access scope. They will create their password during first-time login."
        onClose={() => { if (!busy) setCreateOpen(false) }}
      >
        <form className="grid gap-5 sm:grid-cols-2" onSubmit={(event) => void createAdmin(event)}>
          <div className="space-y-2">
            <Label htmlFor="new-username">Username</Label>
            <Input id="new-username" value={username} onChange={(event) => setUsername(event.target.value)} placeholder="sevaone-govadmin-..." />
          </div>
          <div className="space-y-2">
            <Label htmlFor="new-display-name">Display name</Label>
            <Input id="new-display-name" value={displayName} onChange={(event) => setDisplayName(event.target.value)} placeholder="Full name" />
          </div>
          <div className="space-y-2">
            <Label htmlFor="new-role">Role</Label>
            <select id="new-role" value={role} onChange={(event) => { setRole(event.target.value as GovAdminTier); setDistrict(''); setTaluk(''); setDepartment('') }} className="h-10 w-full rounded-xl border border-border bg-secondary/40 px-3 text-sm outline-none ring-[color:var(--gov-blue)]/30 focus:ring-2">
              {TIERS.map((item) => <option key={item} value={item}>{tierLabel(item)}</option>)}
            </select>
          </div>
          {(role === 'DistrictHead' || role === 'TalukHead') && (
            <div className="space-y-2">
              <Label htmlFor="new-district">District</Label>
              <select id="new-district" value={district} onChange={(event) => { setDistrict(event.target.value); setTaluk('') }} className="h-10 w-full rounded-xl border border-border bg-secondary/40 px-3 text-sm outline-none ring-[color:var(--gov-blue)]/30 focus:ring-2">
                <option value="">Select a district</option>
                {districtsQuery.data?.map((item) => <option key={item.district} value={item.district}>{item.district}</option>)}
              </select>
            </div>
          )}
          {role === 'TalukHead' && (
            <div className="space-y-2">
              <Label htmlFor="new-taluk">Taluk</Label>
              <select id="new-taluk" value={taluk} onChange={(event) => setTaluk(event.target.value)} disabled={!district} className="h-10 w-full rounded-xl border border-border bg-secondary/40 px-3 text-sm outline-none ring-[color:var(--gov-blue)]/30 focus:ring-2 disabled:opacity-50">
                <option value="">{district ? 'Select a taluk' : 'Select a district first'}</option>
                {taluksForDistrict.map((item) => <option key={item} value={item}>{item}</option>)}
              </select>
            </div>
          )}
          {role === 'Department' && (
            <div className="space-y-2">
              <Label htmlFor="new-department">Department</Label>
              <select id="new-department" value={department} onChange={(event) => setDepartment(event.target.value)} className="h-10 w-full rounded-xl border border-border bg-secondary/40 px-3 text-sm outline-none ring-[color:var(--gov-blue)]/30 focus:ring-2">
                <option value="">Select a department</option>
                {departmentsQuery.data?.map((item) => <option key={item.code} value={item.code}>{item.name}</option>)}
              </select>
            </div>
          )}
          <div className="sm:col-span-2">
            <div className="flex flex-wrap items-center justify-end gap-3 border-t border-border pt-5">
              <Button type="button" variant="outline" disabled={busy} onClick={() => setCreateOpen(false)} className="h-10 rounded-xl">Cancel</Button>
              <Button type="submit" disabled={busy} className="h-10 rounded-xl bg-[color:var(--gov-blue)] px-6 text-white hover:bg-[color:var(--gov-blue-dark)]"><ShieldPlus className="mr-2 h-4 w-4" />{busy ? 'Creating...' : 'Create account'}</Button>
            </div>
            <p className="mt-3 text-xs leading-5 text-muted-foreground">No password is set here. The administrator sets one through First-time login, verified by OTP to their mobile.</p>
          </div>
        </form>
      </Modal>
    </>
  )
}

function parseAdminCsv(text: string): GovAdminImportRow[] {
  const lines = text.split(/\r?\n/).filter((line) => line.trim())
  if (lines.length < 2) return []
  const headers = splitCsvLine(lines[0]).map((value) => value.trim())
  const required = ['username', 'displayName', 'tier', 'assignedDistrict', 'assignedTaluk', 'assignedDepartment']
  if (!required.every((header) => headers.includes(header))) throw new Error(`CSV must include: ${required.join(', ')}`)
  return lines.slice(1).map((line) => {
    const values = splitCsvLine(line); const value = (name: string) => values[headers.indexOf(name)]?.trim() || null
    return { username: value('username') ?? '', displayName: value('displayName') ?? '', tier: (value('tier') ?? '') as GovAdminTier, assignedDistrict: value('assignedDistrict'), assignedTaluk: value('assignedTaluk'), assignedDepartment: value('assignedDepartment') }
  })
}

function splitCsvLine(line: string): string[] {
  const values: string[] = []; let current = ''; let quoted = false
  for (let index = 0; index < line.length; index++) { const char = line[index]; if (char === '"') { if (quoted && line[index + 1] === '"') { current += '"'; index++ } else quoted = !quoted } else if (char === ',' && !quoted) { values.push(current); current = '' } else current += char }
  values.push(current); return values
}

function adminScope(admin: { assignedTaluk: string | null; assignedDistrict: string | null; assignedDepartment: string | null }) {
  return admin.assignedTaluk ? `${admin.assignedTaluk}, ${admin.assignedDistrict}` : (admin.assignedDistrict ?? admin.assignedDepartment ?? 'Statewide')
}
