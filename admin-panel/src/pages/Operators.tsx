import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { CheckCircle2, CircleSlash, KeyRound, Search, Store, UserRound } from 'lucide-react'
import { formatNumber, PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { Button } from '../components/ui/button'
import { Input } from '../components/ui/input'
import { govAdminApi } from '../api'
import { useGovOperators } from '../hooks/useGovAdmin'
import type { GovAdminTier } from '../types'

export function OperatorsPage({ tier }: { readonly tier: GovAdminTier }) {
  const isSuperadmin = tier === 'Superadmin'
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<'all' | 'active' | 'inactive'>('all')
  const [type, setType] = useState('')
  const query = useGovOperators(tier === 'Superadmin' || tier === 'ChiefMinister', { search, isActive: status === 'all' ? undefined : status === 'active', operatorType: type || undefined })

  async function toggle(id: string, isActive: boolean) {
    try {
      await govAdminApi.setOperatorActive(id, isActive)
      toast.success(isActive ? 'Operator reactivated.' : 'Operator deactivated.')
      await queryClient.invalidateQueries({ queryKey: ['gov-admin', 'operators'] })
    } catch (error) { toast.error(error instanceof Error ? error.message : 'Could not update operator.') }
  }

  const rows = status === 'inactive' ? (query.data ?? []).filter((row) => !row.isActive) : query.data
  return (
    <>
      <PageIntro eyebrow="Administration" title="Operator directory" description="Review kiosk operator access without exposing credentials or operator activity estimates." />
      <div className="mb-5 grid gap-3 rounded-2xl border border-border bg-card p-4 sm:grid-cols-[1fr_auto_auto]">
        <div className="relative"><Search className="pointer-events-none absolute left-3 top-3 h-4 w-4 text-muted-foreground" /><Input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search code or operator name" className="h-10 rounded-xl pl-9" /></div>
        <select value={type} onChange={(event) => setType(event.target.value)} className="h-10 rounded-xl border border-border bg-secondary/40 px-3 text-sm"><option value="">All operator types</option><option value="G1">G1</option><option value="K1">K1</option><option value="B1">B1</option><option value="UNK">Unmapped</option></select>
        <select value={status} onChange={(event) => setStatus(event.target.value as typeof status)} className="h-10 rounded-xl border border-border bg-secondary/40 px-3 text-sm"><option value="all">All statuses</option><option value="active">Active</option><option value="inactive">Inactive</option></select>
      </div>
      <QueryState isLoading={query.isLoading} isError={query.isError}>
        {rows && <div className="overflow-hidden rounded-2xl border border-border bg-card">
          <div className="flex items-center justify-between border-b border-border p-5"><div className="flex items-center gap-2"><Store className="h-4 w-4 text-[color:var(--gov-blue)]" /><h2 className="font-semibold text-[color:var(--gov-navy)]">All operators</h2></div><span className="rounded-full bg-[color:var(--gov-blue)]/10 px-3 py-1 text-xs font-semibold text-[color:var(--gov-blue)]">{formatNumber(rows.length)} operators</span></div>
          <div className="overflow-x-auto"><table className="w-full min-w-[720px] text-left text-sm"><thead className="bg-secondary/60 text-xs uppercase tracking-wide text-muted-foreground"><tr><th className="px-5 py-3">Operator</th><th className="px-5 py-3">Type</th><th className="px-5 py-3">Contact & identity</th><th className="px-5 py-3">Status</th>{isSuperadmin && <th className="px-5 py-3 text-right">Action</th>}</tr></thead><tbody>{rows.map((row) => <tr key={row.id} className="border-t border-border"><td className="px-5 py-4"><div className="flex items-center gap-3"><div className="flex h-9 w-9 items-center justify-center rounded-xl bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]"><UserRound className="h-4 w-4" /></div><div><p className="font-mono text-xs font-semibold text-[color:var(--gov-blue)]">{row.operatorCode}</p><p className="mt-1 font-medium text-[color:var(--gov-navy)]">{row.operatorName || 'Unnamed operator'}</p></div></div></td><td className="px-5 py-4 font-semibold">{row.operatorType}</td><td className="px-5 py-4"><p className="text-muted-foreground">{row.mobileNo || 'Not available'}</p>{row.isClaimed ? <span className="mt-1 inline-flex items-center gap-1 text-xs font-semibold text-[color:var(--gov-green)]"><KeyRound className="h-3 w-3" /> WSO2 claimed</span> : <span className="mt-1 inline-flex text-xs text-muted-foreground">Awaiting claim</span>}</td><td className="px-5 py-4">{row.isActive ? <span className="inline-flex items-center gap-1 rounded-full bg-[color:var(--gov-green)]/10 px-2.5 py-1 text-xs font-semibold text-[color:var(--gov-green)]"><CheckCircle2 className="h-3 w-3" /> Active</span> : <span className="inline-flex items-center gap-1 rounded-full bg-secondary px-2.5 py-1 text-xs font-semibold text-muted-foreground"><CircleSlash className="h-3 w-3" /> Inactive</span>}</td>{isSuperadmin && <td className="px-5 py-4 text-right"><Button variant="ghost" size="sm" onClick={() => void toggle(row.id, !row.isActive)}>{row.isActive ? 'Deactivate' : 'Reactivate'}</Button></td>}</tr>)}</tbody></table></div>
        </div>}
      </QueryState>
    </>
  )
}
