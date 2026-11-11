import type { MessageKey } from '../i18n'
import type { SchemeSummary } from '../types'
import { categoryOf, type ServiceCategoryId } from './serviceCategories'

// Codes must match backend OccupationCategories (SevaOne.Business/Portal/Validators).
export const OCCUPATIONS = ['student', 'farmer', 'agri_labourer', 'fisherman', 'construction_worker', 'unorganised_worker', 'artisan', 'driver', 'govt_employee', 'private_employee', 'self_employed', 'homemaker', 'retired', 'unemployed', 'other'] as const
export type OccupationCode = (typeof OCCUPATIONS)[number]

export const isOccupation = (v: string | null | undefined): v is OccupationCode => !!v && (OCCUPATIONS as readonly string[]).includes(v)
export const occupationKey = (c: OccupationCode) => `occ_${c}` as MessageKey

type Rule = { cats: ServiceCategoryId[]; words: RegExp; not?: RegExp }

// shortcut: hand-written keyword rules over service names, replace with an eligibility field on the catalogue once departments supply one.
const RULES: Record<OccupationCode, Rule> = {
  student: { cats: ['education'], words: /scholarship|student|hostel|education|exam|vidyasiri|tuition|fee reimbursement|laptop|post matric|pre matric/ },
  farmer: { not: /fish|ornamental|poultry feed/, cats: ['agriculture'], words: /farm|crop|agricultur|horticult|irrigation|seed|tractor|milk|dairy|sheep|poultry|krishi|ganga kalyana|fertili|pump set|bore ?well/ },
  agri_labourer: { cats: ['agriculture'], words: /labour|land|ganga kalyana|housing|unorganised|accident|krishi|livelihood/ },
  fisherman: { cats: [], words: /fish|boat|matsya|net|coastal|marine/ },
  construction_worker: { cats: ['labour'], words: /construction|building|bocw|workers welfare|labour card|maternity|tool ?kit|assistance|pension|accident|funeral/ },
  unorganised_worker: { cats: ['labour'], words: /unorganised|social security|labour|ashadeepa|accident|pension|workers/ },
  artisan: { cats: [], words: /handloom|handicraft|artisan|vishwakarma|weav|khadi|leather|coir|sericult|silk|tool ?kit|craft/ },
  driver: { cats: ['licences'], words: /driving|licen[cs]e|vehicle|transport|permit|taxi|cab|auto|goods|two wheeler|registration of motor|badge/ },
  govt_employee: { cats: [], words: /kgid|pension|gpf|leave|salary|insurance|retire|government employee|service book|transfer/ },
  private_employee: { cats: ['labour'], words: /esi|provident|employment|pension|insurance|skill|labour|training|accident/ },
  self_employed: { cats: ['business'], words: /business|trade|msme|loan|entrepreneur|startup|industr|subsidy|shop|establishment|licen[cs]e|self employment/ },
  homemaker: { cats: ['welfare'], words: /women|widow|maternity|anganwadi|gruha|mahila|child|housing|self help|ration|stree|bhagya/ },
  retired: { cats: ['welfare'], words: /pension|senior citizen|old age|widow|disab|health|ration|funeral|elderly/ },
  unemployed: { cats: [], words: /employment|skill|training|unemployment|stipend|job|self employment|loan|kaushalya|apprentice/ },
  other: { cats: [], words: /(?!)/ },
}

/** Best-matching services for an occupation; same-named services (one per corporation) collapse into one entry. */
export function recommendFor(occ: OccupationCode, schemes: readonly SchemeSummary[], limit = 12): SchemeSummary[] {
  const rule = RULES[occ]
  const seen = new Set<string>()
  return schemes
    .map((s) => {
      const name = s.name.toLowerCase()
      if (rule.not?.test(name)) return { s, score: 0 }
      // Several distinct keyword hits and sitting in a relevant category rank higher than one stray match.
      const hits = new Set(name.match(new RegExp(rule.words.source, 'g')) ?? []).size
      const inCat = rule.cats.includes(categoryOf(s.serviceCode))
      return { s, score: hits ? hits * 2 + (inCat ? 2 : 0) : 0 }
    })
    .filter((x) => x.score > 0)
    .sort((a, b) => b.score - a.score || a.s.name.localeCompare(b.s.name))
    .map((x) => x.s)
    .filter((s) => {
      const k = s.name.replace(/\s+/g, ' ').trim().toLowerCase()
      if (seen.has(k)) return false
      seen.add(k)
      return true
    })
    .slice(0, limit)
}
