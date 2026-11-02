import { useMemo, useState } from 'react'
import { ArrowDownAZ, ArrowUpAZ, Download, Search, TrendingUp, UsersRound } from 'lucide-react'
import { Button } from '../components/ui/button'
import { ChartCard, formatNumber, PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { useGovServiceUsage } from '../hooks/useGovAdmin'
import { downloadCsv } from '../lib/csvExport'

type SortOrder = 'most' | 'least'

export function ServiceUsagePage() {
  const [since, setSince] = useState('')
  const [until, setUntil] = useState('')
  const [departmentCode, setDepartmentCode] = useState('')
  const [sortOrder, setSortOrder] = useState<SortOrder>('most')
  const [zeroOnly, setZeroOnly] = useState(false)
  const [search, setSearch] = useState('')
  const query = useGovServiceUsage({ since: since || undefined, until: until || undefined, departmentCode: departmentCode || undefined }, true)
  const departments = useMemo(() => [...new Map((query.data?.services ?? []).map((row) => [row.departmentCode, row.departmentName])).entries()], [query.data])
  const rows = useMemo(() => {
    const term = search.trim().toLowerCase()
    return [...(query.data?.services ?? [])]
      .filter((row) => !zeroOnly || row.totalApplyClicks === 0)
      .filter((row) => !term || (row.serviceCode + ' ' + row.serviceName + ' ' + row.departmentName).toLowerCase().includes(term))
      .sort((a, b) => sortOrder === 'most' ? b.totalApplyClicks - a.totalApplyClicks || a.serviceName.localeCompare(b.serviceName) : a.totalApplyClicks - b.totalApplyClicks || a.serviceName.localeCompare(b.serviceName))
  }, [query.data, search, sortOrder, zeroOnly])
  const exportCsv = () => downloadCsv('sevaone-service-usage.csv', rows.map((row) => ({ serviceCode: row.serviceCode, serviceName: row.serviceName, departmentCode: row.departmentCode, departmentName: row.departmentName, totalApplyClicks: row.totalApplyClicks, aiApplyClicks: row.aiApplyClicks, catalogueApplyClicks: row.catalogueApplyClicks, lastUsedAt: row.lastUsedAt ?? '' })))
  const totalClicks = rows.reduce((sum, row) => sum + row.totalApplyClicks, 0)
  const zeroCount = rows.filter((row) => row.totalApplyClicks === 0).length

  return (
    <>
      <PageIntro eyebrow="Demand intelligence" title="Service usage" description="Rank active public services by real apply clicks, with AI and catalogue sources kept visible and zero-use services retained." action={<Button variant="outline" size="sm" onClick={exportCsv} disabled={!query.data}><Download className="mr-1.5 h-3.5 w-3.5" />Export CSV</Button>} />
      <div className="mb-5 grid gap-3 rounded-2xl border border-border bg-card p-4 md:grid-cols-[1fr_auto_auto_auto]">
        <div className="relative"><Search className="pointer-events-none absolute left-3 top-3 h-4 w-4 text-muted-foreground" /><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search service or department" className="h-10 w-full rounded-xl border border-border bg-secondary/30 pl-9 pr-3 text-sm outline-none focus:ring-2 focus:ring-[color:var(--gov-blue)]/30" /></div>
        <select value={departmentCode} onChange={(event) => setDepartmentCode(event.target.value)} className="h-10 rounded-xl border border-border bg-secondary/30 px-3 text-sm"><option value="">All departments</option>{departments.map(([code, name]) => <option key={code} value={code}>{name}</option>)}</select>
        <label className="flex h-10 items-center gap-2 rounded-xl border border-border bg-secondary/30 px-3 text-sm"><input type="checkbox" checked={zeroOnly} onChange={(event) => setZeroOnly(event.target.checked)} className="h-4 w-4 accent-[color:var(--gov-blue)]" /> Zero usage only</label>
        <div className="flex gap-1 rounded-xl border border-border bg-secondary/30 p-1"><button type="button" onClick={() => setSortOrder('most')} className={'rounded-lg px-3 text-xs font-semibold ' + (sortOrder === 'most' ? 'bg-white text-[color:var(--gov-blue)] shadow-sm' : 'text-muted-foreground')}><ArrowDownAZ className="mr-1 inline h-3.5 w-3.5" />Most used</button><button type="button" onClick={() => setSortOrder('least')} className={'rounded-lg px-3 text-xs font-semibold ' + (sortOrder === 'least' ? 'bg-white text-[color:var(--gov-blue)] shadow-sm' : 'text-muted-foreground')}><ArrowUpAZ className="mr-1 inline h-3.5 w-3.5" />Least used</button></div>
        <label className="text-xs font-semibold text-muted-foreground">From<input type="date" value={since} onChange={(event) => setSince(event.target.value)} className="mt-1 block h-9 rounded-lg border border-border bg-secondary/30 px-2 text-sm font-normal text-foreground" /></label>
        <label className="text-xs font-semibold text-muted-foreground">To<input type="date" value={until} onChange={(event) => setUntil(event.target.value)} className="mt-1 block h-9 rounded-lg border border-border bg-secondary/30 px-2 text-sm font-normal text-foreground" /></label>
      </div>
      <QueryState isLoading={query.isLoading} isError={query.isError}>
        {query.data && <>
          <div className="mb-5 grid gap-4 sm:grid-cols-3"><Stat icon={TrendingUp} label="Apply clicks shown" value={formatNumber(totalClicks)} detail="Within the selected filters" /><Stat icon={UsersRound} label="Active services" value={formatNumber(query.data.services.length)} detail="Zero-use services included" /><Stat icon={Search} label="Zero usage" value={formatNumber(zeroCount)} detail="No tracked apply clicks" /></div>
          <ChartCard title="Service demand ranking" description={rows.length + ' service(s) shown' + (since || until ? ' for the selected date range.' : '.')}>
            <div className="h-full overflow-y-auto pr-1"><div className="space-y-3">{rows.slice(0, 20).map((row, index) => { const max = rows[0]?.totalApplyClicks || 1; return <div key={row.serviceCode} className="grid grid-cols-[2rem_1fr_auto] items-center gap-3"><span className="text-xs font-semibold text-muted-foreground">{index + 1}</span><div className="min-w-0"><div className="flex justify-between gap-3 text-xs"><span className="truncate font-semibold text-[color:var(--gov-navy)]">{row.serviceName}</span><span className="font-mono text-muted-foreground">{formatNumber(row.totalApplyClicks)}</span></div><div className="mt-1 h-2 overflow-hidden rounded-full bg-secondary"><div className="h-full rounded-full bg-[color:var(--gov-blue)]" style={{ width: Math.max(row.totalApplyClicks === 0 ? 2 : (row.totalApplyClicks / max) * 100, 2) + '%' }} /></div></div><span className="text-[10px] text-muted-foreground">{row.departmentCode}</span></div> })}</div></div>
          </ChartCard>
          <div className="mt-5 overflow-hidden rounded-2xl border border-border bg-card"><div className="overflow-x-auto"><table className="w-full min-w-[760px] text-left text-sm"><thead className="bg-secondary/60 text-xs uppercase tracking-wide text-muted-foreground"><tr><th className="px-5 py-3">Service</th><th className="px-5 py-3">Department</th><th className="px-5 py-3 text-right">Total</th><th className="px-5 py-3 text-right">Source split</th><th className="hidden px-5 py-3 md:table-cell">Last used</th></tr></thead><tbody>{rows.map((row) => <tr key={row.serviceCode} className="border-t border-border"><td className="px-5 py-3"><p className="font-semibold text-[color:var(--gov-navy)]">{row.serviceName}</p><p className="mt-1 font-mono text-xs text-[color:var(--gov-blue)]">{row.serviceCode}</p></td><td className="px-5 py-3 text-muted-foreground">{row.departmentName}</td><td className="px-5 py-3 text-right font-semibold">{formatNumber(row.totalApplyClicks)}</td><td className="px-5 py-3"><div className="flex justify-end gap-3 text-xs"><span title="AI apply clicks"><span className="text-[color:var(--gov-blue)]">AI</span> <span className="font-semibold">{formatNumber(row.aiApplyClicks)}</span></span><span title="Catalogue apply clicks"><span className="text-[color:var(--saffron)]">Catalogue</span> <span className="font-semibold">{formatNumber(row.catalogueApplyClicks)}</span></span></div></td><td className="hidden px-5 py-3 text-xs text-muted-foreground md:table-cell">{row.lastUsedAt ? new Date(row.lastUsedAt).toLocaleString() : 'Never used'}</td></tr>)}{rows.length === 0 && <tr><td colSpan={5} className="px-5 py-12 text-center text-sm text-muted-foreground">No services match the current filters.</td></tr>}</tbody></table></div></div>
        </>}
      </QueryState>
    </>
  )
}

function Stat({ icon: Icon, label, value, detail }: { readonly icon: typeof TrendingUp; readonly label: string; readonly value: string; readonly detail: string }) {
  return <div className="rounded-2xl border border-border bg-card p-5"><Icon className="h-4 w-4 text-[color:var(--gov-blue)]" /><p className="mt-3 text-xs font-semibold text-muted-foreground">{label}</p><p className="mt-1 text-2xl font-semibold text-[color:var(--gov-navy)]">{value}</p><p className="mt-1 text-xs text-muted-foreground">{detail}</p></div>
}
