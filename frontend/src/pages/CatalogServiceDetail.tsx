import { useNavigate, useParams } from '@tanstack/react-router'
import { useEffect } from 'react'
import { findMockServiceContext } from '../lib/catalogMock'

/**
 * /catalog/$code is a deep-link entrypoint only (e.g. an old bookmark or an external
 * link). It immediately redirects into the unified /catalog tree view so the service
 * opens inline next to the sidebar instead of as a bare standalone page — see
 * pages/Catalog.tsx, which owns the actual rendering via the `service` search param.
 */
export function CatalogServiceDetailPage() {
  const { code } = useParams({ from: '/catalog/$code' })
  const navigate = useNavigate()

  useEffect(() => {
    const ctx = findMockServiceContext(code)
    void navigate({
      to: '/catalog',
      search: { dept: ctx?.department.code, sub: ctx?.subDepartment?.code, service: code },
      replace: true,
    })
  }, [code, navigate])

  return null
}
