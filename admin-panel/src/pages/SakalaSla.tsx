import { AlarmClockCheck } from 'lucide-react'
import { ChartCard, formatNumber, MetricCard, PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { useGovSakalaSla } from '../hooks/useGovAdmin'
import type { SakalaSlaDepartmentRow } from '../types'
import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'

function onTimeRate(row: SakalaSlaDepartmentRow) {
  const resolved = row.completedOnTime + row.completedLate
  return resolved === 0 ? null : Math.round((row.completedOnTime / resolved) * 100)
}

export function SakalaSlaPage() {
  const query = useGovSakalaSla(true)
  const data = query.data

  const totals = data?.byDepartment.reduce(
    (acc, row) => ({
      total: acc.total + row.totalSakalaApplications,
      onTime: acc.onTime + row.completedOnTime,
      late: acc.late + row.completedLate,
      overdue: acc.overdue + row.pendingOverdue,
    }),
    { total: 0, onTime: 0, late: 0, overdue: 0 },
  )
  const overallRate = totals && totals.onTime + totals.late > 0 ? Math.round((totals.onTime / (totals.onTime + totals.late)) * 100) : null

  return (
    <>
      <PageIntro
        eyebrow="Legal delivery timeline"
        title="Sakala SLA tracking"
        description="Compare actual service delivery time against each service's Sakala-mandated deadline, by department."
      />
      <QueryState isLoading={query.isLoading} isError={query.isError}>
        {data && totals && (
          <>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <MetricCard label="Sakala applications" value={formatNumber(totals.total)} detail="Applications under a Sakala deadline" tone="navy" />
              <MetricCard label="On-time rate" value={overallRate === null ? 'N/A' : `${overallRate}%`} detail="Of applications completed so far" tone="green" />
              <MetricCard label="Completed late" value={formatNumber(totals.late)} detail="Missed the Sakala deadline" tone="saffron" />
              <MetricCard label="Pending, overdue" value={formatNumber(totals.overdue)} detail="Still open, already past due" tone="blue" />
            </div>

            <div className="mt-5">
              <ChartCard title="Completion outcome by department" description="On-time vs. late completions, and applications still pending past due">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={data.byDepartment} margin={{ bottom: 40, left: 0, right: 12 }}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e9ef" />
                    <XAxis dataKey="departmentCode" axisLine={false} tickLine={false} fontSize={11} angle={-35} textAnchor="end" />
                    <YAxis axisLine={false} tickLine={false} fontSize={11} />
                    <Tooltip formatter={(value) => formatNumber(Number(value))} />
                    <Legend />
                    <Bar dataKey="completedOnTime" name="Completed on time" stackId="a" fill="#138808" radius={[0, 0, 0, 0]} />
                    <Bar dataKey="completedLate" name="Completed late" stackId="a" fill="#e87722" radius={[0, 0, 0, 0]} />
                    <Bar dataKey="pendingOverdue" name="Pending, overdue" stackId="a" fill="#b22222" radius={[6, 6, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </ChartCard>
            </div>

            <div className="mt-5 overflow-hidden rounded-2xl border border-border bg-card">
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border p-5">
                <div>
                  <h2 className="text-base font-semibold text-[color:var(--gov-navy)]">Department breakdown</h2>
                  <p className="mt-1 text-xs text-muted-foreground">Report scope: {data.scope === 'statewide' ? 'Statewide' : data.scope}.</p>
                </div>
                <span className="rounded-full bg-[color:var(--gov-blue)]/10 px-3 py-1 text-xs font-semibold text-[color:var(--gov-blue)]">{data.byDepartment.length} departments</span>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[680px] text-left text-sm">
                  <thead className="bg-secondary/60 text-xs uppercase tracking-wide text-muted-foreground">
                    <tr>
                      <th className="px-5 py-3 font-semibold">Department</th>
                      <th className="hidden px-5 py-3 text-right font-semibold md:table-cell">Sakala applications</th>
                      <th className="px-5 py-3 text-right font-semibold">On time</th>
                      <th className="px-5 py-3 text-right font-semibold">Late</th>
                      <th className="px-5 py-3 text-right font-semibold">Pending</th>
                      <th className="px-5 py-3 text-right font-semibold">On-time rate</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.byDepartment.map((row) => {
                      const rate = onTimeRate(row)
                      return (
                        <tr key={row.departmentCode} className="border-t border-border">
                          <td className="px-5 py-3 font-medium text-[color:var(--gov-navy)]">{row.departmentName}</td>
                          <td className="hidden px-5 py-3 text-right font-semibold md:table-cell">{formatNumber(row.totalSakalaApplications)}</td>
                          <td className="px-5 py-3 text-right text-[color:var(--gov-green)]">{formatNumber(row.completedOnTime)}</td>
                          <td className="px-5 py-3 text-right text-[color:var(--saffron)]">{formatNumber(row.completedLate)}</td>
                          <td className="px-5 py-3 text-right">
                            <div className="flex justify-end gap-3 text-xs">
                              <span title="Pending within SLA"><span className="text-muted-foreground">Within</span> <span className="font-semibold">{formatNumber(row.pendingWithinSla)}</span></span>
                              <span title="Pending overdue"><span className="text-red-700/70">Overdue</span> <span className="font-semibold text-red-700">{formatNumber(row.pendingOverdue)}</span></span>
                            </div>
                          </td>
                          <td className="px-5 py-3 text-right font-semibold">{rate === null ? '—' : `${rate}%`}</td>
                        </tr>
                      )
                    })}
                    {data.byDepartment.length === 0 && (
                      <tr>
                        <td colSpan={6} className="px-5 py-10 text-center text-sm text-muted-foreground">
                          <AlarmClockCheck className="mx-auto mb-2 h-6 w-6 text-muted-foreground/40" />
                          No Sakala-enabled applications found in this scope.
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
    </>
  )
}
