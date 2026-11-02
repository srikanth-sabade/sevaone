import {
  Bus,
  Building2,
  GraduationCap,
  HardHat,
  HeartHandshake,
  HeartPulse,
  Landmark,
  ShoppingBasket,
  Sprout,
  Users,
  type LucideIcon,
} from 'lucide-react'

/**
 * Types matching the backend's Service Catalogue API (CatalogController, /api/v1/catalog/*)
 * — see backend/SevaOne.Business/Discovery/DTOs/CatalogHierarchyDtos.cs. Data is fetched via
 * the hooks in ../hooks/useCatalog, not imported statically — catalogHierarchy.generated.ts
 * is gone (see the migration this replaced in architecture.md).
 */

/** Source spreadsheet rows use the literal text "NA"/"N/A" as a placeholder for "not
 * provided" instead of leaving the cell blank -- treat it the same as null/empty so it never
 * renders to citizens as if it were real content. */
export function isMeaningfulText(value: string | null | undefined): value is string {
  if (!value) return false
  const trimmed = value.trim()
  return trimmed.length > 0 && trimmed.toUpperCase() !== 'NA' && trimmed.toUpperCase() !== 'N/A'
}

export type CatalogDepartment = {
  code: string
  name: string
  departmentUrl: string | null
  departmentUrlKannada: string | null
  subDepartmentCount: number
  serviceCount: number
}

export type CatalogSubDepartment = {
  name: string
  serviceCount: number
}

export type CatalogService = {
  serviceCode: string
  /** `${departmentCode}-${serviceCode}` — globally unique, used in URLs. Computed server-side, not persisted. */
  compositeCode: string
  subDepartmentName: string | null
  name: string
  nameKannada: string | null
  shortDescription: string | null
  description: string | null
  descriptionKannada: string | null
  isSakalaEnabled: boolean | null
  sakalaLabelRaw: string | null
  sakalaDays: number
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
  applyUrl: string | null
  applyUrlKannada: string | null
  serviceUrl: string | null
  /** 'ajsk' | 'sakala_token' | null — which special redirect operator-assisted Apply should use
   *  instead of the normal deep-link token. Citizen Apply never reads this. */
  integrationMethod: string | null
}

export type CatalogDepartmentDetail = {
  code: string
  name: string
  departmentUrl: string | null
  departmentUrlKannada: string | null
  subDepartments: CatalogSubDepartment[]
  services: CatalogService[]
}

export type CatalogSearchServiceResult = {
  departmentCode: string
  departmentName: string
  serviceCode: string
  compositeCode: string
  subDepartmentName: string | null
  name: string
  nameKannada: string | null
  shortDescription: string | null
  sakalaDays: number
}

export type CatalogSearchResult = {
  departments: CatalogDepartment[]
  services: CatalogSearchServiceResult[]
}

/** Presentation-only, admin-settable in spirit but not worth a DB column for ~13 departments
 *  that rarely change — kept here exactly like before the API-backed rewrite. Codes are now
 *  lowercase slugs (`revenue`, `education`, ...), not the short xlsx codes (`RD`, `ED`) —
 *  reconciled onto the pre-existing department rows once real and demo data were merged. */
const DEPARTMENT_ICONS: Record<string, LucideIcon> = {
  revenue: Landmark,
  education: GraduationCap,
  transport: Bus,
  labour: HardHat,
  wcd: HeartHandshake,
  food: ShoppingBasket,
  'social-welfare': Users,
  agriculture: Sprout,
  health: HeartPulse,
  rdpr: Building2,
}

export function departmentIcon(deptCode: string): LucideIcon {
  return DEPARTMENT_ICONS[deptCode] ?? Landmark
}

export function departmentNameMatches(dept: CatalogDepartment, query: string): boolean {
  const q = query.trim().toLowerCase()
  if (!q) return false
  return dept.name.toLowerCase().includes(q)
}

/** Department-scoped search only — the landing page's global search goes through the server
 *  (see useCatalogSearch) since not every department's services are loaded client-side. This
 *  runs against a department's already-fetched CatalogDepartmentDetail.services list. */
export function matchesDepartmentQuery(service: CatalogService, subDepartmentName: string | undefined, lang: 'en' | 'kn', query: string): boolean {
  const q = query.trim().toLowerCase()
  if (!q) return true
  const name = (lang === 'kn' && service.nameKannada) || service.name
  return (
    name.toLowerCase().includes(q) ||
    service.name.toLowerCase().includes(q) ||
    service.serviceCode.toLowerCase().includes(q) ||
    (subDepartmentName?.toLowerCase().includes(q) ?? false)
  )
}
