import { useQuery } from '@tanstack/react-query'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { api } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { useT } from '../hooks/useT'
import { departmentName } from '../lib/catalogKn'

export function KpiPage() {
  const { lang, t } = useT()
  const kpi = useQuery({ queryKey: ['kpi'], queryFn: () => api.kpi() })
  const d = kpi.data
  if (!d) return <div className="mx-auto max-w-6xl px-4 py-10 text-sm text-muted-foreground">{t('kpiLoading')}</div>
  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <p className="text-xs uppercase tracking-wide text-[color:var(--saffron)]">{t('kpiRestricted')}</p>
      <h1 className="text-3xl font-bold text-[color:var(--gov-navy)]">{t('kpiCockpit')}</h1>
      <Breadcrumbs />
      <div className="mt-6 grid gap-4 md:grid-cols-4">
        {[
          ['Received', d.received],
          ['Disposed', d.disposed],
          ['Pending', d.pending],
          ['Sakala %', d.sakalaPercent],
        ].map(([l, v]) => (
          <div key={String(l)} className="rounded-2xl border border-border bg-card p-4 shadow-sm">
            <div className="text-xs text-muted-foreground">{l}</div>
            <div className="text-2xl font-bold text-[color:var(--gov-navy)]">{v}</div>
          </div>
        ))}
      </div>
      <div className="mt-8 h-72 rounded-2xl border border-border bg-card p-4 shadow-sm">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={d.departments}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="departmentCode" hide />
            <YAxis />
            <Tooltip />
            <Bar dataKey="received" fill="oklch(0.24 0.05 278)" />
            <Bar dataKey="disposed" fill="oklch(0.66 0.14 168)" />
          </BarChart>
        </ResponsiveContainer>
      </div>
      <div className="mt-6 overflow-hidden rounded-2xl border border-border shadow-sm">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-[color:var(--gov-navy)] text-left text-white">
              <th className="p-3">{t('grvDepartment')}</th>
              <th className="p-3">{t('kpiReceived')}</th>
              <th className="p-3">{t('statusPending')}</th>
              <th className="p-3">{t('kpiSakalaPct')}</th>
              <th className="p-3">TAT</th>
            </tr>
          </thead>
          <tbody>
            {d.departments.map((row) => (
              <tr key={row.departmentCode} className="border-b border-border bg-card">
                <td className="p-3">{departmentName(lang, row.departmentCode, row.departmentName)}</td>
                <td className="p-3">{row.received}</td>
                <td className="p-3">{row.pending}</td>
                <td className="p-3">{row.sakalaPercent}</td>
                <td className="p-3">{row.averageTatDays}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
