/** Pre-2014 official Karnataka district names that still show up in citizen data (older
 *  DigiLocker/Aadhaar KYC records, or just how people commonly write them) -- mapped to the
 *  current official name used in the map's own geometry (admin-panel/public/karnataka-districts.json)
 *  and in analytics/departments-style displays elsewhere. "Bangalore" alone (no Urban/Rural
 *  qualifier) is deliberately resolved to "Bengaluru Urban" -- there's no way to disambiguate
 *  from free text alone, and Urban is overwhelmingly the more common colloquial meaning; Bengaluru
 *  Rural is normally named explicitly when that's what's meant. */
const DISTRICT_ALIASES: Record<string, string> = {
  bangalore: 'bengaluru urban',
  'bangalore urban': 'bengaluru urban',
  'bangalore rural': 'bengaluru rural',
  mysore: 'mysuru',
  gulbarga: 'kalaburagi',
  bijapur: 'vijayapura',
  belgaum: 'belagavi',
  chikmagalur: 'chikkamagaluru',
  tumkur: 'tumakuru',
  shimoga: 'shivamogga',
  chikkaballapura: 'chikkaballapur',
  bellary: 'ballari',
}

/** Loose match against the free-text district names citizens/applications actually carry
 *  (DigiLocker-sourced, no canonical spelling enforced -- see SESSION_HANDOFF.md's district
 *  free-text-matching note). Resolves the well-known pre-2014 name changes above; anything else
 *  is just lowercased/trimmed. A district with no matching row simply doesn't light up on the
 *  Karnataka map and still appears in the directory table below it. */
export function normalizeDistrictName(name: string): string {
  const trimmed = name.trim().toLowerCase()
  return DISTRICT_ALIASES[trimmed] ?? trimmed
}
