import { Download, Users } from 'lucide-react'
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { ChartCard, formatNumber, MetricCard, PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { Button } from '../components/ui/button'
import { useGovCitizens } from '../hooks/useGovAdmin'
import { downloadCsv } from '../lib/csvExport'
import type { GovAdminMe } from '../types'

function formatTrendDate(value: string) {
  const isMonth = /^\d{4}-\d{2}$/.test(value)
  const date = new Date(`${value}${isMonth ? '-01' : ''}T00:00:00`)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('en-IN', isMonth ? { month: 'short' } : { day: '2-digit', month: 'short' }).format(date)
}

function formatAxisCount(value: number) {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(value >= 10_000_000 ? 0 : 1)}M`
  if (value >= 1_000) return `${(value / 1_000).toFixed(value >= 10_000 ? 0 : 1)}k`
  return String(Math.round(value))
}

export function CitizensPage({ tier }: { readonly tier: GovAdminMe['tier'] }) {
  const available = tier !== 'Department'
  const query = useGovCitizens(available)
  const data = query.data
  const exportCsv = () => data && downloadCsv('sevaone-citizens.csv', data.registrationTrend.map((row) => ({ date: row.date, registrations: row.count })))
  return (
    <>
      <PageIntro eyebrow="Citizen reach" title="Citizens and DigiLocker" description="Monitor registration completion at an aggregate level, with no citizen-level information exposed." action={available && data && <Button variant="outline" size="sm" onClick={exportCsv}><Download className="h-3.5 w-3.5" />Export CSV</Button>} />
      {!available ? (
        <div className="flex min-h-64 flex-col items-center justify-center rounded-2xl border border-dashed border-border bg-card p-8 text-center">
          <Users className="h-9 w-9 text-[color:var(--gov-blue)]" />
          <h2 className="mt-4 text-base font-semibold text-[color:var(--gov-navy)]">Not available for department roles</h2>
          <p className="mt-2 max-w-md text-sm leading-6 text-muted-foreground">Citizen registration has no department relationship. Your department's reports remain available in the sidebar.</p>
        </div>
      ) : (
        <QueryState isLoading={query.isLoading} isError={query.isError}>
          {data && (
            <>
              <div className="grid gap-4 sm:grid-cols-3">
                <MetricCard label="Total registered" value={formatNumber(data.totalRegistered)} detail="Registered SevaOne citizens" tone="navy" />
                <MetricCard label="Profiles completed" value={formatNumber(data.profileCompleted)} detail="Completed onboarding profiles" tone="green" />
                <MetricCard label="Profiles incomplete" value={formatNumber(data.profileIncomplete)} detail="Follow-up opportunity" tone="saffron" />
              </div>
              <div className="mt-5">
                <ChartCard title="Registration trend" description="Daily registration volume over the last 90 days from the live aggregate report">
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={data.registrationTrend} margin={{ right: 12, bottom: 8 }}>
                      <defs>
                        <linearGradient id="registrationFill" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="5%" stopColor="#164c93" stopOpacity={0.25} />
                          <stop offset="95%" stopColor="#164c93" stopOpacity={0} />
                        </linearGradient>
                      </defs>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e9ef" />
                      <XAxis dataKey="date" axisLine={false} tickLine={false} tickMargin={8} minTickGap={28} height={34} fontSize={11} tickFormatter={(value) => formatTrendDate(String(value))} />
                      <YAxis axisLine={false} tickLine={false} width={44} allowDecimals={false} fontSize={11} tickFormatter={(value) => formatAxisCount(Number(value))} />
                      <Tooltip labelFormatter={(value) => formatTrendDate(String(value))} formatter={(value) => formatNumber(Number(value))} />
                      <Area type="monotone" dataKey="count" name="Registrations" stroke="#164c93" fill="url(#registrationFill)" strokeWidth={2.5} />
                    </AreaChart>
                  </ResponsiveContainer>
                </ChartCard>
              </div>
              <div className="mt-5 rounded-2xl border border-[color:var(--gov-green)]/20 bg-[color:var(--gov-green)]/6 p-5 text-sm leading-6 text-[color:var(--gov-navy)]">
                <strong>Privacy boundary:</strong> this report intentionally contains aggregate counts only. Aadhaar, DigiLocker identifiers, names, mobile numbers, photos, and addresses are not part of the page or its API type.
              </div>
            </>
          )}
        </QueryState>
      )}
    </>
  )
}
