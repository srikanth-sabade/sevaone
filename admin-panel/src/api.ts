import type {
  ApplicationAgingAnalytics,
  ApplicationDropoffAnalytics,
  ApplicationFunnelAnalytics,
  CitizensAnalytics,
  DepartmentAnalytics,
  DiscoveryAnalytics,
  DistrictAnalytics,
  GovAdminActivityLogEntry,
  GovAdminListItem,
  GovAdminSession,
  GovAdminImportBatch,
  GovAdminImportRow,
  GovAdminImportValidationRow,
  OperatorAdminListItem,
  GovAdminMe,
  GovAdminTier,
  GovHealth,
  CatalogAdminChange,
  CatalogAdminDepartment,
  CatalogAdminSubDepartment,
  CatalogAdminService,
  CatalogAdminServiceListItem,
  CatalogAdminServiceWriteRequest,
  CatalogImportBatch,
  CatalogImportFormat,
  CatalogImportRow,
  CatalogImportValidationRow,
  GovAiStatus,
  GovAiAssistantResponse,
  AiReingestResult,
  GovNotification,
  LifecycleAnalytics,
  OverviewAnalytics,
  SakalaSlaAnalytics,
  ReferenceDepartment,
  ReferenceDistrict,
  ServiceNotice,
  ServiceNoticeHistory,
  ServiceNoticeSeverity,
  ServiceUsageAnalytics,
  SlaTrendAnalytics,
} from './types'

