import { Download } from 'lucide-react'
import { Bar, BarChart, CartesianGrid, Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { ChartCard, formatNumber, MetricCard, PageIntro, QueryState } from '../components/AnalyticsPrimitives'
import { Button } from '../components/ui/button'
import { useGovOverview } from '../hooks/useGovAdmin'
import { downloadCsv } from '../lib/csvExport'

const colors = ['#164c93', '#138808', '#e87722', '#b22222']

export function OverviewPage() {
  const query = useGovOverview(true)
  const data = query.data
  const exportCsv = () => data && downloadCsv('sevaone-overview.csv', [
    { registeredCitizens: data.registeredCitizens, profileCompletedCitizens: data.profileCompletedCitizens },
    ...data.applicationsByStatus.map((row) => ({ metric: 'applicationsByStatus', status: row.status, count: row.count })),
    ...data.applySource.map((row) => ({ metric: 'applySource', source: row.source, count: row.count })),
  ])
  return <><PageIntro eyebrow="Statewide pulse" title="Overview" description="A clear view of citizen reach, service demand, and the delivery pipeline across SevaOne." action={data && <Button variant="outline" size="sm" onClick={exportCsv}><Download className="h-3.5 w-3.5" />Export CSV</Button>} /><QueryState isLoading={query.isLoading} isError={query.isError}>{data && <>
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4"><MetricCard label="Registered citizens" value={formatNumber(data.registeredCitizens)} detail="Citizens with a SevaOne account" /><MetricCard label="Profiles completed" value={formatNumber(data.profileCompletedCitizens)} detail="Profiles ready for service access" tone="green" /><MetricCard label="Applications tracked" value={formatNumber(data.applicationsByStatus.reduce((sum, row) => sum + row.count, 0))} detail="Across the current delivery lifecycle" tone="navy" /><MetricCard label="AI-originated applies" value={`${Math.round((data.applySource.find((row) => row.source === 'ai')?.count ?? 0) / Math.max(1, data.applySource.reduce((sum, row) => sum + row.count, 0)) * 100)}%`} detail="Share of tracked apply intent" tone="saffron" /></div>
    <div className="mt-5 grid gap-5 xl:grid-cols-[1.3fr_0.7fr]"><ChartCard title="Application status" description="Current applications grouped by delivery outcome"><ResponsiveContainer width="100%" height="100%"><BarChart data={data.applicationsByStatus} layout="vertical" margin={{ left: 12, right: 20 }}><CartesianGrid strokeDasharray="3 3" horizontal={false} stroke="#e5e9ef" /><XAxis type="number" tickFormatter={(value) => `${Math.round(value / 1000000)}M`} axisLine={false} tickLine={false} fontSize={11} /><YAxis type="category" dataKey="status" axisLine={false} tickLine={false} width={72} fontSize={11} /><Tooltip formatter={(value) => formatNumber(Number(value))} /><Bar dataKey="count" fill="#164c93" radius={[0, 6, 6, 0]} barSize={28} /></BarChart></ResponsiveContainer></ChartCard><ChartCard title="Apply source mix" description="AI discovery compared with catalogue browsing"><ResponsiveContainer width="100%" height="100%"><PieChart><Pie data={data.applySource} dataKey="count" nameKey="source" innerRadius={68} outerRadius={98} paddingAngle={4}>{data.applySource.map((entry, index) => <Cell key={entry.source} fill={colors[index]} />)}</Pie><Tooltip formatter={(value) => formatNumber(Number(value))} /><Legend formatter={(value) => value === 'ai' ? 'AI search' : 'Catalogue'} /></PieChart></ResponsiveContainer></ChartCard></div>
  </>}</QueryState></>
}
