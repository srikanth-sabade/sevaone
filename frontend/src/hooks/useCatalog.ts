import { useQuery } from '@tanstack/react-query'
import { api } from '../api'

/** Landing grid data. */
export function useCatalogDepartments() {
  return useQuery({ queryKey: ['catalog', 'departments'], queryFn: api.catalogDepartments })
}

/** A department page: sub-department rail + every one of its services in one call. */
export function useCatalogDepartment(code: string | undefined) {
  return useQuery({
    queryKey: ['catalog', 'department', code],
    queryFn: () => api.catalogDepartment(code!),
    enabled: !!code,
  })
}

/** Direct service lookup -- used for a bookmarked/legacy deep link that only names a service, not its department (rare; the department page's own service list is what normal navigation uses). */
export function useCatalogService(departmentCode: string | undefined, serviceCode: string | undefined) {
  return useQuery({
    queryKey: ['catalog', 'service', departmentCode, serviceCode],
    queryFn: () => api.catalogService(departmentCode!, serviceCode!),
    enabled: !!departmentCode && !!serviceCode,
  })
}

/** Landing-page global search -- server-side, since not every department's services are loaded client-side (unlike the old static-file catalogue, which held all 754 in memory at once). */
export function useCatalogSearch(query: string) {
  const trimmed = query.trim()
  return useQuery({
    queryKey: ['catalog', 'search', trimmed],
    queryFn: () => api.catalogSearch(trimmed),
    enabled: trimmed.length > 0,
  })
}
