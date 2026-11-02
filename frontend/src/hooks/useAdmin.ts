import { useQuery } from '@tanstack/react-query'
import { api } from '../api'

/** Admin session probe -- same retry:false + "any error means signed out" idiom useCatalog's
 *  siblings (and OperatorPage) already use for their own persona-scoped /me endpoints. */
export function useAdminMe() {
  return useQuery({ queryKey: ['admin', 'me'], queryFn: api.adminMe, retry: false })
}

export function useAdminRoles(enabled: boolean) {
  return useQuery({ queryKey: ['admin', 'roles'], queryFn: api.adminRoles, enabled })
}

export function useAdminServiceVisibility(departmentCode: string | undefined, serviceCode: string | undefined) {
  return useQuery({
    queryKey: ['admin', 'visibility', departmentCode, serviceCode],
    queryFn: () => api.adminServiceVisibility(departmentCode!, serviceCode!),
    enabled: !!departmentCode && !!serviceCode,
  })
}

export function useAdminChangeLog(departmentCode: string | undefined, serviceCode: string | undefined, enabled: boolean) {
  return useQuery({
    queryKey: ['admin', 'change-log', departmentCode, serviceCode],
    queryFn: () => api.adminChangeLog(departmentCode, serviceCode),
    enabled,
  })
}

export function useAdminImportBatch(batchId: string | undefined) {
  return useQuery({
    queryKey: ['admin', 'import', batchId],
    queryFn: () => api.adminImportStatus(batchId!),
    enabled: !!batchId,
    // Ingestion validates synchronously today (StartImportAsync finishes before the upload
    // response returns), so this never actually needs to poll -- refetch is here for when a
    // future async validation step lands, at which point a "validating" batch should update
    // without the admin needing to refresh.
    refetchInterval: (q) => (q.state.data?.status === 'validating' ? 2000 : false),
  })
}

export function useAdminImportRows(batchId: string | undefined, action: string | undefined) {
  return useQuery({
    queryKey: ['admin', 'import', batchId, 'rows', action],
    queryFn: () => api.adminImportRows(batchId!, action),
    enabled: !!batchId,
  })
}
