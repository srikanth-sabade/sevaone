/**
 * Service catalog: department -> (optional) sub-department -> service.
 *
 * Data is loaded from the Sakala/Sevasindhu redirect-URL CSV via
 * `catalogFromCsv.generated.ts`. Field shape still mirrors the real schema so
 * this can later be swapped for a live API without reshaping the UI.
 */
import type { Lang } from '../i18n'
import { CSV_CATALOG } from './catalogFromCsv.generated'

export type MockService = {
  code: string
  name: string
  nameKannada: string
  shortDescription: string
  shortDescriptionKannada: string
  description: string
  descriptionKannada: string
  category: string
  sakalaDays: number
  jurisdiction: 'Taluk' | 'District' | 'State'
  modeOfService: 'Online' | 'Offline' | 'Both'
  isMandatory: boolean
  departmentFee: number
  serviceCharge: number
  documents: string[]
  documentsKannada: string[]
  eligibility: string[]
  eligibilityKannada: string[]
  steps: string[]
  stepsKannada: string[]
  aliases: string[]
  remarks?: string
  remarksKannada?: string
  portal?: string
  /** External department apply URL — opened in a new tab by Apply now. */
  applyUrl?: string
}

export type MockSubDepartment = {
  code: string
  name: string
  nameKannada: string
  services: MockService[]
}

export type MockDepartment = {
  code: string
  name: string
  nameKannada: string
  portalName: string
  subDepartments: MockSubDepartment[]
  /** Services directly under the department, with no sub-department — mirrors the
   *  real schema's nullable `sub_department_id` (not every department has sub-departments). */
  directServices: MockService[]
}

const JURISDICTION_KN: Record<MockService['jurisdiction'], string> = {
  Taluk: 'ತಾಲೂಕು',
  District: 'ಜಿಲ್ಲೆ',
  State: 'ರಾಜ್ಯ',
}

const MODE_KN: Record<MockService['modeOfService'], string> = {
  Online: 'ಆನ್‌ಲೈನ್',
  Offline: 'ಆಫ್‌ಲೈನ್',
  Both: 'ಎರಡೂ',
}

export function localizeJurisdiction(lang: Lang, value: MockService['jurisdiction']): string {
  return lang === 'kn' ? JURISDICTION_KN[value] : value
}

export function localizeMode(lang: Lang, value: MockService['modeOfService']): string {
  return lang === 'kn' ? MODE_KN[value] : value
}

export const MOCK_CATALOG: MockDepartment[] = CSV_CATALOG

export function findMockService(code: string): MockService | undefined {
  for (const dept of MOCK_CATALOG) {
    for (const svc of dept.directServices) {
      if (svc.code === code) return svc
    }
    for (const sub of dept.subDepartments) {
      for (const svc of sub.services) {
        if (svc.code === code) return svc
      }
    }
  }
  return undefined
}

export function findMockServiceContext(
  code: string,
): { department: MockDepartment; subDepartment?: MockSubDepartment } | undefined {
  for (const dept of MOCK_CATALOG) {
    if (dept.directServices.some((s) => s.code === code)) return { department: dept }
    for (const sub of dept.subDepartments) {
      if (sub.services.some((s) => s.code === code)) return { department: dept, subDepartment: sub }
    }
  }
  return undefined
}
