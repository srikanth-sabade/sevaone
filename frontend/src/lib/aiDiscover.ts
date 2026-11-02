import type { Lang } from '../i18n'
import { t } from '../i18n'
import type { Faq, SchemeDetail, SchemeSummary } from '../types'
import { formatFeeInr } from './fee'

export type DiscoverPrimary = {
  scheme: SchemeDetail | SchemeSummary
  steps: string[]
  portal: string
}

export type DiscoverResult = {
  answer: string
  matches: SchemeSummary[]
  faqMatches: Faq[]
  primary: DiscoverPrimary | null
  suggestions: string[]
}

const STOP = new Set([
  'the',
  'and',
  'for',
  'want',
  'need',
  'how',
  'can',
  'you',
  'please',
  'apply',
  'applying',
  'application',
  'get',
  'make',
  'create',
  'new',
  'have',
  'with',
  'from',
  'his',
  'her',
  'any',
  'our',
  'are',
  'would',
  'like',
  'karnataka',
  'government',
  'govt',
  'service',
  'services',
  'online',
  'help',
  'tell',
  'about',
  'certificate',
])

const APPLY_INTENT =
  /(apply|applying|application|create|make|get|need|want|issue|register|renew|download|obtain|how do i|how to|hosa|beku)/i

function tokenize(q: string) {
  return q
    .toLowerCase()
    .split(/[^a-z0-9]+/)
    .filter((t) => t.length > 2)
}

function scoreService(q: string, s: SchemeSummary) {
  const lower = q.toLowerCase()
  const name = s.name.toLowerCase()
  let score = 0
  if (lower.includes(name)) score += 12
  for (const a of s.aliases ?? []) {
    if (lower.includes(a.toLowerCase())) score += 10
  }
  const hay =
    `${s.name} ${s.departmentName} ${s.category} ${s.description} ${(s.aliases ?? []).join(' ')} ${s.eligibility.join(' ')}`.toLowerCase()
  for (const t of tokenize(q)) {
    if (hay.includes(t)) score += STOP.has(t) ? 0.5 : 2
  }
  return score
}

/** Known short service codes that have few/no English vowels. */
const SHORT_OK = new Set([
  'rtc',
  'pan',
  'ec',
  'rc',
  'dl',
  'gst',
  'itr',
  'pf',
  'esi',
  'khata',
  'pahani',
  'bhoomi',
  'aadhaar',
  'aadhar',
])

/** @deprecated prefer unclearReply(lang) */
export const UNCLEAR_REPLY = t('en', 'unclearReply')

export function unclearReply(lang: Lang) {
  return t(lang, 'unclearReply')
}

/**
 * True only for empty input, pure numeric/symbol noise, or a letter-ratio too low to be text
 * (keyboard mash) \u2014 never for a short-but-real word. Used to gate the AI backend call, since
 * the backend's own NLU handles genuinely ambiguous input (e.g. a bare "certificate") far
 * better than a client-side stopword heuristic can.
 */
