import type { Lang } from '../i18n'
import { t } from '../i18n'

export type AuditEvent = {
  action: string
  resource: string
  detail?: string | null
}

export type FriendlyAudit = {
  title: string
  subtitle: string
}

const DEPT_KEYS: Record<string, 'deptRevenue' | 'deptRto' | 'deptFood' | 'deptHealth' | 'deptEducation' | 'deptLabour' | 'deptMunicipal' | 'deptBhoomi'> = {
  revenue: 'deptRevenue',
  rto: 'deptRto',
  food: 'deptFood',
  health: 'deptHealth',
  education: 'deptEducation',
  labour: 'deptLabour',
  municipal: 'deptMunicipal',
  bhoomi: 'deptBhoomi',
}

const SERVICE_KEYS: Record<string, 'svcIncome' | 'svcCaste' | 'svcResidence' | 'svcIncomeCaste'> = {
  SVC001: 'svcIncome',
  SVC002: 'svcCaste',
  SVC003: 'svcResidence',
  SVC004: 'svcIncomeCaste',
}

function describeApplyResource(lang: Lang, resource: string): string {
  // Newer audit rows already store a ready display string ("Service · Department", written
  // by PortalApplyService) -- any whitespace means it's already human text. Only the machine
  // forms ("deptCode/serviceCode", or a bare "deptCode") get expanded and localised here.
  if (/\s/.test(resource)) return resource
  const [dept, service] = resource.split('/')
  if (!dept) return t(lang, 'auditApply')
  const deptName = DEPT_KEYS[dept.toLowerCase()] ? t(lang, DEPT_KEYS[dept.toLowerCase()]!) : dept
  // Fall back to the raw code (e.g. "RD-SSP-001") rather than dropping the service entirely
  // when it predates the seeded SVC001-004 lookup.
  const serviceName = service && SERVICE_KEYS[service] ? t(lang, SERVICE_KEYS[service]!) : (service || null)
  if (serviceName) return `${serviceName} · ${deptName}`
  return deptName
}

export function friendlyAudit(a: AuditEvent, lang: Lang = 'en'): FriendlyAudit {
  switch (a.action) {
    case 'auth.login':
      return { title: t(lang, 'auditSignedIn'), subtitle: t(lang, 'auditSignedInSub') }
    case 'auth.logout':
      return { title: t(lang, 'auditSignedOut'), subtitle: t(lang, 'auditSignedOutSub') }
    case 'auth.profile.complete':
      return { title: t(lang, 'auditProfile'), subtitle: t(lang, 'auditProfileSub') }
    // Every real "apply" outcome -- a minted deep-link token, a homepage fallback when the
    // department has no deep link, or an AJSK / Sakala-token redirect -- lands the citizen on
    // a department service, so all read as "started an application" with the service and
    // department as the subtitle. Without these cases the raw action ("apply homepage_fallback")
    // leaked into the Recent Activity feed via the default branch below.
    case 'apply.token.issued':
    case 'apply.token.issued.test':
    case 'apply.homepage_fallback':
    case 'apply.homepage_fallback.test':
      return { title: t(lang, 'auditApply'), subtitle: describeApplyResource(lang, a.resource) }
    case 'apply.token.issued.assisted':
    case 'apply.homepage_fallback.assisted':
    case 'apply.ajsk_redirect.assisted':
    case 'apply.sakala_token_redirect.assisted':
      return { title: t(lang, 'auditApplyAssisted'), subtitle: describeApplyResource(lang, a.resource) }
    case 'application.submit':
      return { title: t(lang, 'auditSubmit'), subtitle: t(lang, 'auditSubmitSub') }
    case 'grievance.submit':
      return { title: t(lang, 'auditGrievance'), subtitle: t(lang, 'auditGrievanceSub') }
    case 'department.session.open':
      return { title: t(lang, 'auditDeptSession'), subtitle: describeApplyResource(lang, a.resource) }
    default:
      return {
        title: a.action.replace(/\./g, ' '),
        subtitle: t(lang, 'auditFallback'),
      }
  }
}
