import { useQuery } from '@tanstack/react-query'
import { govAdminApi } from '../api'

export function useCatalogAdminDepartments(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'catalog', 'departments'], queryFn: govAdminApi.catalogDepartments, enabled })
}

export function useCatalogAdminServices(params: { departmentCode?: string; search?: string; isActive?: boolean }, enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'catalog', 'services', params], queryFn: () => govAdminApi.catalogServices(params), enabled, staleTime: 15_000 })
}

export function useCatalogAdminSubDepartments(params: { departmentCode?: string; search?: string; isActive?: boolean }, enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'catalog', 'sub-departments', params], queryFn: () => govAdminApi.catalogSubDepartments(params), enabled, staleTime: 15_000 })
}

export function useCatalogAdminChanges(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'catalog', 'changes'], queryFn: () => govAdminApi.catalogChanges(), enabled })
}
