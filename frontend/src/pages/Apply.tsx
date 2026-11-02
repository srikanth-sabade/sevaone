import { useParams } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import { useEffect } from 'react'
import { api } from '../api'
import { applyToService } from '../applyFlow'
import { useT } from '../hooks/useT'
import { useSession } from '../store'

export function ApplyPage() {
  const { t } = useT()
  const { code } = useParams({ from: '/apply/$code' })
  const scheme = useQuery({ queryKey: ['scheme', code], queryFn: () => api.scheme(code) })
  const { me, loaded } = useSession()

  useEffect(() => {
    if (!loaded || !scheme.data) return
    void applyToService(scheme.data, me)
  }, [loaded, me, scheme.data])

  return (
    <div className="mx-auto flex min-h-[40vh] max-w-6xl items-center justify-center px-4 py-10">
      <div className="rounded-2xl border border-border bg-card px-6 py-5 text-sm text-muted-foreground shadow-sm">
        {t('applyOpeningPortal')}
      </div>
    </div>
  )
}
