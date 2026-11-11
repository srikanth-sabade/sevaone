import { flow } from './flowLog'
import type {
  DigiLockerDocument,
  GramaOneCentre,
  ApplicationDetail,
  ApplicationSummary,
  Department,
  Grievance,
  KpiDashboard,
  Me,
  SchemeDetail,
  SchemeSummary,
  TrackApiResult,
  PublicServiceNotice,
  PublicGovNotification,
} from './types'
import type {
  CatalogDepartment,
  CatalogDepartmentDetail,
  CatalogSearchResult,
  CatalogService,
} from './lib/catalogHierarchy'

/** timeoutMs is optional so none of the existing call sites need to change -- only callers
 *  that pass it (e.g. AI chat, which can genuinely take a while) get the AbortController
 *  wrapping; everyone else keeps today's behavior (fetch runs to completion or fails on its
 *  own network error). */
export async function request<T>(path: string, init?: RequestInit, timeoutMs?: number): Promise<T> {
  const method = (init?.method ?? 'GET').toUpperCase()
  flow(`browser → BFF ${method} ${path}`)
  const controller = timeoutMs ? new AbortController() : undefined
  const timer = timeoutMs ? window.setTimeout(() => controller!.abort(), timeoutMs) : undefined
  let response: Response
  try {
    response = await fetch(path, {
      credentials: 'include',
      headers: {
        Accept: 'application/json',
        ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
        ...init?.headers,
      },
      ...init,
      ...(controller ? { signal: controller.signal } : {}),
    })
  } finally {
    if (timer) window.clearTimeout(timer)
  }
  if (!response.ok) {
    let title = response.statusText
    try {
      const body = (await response.json()) as {
        title?: string
        errors?: { propertyName?: string; errorMessage?: string; PropertyName?: string; ErrorMessage?: string }[]
      }
      if (body.errors?.length) {
        title = body.errors
          .map((e) => e.errorMessage || e.ErrorMessage || '')
          .filter(Boolean)
          .join(' ') || body.title || title
      } else if (body.title) {
        title = body.title
      }
    } catch {
      /* ignore */
    }
    flow(`BFF ← ${method} ${path} ${response.status}`, { error: title })
    throw new Error(title)
  }
  if (response.status === 204) {
    flow(`BFF ← ${method} ${path} 204`)
    return undefined as T
  }
  // A bare `return Ok()` (no body) is still a 200, not a 204 -- .json() throws
  // "Unexpected end of JSON input" on the empty string. Read as text first and only parse
  // when there's actually something to parse.
  const text = await response.text()
  if (!text) {
    flow(`BFF ← ${method} ${path} ${response.status} (empty body)`)
    return undefined as T
  }
  const json = JSON.parse(text) as T
  flow(`BFF ← ${method} ${path} ${response.status}`, summarize(json))
  return json
}

/** Mirrors request()'s error handling, but never sets Content-Type -- the browser must set its
 *  own multipart boundary for a FormData body, which request() would otherwise clobber with a
 *  forced 'application/json' (it adds that header whenever init.body is truthy, and FormData is
 *  truthy). timeoutMs is optional, same as request(). */
export async function requestForm<T>(path: string, formData: FormData, timeoutMs?: number): Promise<T> {
  flow(`browser → BFF POST ${path}`)
  const controller = timeoutMs ? new AbortController() : undefined
  const timer = timeoutMs ? window.setTimeout(() => controller!.abort(), timeoutMs) : undefined
  let response: Response
  try {
    response = await fetch(path, {
      method: 'POST',
      credentials: 'include',
      body: formData,
      ...(controller ? { signal: controller.signal } : {}),
    })
  } finally {
    if (timer) window.clearTimeout(timer)
  }
  if (!response.ok) {
    let title = response.statusText
    try {
      const body = (await response.json()) as { title?: string }
      if (body.title) title = body.title
    } catch {
      /* ignore */
    }
    flow(`BFF ← POST ${path} ${response.status}`, { error: title })
    throw new Error(title)
  }
  const text = await response.text()
  if (!text) {
    flow(`BFF ← POST ${path} ${response.status} (empty body)`)
    return undefined as T
  }
  const json = JSON.parse(text) as T
  flow(`BFF ← POST ${path} ${response.status}`, summarize(json))
  return json
}

