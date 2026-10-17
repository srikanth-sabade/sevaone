import { useMemo, useState } from 'react'
import { Download, MapPinned, Search } from 'lucide-react'
import { formatNumber, MetricCard, PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { KarnatakaMap } from '../components/KarnatakaMap'
import { Button } from '../components/ui/button'
import { Input } from '../components/ui/input'
import { useGovDistricts } from '../hooks/useGovAdmin'
import { downloadCsv } from '../lib/csvExport'
import type { DistrictRow, GovAdminMe } from '../types'

type SortBy = 'citizens' | 'applications' | 'status'

export function DistrictsPage({ admin }: { readonly admin: GovAdminMe }) {
  const { tier } = admin
  const available = tier !== 'Department'
  const query = useGovDistricts(available)
  const [search, setSearch] = useState('')
  const [sortBy, setSortBy] = useState<SortBy>('citizens')

  // Read the focus district/taluk off the admin's own assigned scope, not off the analytics
  // rows -- a district/taluk with zero citizens or applications yet still returns `rows: []`
  // (confirmed live), which would otherwise leave the map unfocused for a brand new assignment.
  const focusDistrictName = tier === 'DistrictHead' || tier === 'TalukHead' ? admin.assignedDistrict ?? undefined : undefined
  const focusTalukLabel = tier === 'TalukHead' ? admin.assignedTaluk ?? undefined : undefined

  const visibleRows = useMemo(() => {
    if (!query.data) return []
    const term = search.trim().toLowerCase()
    const filtered = term
      ? query.data.rows.filter((row) => row.district.toLowerCase().includes(term) || row.taluk.toLowerCase().includes(term))
      : query.data.rows
    const sortKey: Record<SortBy, (row: DistrictRow) => number> = {
      citizens: (row) => row.citizenCount,
      applications: (row) => row.applicationCount,
      status: (row) => row.completedCount,
    }
    return [...filtered].sort((a, b) => sortKey[sortBy](b) - sortKey[sortBy](a))
  }, [query.data, search, sortBy])

  const totalApplications = visibleRows.reduce((sum, row) => sum + row.applicationCount, 0)
  const exportCsv = () => downloadCsv('sevaone-districts.csv', visibleRows.map((row) => ({
    district: row.district, taluk: row.taluk, applicationCount: row.applicationCount, citizenCount: row.citizenCount,
    submittedCount: row.submittedCount, inProgressCount: row.inProgressCount, completedCount: row.completedCount, rejectedCount: row.rejectedCount,
  })))

  return (
    <>
      <PageIntro
        eyebrow="Geographic lens"
        title="District usage"
        description="Compare service demand across the geographic scope assigned to your government role."
        action={available && query.data && <Button variant="outline" size="sm" onClick={exportCsv}><Download className="h-3.5 w-3.5" />Export CSV</Button>}
      />
      {!available ? (
        <div className="flex min-h-64 flex-col items-center justify-center rounded-2xl border border-dashed border-border bg-card p-8 text-center">
          <MapPinned className="h-9 w-9 text-[color:var(--gov-blue)]" />
          <h2 className="mt-4 text-base font-semibold text-[color:var(--gov-navy)]">Not available for department roles</h2>
          <p className="mt-2 max-w-md text-sm leading-6 text-muted-foreground">District usage has no department relationship. Your department's reports remain available in the sidebar.</p>
        </div>
      ) : (
      <QueryState isLoading={query.isLoading} isError={query.isError}>
        {query.data && (
          <>
            <div className="grid gap-4 sm:grid-cols-2">
              <MetricCard label="Report scope" value={query.data.scope === 'statewide' ? 'Statewide' : query.data.scope} detail="Server-side authorization scope" tone="navy" />
              <MetricCard label="Applications in scope" value={formatNumber(totalApplications)} detail="Aggregated application volume" tone="blue" />
            </div>

            <div className="mt-5">
              <KarnatakaMap rows={visibleRows} focusDistrictName={focusDistrictName} focusTalukLabel={focusTalukLabel} />
            </div>

            <div className="mt-5 overflow-hidden rounded-2xl border border-border bg-card">
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border p-5">
                <div>
                  <h2 className="text-base font-semibold text-[color:var(--gov-navy)]">District and taluk directory</h2>
                  <p className="mt-1 text-xs text-muted-foreground">No IP-location data is included in this report.</p>
                </div>
                <span className="rounded-full bg-[color:var(--gov-blue)]/10 px-3 py-1 text-xs font-semibold text-[color:var(--gov-blue)]">{visibleRows.length} rows</span>
              </div>
              <div className="flex flex-wrap gap-3 border-b border-border p-5">
                <div className="relative min-w-[230px] flex-1">
                  <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search district or taluk" className="h-11 rounded-xl pl-9" />
                </div>
                <select value={sortBy} onChange={(event) => setSortBy(event.target.value as SortBy)} className="h-11 rounded-xl border border-border bg-white px-3 text-sm text-[color:var(--gov-navy)]">
                  <option value="citizens">Sort by citizen count</option>
                  <option value="applications">Sort by application count</option>
                  <option value="status">Sort by delivery status</option>
                </select>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[680px] text-left text-sm">
                  <thead className="bg-secondary/60 text-xs uppercase tracking-wide text-muted-foreground">
                    <tr>
                      <th className="px-5 py-3 font-semibold">District</th>
                      <th className="px-5 py-3 font-semibold">Taluk</th>
                      <th className="px-5 py-3 text-right font-semibold">Applications</th>
                      <th className="px-5 py-3 text-right font-semibold">Citizens</th>
                    </tr>
                  </thead>
                  <tbody>
                    {visibleRows.map((row) => (
                      <tr key={`${row.district}-${row.taluk}`} className="border-t border-border">
                        <td className="px-5 py-3 font-semibold text-[color:var(--gov-navy)]">{row.district}</td>
                        <td className="px-5 py-3 text-muted-foreground">{row.taluk}</td>
                        <td className="px-5 py-3 text-right font-semibold">{formatNumber(row.applicationCount)}</td>
                        <td className="px-5 py-3 text-right">{formatNumber(row.citizenCount)}</td>
                      </tr>
                    ))}
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