export function isGarbageInput(q: string): boolean {
  const trimmed = q.trim()
  if (!trimmed) return true

  const compact = trimmed.replace(/\s+/g, '')
  // Pure / mostly numeric noise: "12345", "98 76", "12-34"
  if (/^[\d\s.+#-]+$/.test(trimmed)) return true
  if (compact.length >= 2) {
    const letters = (compact.match(/[a-zA-Z\u0c80-\u0cff]/g) || []).length
    if (letters / compact.length < 0.35) return true
  }
  return false
}

/**
 * True for random numbers, keyboard mash, or input with no readable service intent.
 * Catalogue matching still decides close natural-language misses.
 */
export function isUnclearQuery(q: string): boolean {
  if (isGarbageInput(q)) return true

  const words = q
    .trim()
    .toLowerCase()
    .split(/[^a-z0-9\u0c80-\u0cff]+/)
    .filter(Boolean)
  if (!words.length) return true

  const meaningful = words.some((w) => {
    if (SHORT_OK.has(w)) return true
    if (STOP.has(w)) return false
    if (w.length <= 2) return false
    if (/^([a-z0-9])\1{2,}$/i.test(w)) return false // aaa, 1111
    // Has a vowel (Latin) or is long enough Kannada/letter token
    if (/[aeiou]/.test(w)) return true
    if (/[\u0c80-\u0cff]/.test(w) && w.length >= 3) return true
    return false
  })

  return !meaningful
}

export function rankSchemes(q: string, schemes: SchemeSummary[], limit = 6): SchemeSummary[] {
  if (!schemes.length) return []
  const scored = schemes.map((s) => ({ s, score: scoreService(q, s) }))
  const hits = scored.filter((x) => x.score > 0).sort((a, b) => b.score - a.score)
  return hits.slice(0, limit).map((x) => x.s)
}

function buildPrimary(q: string, matches: SchemeSummary[], detail?: SchemeDetail | null): DiscoverPrimary | null {
  if (!matches.length) return null
  const top = matches[0]
  const score = scoreService(q, top)
  if (score < 8) return null
  if (!APPLY_INTENT.test(q) && score < 10) return null
  const scheme = detail && detail.serviceCode === top.serviceCode ? detail : top
  const steps =
    detail?.steps?.length
      ? detail.steps
      : [
          'Sign in on SevaOne with your mobile number and OTP',
          'Fill the application form — your profile details are pre-filled',
          'Attach or fetch the required documents from DigiLocker',
          `Pay the service fee (${formatFeeInr(top)}) through the Khajane-II gateway`,
          `Track progress on your dashboard — Sakala guarantees ${top.sakalaDays} days`,
        ]
  return {
    scheme,
    steps,
    portal: detail?.portal || `${top.departmentName} portal`,
  }
}

function buildAnswer(primary: DiscoverPrimary | null, matches: SchemeSummary[]): string {
  if (primary) {
    const documentsNote =
      'documents' in primary.scheme && Array.isArray(primary.scheme.documents) && primary.scheme.documents.length
        ? ` You will need: ${primary.scheme.documents.join(', ')}.`
        : ''
    return `Yes — you can apply for the ${primary.scheme.name} online through SevaOne. It is issued by the ${primary.scheme.departmentName} on ${primary.portal}, costs ${formatFeeInr(primary.scheme)}, and is guaranteed within ${primary.scheme.sakalaDays} days under Sakala.${documentsNote} The step-by-step guide and the Apply button are below.`
  }
  if (matches.length) {
    return `I found ${matches.length} services matching your query. The most relevant appears to be "${matches[0].name}" under ${matches[0].departmentName}.`
  }
  return unclearReply('en')
}

function buildSuggestions(primary: DiscoverPrimary | null, unclear: boolean): string[] {
  if (primary) {
    return [
      `Documents for ${primary.scheme.name}`,
      `How long does ${primary.scheme.name} take?`,
      'Track my application',
      'What is Sakala?',
    ]
  }
  if (unclear) {
    return ['Income certificate', 'Caste certificate', 'RTC land record', 'Ration card']
  }
  return ['Income Certificate', 'RTC / Pahani extract', 'Khata transfer', 'Encumbrance Certificate']
}

export function buildDiscoverResult(
  q: string,
  schemes: SchemeSummary[],
  faqs: Faq[] = [],
  detail?: SchemeDetail | null,
): DiscoverResult {
  const matches = rankSchemes(q, schemes)
  const primary = buildPrimary(q, matches, detail)
  const tokens = tokenize(q)
  const faqMatches = faqs
    .map((f) => {
      const hay = `${f.question} ${f.answer} ${f.category}`.toLowerCase()
      const score = tokens.reduce((a, t) => a + (hay.includes(t) ? 1 : 0), 0)
      return { f, score }
    })
    .filter((x) => x.score > 0)
    .sort((a, b) => b.score - a.score)
    .slice(0, 3)
    .map((x) => x.f)

  const unclear = isUnclearQuery(q) || (!primary && matches.length === 0 && faqMatches.length === 0)

  const answer = buildAnswer(primary, matches)
  const suggestions = buildSuggestions(primary, unclear)

  return {
    answer,
    matches: unclear && !primary ? [] : matches,
    faqMatches: unclear && !primary ? [] : faqMatches,
    primary,
    suggestions,
  }
}