function summarize(value: unknown): unknown {
  if (Array.isArray(value)) {
    return { count: value.length, sample: value[0] ?? null }
  }
  if (value && typeof value === 'object') {
    const record = value as Record<string, unknown>
    const pick = ['name', 'sevaId', 'sevaReference', 'departmentReference', 'status', 'serviceCode', 'departmentCode', 'expiresAt']
    const slim: Record<string, unknown> = {}
    for (const key of pick) {
      if (key in record) slim[key] = record[key]
    }
    return Object.keys(slim).length > 0 ? slim : value
  }
  return value
}

export const api = {
  serviceNotice: () => request<PublicServiceNotice | null>('/api/v1/service-notice'),
  governmentNotifications: () => request<PublicGovNotification[]>('/api/v1/notifications'),
  me: () => request<Me>('/api/v1/me'),
  logout: () => request<{ ok: boolean }>('/api/v1/auth/logout', { method: 'POST' }),
  mobileStart: (mobile: string) =>
    request<{ challengeId: string; debugOtp?: string | null }>('/api/v1/auth/mobile/start', {
      method: 'POST',
      body: JSON.stringify({ mobile }),
    }),
  mobileVerify: (body: { challengeId: string; otp: string }) =>
    request<{ found: boolean; mobile: string; returnTo?: string | null; digiLockerPendingId?: string | null }>(
      '/api/v1/auth/mobile/verify',
      { method: 'POST', body: JSON.stringify(body) },
    ),
  digilockerStart: (body: { flow: string; returnTo?: string; pendingId?: string }) =>
    request<{ redirectUrl: string }>('/api/v1/auth/digilocker/start', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  digilockerDocuments: () => request<DigiLockerDocument[]>('/api/v1/me/digilocker/documents'),
  nearestGramaOne: (lat: number, lon: number, limit = 5) =>
    request<GramaOneCentre[]>(`/api/v1/centres/gramaone/nearest?lat=${lat}&lon=${lon}&limit=${limit}`),
  gramaOneDistricts: () => request<string[]>('/api/v1/centres/gramaone/districts'),
  searchGramaOne: (district: string, q?: string) =>
    request<GramaOneCentre[]>(`/api/v1/centres/gramaone?district=${encodeURIComponent(district)}${q ? `&q=${encodeURIComponent(q)}` : ''}`),
  digilockerSync: () => request<DigiLockerDocument[]>('/api/v1/me/digilocker/documents/sync', { method: 'POST' }),
  departments: () => request<Department[]>('/api/v1/departments'),
  schemes: (q?: string, department?: string, category?: string) => {
    const p = new URLSearchParams()
    if (q) p.set('q', q)
    if (department) p.set('department', department)
    if (category) p.set('category', category)
    const qs = p.toString()
    return request<SchemeSummary[]>(`/api/v1/schemes${qs ? `?${qs}` : ''}`)
  },
  /** Landing page's "Frequently requested" -- ranked by real apply counts, not an arbitrary
   *  slice of the catalog. See CatalogController.ListPopularSchemes. */
  schemesPopular: (limit = 6) => request<SchemeSummary[]>(`/api/v1/schemes/popular?limit=${limit}`),
  search: (q: string) =>
    request<{
      kind: 'none' | 'single' | 'clarify'
      service?: SchemeSummary | null
      clarify?: { question: string; options: { serviceCode: string; name: string; departmentName: string }[] } | null
    }>(`/api/v1/search?q=${encodeURIComponent(q)}`),
  scheme: (code: string) => request<SchemeDetail>(`/api/v1/schemes/${code}`),
  // departmentCode is required -- service_id is only unique within a department, not globally.
  // requiresLogin=true means a real deep link exists for this service but nobody's logged in --
  // applyUrl/expiresAt are null in that case (see CatalogController.CreateApplyToken). A plain
  // homepage-fallback service resolves with no login needed at all, so this is reachable
  // (and meaningful) for an anonymous caller, unlike most other bff calls.
  createApplyToken: (departmentCode: string, serviceCode: string, answers?: Record<string, string>) =>
    request<{ requiresLogin: boolean; applyUrl: string | null; expiresAt: string | null; isHomepageFallback: boolean; postForm?: { url: string; fields: Record<string, string> } | null; needsInput?: { key: string; label: string }[] | null }>(
      `/api/v1/departments/${encodeURIComponent(departmentCode)}/schemes/${encodeURIComponent(serviceCode)}/apply-token`,
      { method: 'POST', body: JSON.stringify({ answers: answers ?? null }) },
    ),
  /** Public interest-measurement click, decoupled from apply-token minting -- fire-and-forget
   *  from callers (see applyFlow.ts's trackApplyClickBestEffort), never throw into a
   *  navigation path. assistedMode/sevaOneUserId only matter for the operator-assisted
   *  trigger point. */
  trackApplyClick: (departmentCode: string, serviceCode: string, opts?: { assistedMode?: boolean; sevaOneUserId?: number }) =>
    request<{ tracked: boolean }>(
      `/api/v1/departments/${encodeURIComponent(departmentCode)}/schemes/${encodeURIComponent(serviceCode)}/apply-click`,
      { method: 'POST', body: JSON.stringify({ assistedMode: opts?.assistedMode ?? false, sevaOneUserId: opts?.sevaOneUserId ?? null }) },
    ),
  /** The AI chat's Apply button calls this instead of opening sevaone-rag's own apply_url
   *  directly -- the backend is the single source of truth for both the real apply URL and the
   *  click count now, matched by service NAME (sevaone-rag has no code that lines up with this
   *  catalog's own). matched:false means sevaone-rag resolved a service this catalog doesn't
   *  have yet -- the caller falls back to whatever URL the AI already had. */
  aiApplyClick: (
    serviceName: string,
    departmentCode?: string | null,
    operatorHint?: string | null,
    opts?: { assistedMode?: boolean; sevaOneUserId?: number },
  ) =>
    request<{ matched: boolean; requiresLogin: boolean; applyUrl: string | null; expiresAt: string | null; isHomepageFallback: boolean }>(
      '/api/v1/schemes/ai-apply-click',
      {
        method: 'POST',
        body: JSON.stringify({
          serviceName,
          departmentCode: departmentCode ?? null,
          operatorHint: operatorHint ?? null,
          assistedMode: opts?.assistedMode ?? false,
          sevaOneUserId: opts?.sevaOneUserId ?? null,
        }),
      },
    ),
  notifications: () =>
    request<{ id: string; channel: string; subject: string; body: string; createdAt: string; sentAt?: string | null }[]>(
      '/api/v1/me/notifications',
    ),
  audit: () =>
    request<{ id: string; action: string; resource: string; detail?: string | null; at: string }[]>('/api/v1/me/audit'),
  digilockerFetch: (departmentCode: string, documentType: string) =>
    request<{ documentType: string; documentId: string; found: boolean; note: string }>(
      `/api/v1/departments/${encodeURIComponent(departmentCode)}/digilocker/fetch`,
      { method: 'POST', body: JSON.stringify({ documentType }) },
    ),
  initiatePayment: (departmentCode: string, body: { serviceCode: string; amountInr: number; purpose: string }) =>
    request<{ paymentId: string; gateway: string; status: string; note: string }>(
      `/api/v1/departments/${encodeURIComponent(departmentCode)}/payments`,
      { method: 'POST', body: JSON.stringify(body) },
    ),
  requestESign: (departmentCode: string, body: { documentId: string; signerName: string }) =>
    request<{ requestId: string; status: string; note: string }>(
      `/api/v1/departments/${encodeURIComponent(departmentCode)}/esign`,
      { method: 'POST', body: JSON.stringify(body) },
    ),
  // Matches OperatorController's real OperatorLoginBody(OperatorCode, Password) -- this
  // previously sent {email,password}, which the backend never accepted.
  operatorLogin: (body: { operatorCode: string; password: string }) =>
    request<{ operatorName: string | null }>('/api/v1/operator/login', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  operatorLogout: () => request<{ ok: boolean }>('/api/v1/operator/logout', { method: 'POST' }),
  // Self-claim: an operator on the whitelist roster (portal.operator_whitelist) sets their own
  // credentials the first time -- see OperatorClaimService. No existing operator has a password
  // otherwise; this is how one gets created.
  operatorClaimStart: (body: { userId: number; mobile: string; password: string }) =>
    request<{ challengeId: string }>('/api/v1/operator/claim/start', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  operatorClaimVerify: (body: { challengeId: string; otp: string }) =>
    request<{ operatorCode: string }>('/api/v1/operator/claim/verify', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  // Real /api/v1/operator/me contract (OperatorMeResult) -- deliberately not reusing the
  // operatorMe() below, whose {centreCode,channel,walletBalance,...} shape has no backend
  // behind it yet. Used only by the assisted-mode login/apply flow (Catalog 2.0 bind-and-apply).
  operatorSession: () =>
    request<{
      operatorCode: string
      operatorName: string | null
      boundCitizen: { sevaOneUserId: number; name: string; mobile: string | null } | null
    }>('/api/v1/operator/me'),
  operatorSendCitizenOtp: (mobile: string) =>
    request<{ challengeId: string }>('/api/v1/operator/citizens/otp', {
      method: 'POST',
      body: JSON.stringify({ mobile }),
    }),
  operatorVerifyCitizenOtp: (challengeId: string, otp: string) =>
    request<{ sevaOneUserId: number; name: string; mobile: string | null }>('/api/v1/operator/citizens/verify', {
      method: 'POST',
      body: JSON.stringify({ challengeId, otp }),
    }),
  operatorBoundCitizen: () =>
    request<{ sevaOneUserId: number; name: string; mobile: string | null } | null>('/api/v1/operator/citizens/bound'),
  // Matches OperatorController's real assisted apply-token endpoint (OperatorController.
  // AssistedApplyToken) -- distinct from createApplyToken above, which is the citizen-session
  // endpoint and ignores assistedMode entirely. sevaOneUserId is the operator's already-bound
  // citizen, never anything this call invents.
  operatorApplyToken: (departmentCode: string, serviceCode: string, sevaOneUserId: number) =>
    request<{ applyUrl: string; expiresAt: string | null; isHomepageFallback: boolean }>('/api/v1/operator/apply-token', {
      method: 'POST',
      body: JSON.stringify({ departmentCode, serviceCode, sevaOneUserId }),
    }),
  operatorMe: () =>
    request<{
      operatorId: string
      operatorName: string
      centreCode: string
      centreName: string
      channel: string
      walletBalance: number
      email?: string | null
      activeCitizenId?: string | null
    }>('/api/v1/operator/me'),
  operatorCollectFee: (body: {
    serviceCode: string
    amountInr: number
    idempotencyKey: string
    citizenId?: string
  }) =>
    request<{ txnId: string; status: string; amount: number; walletBalance: number }>(
      '/api/v1/operator/wallet/collect',
      { method: 'POST', body: JSON.stringify(body) },
    ),
  operatorReverseFee: (body: { txnId: string; reason?: string }) =>
    request<{ txnId: string; status: string; amount: number; walletBalance: number }>(
      '/api/v1/operator/wallet/reverse',
      { method: 'POST', body: JSON.stringify(body) },
    ),
  operatorTxns: () =>
    request<{ id: string; status: string; amount: number; serviceCode?: string | null; createdAt: string; citizenId?: string | null }[]>(
      '/api/v1/operator/me/txns',
    ),
  operatorShiftReport: () =>
    request<{
      operatorId: string
      centreCode: string
      channel: string
      shiftStartedAt: string
      walletBalance: number
      collections: number
      reversals: number
      collectedInr: number
      reversedInr: number
    }>('/api/v1/operator/me/shift-report'),
  operatorCitizenLookup: (mobile: string) =>
    request<{
      citizenId: string
      sevaOneUserId: number
      name: string
      mobile?: string | null
      aadhaarMasked?: string | null
      email?: string | null
      profileCompleted: boolean
      found: boolean
    }>('/api/v1/operator/citizens/lookup', { method: 'POST', body: JSON.stringify({ mobile }) }),
  operatorCitizenChallenge: (mobile: string) =>
    request<{ challengeId: string; mobileHint: string }>('/api/v1/operator/citizens/challenge', {
      method: 'POST',
      body: JSON.stringify({ mobile }),
    }),
  operatorCitizenBind: (body: { challengeId: string; otp: string }) =>
    request<{
      citizenId: string
      sevaOneUserId: number
      name: string
      mobile?: string | null
      aadhaarMasked?: string | null
      email?: string | null
      profileCompleted: boolean
      found: boolean
    }>('/api/v1/operator/citizens/bind', { method: 'POST', body: JSON.stringify(body) }),
  operatorCitizenRegister: (body: {
    mobile: string
    name: string
    aadhaarLast4?: string
    address?: string
    district?: string
    taluk?: string
    email?: string
  }) =>
    request<{
      citizenId: string
      sevaOneUserId: number
      name: string
      mobile?: string | null
      aadhaarMasked?: string | null
      email?: string | null
      profileCompleted: boolean
      found: boolean
    }>('/api/v1/operator/citizens/register', { method: 'POST', body: JSON.stringify(body) }),
  operatorCitizenProfile: (body: {
    mobile: string
    name: string
    aadhaarLast4?: string
    address?: string
    district?: string
    taluk?: string
    email?: string
  }) =>
    request<{
      citizenId: string
      sevaOneUserId: number
      name: string
      mobile?: string | null
      aadhaarMasked?: string | null
      email?: string | null
      profileCompleted: boolean
      found: boolean
    }>('/api/v1/operator/citizens/profile', { method: 'POST', body: JSON.stringify(body) }),
  operatorCitizenUnbind: () => request<{ ok: boolean }>('/api/v1/operator/citizens/unbind', { method: 'POST' }),
  operatorCitizenBound: () =>
    request<{
      bound: boolean
      citizen?: {
        citizenId: string
        sevaOneUserId: number
        name: string
        mobile?: string | null
        aadhaarMasked?: string | null
        email?: string | null
        profileCompleted: boolean
        found: boolean
      }
    }>('/api/v1/operator/citizens/bound'),
  operatorSubmit: (body: {
    departmentCode: string
    serviceCode: string
    applicantName: string
    address: string
    purpose?: string
    district?: string
    taluk?: string
  }) => request<ApplicationDetail>('/api/v1/operator/applications', { method: 'POST', body: JSON.stringify(body) }),
  applications: () => request<ApplicationSummary[]>('/api/v1/applications'),
  application: (ref: string) => request<ApplicationDetail>(`/api/v1/applications/${encodeURIComponent(ref)}`),
  /** Asks the department for the application's current status (token integration), then returns it as it is now.
   *  The server asks the department at most once every few minutes, however often this is called. */
  refreshApplicationStatus: (ref: string) =>
    request<ApplicationDetail>(`/api/v1/applications/${encodeURIComponent(ref)}/status-refresh`, { method: 'POST', body: '{}' }),
  // departmentCode is required -- service_id is only unique within a department, not globally.
  submit: (body: {
    departmentCode: string
    serviceCode: string
    applicantName: string
    address: string
    purpose?: string
    district?: string
    taluk?: string
  }) => request<ApplicationDetail>('/api/v1/applications', { method: 'POST', body: JSON.stringify(body) }),
  track: (ref: string) => request<TrackApiResult>(`/api/v1/track/${encodeURIComponent(ref)}`),
  kpi: (department?: string) =>
    request<KpiDashboard>(`/api/v1/kpi${department ? `?department=${encodeURIComponent(department)}` : ''}`),
  grievances: () => request<Grievance[]>('/api/v1/grievances'),
  submitGrievance: (body: { name: string; mobile: string; departmentCode: string; details: string }) =>
    request<Grievance>('/api/v1/grievances', { method: 'POST', body: JSON.stringify(body) }),
  openDeptSession: (code: string, service?: string) =>
    request<{
      departmentCode: string
      expiresAt: string
      reused: boolean
    }>(
      `/api/v1/departments/${encodeURIComponent(code)}/sessions${service ? `?service=${encodeURIComponent(service)}` : ''}`,
      { method: 'POST' },
    ),
  updateProfile: (body: {
    name: string
    mobile: string
    email?: string
    address: string
    district: string
    taluk: string
    stateName?: string
    villageName?: string
    dateOfBirth?: string
    gender?: string
    aadhaarLast4?: string
    occupation?: string
  }) => request<Me>('/api/v1/me/profile', { method: 'PATCH', body: JSON.stringify(body) }),
  passwordLogin: (body: { username: string; password: string; returnTo?: string }) =>
    request<{
      requiresOtp: boolean
      challengeId?: string | null
      emailHint?: string | null
      debugOtp?: string | null
      returnTo?: string | null
    }>('/api/v1/auth/password', { method: 'POST', body: JSON.stringify(body) }),
  verifyOtp: (body: { challengeId: string; otp: string }) =>
    request<{ returnTo: string }>('/api/v1/auth/otp', { method: 'POST', body: JSON.stringify(body) }),

  // ---- Service Catalogue (department -> sub-department -> service) ----
  catalogDepartments: () => request<CatalogDepartment[]>('/api/v1/catalog/departments'),
  catalogDepartment: (code: string) => request<CatalogDepartmentDetail>(`/api/v1/catalog/departments/${encodeURIComponent(code)}`),
  catalogService: (departmentCode: string, serviceCode: string) =>
    request<CatalogService>(`/api/v1/catalog/services/${encodeURIComponent(departmentCode)}/${encodeURIComponent(serviceCode)}`),
  catalogSearch: (q: string) => request<CatalogSearchResult>(`/api/v1/catalog/search?q=${encodeURIComponent(q)}`),

  // ---- Catalog admin: auth ----
  adminLogin: (body: { adminCode: string; password: string }) =>
    request<{ adminName: string }>('/api/v1/admin/catalog/auth/login', { method: 'POST', body: JSON.stringify(body) }),
  adminLogout: () => request<{ ok: boolean }>('/api/v1/admin/catalog/auth/logout', { method: 'POST' }),
  adminMe: () => request<{ adminName: string }>('/api/v1/admin/catalog/auth/me'),
  adminRoles: () => request<{ roleKey: string; displayName: string; description: string | null }[]>('/api/v1/admin/catalog/roles'),

  // ---- Catalog admin: ingestion ----
  adminImportUpload: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return requestForm<{ batchId: string }>('/api/v1/admin/catalog/imports/file', form)
  },
  adminImportStatus: (batchId: string) =>
    request<AdminImportBatch>(`/api/v1/admin/catalog/imports/${encodeURIComponent(batchId)}`),
  adminImportRows: (batchId: string, action?: string) =>
    request<AdminImportRow[]>(
      `/api/v1/admin/catalog/imports/${encodeURIComponent(batchId)}/rows${action ? `?action=${encodeURIComponent(action)}` : ''}`,
    ),
  adminImportPublish: (batchId: string) =>
    request<{ published: number; skipped: number }>(`/api/v1/admin/catalog/imports/${encodeURIComponent(batchId)}/publish`, {
      method: 'POST',
    }),
  adminImportDiscard: (batchId: string) =>
    request<void>(`/api/v1/admin/catalog/imports/${encodeURIComponent(batchId)}/discard`, { method: 'POST' }),

  // ---- Catalog admin: visibility & audit ----
  adminServiceVisibility: (departmentCode: string, serviceCode: string) =>
    request<AdminServiceVisibility[]>(
      `/api/v1/admin/catalog/services/${encodeURIComponent(departmentCode)}/${encodeURIComponent(serviceCode)}/visibility`,
    ),
  adminSetServiceVisibility: (departmentCode: string, serviceCode: string, roleKey: string, grant: boolean) =>
    request<void>(
      `/api/v1/admin/catalog/services/${encodeURIComponent(departmentCode)}/${encodeURIComponent(serviceCode)}/visibility/${encodeURIComponent(roleKey)}`,
      { method: 'PUT', body: JSON.stringify({ grant }) },
    ),
  adminBulkVisibility: (body: { departmentCode: string; subDepartmentName?: string | null; roleKeys: string[]; grant: boolean }) =>
    request<{ matchedServices: number; applied: number }>('/api/v1/admin/catalog/services/visibility/bulk', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  adminChangeLog: (departmentCode?: string, serviceCode?: string, limit = 50) => {
    const params = new URLSearchParams()
    if (departmentCode) params.set('departmentCode', departmentCode)
    if (serviceCode) params.set('serviceCode', serviceCode)
    params.set('limit', String(limit))
    return request<AdminChangeLogEntry[]>(`/api/v1/admin/catalog/change-log?${params.toString()}`)
  },
}

export type AdminImportBatch = {
  id: string
  sourceFilename: string
  sourceFormat: string
  uploadedBy: string
  uploadedAt: string
  status: 'validating' | 'ready_to_publish' | 'published' | 'failed' | 'discarded'
  rowCount: number
  errorCount: number
  warningCount: number
  publishedAt: string | null
  publishedBy: string | null
}

export type AdminImportRow = {
  id: string
  rowNumber: number
  action: 'insert' | 'update' | 'unchanged' | 'conflict' | null
  errors: { field: string; message: string }[]
  warnings: { field: string; message: string }[]
  departmentCode: string
  serviceCode: string
  serviceName: string
}

export type AdminServiceVisibility = { roleKey: string; displayName: string; grantedAt: string; grantedBy: string | null }

export type AdminChangeLogEntry = {
  id: string
  serviceCode: string | null
  serviceName: string | null
  batchId: string | null
  changedAt: string
  changedBy: string
  changeType: string
  diff: string | null
}
