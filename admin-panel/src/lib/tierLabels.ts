import type { GovAdminTier } from '../types'

/** Access-level display names -- deliberately not literal office titles (e.g. not "Chief
 * Minister") since a WSO2 account holding this tier need not be that actual office holder.
 * Backend GovAdminTier enum values and DB-stored strings are unchanged; this is a display-only
 * relabel. */
export const TIER_LABELS: Record<GovAdminTier, string> = {
  Superadmin: 'Platform Admin',
  ChiefMinister: 'State-Level Admin',
  Department: 'Department Admin',
  DistrictHead: 'District-Level Admin',
  TalukHead: 'Taluk-Level Admin',
}

export function tierLabel(tier: string): string {
  return TIER_LABELS[tier as GovAdminTier] ?? tier
}
