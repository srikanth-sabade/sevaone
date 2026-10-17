import * as XLSX from 'xlsx'
import type { CatalogImportFormat, CatalogImportRow } from '../types'

export interface CatalogueImportIssue {
  row: number
  message: string
}

export interface CatalogueImportPreview {
  filename: string
  format: CatalogImportFormat
  rows: CatalogImportRow[]
  issues: CatalogueImportIssue[]
}

const columns = [
  'department_code', 'department_name', 'department_home_url_en', 'department_home_url_kn', 'sub_department_name',
  'service_code', 'service_name', 'service_name_kn', 'short_description', 'description', 'description_kn',
  'is_sakala_enabled', 'sakala_label', 'sakala_days', 'department_fee', 'service_charge', 'documents', 'documents_kn',
  'eligibility', 'eligibility_kn', 'procedure', 'procedure_kn', 'jurisdiction', 'category', 'mode_of_service',
  'is_mandatory', 'department_service_id', 'service_url', 'apply_url', 'apply_url_kn',
] as const

const requiredColumns = ['department_code', 'department_name', 'service_code', 'service_name'] as const

function text(value: unknown): string {
  return value === null || value === undefined ? '' : String(value).trim()
}

function nullableText(value: unknown): string | null {
  const valueText = text(value)
  return valueText || null
}

function list(value: unknown): string[] {
  return text(value).split(';').map((item) => item.trim()).filter(Boolean)
}

function booleanValue(value: unknown): boolean | null {
  const valueText = text(value).toLowerCase()
  if (!valueText) return null
  if (['true', 'yes', 'y', '1', 'enabled'].includes(valueText)) return true
  if (['false', 'no', 'n', '0', 'disabled'].includes(valueText)) return false
  return null
}

function numberValue(value: unknown): number | null {
  const valueText = text(value)
  if (!valueText) return null
  const parsed = Number(valueText)
  return Number.isFinite(parsed) ? parsed : null
}

function normalizedHeaders(row: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(row).map(([key, value]) => [key.trim().toLowerCase().replace(/\s+/g, '_'), value]))
}

export async function parseCatalogueImport(file: File): Promise<CatalogueImportPreview> {
  const format: CatalogImportFormat = file.name.toLowerCase().endsWith('.csv') ? 'csv' : 'xlsx'
  const workbook = XLSX.read(await file.arrayBuffer(), { type: 'array', cellDates: false })
  const firstSheet = workbook.Sheets[workbook.SheetNames[0]]
  if (!firstSheet) throw new Error('The file does not contain a worksheet.')

  const rawRows = XLSX.utils.sheet_to_json<Record<string, unknown>>(firstSheet, { defval: '' })
  const firstRow = rawRows[0]
  if (!firstRow) throw new Error('The file has no service rows. Add a header row and at least one service.')

  const headerNames = new Set(Object.keys(firstRow).map((key) => key.trim().toLowerCase().replace(/\s+/g, '_')))
  const missingColumns = requiredColumns.filter((column) => !headerNames.has(column))
  if (missingColumns.length > 0) throw new Error(`Missing required column(s): ${missingColumns.join(', ')}`)

  const issues: CatalogueImportIssue[] = []
  const rows = rawRows.map((rawRow, index) => {
    const row = normalizedHeaders(rawRow)
    const serviceCode = text(row.service_code)
    const serviceName = text(row.service_name)
    if (!text(row.department_code) || !text(row.department_name) || !serviceCode || !serviceName) {
      issues.push({ row: index + 2, message: 'Missing one or more required values.' })
    }
    if (text(row.sakala_days) && numberValue(row.sakala_days) === null) issues.push({ row: index + 2, message: 'sakala_days must be a number.' })
    if (text(row.department_fee) && numberValue(row.department_fee) === null) issues.push({ row: index + 2, message: 'department_fee must be a number.' })
    if (text(row.service_charge) && numberValue(row.service_charge) === null) issues.push({ row: index + 2, message: 'service_charge must be a number.' })

    return {
      department: { code: text(row.department_code), name: text(row.department_name), homeUrlEn: nullableText(row.department_home_url_en), homeUrlKn: nullableText(row.department_home_url_kn) },
      subDepartment: nullableText(row.sub_department_name) ? { name: text(row.sub_department_name) } : null,
      service: {
        serviceCode, name: serviceName, nameKannada: nullableText(row.service_name_kn), shortDescription: nullableText(row.short_description),
        description: nullableText(row.description), descriptionKannada: nullableText(row.description_kn), isSakalaEnabled: booleanValue(row.is_sakala_enabled),
        sakalaLabelRaw: nullableText(row.sakala_label), sakalaDays: numberValue(row.sakala_days), departmentFee: numberValue(row.department_fee),
        serviceCharge: numberValue(row.service_charge), documentsEn: list(row.documents), documentsKn: list(row.documents_kn), eligibilityEn: list(row.eligibility),
        eligibilityKn: list(row.eligibility_kn), procedureEn: list(row.procedure), procedureKn: list(row.procedure_kn), jurisdiction: nullableText(row.jurisdiction),
        category: nullableText(row.category), modeOfService: nullableText(row.mode_of_service), isMandatory: booleanValue(row.is_mandatory),
        departmentServiceId: nullableText(row.department_service_id), serviceUrl: nullableText(row.service_url), applyUrl: nullableText(row.apply_url), applyUrlKannada: nullableText(row.apply_url_kn),
      },
    }
  })

  return { filename: file.name, format, rows, issues }
}

export const catalogueImportColumns = columns
