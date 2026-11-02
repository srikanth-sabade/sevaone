export type Me = {
  id: string
  sevaId: string
  name: string
  username: string
  mobile?: string | null
  email?: string | null
  address?: string | null
  district?: string | null
  taluk?: string | null
  stateName?: string | null
  villageName?: string | null
  dateOfBirth?: string | null
  gender?: string | null
  aadhaarMasked?: string | null
  nameKannada?: string | null
  addressKannada?: string | null
  districtKannada?: string | null
  stateKannada?: string | null
  villageKannada?: string | null
  profileCompleted: boolean
}

export type PublicServiceNotice = {
  title: string
  message: string
  severity: 'info' | 'warning' | 'critical'
  startsAt: string | null
  endsAt: string | null
  updatedAt: string
}

export type PublicGovNotification = {
  id: string
  title: string
  body: string
  publishedAt: string
}

export type Department = {
  id: string
  code: string
  name: string
  portalName: string
  portalDomain: string
  isIntegrated: boolean
  schemeCount: number
}

export type SchemeSummary = {
  serviceCode: string
  name: string
  departmentCode: string
  departmentName: string
  category: string
  sakalaDays: number
  departmentFee: number | null
  serviceCharge: number | null
  description: string
  eligibility: string[]
  aliases: string[]
  departmentIntegrated: boolean
  // Only meaningful from schemesPopular() - every other caller of this shared type leaves
  // these at 0. catalogueApplyCount + aiApplyCount always sum to totalApplyCount.
  catalogueApplyCount: number
  aiApplyCount: number
  totalApplyCount: number
}

export type SchemeDetail = SchemeSummary & {
  documents: string[]
  steps: string[]
  portal?: string | null
}

export type ApplicationSummary = {
  sevaReference: string
  departmentReference?: string | null
  serviceCode: string
  serviceName: string
  departmentCode: string
  departmentName: string
  status: string
  submittedAt: string
  sakalaDueAt: string
}

export type ApplicationDetail = ApplicationSummary & {
  applicantName: string
  address: string
  purpose?: string | null
  district?: string | null
  taluk?: string | null
  timeline: { status: string; note: string; at: string }[]
  acknowledgementAvailable?: boolean
  statusCheckAvailable?: boolean
}

export type TrackResult = {
  sevaReference: string
  serviceName: string
  departmentName: string
  status: string
  submittedAt: string
  sakalaDueAt: string
}

/** One escalation/complaint slot on a SakalaTrackResult -- absent entirely when that complaint
 *  was never filed (not an object with blank fields). */
export type SakalaComplaint = {
  complaintNo: string
  complaintDate: string | null
  closedDate: string | null
  dueDate: string | null
  statusCode: string | null
  statusDescription: string | null
  daysPending: number | null
}

/** A real Sakala government record, looked up live by GSC number when the tracking number a
 *  citizen entered doesn't match any SevaOne application -- see TrackApiResult.source. */
export type SakalaTrackResult = {
  gscNo: string
  applicantName: string
  serviceName: string
  institutionName: string
  officerName: string | null
  sakalaDate: string | null
  sakalaClosedDate: string | null
  dueDate: string | null
  daysPending: number | null
  statusCode: string
  statusDescription: string
  firstComplaint: SakalaComplaint | null
  secondComplaint: SakalaComplaint | null
}

/** GET /api/v1/track/{reference}'s actual response shape: exactly one of sevaOne/sakala is set,
 *  discriminated by source. */
export type TrackApiResult = {
  source: 'sevaone' | 'sakala'
  sevaOne: TrackResult | null
  sakala: SakalaTrackResult | null
}

export type Faq = {
  id: string
  category: string
  question: string
  answer: string
  relatedServices: string[]
}

export type KpiDashboard = {
  received: number
  disposed: number
  pending: number
  sakalaPercent: number
  averageTatDays: number
  departments: {
    departmentCode: string
    departmentName: string
    received: number
    disposed: number
    pending: number
    sakalaPercent: number
    averageTatDays: number
    grievances: number
  }[]
  ageing: { bucket: string; count: number }[]
  channels: { channel: string; percent: number }[]
}

export type Grievance = {
  reference: string
  status: string
  departmentCode: string
  createdAt: string
  details: string
}

export type AiResponseType = 'Clarify' | 'Resolved' | 'Fallback' | 'NoMatch' | 'Greeting'

export type AiResolvedService = {
  /** sevaone-rag's own free-text department label (e.g. "AJSK (Atal Jana Snehi Kendra)") —
   *  display only, never a real catalog match key. */
  departmentId?: string | null
  /** Best-guess real .NET-catalog department code (e.g. "revenue") — sevaone-rag's own
   *  disambiguating hint for the AI apply-click endpoint, per app/agent/department_mapping.py.
   *  May be null when nothing recognized maps to a real department. */
  departmentCode?: string | null
  /** Specific real-catalog RTC operator substring (KSRTC/NWKRTC/NEKRTC/BMTC) for the regional
   *  bus-pass family only — null for the ~95% of services that aren't one. See
   *  department_mapping.map_department_to_operator_hint's own doc comment for why this exists
   *  separately from departmentCode (which is the same broad "transport" code for every
   *  operator and can't tell them apart). */
  operatorHint?: string | null
  serviceId: string
  serviceName: string
  kannadaServiceName?: string | null
  /** Catalog service code (e.g. "111") — used for View more → /{serviceCode}. */
  serviceCode?: string | null
}

export type AiCandidate = {
  serviceId: string
  serviceName: string
  kannadaServiceName?: string | null
  shortDescription?: string | null
  serviceCode?: string | null
}

export type AiChatResponse = {
  sessionId: string
  responseType: AiResponseType
  message: string
  resolvedService?: AiResolvedService | null
  candidates: AiCandidate[]
  references: string[]
  suggestedQuestions: string[]
  /** Optional top-level catalog service code when a service is resolved. */
  serviceCode?: string | null
}
