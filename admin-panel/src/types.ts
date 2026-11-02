export type GovAdminTier = 'Superadmin' | 'ChiefMinister' | 'Department' | 'DistrictHead' | 'TalukHead'
export type ServiceNoticeSeverity = 'info' | 'warning' | 'critical'

export interface GovAdminMe {
  displayName: string
  tier: GovAdminTier
  assignedDistrict: string | null
  assignedTaluk: string | null
  assignedDepartment: string | null
}

export interface ServiceNotice {
  title: string
  message: string
  severity: ServiceNoticeSeverity
  isActive: boolean
  startsAt: string | null
  endsAt: string | null
  updatedAt: string
}

export interface ServiceNoticeHistory {
  id: string
  title: string
  message: string
  severity: ServiceNoticeSeverity
  wasActive: boolean
  startsAt: string | null
  endsAt: string | null
  savedAt: string
}

export interface GovNotification {
  id: string
  title: string
  body: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export type GovHealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy'

export interface GovHealth {
  status: GovHealthStatus
  checkedAt: string
  checks: Array<{
    name: string
    status: GovHealthStatus
    description: string | null
    durationMs: number
  }>
  node: {
    machineName: string
    runtime: string
    processId: number
    processorCount: number
    uptimeSeconds: number
    workingSetBytes: number
    privateMemoryBytes: number
    managedHeapBytes: number
  }
}

export type AiBackendStatus = 'Healthy' | 'Degraded' | 'Unhealthy'

export interface GovAiStatus {
  status: AiBackendStatus
  checkedAt: string
  health: { status: string; service: string } | null
  readiness: {
    status: string
    activeKnowledgeSource: string
    serviceIndexPrefix: string
    totalServicesIndexed: number
    embeddingDimension: number
  } | null
  telemetry: {
    collectedAt: string
    host: string
    cpu: { usagePercent: number; load1: number | null; cores: number }
    memory: { totalBytes: number; usedBytes: number; availableBytes: number; usagePercent: number }
    disk: { mount: string; totalBytes: number; usedBytes: number; availableBytes: number; usagePercent: number }
    gpus: Array<{
      name: string
      utilizationPercent: number
      memoryTotalBytes: number
      memoryUsedBytes: number
      memoryUsagePercent: number
      temperatureCelsius: number | null
    }>
  } | null
  telemetryNote: string
}

export interface AiReingestResult {
  status: string
  message: string
  summary: unknown
}

export interface GovAiAssistantResponse {
  reply: string
  sources: Array<{ tool: string; description: string }>
  scopeNote: string
}

export interface GovAdminListItem {
  id: string
  username: string
  displayName: string
  tier: GovAdminTier
  assignedDistrict: string | null
  assignedTaluk: string | null
  assignedDepartment: string | null
  isActive: boolean
  claimed: boolean
  createdAt: string
}

export interface GovAdminSession {
  sessionId: string
  govAdminId: string
  displayName: string
  tier: GovAdminTier
  expiresAt: string
  isCurrent: boolean
}

export interface OperatorAdminListItem {
  id: string
  operatorCode: string
  operatorType: string
  operatorName: string | null
  mobileNo: string | null
  isClaimed: boolean
  isActive: boolean
  createdAt: string
}

export interface GovAdminImportRow {
  username: string
  displayName: string
  tier: GovAdminTier
  assignedDistrict: string | null
  assignedTaluk: string | null
  assignedDepartment: string | null
}

export interface GovAdminImportBatch {
  id: string
  sourceFilename: string
  sourceFormat: string
  uploadedBy: string
  uploadedAt: string
  status: string
  rowCount: number
  errorCount: number
  warningCount: number
  publishedAt: string | null
  publishedBy: string | null
}

export interface GovAdminImportValidationRow {
  id: string
  rowNumber: number
  action: string | null
  errors: string[]
  warnings: string[]
  data: GovAdminImportRow | null
}

export interface ReferenceDepartment {
  code: string
  name: string
}

export interface ReferenceDistrict {
  district: string
  taluks: string[]
}

export interface ApplicationsByStatus {
  status: string
  count: number
}

export interface ApplySource {
  source: 'ai' | 'catalogue'
  count: number
}

export interface OverviewAnalytics {
  registeredCitizens: number
  profileCompletedCitizens: number
  applicationsByStatus: ApplicationsByStatus[]
  applySource: ApplySource[]
}

export interface DepartmentAnalytics {
  departmentCount: number
  activeServiceCount: number
  byDepartment: Array<{ departmentCode: string; departmentName: string; serviceCount: number }>
}

export interface DistrictRow {
  district: string
  taluk: string
  applicationCount: number
  citizenCount: number
  submittedCount: number
  inProgressCount: number
  completedCount: number
  rejectedCount: number
}

export interface DistrictAnalytics {
  scope: 'statewide' | 'district' | 'taluk'
  rows: DistrictRow[]
}

export interface DiscoveryAnalytics {
  totalApplyClicks: number
  aiApplyClicks: number
  catalogueApplyClicks: number
  byDepartment: Array<{ departmentCode: string; aiCount: number; catalogueCount: number }>
}

export interface CitizensAnalytics {
  totalRegistered: number
  profileCompleted: number
  profileIncomplete: number
  registrationTrend: Array<{ date: string; count: number }>
}

export interface LifecycleAnalytics {
  byStatus: ApplicationsByStatus[]
  trend: Array<{ date: string; submitted: number; approved: number; rejected: number }>
}

export interface SakalaSlaDepartmentRow {
  departmentCode: string
  departmentName: string
  totalSakalaApplications: number
  completedOnTime: number
  completedLate: number
  pendingWithinSla: number
  pendingOverdue: number
}

export interface SakalaSlaAnalytics {
  scope: 'statewide' | 'district' | 'taluk'
  byDepartment: SakalaSlaDepartmentRow[]
}

export interface ApplicationAgingRow {
  departmentCode: string
  departmentName: string
  openApplications: number
  dueWithin24h: number
  age0To3Days: number
  age4To7Days: number
  age8To14Days: number
  age15To30Days: number
  age30PlusDays: number
  avgProcessingHours: number | null
}

export interface ApplicationAgingAnalytics {
  scope: 'statewide' | 'district' | 'taluk'
  byDepartment: ApplicationAgingRow[]
}

export interface SlaTrendPoint {
  periodStart: string
  total: number
  completedOnTime: number
  completedLate: number
}

export interface SlaTrendAnalytics {
  scope: 'statewide' | 'district' | 'taluk'
  trend: SlaTrendPoint[]
}

export interface ServiceUsageRow {
  serviceCode: string
  serviceName: string
  departmentCode: string
  departmentName: string
  totalApplyClicks: number
  aiApplyClicks: number
  catalogueApplyClicks: number
  lastUsedAt: string | null
}

export interface ServiceUsageAnalytics {
  scope: 'statewide' | 'district' | 'taluk'
  services: ServiceUsageRow[]
}

export interface ApplicationFunnelAnalytics {
  scope: 'statewide' | 'district' | 'taluk'
  registeredCitizens: number
  profileCompletedCitizens: number
  citizensSubmitted: number
  citizensCompleted: number
}

export interface ApplicationDropoffRow {
  status: string
  openCount: number
  avgTimeInStatusHours: number
}

export interface ApplicationDropoffAnalytics {
  scope: 'statewide' | 'district' | 'taluk'
  byStatus: ApplicationDropoffRow[]
}

export interface CatalogAdminDepartment {
  id: string
  code: string
  name: string
  departmentNameKannada: string | null
  departmentUrl: string | null
  departmentUrlKannada: string | null
  displayOrder: number
  isActive: boolean
  serviceCount: number
}

export interface CatalogAdminSubDepartment {
  id: string
  departmentCode: string
  departmentName: string
  name: string
  nameKannada: string | null
  displayOrder: number
  isActive: boolean
}

export interface CatalogAdminServiceListItem {
  id: string
  departmentCode: string
  departmentName: string
  serviceCode: string
  name: string
  nameKannada: string | null
  category: string | null
  isActive: boolean
  version: number
}

export interface CatalogAdminService extends CatalogAdminServiceListItem {
  subDepartmentName: string | null
  shortDescription: string | null
  shortDescriptionKannada: string | null
  description: string | null
  descriptionKannada: string | null
  isSakalaEnabled: boolean | null
  sakalaLabelRaw: string | null
  sakalaDays: number | null
  departmentFee: number | null
  serviceCharge: number | null
  documents: string[]
  documentsKannada: string[]
  eligibility: string[]
  eligibilityKannada: string[]
  aliases: string[]
  steps: string[]
  stepsKannada: string[]
  jurisdiction: string | null
  modeOfService: string | null
  departmentServiceId: string | null
  portal: string | null
  serviceUrl: string | null
  deeplinkCitizenUrl: string | null
  deeplinkCitizenUrlKannada: string | null
  deeplinkOperatorUrl: string | null
  homepageUrl: string | null
  integrationMethod: string | null
  sevasindhuServiceId: string | null
  remarks: string | null
  visibleToRoles: string[]
}

export interface CatalogAdminServiceWriteRequest extends Omit<CatalogAdminService, 'id' | 'departmentName' | 'version' | 'visibleToRoles'> {
  visibleToRoles?: string[]
}

export interface CatalogAdminChange {
  id: string
  entityType: 'department' | 'service' | string
  departmentCode: string | null
  serviceCode: string | null
  changedAt: string
  changedBy: string
  changeType: string
  diff: string | null
}

export type CatalogImportFormat = 'csv' | 'xlsx'

export interface CatalogImportRow {
  department: {
    code: string
    name: string
    homeUrlEn: string | null
    homeUrlKn: string | null
  }
  subDepartment: { name: string } | null
  service: {
    serviceCode: string
    name: string
    nameKannada: string | null
    shortDescription: string | null
    description: string | null
    descriptionKannada: string | null
    isSakalaEnabled: boolean | null
    sakalaLabelRaw: string | null
    sakalaDays: number | null
    departmentFee: number | null
    serviceCharge: number | null
    documentsEn: string[]
    documentsKn: string[]
    eligibilityEn: string[]
    eligibilityKn: string[]
    procedureEn: string[]
    procedureKn: string[]
    jurisdiction: string | null
    category: string | null
    modeOfService: string | null
    isMandatory: boolean | null
    departmentServiceId: string | null
    serviceUrl: string | null
    applyUrl: string | null
    applyUrlKannada: string | null
  }
}

export interface CatalogImportBatch {
  id: string
  sourceFilename: string
  sourceFormat: CatalogImportFormat
  uploadedBy: string
  uploadedAt: string
  status: string
  rowCount: number
  errorCount: number
  warningCount: number
  publishedAt: string | null
  publishedBy: string | null
}

export interface CatalogImportValidationRow {
  id: string
  rowNumber: number
  action: string | null
  errors: Array<{ field: string; message: string }>
  warnings: Array<{ field: string; message: string }>
  departmentCode: string
  serviceCode: string
  serviceName: string
}

export interface GovAdminActivityLogEntry {
  id: string
  actorUsername: string
  actorDisplayName: string
  action: string
  target: string | null
  detail: string | null
  at: string
}

export interface TokenIntegrationType {
  typeKey: string
  displayName: string
  description: string | null
  fieldsJson: string
}

export interface TokenIntegrationListItem {
  serviceId: string
  departmentCode: string
  departmentName: string
  serviceCode: string
  name: string
  nameKannada: string | null
  citizenVisible: boolean
  operatorMethod: string | null
  typeKey: string | null
  isConfigured: boolean
  isEnabled: boolean
  landingUrl: string | null
}

export interface TokenIntegration {
  serviceId: string
  departmentCode: string
  departmentName: string
  serviceCode: string
  name: string
  operatorMethod: string | null
  operatorSevasindhuId: string | null
  isConfigured: boolean
  typeKey: string | null
  landingUrl: string | null
  sendMobile: boolean
  sendDigilockerId: boolean
  statusUrl: string | null
  statusConfig: TokenStatusConfig
  hasStatusSecret: boolean
  kaveriServiceId: number | null
  providerReady: boolean
  allowedIps: string[]
  isEnabled: boolean
  updatedAt: string | null
  updatedBy: string | null
  version: number | null
  warnings: string[]
  /** Only for typeKey "edcs_api": optional Key1..Key5 fields a department's generate call may need. Usually blank. */
  edcsKey1?: string | null
  edcsKey2?: string | null
  edcsKey3?: string | null
  edcsKey4?: string | null
  edcsKey5?: string | null
}

export interface TokenIntegrationWriteRequest {
  typeKey: string
  landingUrl: string | null
  sendMobile: boolean
  sendDigilockerId: boolean
  statusUrl: string | null
  allowedIps: string[]
  isEnabled: boolean
  statusConfig?: TokenStatusConfig
  kaveriServiceId?: number
  /** Only for typeKey "edcs_api": the Seva Sindhu-side service id its generate call needs. Null leaves it unchanged. */
  sevasindhuServiceId?: string | null
  /** Only for typeKey "edcs_api": optional Key1..Key5 some departments need (e.g. an Aadhaar hash). Usually null. */
  edcsKey1?: string | null
  edcsKey2?: string | null
  edcsKey3?: string | null
  edcsKey4?: string | null
  edcsKey5?: string | null
  /** Leave out to keep the saved secret, send an empty string to remove it. */
  statusSecret?: string
}

export interface TokenIntegrationCredential {
  id: string
  username: string
  isActive: boolean
  createdAt: string
  createdBy: string
  passwordChangedAt: string
  lastUsedAt: string | null
}

export interface TokenIntegrationNewCredential {
  id: string
  username: string
  password: string
}

export interface TokenStatusConfig {
  authType: string
  authUser: string | null
  authHeaderName: string | null
  statusPath: string
  messagePath: string | null
  statusMap: Record<string, string> | null
  requestField: string
  numberSource: string
}

export interface TokenStatusTestResult {
  httpStatus: number | null
  rawBody: string | null
  departmentStatus: string | null
  message: string | null
  mappedStatus: string | null
  error: string | null
}

export interface KaveriSettings {
  loginUrl: string
  refreshUrl: string
  ssoUrl: string
  statusUrl: string
  username: string
  assumeEmailVerified: boolean
}

export interface TokenProvider {
  typeKey: string
  kaveri: KaveriSettings | null
  hasSecret: boolean
  isReady: boolean
  updatedAt: string | null
  updatedBy: string | null
}

export interface TokenProviderTestResult {
  ok: boolean
  message: string
}