const useMockApi = import.meta.env.DEV && import.meta.env.VITE_MOCK_API !== 'false'
let mockAuthenticated = false
let mockOperators: OperatorAdminListItem[] = [
  { id: 'operator-1', operatorCode: 'G1-BLR-001', operatorType: 'G1', operatorName: 'Bengaluru Grama One', mobileNo: '98******21', isClaimed: true, isActive: true, createdAt: '2026-08-14T06:30:00Z' },
  { id: 'operator-2', operatorCode: 'K1-MYS-014', operatorType: 'K1', operatorName: 'Mysuru Karnataka One', mobileNo: null, isClaimed: false, isActive: true, createdAt: '2026-08-12T10:20:00Z' },
]
let mockSessions: GovAdminSession[] = [{ sessionId: 'demo-session', govAdminId: '1', displayName: 'Demo Government Administrator', tier: 'Superadmin', expiresAt: new Date(Date.now() + 7 * 3600000).toISOString(), isCurrent: true }]
const mockAdminImports: Record<string, { batch: GovAdminImportBatch; rows: GovAdminImportValidationRow[] }> = {}
let mockServiceNotice: ServiceNotice | null = null
let mockServiceNoticeHistory: ServiceNoticeHistory[] = []
let mockGovNotifications: GovNotification[] = [
  { id: 'gov-notice-1', title: 'Welcome to SevaOne notifications', body: 'Government departments can now share important service updates, deadlines, and citizen guidance directly through SevaOne.', isActive: true, createdAt: '2026-09-10T08:30:00Z', updatedAt: '2026-09-10T08:30:00Z' },
]
let mockCatalogDepartments: CatalogAdminDepartment[] = [
  { id: 'dept-1', code: 'REV', name: 'Revenue Department', departmentNameKannada: 'ಕಂದಾಯ ಇಲಾಖೆ', departmentUrl: 'https://revenue.karnataka.gov.in', departmentUrlKannada: null, displayOrder: 1, isActive: true, serviceCount: 2 },
  { id: 'dept-2', code: 'HEALTH', name: 'Health and Family Welfare', departmentNameKannada: 'ಆರೋಗ್ಯ ಮತ್ತು ಕುಟುಂಬ ಕಲ್ಯಾಣ ಇಲಾಖೆ', departmentUrl: 'https://health.karnataka.gov.in', departmentUrlKannada: null, displayOrder: 2, isActive: true, serviceCount: 1 },
  { id: 'dept-3', code: 'OLD', name: 'Archived Department', departmentNameKannada: null, departmentUrl: null, departmentUrlKannada: null, displayOrder: 9, isActive: false, serviceCount: 0 },
]
let mockCatalogServices: CatalogAdminService[] = [
  { id: 'svc-1', departmentCode: 'REV', departmentName: 'Revenue Department', serviceCode: 'REV-001', name: 'Income Certificate', nameKannada: 'ಆದಾಯ ಪ್ರಮಾಣಪತ್ರ', subDepartmentName: null, shortDescription: 'Apply for an income certificate.', shortDescriptionKannada: null, description: 'A certificate issued by the Revenue Department.', descriptionKannada: null, isSakalaEnabled: true, sakalaLabelRaw: 'Sakala', sakalaDays: 15, departmentFee: 0, serviceCharge: 0, documents: ['Identity proof'], documentsKannada: [], eligibility: ['Residents of Karnataka'], eligibilityKannada: [], aliases: ['Income certificate'], steps: ['Submit application', 'Track status'], stepsKannada: [], jurisdiction: 'State', category: 'Certificates', modeOfService: 'Online', departmentServiceId: 'INC-01', portal: null, serviceUrl: 'https://revenue.karnataka.gov.in/income', deeplinkCitizenUrl: null, deeplinkCitizenUrlKannada: null, deeplinkOperatorUrl: null, homepageUrl: 'https://revenue.karnataka.gov.in', integrationMethod: null, sevasindhuServiceId: null, remarks: null, isActive: true, version: 3, visibleToRoles: ['citizen', 'operator'] },
  { id: 'svc-2', departmentCode: 'REV', departmentName: 'Revenue Department', serviceCode: 'REV-002', name: 'Caste Certificate', nameKannada: null, subDepartmentName: 'Revenue Services', shortDescription: null, shortDescriptionKannada: null, description: null, descriptionKannada: null, isSakalaEnabled: true, sakalaLabelRaw: null, sakalaDays: 21, departmentFee: 0, serviceCharge: 0, documents: [], documentsKannada: [], eligibility: [], eligibilityKannada: [], aliases: [], steps: [], stepsKannada: [], jurisdiction: 'State', category: 'Certificates', modeOfService: 'Both', departmentServiceId: null, portal: null, serviceUrl: null, deeplinkCitizenUrl: null, deeplinkCitizenUrlKannada: null, deeplinkOperatorUrl: null, homepageUrl: null, integrationMethod: null, sevasindhuServiceId: null, remarks: null, isActive: false, version: 1, visibleToRoles: [] },
  { id: 'svc-3', departmentCode: 'HEALTH', departmentName: 'Health and Family Welfare', serviceCode: 'HEALTH-001', name: 'Health Card', nameKannada: null, subDepartmentName: 'Public Health', shortDescription: 'Request a state health card.', shortDescriptionKannada: null, description: null, descriptionKannada: null, isSakalaEnabled: false, sakalaLabelRaw: null, sakalaDays: 0, departmentFee: null, serviceCharge: null, documents: [], documentsKannada: [], eligibility: [], eligibilityKannada: [], aliases: [], steps: [], stepsKannada: [], jurisdiction: 'State', category: 'Health', modeOfService: 'Online', departmentServiceId: null, portal: null, serviceUrl: null, deeplinkCitizenUrl: null, deeplinkCitizenUrlKannada: null, deeplinkOperatorUrl: null, homepageUrl: null, integrationMethod: null, sevasindhuServiceId: null, remarks: null, isActive: true, version: 1, visibleToRoles: ['citizen'] },
]
let mockCatalogSubDepartments: CatalogAdminSubDepartment[] = [
  { id: 'sub-1', departmentCode: 'REV', departmentName: 'Revenue Department', name: 'Revenue Services', nameKannada: 'ಕಂದಾಯ ಸೇವೆಗಳು', displayOrder: 1, isActive: true },
  { id: 'sub-2', departmentCode: 'HEALTH', departmentName: 'Health and Family Welfare', name: 'Public Health', nameKannada: null, displayOrder: 1, isActive: true },
]
let mockCatalogChanges: CatalogAdminChange[] = []
const mockCatalogImports: Record<string, { batch: CatalogImportBatch; rows: CatalogImportRow[]; validationRows: CatalogImportValidationRow[] }> = {}
const mockActivityLog: GovAdminActivityLogEntry[] = [
  { id: 'act-1', actorUsername: 'demo-superadmin', actorDisplayName: 'Demo Government Administrator', action: 'login', target: null, detail: null, at: new Date(Date.now() - 3600000).toISOString() },
  { id: 'act-2', actorUsername: 'demo-superadmin', actorDisplayName: 'Demo Government Administrator', action: 'admin.create', target: 'sevaone-govadmin-district-seed', detail: 'tier=DistrictHead', at: new Date(Date.now() - 7200000).toISOString() },
]
let mockHealth: GovHealth = {
  status: 'Healthy',
  checkedAt: new Date().toISOString(),
  checks: [
    { name: 'postgres', status: 'Healthy', description: 'Postgres connection accepted.', durationMs: 8.4 },
    { name: 'redis', status: 'Healthy', description: 'Ping 2ms.', durationMs: 3.1 },
    { name: 'kafka', status: 'Healthy', description: '1 broker reachable.', durationMs: 24.6 },
    { name: 'wso2apim', status: 'Healthy', description: 'WSO2 APIM responded 200.', durationMs: 41.8 },
    { name: 'wso2is', status: 'Healthy', description: 'WSO2 IS responded 200.', durationMs: 38.2 },
  ],
  node: {
    machineName: 'sevaone-bff-1',
    runtime: '.NET 10.0.0',
    processId: 1,
    processorCount: 4,
    uptimeSeconds: 86400,
    workingSetBytes: 156_000_000,
    privateMemoryBytes: 248_000_000,
    managedHeapBytes: 72_000_000,
  },
}
let mockAiStatus: GovAiStatus = {
  status: 'Healthy',
  checkedAt: new Date().toISOString(),
  health: { status: 'ok', service: 'SevaOne AI Discovery Backend' },
  readiness: { status: 'ready', activeKnowledgeSource: 'matched_combined', serviceIndexPrefix: 'service_matching_matched114', totalServicesIndexed: 126, embeddingDimension: 1024 },
  telemetry: {
    collectedAt: new Date().toISOString(),
    host: 'gpu-node-01',
    cpu: { usagePercent: 18.4, load1: 1.42, cores: 64 },
    memory: { totalBytes: 2199023255552, usedBytes: 197568495616, availableBytes: 1988654761984, usagePercent: 9.0 },
    disk: { mount: '/', totalBytes: 1979120929996, usedBytes: 439804651110, availableBytes: 1539316278886, usagePercent: 22.2 },
    gpus: [{ name: 'NVIDIA H100 80GB HBM3', utilizationPercent: 0, memoryTotalBytes: 85410824192, memoryUsedBytes: 69600000000, memoryUsagePercent: 81.5, temperatureCelsius: 31 }],
  },
  telemetryNote: 'Read-only telemetry collected by the separate GPU-host ai-monitor sidecar.',
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...init?.headers },
    ...init,
  })

  if (!response.ok) {
    let message = `Request failed (${response.status})`
    try {
      const body = (await response.json()) as { message?: string; title?: string }
      message = body.message ?? body.title ?? message
    } catch {
      // Preserve the useful HTTP status when the server did not return JSON.
    }
    throw new Error(message)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

function mockMe(): GovAdminMe {
  if (!mockAuthenticated) throw new Error('Not authenticated')
  return {
    displayName: 'Demo Government Administrator',
    tier: 'Superadmin',
    assignedDistrict: null,
    assignedTaluk: null,
    assignedDepartment: null,
  }
}

const mockAdmins: GovAdminListItem[] = [
  { id: '1', username: 'sevaone-govadmin-superadmin-seed', displayName: 'Seed Superadmin', tier: 'Superadmin', assignedDistrict: null, assignedTaluk: null, assignedDepartment: null, isActive: true, claimed: true, createdAt: '2026-09-01T00:00:00Z' },
  { id: '2', username: 'sevaone-govadmin-cm-seed', displayName: 'Seed Chief Minister', tier: 'ChiefMinister', assignedDistrict: null, assignedTaluk: null, assignedDepartment: null, isActive: true, claimed: true, createdAt: '2026-09-01T00:00:00Z' },
  { id: '3', username: 'sevaone-govadmin-health', displayName: 'Health Department Admin', tier: 'Department', assignedDistrict: null, assignedTaluk: null, assignedDepartment: 'health', isActive: true, claimed: false, createdAt: '2026-09-02T00:00:00Z' },
]

const mockDepartmentOptions: ReferenceDepartment[] = [
  { code: 'health', name: 'Health & Family Welfare' },
  { code: 'revenue', name: 'Revenue Department' },
  { code: 'transport', name: 'Transport Department' },
]

const mockDistrictOptions: ReferenceDistrict[] = [
  { district: 'Bengaluru Urban', taluks: ['Bengaluru', 'Kengeri', 'Anekal', 'Yelahanka'] },
  { district: 'Kalaburagi', taluks: ['Kalaburagi', 'Jevargi', 'Sedam', 'Chincholi'] },
  { district: 'Mysuru', taluks: ['Mysuru', 'Hunasuru', 'Nanjanagodu'] },
]

const mockOverview: OverviewAnalytics = {
  registeredCitizens: 4136209,
  profileCompletedCitizens: 3545879,
  applicationsByStatus: [
    { status: 'Completed', count: 11857073 },
    { status: 'Pending', count: 1714275 },
    { status: 'Rejected', count: 714282 },
  ],
  applySource: [
    { source: 'ai', count: 5842110 },
    { source: 'catalogue', count: 8443520 },
  ],
}

const mockDepartments: DepartmentAnalytics = {
  departmentCount: 28,
  activeServiceCount: 412,
  byDepartment: [
    { departmentCode: 'REV', departmentName: 'Revenue Department', serviceCount: 86 },
    { departmentCode: 'HEALTH', departmentName: 'Health and Family Welfare', serviceCount: 64 },
    { departmentCode: 'RDPR', departmentName: 'Rural Development and Panchayat Raj', serviceCount: 58 },
    { departmentCode: 'TRANSPORT', departmentName: 'Transport Department', serviceCount: 42 },
    { departmentCode: 'WCD', departmentName: 'Women and Child Development', serviceCount: 36 },
    { departmentCode: 'FOOD', departmentName: 'Food and Civil Supplies', serviceCount: 31 },
  ],
}

const mockDistricts: DistrictAnalytics = {
  scope: 'statewide',
  rows: [
    { district: 'Bengaluru Urban', taluk: 'Bengaluru North', applicationCount: 3551388, citizenCount: 592451, submittedCount: 355138, inProgressCount: 887847, completedCount: 2166847, rejectedCount: 141556 },
    { district: 'Belagavi', taluk: 'Belagavi', applicationCount: 751519, citizenCount: 271153, submittedCount: 82667, inProgressCount: 187880, completedCount: 451911, rejectedCount: 29061 },
    { district: 'Mysuru', taluk: 'Mysuru', applicationCount: 628823, citizenCount: 223794, submittedCount: 62882, inProgressCount: 157206, completedCount: 383182, rejectedCount: 25553 },
    { district: 'Tumakuru', taluk: 'Tumakuru', applicationCount: 475451, citizenCount: 170864, submittedCount: 52300, inProgressCount: 118863, completedCount: 285270, rejectedCount: 19018 },
    { district: 'Kalaburagi', taluk: 'Kalaburagi', applicationCount: 452446, citizenCount: 168078, submittedCount: 49769, inProgressCount: 113112, completedCount: 271468, rejectedCount: 18097 },
    { district: 'Ballari', taluk: 'Ballari', applicationCount: 401501, citizenCount: 158460, submittedCount: 44165, inProgressCount: 100375, completedCount: 240901, rejectedCount: 16060 },
  ],
}

const mockDiscovery: DiscoveryAnalytics = {
  totalApplyClicks: 14285630,
  aiApplyClicks: 5842110,
  catalogueApplyClicks: 8443520,
  byDepartment: [
    { departmentCode: 'REV', aiCount: 1131900, catalogueCount: 1318100 },
    { departmentCode: 'HEALTH', aiCount: 731240, catalogueCount: 1028650 },
    { departmentCode: 'RDPR', aiCount: 618900, catalogueCount: 901220 },
    { departmentCode: 'TRANSPORT', aiCount: 526300, catalogueCount: 812440 },
    { departmentCode: 'WCD', aiCount: 414800, catalogueCount: 612120 },
  ],
}

const mockCitizens: CitizensAnalytics = {
  totalRegistered: 4136209,
  profileCompleted: 3545879,
  profileIncomplete: 590330,
  registrationTrend: [
    { date: '2026-04', count: 428110 },
    { date: '2026-05', count: 511240 },
    { date: '2026-06', count: 602880 },
    { date: '2026-07', count: 715420 },
    { date: '2026-08', count: 784119 },
  ],
}

const mockLifecycle: LifecycleAnalytics = {
  byStatus: mockOverview.applicationsByStatus,
  trend: [
    { date: '2026-04', submitted: 2291000, approved: 1903000, rejected: 112000 },
    { date: '2026-05', submitted: 2534000, approved: 2115000, rejected: 124000 },
    { date: '2026-06', submitted: 2817000, approved: 2341000, rejected: 141000 },
    { date: '2026-07', submitted: 3105000, approved: 2598000, rejected: 158000 },
    { date: '2026-08', submitted: 3321000, approved: 2776000, rejected: 179282 },
  ],
}

const mockSakalaSla: SakalaSlaAnalytics = {
  scope: 'statewide',
  byDepartment: [
    { departmentCode: 'REV', departmentName: 'Revenue Department', totalSakalaApplications: 182400, completedOnTime: 148200, completedLate: 21600, pendingWithinSla: 9800, pendingOverdue: 2800 },
    { departmentCode: 'HEALTH', departmentName: 'Health and Family Welfare', totalSakalaApplications: 96500, completedOnTime: 81200, completedLate: 8900, pendingWithinSla: 5100, pendingOverdue: 1300 },
    { departmentCode: 'RDPR', departmentName: 'Rural Development and Panchayat Raj', totalSakalaApplications: 64200, completedOnTime: 51800, completedLate: 7400, pendingWithinSla: 3900, pendingOverdue: 1100 },
    { departmentCode: 'TRANSPORT', departmentName: 'Transport Department', totalSakalaApplications: 58300, completedOnTime: 49100, completedLate: 5200, pendingWithinSla: 3100, pendingOverdue: 900 },
  ],
}

const mockApplicationAging: ApplicationAgingAnalytics = {
  scope: 'statewide',
  byDepartment: [
    { departmentCode: 'REV', departmentName: 'Revenue Department', openApplications: 12600, dueWithin24h: 340, age0To3Days: 6200, age4To7Days: 3100, age8To14Days: 1900, age15To30Days: 1100, age30PlusDays: 300, avgProcessingHours: 118.4 },
    { departmentCode: 'HEALTH', departmentName: 'Health and Family Welfare', openApplications: 5400, dueWithin24h: 180, age0To3Days: 2900, age4To7Days: 1400, age8To14Days: 700, age15To30Days: 320, age30PlusDays: 80, avgProcessingHours: 96.2 },
    { departmentCode: 'RDPR', departmentName: 'Rural Development and Panchayat Raj', openApplications: 3800, dueWithin24h: 110, age0To3Days: 2000, age4To7Days: 980, age8To14Days: 520, age15To30Days: 240, age30PlusDays: 60, avgProcessingHours: 142.7 },
    { departmentCode: 'TRANSPORT', departmentName: 'Transport Department', openApplications: 2900, dueWithin24h: 90, age0To3Days: 1600, age4To7Days: 720, age8To14Days: 380, age15To30Days: 160, age30PlusDays: 40, avgProcessingHours: 84.5 },
  ],
}

const mockSlaTrend: SlaTrendAnalytics = {
  scope: 'statewide',
  trend: [
    { periodStart: '2026-06-22', total: 18400, completedOnTime: 15600, completedLate: 2800 },
    { periodStart: '2026-06-29', total: 19100, completedOnTime: 16300, completedLate: 2800 },
    { periodStart: '2026-07-06', total: 20200, completedOnTime: 17500, completedLate: 2700 },
    { periodStart: '2026-07-13', total: 21000, completedOnTime: 18400, completedLate: 2600 },
    { periodStart: '2026-07-20', total: 20700, completedOnTime: 18200, completedLate: 2500 },
    { periodStart: '2026-07-27', total: 21500, completedOnTime: 19100, completedLate: 2400 },
  ],
}

const mockServiceUsage: ServiceUsageAnalytics = {
  scope: 'statewide',
  services: [
    { serviceCode: 'REV-001', serviceName: 'Income Certificate', departmentCode: 'REV', departmentName: 'Revenue Department', totalApplyClicks: 18420, aiApplyClicks: 7920, catalogueApplyClicks: 10500, lastUsedAt: '2026-09-16T14:20:00Z' },
    { serviceCode: 'HEALTH-001', serviceName: 'Health Card', departmentCode: 'HEALTH', departmentName: 'Health and Family Welfare', totalApplyClicks: 12180, aiApplyClicks: 6180, catalogueApplyClicks: 6000, lastUsedAt: '2026-09-16T13:05:00Z' },
    { serviceCode: 'REV-002', serviceName: 'Caste Certificate', departmentCode: 'REV', departmentName: 'Revenue Department', totalApplyClicks: 0, aiApplyClicks: 0, catalogueApplyClicks: 0, lastUsedAt: null },
  ],
}

const mockApplicationFunnel: ApplicationFunnelAnalytics = {
  scope: 'statewide', registeredCitizens: mockCitizens.totalRegistered, profileCompletedCitizens: mockCitizens.profileCompleted,
  citizensSubmitted: 2489100, citizensCompleted: 1734200,
}

const mockApplicationDropoff: ApplicationDropoffAnalytics = {
  scope: 'statewide', byStatus: [
    { status: 'Submitted', openCount: 184200, avgTimeInStatusHours: 18.4 },
    { status: 'DocumentVerification', openCount: 92500, avgTimeInStatusHours: 42.7 },
    { status: 'UnderReview', openCount: 38200, avgTimeInStatusHours: 76.1 },
  ],
}

async function mockRequest<T>(value: T): Promise<T> {
  await new Promise((resolve) => window.setTimeout(resolve, 180))
  if (!mockAuthenticated) throw new Error('Not authenticated')
  return value
}

export const govAdminApi = {
  govAdminMe: () => (useMockApi ? mockRequest(mockMe()) : request<GovAdminMe>('/api/v1/admin/gov/auth/me')),
  govAdminLogin: async (username: string, password: string) => {
    if (useMockApi) {
      await new Promise((resolve) => window.setTimeout(resolve, 260))
      if (!username.trim() || !password) throw new Error('Invalid username or password.')
      mockAuthenticated = true
      return { displayName: 'Demo Government Administrator', tier: 'Superadmin' as GovAdminTier }
    }
    return request<{ displayName: string; tier: GovAdminTier }>('/api/v1/admin/gov/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    })
  },
  govAdminLogout: async () => {
    if (useMockApi) {
      mockAuthenticated = false
      return
    }
    await request<void>('/api/v1/admin/gov/auth/logout', { method: 'POST' })
  },
  overview: () => (useMockApi ? mockRequest(mockOverview) : request<OverviewAnalytics>('/api/v1/admin/gov/analytics/overview')),
  departments: () => (useMockApi ? mockRequest(mockDepartments) : request<DepartmentAnalytics>('/api/v1/admin/gov/analytics/departments')),
  districts: () => (useMockApi ? mockRequest(mockDistricts) : request<DistrictAnalytics>('/api/v1/admin/gov/analytics/districts')),
  discovery: () => (useMockApi ? mockRequest(mockDiscovery) : request<DiscoveryAnalytics>('/api/v1/admin/gov/analytics/discovery')),
  citizens: () => (useMockApi ? mockRequest(mockCitizens) : request<CitizensAnalytics>('/api/v1/admin/gov/analytics/citizens')),
  lifecycle: () => (useMockApi ? mockRequest(mockLifecycle) : request<LifecycleAnalytics>('/api/v1/admin/gov/analytics/lifecycle')),
  sakalaSla: () => (useMockApi ? mockRequest(mockSakalaSla) : request<SakalaSlaAnalytics>('/api/v1/admin/gov/analytics/sakala-sla')),
  applicationAging: () => (useMockApi ? mockRequest(mockApplicationAging) : request<ApplicationAgingAnalytics>('/api/v1/admin/gov/analytics/application-aging')),
  slaTrend: () => (useMockApi ? mockRequest(mockSlaTrend) : request<SlaTrendAnalytics>('/api/v1/admin/gov/analytics/sla-trend')),
  serviceUsage: (params?: { since?: string; until?: string; departmentCode?: string }) => {
    if (useMockApi) {
      let services = mockServiceUsage.services
      if (params?.departmentCode) services = services.filter((row) => row.departmentCode === params.departmentCode)
      return mockRequest({ ...mockServiceUsage, services })
    }
    const query = new URLSearchParams()
    if (params?.since) query.set('since', params.since)
    if (params?.until) query.set('until', params.until)
    if (params?.departmentCode) query.set('departmentCode', params.departmentCode)
    return request<ServiceUsageAnalytics>(`/api/v1/admin/gov/analytics/service-usage${query.toString() ? `?${query}` : ''}`)
  },
  applicationFunnel: () => useMockApi ? mockRequest(mockApplicationFunnel) : request<ApplicationFunnelAnalytics>('/api/v1/admin/gov/analytics/application-funnel'),
  applicationDropoff: () => useMockApi ? mockRequest(mockApplicationDropoff) : request<ApplicationDropoffAnalytics>('/api/v1/admin/gov/analytics/application-dropoff'),

  listAdmins: () => (useMockApi ? mockRequest(mockAdmins) : request<GovAdminListItem[]>('/api/v1/admin/gov/management/admins')),
  createAdmin: (body: { username: string; displayName: string; tier: GovAdminTier; assignedDistrict: string | null; assignedTaluk: string | null; assignedDepartment: string | null }) =>
    useMockApi
      ? mockRequest({ id: crypto.randomUUID(), username: body.username })
      : request<{ id: string; username: string }>('/api/v1/admin/gov/management/admins', { method: 'POST', body: JSON.stringify(body) }),
  setAdminActive: (id: string, isActive: boolean) =>
    useMockApi ? mockRequest(undefined) : request<void>(`/api/v1/admin/gov/management/admins/${id}/active`, { method: 'PATCH', body: JSON.stringify({ isActive }) }),
  listAdminSessions: (id: string) => useMockApi ? mockRequest(mockSessions.filter((session) => session.govAdminId === id)) : request<GovAdminSession[]>(`/api/v1/admin/gov/management/admins/${id}/sessions`),
  forceLogoutAdminSession: (id: string, sessionId: string) => {
    if (useMockApi) { mockSessions = mockSessions.filter((session) => !(session.govAdminId === id && session.sessionId === sessionId)); return mockRequest(undefined) }
    return request<void>(`/api/v1/admin/gov/management/admins/${id}/sessions/${encodeURIComponent(sessionId)}`, { method: 'DELETE' })
  },
  listOperators: (params?: { search?: string; isActive?: boolean; operatorType?: string }) => {
    if (useMockApi) {
      let rows = mockOperators
      if (params?.search) { const q = params.search.toLowerCase(); rows = rows.filter((row) => `${row.operatorCode} ${row.operatorName ?? ''}`.toLowerCase().includes(q)) }
      if (params?.isActive !== undefined) rows = rows.filter((row) => row.isActive === params.isActive)
      if (params?.operatorType) rows = rows.filter((row) => row.operatorType === params.operatorType)
      return mockRequest(rows)
    }
    const query = new URLSearchParams()
    if (params?.search) query.set('search', params.search)
    if (params?.isActive !== undefined) query.set('isActive', String(params.isActive))
    if (params?.operatorType) query.set('operatorType', params.operatorType)
    return request<OperatorAdminListItem[]>(`/api/v1/admin/gov/management/operators${query.toString() ? `?${query}` : ''}`)
  },
  setOperatorActive: (id: string, isActive: boolean) => {
    if (useMockApi) { mockOperators = mockOperators.map((row) => row.id === id ? { ...row, isActive } : row); return mockRequest(undefined) }
    return request<void>(`/api/v1/admin/gov/management/operators/${id}/active`, { method: 'PATCH', body: JSON.stringify({ isActive }) })
  },
  startAdminImport: (body: { sourceFilename: string; sourceFormat: string; rows: GovAdminImportRow[] }) => {
    if (useMockApi) {
      const batchId = crypto.randomUUID(); const now = new Date().toISOString()
      const batch: GovAdminImportBatch = { id: batchId, sourceFilename: body.sourceFilename, sourceFormat: body.sourceFormat, uploadedBy: 'Demo Government Administrator', uploadedAt: now, status: 'ready_to_publish', rowCount: body.rows.length, errorCount: 0, warningCount: 0, publishedAt: null, publishedBy: null }
      mockAdminImports[batchId] = { batch, rows: body.rows.map((data, index) => ({ id: crypto.randomUUID(), rowNumber: index + 1, action: 'create', errors: [], warnings: [], data })) }
      return mockRequest({ batchId })
    }
    return request<{ batchId: string }>('/api/v1/admin/gov/management/admins/imports', { method: 'POST', body: JSON.stringify(body) })
  },
  adminImportStatus: (batchId: string) => useMockApi ? mockRequest(mockAdminImports[batchId]?.batch) : request<GovAdminImportBatch>(`/api/v1/admin/gov/management/admins/imports/${batchId}`),
  adminImportRows: (batchId: string) => useMockApi ? mockRequest(mockAdminImports[batchId]?.rows ?? []) : request<GovAdminImportValidationRow[]>(`/api/v1/admin/gov/management/admins/imports/${batchId}/rows`),
  publishAdminImport: (batchId: string) => {
    if (useMockApi) { const entry = mockAdminImports[batchId]; if (!entry) throw new Error('Import batch not found.'); entry.batch = { ...entry.batch, status: 'published', publishedAt: new Date().toISOString(), publishedBy: 'Demo Government Administrator' }; return mockRequest({ created: entry.batch.rowCount, failed: 0 }) }
    return request<{ created: number; failed: number }>(`/api/v1/admin/gov/management/admins/imports/${batchId}/publish`, { method: 'POST' })
  },
  discardAdminImport: (batchId: string) => {
    if (useMockApi) { const entry = mockAdminImports[batchId]; if (entry) entry.batch = { ...entry.batch, status: 'discarded' }; return mockRequest(undefined) }
    return request<void>(`/api/v1/admin/gov/management/admins/imports/${batchId}/discard`, { method: 'POST' })
  },

  listGovNotifications: () => (useMockApi ? mockRequest(mockGovNotifications) : request<GovNotification[]>('/api/v1/admin/gov/management/notifications')),
  createGovNotification: (body: { title: string; body: string }) => {
    if (useMockApi) {
      const now = new Date().toISOString()
      const created = { id: crypto.randomUUID(), ...body, isActive: true, createdAt: now, updatedAt: now }
      mockGovNotifications = [created, ...mockGovNotifications]
      return mockRequest(created)
    }
    return request<GovNotification>('/api/v1/admin/gov/management/notifications', { method: 'POST', body: JSON.stringify(body) })
  },
  setGovNotificationActive: (id: string, isActive: boolean) => {
    if (useMockApi) {
      mockGovNotifications = mockGovNotifications.map((notification) => notification.id === id ? { ...notification, isActive, updatedAt: new Date().toISOString() } : notification)
      return mockRequest(undefined)
    }
    return request<void>(`/api/v1/admin/gov/management/notifications/${id}/active`, { method: 'PATCH', body: JSON.stringify({ isActive }) })
  },

  catalogDepartments: () => (useMockApi ? mockRequest(mockCatalogDepartments) : request<CatalogAdminDepartment[]>('/api/v1/admin/gov/management/catalog/departments')),
  createCatalogDepartment: (body: { code: string; name: string; departmentNameKannada: string | null; departmentUrl: string | null; departmentUrlKannada: string | null; displayOrder: number }) => {
    if (useMockApi) {
      const created = { id: crypto.randomUUID(), ...body, isActive: true, serviceCount: 0 }
      mockCatalogDepartments = [...mockCatalogDepartments, created]
      mockCatalogChanges = [{ id: crypto.randomUUID(), entityType: 'department', departmentCode: body.code, serviceCode: null, changedAt: new Date().toISOString(), changedBy: 'Demo Government Administrator', changeType: 'created', diff: null }, ...mockCatalogChanges]
      return mockRequest(created)
    }
    return request<CatalogAdminDepartment>('/api/v1/admin/gov/management/catalog/departments', { method: 'POST', body: JSON.stringify(body) })
  },
  updateCatalogDepartment: (code: string, body: { code: string; name: string; departmentNameKannada: string | null; departmentUrl: string | null; departmentUrlKannada: string | null; displayOrder: number }) => {
    if (useMockApi) {
      mockCatalogDepartments = mockCatalogDepartments.map((department) => department.code === code ? { ...department, ...body } : department)
      return mockRequest(mockCatalogDepartments.find((department) => department.code === code)!)
    }
    return request<CatalogAdminDepartment>(`/api/v1/admin/gov/management/catalog/departments/${encodeURIComponent(code)}`, { method: 'PATCH', body: JSON.stringify(body) })
  },
  setCatalogDepartmentActive: (code: string, isActive: boolean) => {
    if (useMockApi) {
      mockCatalogDepartments = mockCatalogDepartments.map((department) => department.code === code ? { ...department, isActive } : department)
      mockCatalogChanges = [{ id: crypto.randomUUID(), entityType: 'department', departmentCode: code, serviceCode: null, changedAt: new Date().toISOString(), changedBy: 'Demo Government Administrator', changeType: isActive ? 'reactivated' : 'deactivated', diff: null }, ...mockCatalogChanges]
      return mockRequest(undefined)
    }
    return request<void>(`/api/v1/admin/gov/management/catalog/departments/${encodeURIComponent(code)}/active`, { method: 'PATCH', body: JSON.stringify({ isActive }) })
  },
  catalogSubDepartments: (params?: { departmentCode?: string; search?: string; isActive?: boolean }) => {
    if (useMockApi) {
      let rows = mockCatalogSubDepartments
      if (params?.departmentCode) rows = rows.filter((row) => row.departmentCode === params.departmentCode)
      if (params?.search) { const q = params.search.toLowerCase(); rows = rows.filter((row) => `${row.departmentCode} ${row.departmentName} ${row.name} ${row.nameKannada ?? ''}`.toLowerCase().includes(q)) }
      if (params?.isActive !== undefined) rows = rows.filter((row) => row.isActive === params.isActive)
      return mockRequest(rows)
    }
    const query = new URLSearchParams()
    if (params?.departmentCode) query.set('departmentCode', params.departmentCode)
    if (params?.search) query.set('search', params.search)
    if (params?.isActive !== undefined) query.set('isActive', String(params.isActive))
    const suffix = query.toString() ? `?${query.toString()}` : ''
    return request<CatalogAdminSubDepartment[]>(`/api/v1/admin/gov/management/catalog/sub-departments${suffix}`)
  },
  createCatalogSubDepartment: (body: { departmentCode: string; name: string; nameKannada: string | null; displayOrder: number; isActive: boolean }) => {
    if (useMockApi) {
      const department = mockCatalogDepartments.find((item) => item.code === body.departmentCode)
      const created: CatalogAdminSubDepartment = { ...body, id: crypto.randomUUID(), departmentName: department?.name ?? body.departmentCode }
      mockCatalogSubDepartments = [created, ...mockCatalogSubDepartments]
      mockCatalogChanges = [{ id: crypto.randomUUID(), entityType: 'sub_department', departmentCode: body.departmentCode, serviceCode: null, changedAt: new Date().toISOString(), changedBy: 'Demo Government Administrator', changeType: 'created', diff: null }, ...mockCatalogChanges]
      return mockRequest(created)
    }
    return request<CatalogAdminSubDepartment>('/api/v1/admin/gov/management/catalog/sub-departments', { method: 'POST', body: JSON.stringify(body) })
  },
  updateCatalogSubDepartment: (id: string, body: { departmentCode: string; name: string; nameKannada: string | null; displayOrder: number; isActive: boolean }) => {
    if (useMockApi) {
      mockCatalogSubDepartments = mockCatalogSubDepartments.map((row) => row.id === id ? { ...row, ...body, departmentName: row.departmentName } : row)
      return mockRequest(mockCatalogSubDepartments.find((row) => row.id === id)!)
    }
    return request<CatalogAdminSubDepartment>(`/api/v1/admin/gov/management/catalog/sub-departments/${encodeURIComponent(id)}`, { method: 'PATCH', body: JSON.stringify(body) })
  },
  setCatalogSubDepartmentActive: (id: string, isActive: boolean) => {
    if (useMockApi) {
      mockCatalogSubDepartments = mockCatalogSubDepartments.map((row) => row.id === id ? { ...row, isActive } : row)
      return mockRequest(undefined)
    }
    return request<void>(`/api/v1/admin/gov/management/catalog/sub-departments/${encodeURIComponent(id)}/active`, { method: 'PATCH', body: JSON.stringify({ isActive }) })
  },
  catalogServices: (params?: { departmentCode?: string; search?: string; isActive?: boolean }) => {
    if (useMockApi) {
      let rows = mockCatalogServices
      if (params?.departmentCode) rows = rows.filter((service) => service.departmentCode === params.departmentCode)
      if (params?.search) { const q = params.search.toLowerCase(); rows = rows.filter((service) => `${service.serviceCode} ${service.name} ${service.nameKannada ?? ''}`.toLowerCase().includes(q)) }
      if (params?.isActive !== undefined) rows = rows.filter((service) => service.isActive === params.isActive)
      return mockRequest(rows.map(({ id, departmentCode, departmentName, serviceCode, name, nameKannada, category, isActive, version }) => ({ id, departmentCode, departmentName, serviceCode, name, nameKannada, category, isActive, version })))
    }
    const query = new URLSearchParams()
    if (params?.departmentCode) query.set('departmentCode', params.departmentCode)
    if (params?.search) query.set('search', params.search)
    if (params?.isActive !== undefined) query.set('isActive', String(params.isActive))
    const suffix = query.toString() ? `?${query.toString()}` : ''
    return request<CatalogAdminServiceListItem[]>(`/api/v1/admin/gov/management/catalog/services${suffix}`)
  },
  getCatalogService: (departmentCode: string, serviceCode: string) => useMockApi
    ? mockRequest(mockCatalogServices.find((service) => service.departmentCode === departmentCode && service.serviceCode === serviceCode)!)
    : request<CatalogAdminService>(`/api/v1/admin/gov/management/catalog/services/${encodeURIComponent(departmentCode)}/${encodeURIComponent(serviceCode)}`),
  createCatalogService: (body: CatalogAdminServiceWriteRequest) => {
    if (useMockApi) {
      const department = mockCatalogDepartments.find((item) => item.code === body.departmentCode)
      const created: CatalogAdminService = { ...body, id: crypto.randomUUID(), departmentName: department?.name ?? body.departmentCode, version: 1, visibleToRoles: body.visibleToRoles ?? [] }
      mockCatalogServices = [created, ...mockCatalogServices]
      mockCatalogDepartments = mockCatalogDepartments.map((item) => item.code === body.departmentCode ? { ...item, serviceCount: item.serviceCount + 1 } : item)
      mockCatalogChanges = [{ id: crypto.randomUUID(), entityType: 'service', departmentCode: body.departmentCode, serviceCode: body.serviceCode, changedAt: new Date().toISOString(), changedBy: 'Demo Government Administrator', changeType: 'created', diff: null }, ...mockCatalogChanges]
      return mockRequest(created)
    }
    return request<CatalogAdminService>('/api/v1/admin/gov/management/catalog/services', { method: 'POST', body: JSON.stringify(body) })
  },
  updateCatalogService: (departmentCode: string, serviceCode: string, body: CatalogAdminServiceWriteRequest) => {
    if (useMockApi) {
      mockCatalogServices = mockCatalogServices.map((service) => service.departmentCode === departmentCode && service.serviceCode === serviceCode ? { ...service, ...body, version: service.version + 1, departmentName: service.departmentName, visibleToRoles: body.visibleToRoles ?? service.visibleToRoles } : service)
      return mockRequest(mockCatalogServices.find((service) => service.departmentCode === departmentCode && service.serviceCode === serviceCode)!)
    }
    return request<CatalogAdminService>(`/api/v1/admin/gov/management/catalog/services/${encodeURIComponent(departmentCode)}/${encodeURIComponent(serviceCode)}`, { method: 'PATCH', body: JSON.stringify(body) })
  },
  setCatalogServiceActive: (departmentCode: string, serviceCode: string, isActive: boolean) => {
    if (useMockApi) {
      mockCatalogServices = mockCatalogServices.map((service) => service.departmentCode === departmentCode && service.serviceCode === serviceCode ? { ...service, isActive, version: service.version + 1 } : service)
      mockCatalogChanges = [{ id: crypto.randomUUID(), entityType: 'service', departmentCode, serviceCode, changedAt: new Date().toISOString(), changedBy: 'Demo Government Administrator', changeType: isActive ? 'reactivated' : 'deactivated', diff: null }, ...mockCatalogChanges]
      return mockRequest(undefined)
    }
    return request<void>(`/api/v1/admin/gov/management/catalog/services/${encodeURIComponent(departmentCode)}/${encodeURIComponent(serviceCode)}/active`, { method: 'PATCH', body: JSON.stringify({ isActive }) })
  },
  catalogChanges: (limit = 100) => useMockApi ? mockRequest(mockCatalogChanges.slice(0, limit)) : request<CatalogAdminChange[]>(`/api/v1/admin/gov/management/catalog/changes?limit=${limit}`),
  activityLog: (params?: { limit?: number; action?: string }) => {
    if (useMockApi) {
      let rows = mockActivityLog
      if (params?.action) rows = rows.filter((row) => row.action === params.action)
      return mockRequest(rows.slice(0, params?.limit ?? 100))
    }
    const query = new URLSearchParams()
    query.set('limit', String(params?.limit ?? 100))
    if (params?.action) query.set('action', params.action)
    return request<GovAdminActivityLogEntry[]>(`/api/v1/admin/gov/management/activity-log?${query.toString()}`)
  },
  startCatalogImport: (body: { sourceFilename: string; sourceFormat: CatalogImportFormat; rows: CatalogImportRow[] }) => {
    if (useMockApi) {
      const batchId = crypto.randomUUID()
      const batch: CatalogImportBatch = { id: batchId, sourceFilename: body.sourceFilename, sourceFormat: body.sourceFormat, uploadedBy: 'Demo Government Administrator', uploadedAt: new Date().toISOString(), status: 'ready_to_publish', rowCount: body.rows.length, errorCount: 0, warningCount: 0, publishedAt: null, publishedBy: null }
      mockCatalogImports[batchId] = { batch, rows: body.rows, validationRows: body.rows.map((row, index) => ({ id: crypto.randomUUID(), rowNumber: index + 2, action: 'insert', errors: [], warnings: [], departmentCode: row.department.code, serviceCode: row.service.serviceCode, serviceName: row.service.name })) }
      return mockRequest({ batchId })
    }
    return request<{ batchId: string }>('/api/v1/admin/gov/management/catalog/imports', { method: 'POST', body: JSON.stringify(body) })
  },
  catalogImportStatus: (batchId: string) => useMockApi
    ? mockRequest(mockCatalogImports[batchId]?.batch)
    : request<CatalogImportBatch>(`/api/v1/admin/gov/management/catalog/imports/${encodeURIComponent(batchId)}`),
  catalogImportRows: (batchId: string) => useMockApi
    ? mockRequest(mockCatalogImports[batchId]?.validationRows ?? [])
    : request<CatalogImportValidationRow[]>(`/api/v1/admin/gov/management/catalog/imports/${encodeURIComponent(batchId)}/rows`),
  publishCatalogImport: (batchId: string) => {
    if (useMockApi) {
      const entry = mockCatalogImports[batchId]
      if (!entry) throw new Error('Import batch not found.')
      entry.batch = { ...entry.batch, status: 'published', publishedAt: new Date().toISOString(), publishedBy: 'Demo Government Administrator' }
      return mockRequest({ published: entry.rows.length, skipped: 0 })
    }
    return request<{ published: number; skipped: number }>(`/api/v1/admin/gov/management/catalog/imports/${encodeURIComponent(batchId)}/publish`, { method: 'POST' })
  },
  discardCatalogImport: (batchId: string) => {
    if (useMockApi) {
      const entry = mockCatalogImports[batchId]
      if (entry) entry.batch = { ...entry.batch, status: 'discarded' }
      return mockRequest(undefined)
    }
    return request<void>(`/api/v1/admin/gov/management/catalog/imports/${encodeURIComponent(batchId)}/discard`, { method: 'POST' })
  },

  health: () => {
    if (useMockApi) {
      mockHealth = { ...mockHealth, checkedAt: new Date().toISOString() }
      return mockRequest(mockHealth)
    }
    return request<GovHealth>('/api/v1/admin/gov/management/health')
  },
  aiStatus: () => {
    if (useMockApi) {
      mockAiStatus = { ...mockAiStatus, checkedAt: new Date().toISOString() }
      return mockRequest(mockAiStatus)
    }
    return request<GovAiStatus>('/api/v1/admin/gov/management/ai/status')
  },
  reingestAi: (confirmed: boolean) => {
    if (useMockApi) {
      if (!confirmed) throw new Error('Reingestion requires an explicit confirmation.')
      return mockRequest<AiReingestResult>({ status: 'mocked', message: 'Mock mode: no AI ingestion was started.', summary: {} })
    }
    return request<AiReingestResult>('/api/v1/admin/gov/management/ai/reingest', { method: 'POST', body: JSON.stringify({ confirmed }) })
  },
  askGovAi: (message: string) => {
    if (useMockApi) {
      return mockRequest<GovAiAssistantResponse>({
        reply: `Mock assistant response for: "${message}"\n\nConnect the BFF and gov-admin-ai sidecar to receive a live scoped report.`,
        sources: [{ tool: 'mock', description: 'Development-only mock response' }],
        scopeNote: 'Mock mode is not connected to live reports.',
      })
    }
    return request<GovAiAssistantResponse>('/api/v1/admin/gov/management/ai/ask', { method: 'POST', body: JSON.stringify({ message }) })
  },

  getServiceNotice: () => (useMockApi ? mockRequest(mockServiceNotice) : request<ServiceNotice | null>('/api/v1/admin/gov/management/service-notice')),
  listServiceNoticeHistory: () => (useMockApi ? mockRequest(mockServiceNoticeHistory) : request<ServiceNoticeHistory[]>('/api/v1/admin/gov/management/service-notice/history')),
  saveServiceNotice: (body: { title: string; message: string; severity: ServiceNoticeSeverity; isActive: boolean; startsAt: string | null; endsAt: string | null }) => {
    if (useMockApi) {
      if (mockServiceNotice) {
        mockServiceNoticeHistory = [{ ...mockServiceNotice, id: crypto.randomUUID(), wasActive: mockServiceNotice.isActive, savedAt: mockServiceNotice.updatedAt }, ...mockServiceNoticeHistory]
      }
      mockServiceNotice = { ...body, updatedAt: new Date().toISOString() }
      return mockRequest(mockServiceNotice)
    }
    return request<ServiceNotice>('/api/v1/admin/gov/management/service-notice', { method: 'PUT', body: JSON.stringify(body) })
  },
  stopServiceNotice: () => {
    if (useMockApi) {
      if (mockServiceNotice) {
        mockServiceNoticeHistory = [{ ...mockServiceNotice, id: crypto.randomUUID(), wasActive: mockServiceNotice.isActive, savedAt: new Date().toISOString() }, ...mockServiceNoticeHistory]
      }
      mockServiceNotice = null
      return mockRequest(undefined)
    }
    return request<void>('/api/v1/admin/gov/management/service-notice', { method: 'DELETE' })
  },

  referenceDepartments: () => (useMockApi ? mockRequest(mockDepartmentOptions) : request<ReferenceDepartment[]>('/api/v1/admin/gov/reference/departments')),
  referenceDistricts: () => (useMockApi ? mockRequest(mockDistrictOptions) : request<ReferenceDistrict[]>('/api/v1/admin/gov/reference/districts')),

  claimStart: (body: { username: string; mobile: string; password: string }) =>
    useMockApi
      ? mockRequest({ challengeId: 'mock-challenge' })
      : request<{ challengeId: string }>('/api/v1/admin/gov/claim/start', { method: 'POST', body: JSON.stringify(body) }),
  claimVerify: (body: { challengeId: string; otp: string }) =>
    useMockApi
      ? mockRequest({ username: 'demo-admin' })
      : request<{ username: string }>('/api/v1/admin/gov/claim/verify', { method: 'POST', body: JSON.stringify(body) }),
}
