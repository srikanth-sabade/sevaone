import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { useSession } from '../store'
import { useT } from '../hooks/useT'

export function GrievancePage() {
  const { t } = useT()
  const { me } = useSession()
  const [ref, setRef] = useState<string | null>(null)
  const [form, setForm] = useState({
    name: me?.name ?? '',
    mobile: me?.mobile ?? '',
    departmentCode: 'revenue',
    details: '',
  })
  const mine = useQuery({
    queryKey: ['grievances', me?.id],
    queryFn: () => api.grievances(),
    enabled: Boolean(me),
  })

  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="text-3xl font-bold text-[color:var(--gov-navy)]">{t('grvTitle')}</h1>
      <Breadcrumbs />
      <p className="mt-3 text-sm text-muted-foreground">{t('grvHint')}</p>
      <form
        className="mt-6 space-y-3 rounded-2xl border border-border bg-card p-6 shadow-sm"
        onSubmit={(e) => {
          e.preventDefault()
          void api.submitGrievance(form).then((g) => {
            setRef(g.reference)
            void mine.refetch()
          })
        }}
      >
        <label className="block text-sm">
          {t('grvName')}
          <input
            required
            className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
          />
        </label>
        <label className="block text-sm">
          {t('grvMobile')}
          <input
            required
            maxLength={10}
            className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
            value={form.mobile}
            onChange={(e) => setForm({ ...form, mobile: e.target.value })}
          />
        </label>
        <label className="block text-sm">
          {t('grvDepartment')}
          <select
            className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
            value={form.departmentCode}
            onChange={(e) => setForm({ ...form, departmentCode: e.target.value })}
          >
            <option value="revenue">{t('grvDeptRevenue')}</option>
            <option value="transport">{t('grvDeptTransport')}</option>
            <option value="food">{t('grvDeptFood')}</option>
            <option value="urban">{t('grvDeptUrban')}</option>
          </select>
        </label>
        <label className="block text-sm">
          {t('grvDetails')}
          <textarea
            required
            className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2"
            rows={5}
            value={form.details}
            onChange={(e) => setForm({ ...form, details: e.target.value })}
          />
        </label>
        <button className="rounded-lg bg-[color:var(--gov-blue)] px-4 py-2 text-sm font-medium text-white" type="submit">
          {t('grvSubmit')}
        </button>
      </form>
      {ref && <p className="mt-4 text-sm font-medium text-[color:var(--gov-green)]">Registered — {ref}</p>}

      {me && (
        <div className="mt-10">
          <h2 className="text-lg font-semibold text-[color:var(--gov-navy)]">{t('grvMyGrievances')}</h2>
          {mine.isLoading && <p className="mt-2 text-sm text-muted-foreground">{t('svcLoading')}</p>}
          {mine.isError && (
            <p className="mt-2 text-sm text-red-700">{(mine.error as Error).message}</p>
          )}
          <ul className="mt-3 space-y-2">
            {(mine.data ?? []).map((g) => (
              <li key={g.reference} className="rounded-xl border border-border bg-card p-4 text-sm">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span className="font-medium">{g.reference}</span>
                  <span className="text-xs text-muted-foreground">{g.status}</span>
                </div>
                <p className="mt-1 text-muted-foreground">{g.departmentCode}</p>
                <p className="mt-1">{g.details}</p>
                <p className="mt-1 text-xs text-muted-foreground">{new Date(g.createdAt).toLocaleString()}</p>
              </li>
            ))}
            {mine.data && mine.data.length === 0 && (
              <p className="text-sm text-muted-foreground">{t('grvNone')}</p>
            )}
          </ul>
        </div>
      )}
    </div>
  )
}
