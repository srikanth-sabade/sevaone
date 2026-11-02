import type { SchemeSummary } from '../types'

/** Sum of the department's own statutory fee and SevaOne's service charge — the two numeric fields the backend now returns instead of one legacy formatted string. */
export function totalFeeInr(service: Pick<SchemeSummary, 'departmentFee' | 'serviceCharge'>): number {
  return (service.departmentFee ?? 0) + (service.serviceCharge ?? 0)
}

export function formatFeeInr(service: Pick<SchemeSummary, 'departmentFee' | 'serviceCharge'>): string {
  return `₹${totalFeeInr(service)}`
}
