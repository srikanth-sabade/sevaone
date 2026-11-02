import { Link, useNavigate, useParams } from '@tanstack/react-router'
import { useEffect } from 'react'
import { findMockService, findMockServiceContext } from '../lib/catalogMock'
import { useT } from '../hooks/useT'

/**
 * Deep link `/{serviceCode}` (e.g. /111 for Income Certificate).
 * Resolves the CSV service code and opens it inside the catalog tree view.
 */
export function ServiceCodePage() {
  const { serviceCode } = useParams({ from: '/$serviceCode' })
  const navigate = useNavigate()
  const { t } = useT()
  const service = findMockService(serviceCode)
  const ctx = service ? findMockServiceContext(serviceCode) : undefined

  useEffect(() => {
    if (!service || !ctx) return
    void navigate({
      to: '/catalog',
      search: {
        dept: ctx.department.code,
        sub: ctx.subDepartment?.code,
        service: service.code,
      },
      replace: true,
    })
  }, [service, ctx, navigate])

  if (!service) {
    return (
      <div className="mx-auto max-w-lg px-4 py-20 text-center">
        <h1 className="text-xl font-bold text-[color:var(--gov-navy)]">{t('catalogServiceNotFound')}</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          {t('serviceCode')}: <span className="font-medium text-foreground">{serviceCode}</span>
        </p>
        <Link
          to="/catalog"
          search={{ dept: undefined, sub: undefined, service: undefined }}
          className="mt-6 inline-block text-sm font-medium text-[color:var(--gov-blue)] hover:underline"
        >
          {t('catalogBackToCatalog')}
        </Link>
      </div>
    )
  }

  return <div className="mx-auto max-w-lg px-4 py-20 text-center text-sm text-muted-foreground">…</div>
}
