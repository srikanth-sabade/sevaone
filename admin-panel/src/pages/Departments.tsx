import { useMemo, useState } from 'react'
import { ArrowDownAZ, ArrowUpAZ, Building2, Download, Search } from 'lucide-react'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { ChartCard, formatNumber, MetricCard, PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { Button } from '../components/ui/button'
import type { GovAdminMe } from '../types'
import { useGovDepartments } from '../hooks/useGovAdmin'
import { downloadCsv } from '../lib/csvExport'

type SortKey = 'name-asc' | 'name-desc' | 'services-desc' | 'services-asc'

const SORTS: { id: SortKey; label: string }[] = [
  { id: 'services-desc', label: 'Most services first' },
  { id: 'services-asc', label: 'Fewest services first' },
  { id: 'name-asc', label: 'Department name (A-Z)' },
  { id: 'name-desc', label: 'Department name (Z-A)' },
]

export function DepartmentsPage({ tier }: { readonly tier: GovAdminMe['tier'] }) {
  const statewide = tier !== 'DistrictHead' && tier !== 'TalukHead'
  const query = useGovDepartments(statewide)
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState<SortKey>('services-desc')

  const rows = useMemo(() => {
    const source = query.data?.byDepartment ?? []
    const term = search.trim().toLowerCase()
    const filtered = term
      ? source.filter((row) => row.departmentName.toLowerCase().includes(term) || row.departmentCode.toLowerCase().includes(term))
      : source
    const sorted = [...filtered].sort((a, b) => {
      if (sort === 'name-asc') return a.departmentName.localeCompare(b.departmentName)
      if (sort === 'name-desc') return b.departmentName.localeCompare(a.departmentName)
      if (sort === 'services-asc') return a.serviceCount - b.serviceCount
      return b.serviceCount - a.serviceCount
    })
    return sorted
  }, [query.data, search, sort])

  const exportCsv = () => query.data && downloadCsv('sevaone-departments.csv', query.data.byDepartment.map((row) => ({ departmentCode: row.departmentCode, departmentName: row.departmentName, serviceCount: row.serviceCount })))

  return (
    <>
      <PageIntro eyebrow="Service network" title="Department usage" description="Understand how government departments are contributing to the statewide service network." action={statewide && query.data && <Button variant="outline" size="sm" onClick={exportCsv}><Download className="h-3.5 w-3.5" />Export CSV</Button>} />
      {!statewide ? (
        <div className="flex min-h-64 flex-col items-center justify-center rounded-2xl border border-dashed border-border bg-card p-8 text-center">
          <Building2 className="h-9 w-9 text-[color:var(--gov-blue)]" />
          <h2 className="mt-4 text-base font-semibold text-[color:var(--gov-navy)]">Statewide report restricted</h2>
          <p className="mt-2 max-w-md text-sm leading-6 text-muted-foreground">
            Department usage is available only to statewide government roles. Your geographic reports remain available in the sidebar.
          </p>
        </div>
      ) : (
        <QueryState isLoading={query.isLoading} isError={query.isError}>
          {query.data && (
            <>
              <div className="grid gap-4 sm:grid-cols-2">
                <MetricCard label="Live departments" value={formatNumber(query.data.departmentCount)} detail="Departments with active services" />
                <MetricCard label="Active services" value={formatNumber(query.data.activeServiceCount)} detail="Published services on SevaOne" tone="green" />
              </div>

              <div className="mt-5">
                <ChartCard title="Services by department" description="Active service catalogue footprint across departments">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={query.data.byDepartment} margin={{ bottom: 40, left: 0, right: 12 }}>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e9ef" />
                      <XAxis dataKey="departmentCode" axisLine={false} tickLine={false} fontSize={11} angle={-35} textAnchor="end" />
                      <YAxis axisLine={false} tickLine={false} fontSize={11} />
                      <Tooltip formatter={(value) => formatNumber(Number(value))} />
                      <Bar dataKey="serviceCount" name="Active services" fill="#164c93" radius={[6, 6, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                </ChartCard>
              </div>

              <div className="mt-5 overflow-hidden rounded-2xl border border-border bg-card">
                <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border p-5">
                  <div>
                    <h2 className="text-base font-semibold text-[color:var(--gov-navy)]">Department directory</h2>
                    <p className="mt-1 text-xs text-muted-foreground">The department and service counts returned by the live report.</p>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <div className="relative">
                      <Search className="pointer-events-none absolute left-3 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
                      <input
                        value={search}
                        onChange={(event) => setSearch(event.target.value)}
                        placeholder="Filter by name or code"
                        className="h-9 w-52 rounded-full border border-border bg-secondary/40 pl-8 pr-3 text-xs outline-none ring-[color:var(--gov-blue)]/30 placeholder:text-muted-foreground focus:ring-2"
                      />
                    </div>
                    <div className="relative">
                      <select
                        value={sort}
                        onChange={(event) => setSort(event.target.value as SortKey)}
                        className="h-9 appearance-none rounded-full border border-border bg-secondary/40 py-0 pl-8 pr-8 text-xs font-semibold text-[color:var(--gov-navy)] outline-none ring-[color:var(--gov-blue)]/30 focus:ring-2"
                      >
                        {SORTS.map((option) => (
                          <option key={option.id} value={option.id}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                      {sort.endsWith('asc') ? (
                        <ArrowUpAZ className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
                      ) : (
                        <ArrowDownAZ className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
                      )}
                    </div>
                    <span className="rounded-full bg-[color:var(--gov-blue)]/10 px-3 py-1 text-xs font-semibold text-[color:var(--gov-blue)]">{rows.length} of {query.data.byDepartment.length}</span>
                  </div>
                </div>
                <div className="max-h-[420px] overflow-y-auto">
                  <table className="w-full min-w-[520px] text-left text-sm">
                    <thead className="sticky top-0 z-10 bg-secondary/95 text-xs uppercase tracking-wide text-muted-foreground backdrop-blur">
                      <tr>
                        <th className="px-5 py-3 font-semibold">Code</th>
                        <th className="px-5 py-3 font-semibold">Department</th>
                        <th className="px-5 py-3 text-right font-semibold">Services</th>
                      </tr>
                    </thead>
                    <tbody>
                      {rows.map((row) => (
                        <tr key={row.departmentCode} className="border-t border-border">
                          <td className="px-5 py-3 font-mono text-xs text-[color:var(--gov-blue)]">{row.departmentCode}</td>
                          <td className="px-5 py-3 font-medium text-[color:var(--gov-navy)]">{row.departmentName}</td>
                          <td className="px-5 py-3 text-right font-semibold">{formatNumber(row.serviceCount)}</td>
                        </tr>
                      ))}
                      {rows.length === 0 && (
                        <tr>
                          <td colSpan={3} className="px-5 py-10 text-center text-sm text-muted-foreground">
                            No departments match "{search}".
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </>
          )}
        </QueryState>
      )}
    </>
  )
}
