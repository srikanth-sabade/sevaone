import { useQuery } from '@tanstack/react-query'
import { govAdminApi } from '../api'

export function useGovAdminMe() {
  return useQuery({ queryKey: ['gov-admin', 'me'], queryFn: govAdminApi.govAdminMe, retry: false })
}

export function useGovOverview(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'overview'], queryFn: govAdminApi.overview, enabled })
}

export function useGovDepartments(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'departments'], queryFn: govAdminApi.departments, enabled })
}

export function useGovDistricts(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'districts'], queryFn: govAdminApi.districts, enabled })
}

export function useGovDiscovery(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'discovery'], queryFn: govAdminApi.discovery, enabled })
}

export function useGovCitizens(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'citizens'], queryFn: govAdminApi.citizens, enabled })
}

export function useGovLifecycle(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'lifecycle'], queryFn: govAdminApi.lifecycle, enabled })
}

export function useGovSakalaSla(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'sakala-sla'], queryFn: govAdminApi.sakalaSla, enabled })
}

export function useGovApplicationAging(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'application-aging'], queryFn: govAdminApi.applicationAging, enabled })
}

export function useGovSlaTrend(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'sla-trend'], queryFn: govAdminApi.slaTrend, enabled })
}

export function useGovServiceUsage(params: { since?: string; until?: string; departmentCode?: string }, enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'service-usage', params], queryFn: () => govAdminApi.serviceUsage(params), enabled })
}

export function useGovApplicationFunnel(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'application-funnel'], queryFn: govAdminApi.applicationFunnel, enabled })
}

export function useGovApplicationDropoff(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'application-dropoff'], queryFn: govAdminApi.applicationDropoff, enabled })
}

export function useGovAdminList(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'admin-list'], queryFn: govAdminApi.listAdmins, enabled })
}

export function useGovAdminActivityLog(params: { limit?: number; action?: string }, enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'activity-log', params], queryFn: () => govAdminApi.activityLog(params), enabled })
}

export function useGovAdminSessions(id: string | null, enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'admin-sessions', id], queryFn: () => govAdminApi.listAdminSessions(id!), enabled: enabled && Boolean(id) })
}

export function useGovOperators(enabled: boolean, params?: { search?: string; isActive?: boolean; operatorType?: string }) {
  return useQuery({ queryKey: ['gov-admin', 'operators', params], queryFn: () => govAdminApi.listOperators(params), enabled })
}

export function useReferenceDepartments(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'reference-departments'], queryFn: govAdminApi.referenceDepartments, enabled })
}

export function useReferenceDistricts(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'reference-districts'], queryFn: govAdminApi.referenceDistricts, enabled })
}

export function useGovServiceNotice(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'service-notice'], queryFn: govAdminApi.getServiceNotice, enabled })
}

export function useGovServiceNoticeHistory(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'service-notice-history'], queryFn: govAdminApi.listServiceNoticeHistory, enabled })
}

export function useGovNotifications(enabled: boolean) {
  return useQuery({ queryKey: ['gov-admin', 'notifications'], queryFn: govAdminApi.listGovNotifications, enabled })
}

export function useGovHealth(enabled: boolean) {
  return useQuery({
    queryKey: ['gov-admin', 'health'],
    queryFn: govAdminApi.health,
    enabled,
    staleTime: 10_000,
    refetchInterval: 30_000,
    retry: false,
  })
}

export function useGovAiStatus(enabled: boolean) {
  return useQuery({
    queryKey: ['gov-admin', 'ai-status'],
    queryFn: govAdminApi.aiStatus,
    enabled,
    staleTime: 10_000,
    refetchInterval: 30_000,
    retry: false,
  })
}
