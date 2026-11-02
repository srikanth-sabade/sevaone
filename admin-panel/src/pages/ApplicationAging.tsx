import { Hourglass } from 'lucide-react'
import { Area, AreaChart, Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { ChartCard, formatNumber, MetricCard, PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { useGovApplicationAging, useGovSlaTrend } from '../hooks/useGovAdmin'

function formatTrendDate(value: string) {
  const date = new Date(`${value}T00:00:00`)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('en-IN', { day: '2-digit', month: 'short' }).format(date)
}

function formatHours(hours: number | null) {
  if (hours === null) return '—'
  if (hours >= 24) return `${(hours / 24).toFixed(1)} days`
  return `${Math.round(hours)} hrs`
}

export function ApplicationAgingPage() {
  const agingQuery = useGovApplicationAging(true)
  const trendQuery = useGovSlaTrend(true)
  const data = agingQuery.data

  const totals = data?.byDepartment.reduce(
    (acc, row) => ({
      open: acc.open + row.openApplications,
      due24h: acc.due24h + row.dueWithin24h,
      stuck30Plus: acc.stuck30Plus + row.age30PlusDays,
    }),
    { open: 0, due24h: 0, stuck30Plus: 0 },
  )
  const withProcessingTime = data?.byDepartment.filter((row) => row.avgProcessingHours !== null) ?? []
  const avgProcessingHours = withProcessingTime.length
    ? withProcessingTime.reduce((sum, row) => sum + (row.avgProcessingHours ?? 0), 0) / withProcessingTime.length
    : null

  return (
    <>
      <PageIntro
        eyebrow="Delivery timeliness"
        title="Application aging & processing time"
        description="How long open applications have been waiting, and how quickly departments actually complete them."
      />
      <QueryState isLoading={agingQuery.isLoading} isError={agingQuery.isError}>
        {data && totals && (
          <>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <MetricCard label="Open applications" value={formatNumber(totals.open)} detail="Not yet approved or rejected" tone="navy" />
              <MetricCard label="Due within 24h" value={formatNumber(totals.due24h)} detail="Sakala-governed, deadline imminent" tone="saffron" />
              <MetricCard label="Stuck 30+ days" value={formatNumber(totals.stuck30Plus)} detail="Open more than a month" tone="blue" />
              <MetricCard
                label="Avg processing time"
                value={formatHours(avgProcessingHours)}
                detail="Simple average across departments, not weighted by volume"
                tone="green"
              />
            </div>

            <div className="mt-5">
              <ChartCard title="Aging of open applications by department" description="How long currently-open applications have been waiting, since submission">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={data.byDepartment} margin={{ bottom: 40, left: 0, right: 12 }}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e9ef" />
                    <XAxis dataKey="departmentCode" axisLine={false} tickLine={false} fontSize={11} angle={-35} textAnchor="end" />
                    <YAxis axisLine={false} tickLine={false} fontSize={11} />
                    <Tooltip formatter={(value) => formatNumber(Number(value))} />
                    <Legend />
                    <Bar dataKey="age0To3Days" name="0-3 days" stackId="a" fill="#138808" radius={[0, 0, 0, 0]} />
                    <Bar dataKey="age4To7Days" name="4-7 days" stackId="a" fill="#164c93" radius={[0, 0, 0, 0]} />
                    <Bar dataKey="age8To14Days" name="8-14 days" stackId="a" fill="#e87722" radius={[0, 0, 0, 0]} />
                    <Bar dataKey="age15To30Days" name="15-30 days" stackId="a" fill="#b22222" radius={[0, 0, 0, 0]} />
                    <Bar dataKey="age30PlusDays" name="30+ days" stackId="a" fill="#5b1a1a" radius={[6, 6, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </ChartCard>
            </div>

            <div className="mt-5">
              <ChartCard title="SLA performance over time" description="Weekly on-time vs. late completions for Sakala-governed applications, last 90 days">
                {trendQuery.isLoading || trendQuery.isError || !trendQuery.data?.trend.length ? (
                  <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
                    {trendQuery.isLoading ? 'Loading trend…' : 'No trend data available yet for this scope.'}
                  </div>
                ) : (
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={trendQuery.data.trend} margin={{ right: 12, bottom: 8 }}>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e9ef" />
                      <XAxis dataKey="periodStart" axisLine={false} tickLine={false} tickMargin={8} minTickGap={28} height={34} fontSize={11} tickFormatter={(value) => formatTrendDate(String(value))} />
                      <YAxis axisLine={false} tickLine={false} width={44} allowDecimals={false} fontSize={11} tickFormatter={(value) => formatNumber(Number(value))} />
                      <Tooltip labelFormatter={(value) => formatTrendDate(String(value))} formatter={(value) => formatNumber(Number(value))} />
                      <Legend />
                      <Area type="monotone" dataKey="completedOnTime" name="Completed on time" stroke="#138808" fill="#138808" fillOpacity={0.18} strokeWidth={2.5} />
                      <Area type="monotone" dataKey="completedLate" name="Completed late" stroke="#e87722" fill="#e87722" fillOpacity={0.18} strokeWidth={2.5} />
                    </AreaChart>
                  </ResponsiveContainer>
                )}
              </ChartCard>
            </div>

            <div className="mt-5 overflow-hidden rounded-2xl border border-border bg-card">
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border p-5">
                <div>
                  <h2 className="text-base font-semibold text-[color:var(--gov-navy)]">Department turnaround</h2>
                  <p className="mt-1 text-xs text-muted-foreground">Report scope: {data.scope === 'statewide' ? 'Statewide' : data.scope}.</p>
                </div>
                <span className="rounded-full bg-[color:var(--gov-blue)]/10 px-3 py-1 text-xs font-semibold text-[color:var(--gov-blue)]">{data.byDepartment.length} departments</span>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[600px] text-left text-sm">
                  <thead className="bg-secondary/60 text-xs uppercase tracking-wide text-muted-foreground">
                    <tr>
                      <th className="px-5 py-3 font-semibold">Department</th>
                      <th className="px-5 py-3 text-right font-semibold">Open applications</th>
                      <th className="px-5 py-3 text-right font-semibold">Due within 24h</th>
                      <th className="px-5 py-3 text-right font-semibold">Avg processing time</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.byDepartment.map((row) => (
                      <tr key={row.departmentCode} className="border-t border-border">
                        <td className="px-5 py-3 font-medium text-[color:var(--gov-navy)]">{row.departmentName}</td>
                        <td className="px-5 py-3 text-right font-semibold">{formatNumber(row.openApplications)}</td>
                        <td className="px-5 py-3 text-right text-[color:var(--saffron)]">{formatNumber(row.dueWithin24h)}</td>
                        <td className="px-5 py-3 text-right">{formatHours(row.avgProcessingHours)}</td>
                      </tr>
                    ))}
                    {data.byDepartment.length === 0 && (
                      <tr>
                        <td colSpan={4} className="px-5 py-10 text-center text-sm text-muted-foreground">
                          <Hourglass className="mx-auto mb-2 h-6 w-6 text-muted-foreground/40" />
                          No applications found in this scope.
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
