import { Download } from 'lucide-react'
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { ChartCard, formatNumber, MetricCard, PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { Button } from '../components/ui/button'
import { useGovLifecycle } from '../hooks/useGovAdmin'
import { downloadCsv } from '../lib/csvExport'

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

export function LifecyclePage() {
  const query = useGovLifecycle(true)
  const data = query.data
  const get = (name: string) => data?.byStatus.find((row) => row.status.toLowerCase() === name.toLowerCase())?.count ?? 0
  const exportCsv = () => data && downloadCsv('sevaone-lifecycle.csv', data.trend.map((row) => ({ date: row.date, submitted: row.submitted, approved: row.approved, rejected: row.rejected })))
  return <><PageIntro eyebrow="Delivery pipeline" title="Service lifecycle" description="Follow applications from submission through completed, pending, and rejected outcomes." action={data && <Button variant="outline" size="sm" onClick={exportCsv}><Download className="h-3.5 w-3.5" />Export CSV</Button>} /><QueryState isLoading={query.isLoading} isError={query.isError}>{data && <><div className="grid gap-4 sm:grid-cols-3"><MetricCard label="Completed" value={formatNumber(get('Completed'))} detail="Applications reaching completion" tone="green" /><MetricCard label="Pending" value={formatNumber(get('Pending'))} detail="Applications still in progress" tone="saffron" /><MetricCard label="Rejected" value={formatNumber(get('Rejected'))} detail="Applications not approved" tone="blue" /></div><div className="mt-5"><ChartCard title="Lifecycle trend" description="Daily submitted volume over the last 90 days, alongside approved and rejected outcomes"><ResponsiveContainer width="100%" height="100%"><AreaChart data={data.trend} margin={{ right: 12, bottom: 8 }}><defs><linearGradient id="submittedFill" x1="0" y1="0" x2="0" y2="1"><stop offset="5%" stopColor="#164c93" stopOpacity={0.22} /><stop offset="95%" stopColor="#164c93" stopOpacity={0} /></linearGradient></defs><CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e9ef" /><XAxis dataKey="date" axisLine={false} tickLine={false} tickMargin={8} minTickGap={28} height={34} fontSize={11} tickFormatter={(value) => formatTrendDate(String(value))} /><YAxis axisLine={false} tickLine={false} width={44} allowDecimals={false} fontSize={11} tickFormatter={(value) => formatAxisCount(Number(value))} /><Tooltip labelFormatter={(value) => formatTrendDate(String(value))} formatter={(value) => formatNumber(Number(value))} /><Area type="monotone" dataKey="submitted" name="Submitted" stroke="#164c93" fill="url(#submittedFill)" strokeWidth={2.5} /><Area type="monotone" dataKey="approved" name="Approved" stroke="#138808" fill="none" strokeWidth={2} /><Area type="monotone" dataKey="rejected" name="Rejected" stroke="#e87722" fill="none" strokeWidth={2} /></AreaChart></ResponsiveContainer></ChartCard></div></>}</QueryState></>
}
