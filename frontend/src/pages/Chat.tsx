import { Link, useNavigate, useSearch } from '@tanstack/react-router'
import { SiteFooter } from '../components/SiteFooter'
import { RecommendedServices } from '../components/RecommendedServices'
import { useQuery } from '@tanstack/react-query'
import {
  ArrowRight,
  ArrowUp,
  Building2,
  CheckCircle2,
  Clock,
  ExternalLink,
  Flame,
  FileCheck2,
  Layers,
  Loader2,
  Megaphone,
  MapPin,
  Mic,
  ShieldCheck,
  Sparkles,
} from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import dks from '../assets/dks.webp'
import digilockerSymbol from '../assets/digilocker-symbol.webp'
import gokEmblem from '../assets/gok-emblem.webp'
import karnatakaEmblem from '../assets/karnataka-emblem.webp'
import nammaSeveLogo from '../assets/namma-seve-logo.webp'
import parameshwara from '../assets/parameshwara.webp'
import { api } from '../api'
import { formatFeeInr } from '../lib/fee'
import { deptLogoOrEmblem } from '../lib/deptLogos'
import { ensureUrlScheme } from '../lib/utils'
import { sendAiChatMessage, transcribeVoiceAudio } from '../aiChat'
import { applyToService, loginPath, queueLoginRedirectToast } from '../applyFlow'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { SERVICE_CATEGORIES } from '../lib/serviceCategories'
import { BrandWordmark } from '../components/BrandWordmark'
import { FloatingRail } from '../components/FloatingRail'
import { TopRightUserControls } from '../components/TopRightUserControls'
import { GovernmentNotifications } from '../components/GovernmentNotifications'
import {
  BackgroundThemePreview,
  loadBackgroundPreviewMode,
  persistBackgroundPreviewMode,
  type BackgroundPreviewMode,
} from '../components/dev/BackgroundThemePreview'
import { flow } from '../flowLog'
import { useT } from '../hooks/useT'
import { selectHelpFaqs } from '../lib/helpFaqs'
import { categoryLabel as catKn, departmentName as deptNameKn, serviceName as svcNameKn } from '../lib/catalogKn'
import { buildDiscoverResult, isGarbageInput, unclearReply, type DiscoverResult } from '../lib/aiDiscover'
import { useSession } from '../store'
import type { Lang, MessageKey } from '../i18n'
import { t as tr } from '../i18n'
import { findMockService, MOCK_CATALOG } from '../lib/catalogMock'
import type { AiCandidate, AiChatResponse, AiResolvedService, Faq, SchemeDetail, SchemeSummary } from '../types'

/** Prefer API serviceCode; otherwise map resolved service name onto the CSV catalog. */
function resolveCatalogServiceCode(resolved: AiResolvedService, topLevelCode?: string | null): string | undefined {
  const candidates = [topLevelCode, resolved.serviceCode, resolved.serviceId]
  for (const raw of candidates) {
    const code = (raw ?? '').toString().trim()
    if (code && findMockService(code)) return code
  }
  const name = resolved.serviceName.trim().toLowerCase()
  if (!name) return undefined
  for (const dept of MOCK_CATALOG) {
    for (const svc of dept.directServices) {
      if (svc.name.toLowerCase() === name) return svc.code
    }
    for (const sub of dept.subDepartments) {
      for (const svc of sub.services) {
        if (svc.name.toLowerCase() === name) return svc.code
      }
    }
  }
  return undefined
}


const LINK_CARDS: {
  to: '/track' | '/digilocker' | '/centres' | '/grievance'
  titleKey: MessageKey
  bodyKey: MessageKey
  icon?: typeof MapPin
  tile: string
}[] = [
  { to: '/track', titleKey: 'trackApp', bodyKey: 'linkTrackBody', icon: MapPin, tile: 'from-indigo-500 to-blue-600' },
  { to: '/digilocker', titleKey: 'linkDigilockerTitle', bodyKey: 'linkDigilockerBody', tile: '' },
  { to: '/centres', titleKey: 'linkCentreTitle', bodyKey: 'linkCentreBody', icon: Building2, tile: 'from-orange-400 to-amber-500' },
  { to: '/grievance', titleKey: 'grievanceIpgrs', bodyKey: 'linkGrievanceBody', icon: Megaphone, tile: 'from-rose-500 to-pink-600' },
]

const CATEGORY_TILE: Record<string, string> = {
  certificates: 'from-blue-500 to-indigo-600',
  land: 'from-amber-500 to-orange-600',
  licences: 'from-sky-500 to-cyan-600',
  loans: 'from-emerald-500 to-teal-600',
  education: 'from-blue-700 to-blue-900',
  agriculture: 'from-lime-500 to-green-600',
  utilities: 'from-yellow-400 to-amber-500',
  food: 'from-orange-400 to-red-500',
  labour: 'from-slate-500 to-slate-700',
  business: 'from-fuchsia-500 to-pink-600',
  welfare: 'from-rose-500 to-pink-600',
  environment: 'from-teal-500 to-emerald-600',
  other: 'from-gray-500 to-gray-600',
}

const LEGACY_LINK_CARDS: { to: '/track' | '/departments' | '/help' | '/grievance'; titleKey: MessageKey; bodyKey: MessageKey }[] = [
  { to: '/track', titleKey: 'trackApp', bodyKey: 'linkTrackBody' },
  { to: '/departments', titleKey: 'departments', bodyKey: 'linkDeptsBody' },
  { to: '/help', titleKey: 'helpCentre', bodyKey: 'linkHelpBody' },
  { to: '/grievance', titleKey: 'grievanceIpgrs', bodyKey: 'linkGrievanceBody' },
]

const SUGGESTIONS = [
  {
    en: 'Income certificate',
    kn: 'ಆದಾಯ ಪ್ರಮಾಣಪತ್ರ',
    q: 'Apply for an income certificate',
  },
  {
    en: 'Caste certificate',
    kn: 'ಜಾತಿ ಪ್ರಮಾಣಪತ್ರ',
    q: 'Apply for a caste certificate',
  },
  {
    en: 'Land record (RTC)',
    kn: 'ಪಹಣಿ / ಆರ್‌ಟಿಸಿ',
    q: 'Get my RTC land record',
  },
  { en: 'Ration card', kn: 'ಪಡಿತರ ಚೀಟಿ', q: 'Add a member to my ration card' },
  { en: 'Driving licence', kn: 'ಚಾಲನಾ ಪರವಾನಗಿ', q: 'Renew my driving licence' },
]

type UserMsg = { id: string; role: 'user'; text: string }
type TextMsg = { id: string; role: 'assistant'; kind: 'text'; text: string }
type ServiceMsg = {
  id: string
  role: 'assistant'
  kind: 'service'
  scheme: SchemeSummary | SchemeDetail
  portal: string
  expanded?: boolean
  steps?: string[]
}
type MatchesMsg = {
  id: string
  role: 'assistant'
  kind: 'matches'
  schemes: SchemeSummary[]
}
type AiServiceMsg = {
  id: string
  role: 'assistant'
  kind: 'ai-service'
  resolvedService: AiResolvedService
  references: string[]
  serviceCode?: string | null
}
type AiCandidatesMsg = {
  id: string
  role: 'assistant'
  kind: 'ai-candidates'
  candidates: AiCandidate[]
}
type ChatMessage = UserMsg | TextMsg | ServiceMsg | MatchesMsg | AiServiceMsg | AiCandidatesMsg

/** Tracks an in-progress AI backend conversation (Clarify / Resolved) so follow-ups reuse its sessionId. */
type AiSession = {
  sessionId: string
  resolvedService: AiResolvedService | null
  references: string[]
}

type StatusPhase = { label: string; detail?: string } | null

let uid = 0
function nextId() {
  uid += 1
  return `m${Date.now()}-${uid}`
}

function sleep(ms: number) {
  return new Promise<void>((resolve) => {
    window.setTimeout(resolve, ms)
  })
}

function shortAnswer(result: DiscoverResult, lang: Lang): string {
  if (result.primary) {
    const s = result.primary.scheme
    return tr(lang, 'chatMatchStrong', {
      name: s.name,
      dept: s.departmentName,
      fee: formatFeeInr(s),
      days: s.sakalaDays,
    })
  }
  if (result.matches.length) {
    return tr(lang, 'chatMatchMany', { count: result.matches.length })
  }
  return unclearReply(lang)
}

type MdBlock =
  | { kind: 'heading'; text: string }
  | { kind: 'ul'; items: string[] }
  | { kind: 'ol'; items: string[] }
  | { kind: 'p'; text: string }

// The model doesn't always pick the same glyphs run to run (bold-line vs `#` ATX headings,
// `•`/`-`/`*` bullets), so these match the realistic range rather than one exact convention.
const MD_BOLD_HEADING = /^\*\*(.+?)\*\*:?$/
const MD_ATX_HEADING = /^#{1,6}\s+(.+)$/
const MD_BULLET = /^[•*-]\s+/
const MD_NUMBERED = /^\d+\.\s+/
const MD_RULE = /^-{3,}$/
const MD_BLOCK_STARTS = [MD_BOLD_HEADING, MD_ATX_HEADING, MD_BULLET, MD_NUMBERED]

function isBlockStart(line: string) {
  return MD_RULE.test(line) || MD_BLOCK_STARTS.some((re) => re.test(line))
}

/** Groups AI/template answer text (headers, bullets, numbered steps, `---` rules) into blocks so it renders as a real structured answer instead of a raw text dump. */
function parseMarkdownish(text: string): MdBlock[] {
  const lines = text.replace(/\r\n/g, '\n').split('\n')
  const blocks: MdBlock[] = []
  let i = 0
  while (i < lines.length) {
    const line = lines[i].trim()
    if (!line || MD_RULE.test(line)) {
      i++
      continue
    }

    const boldHeading = line.match(MD_BOLD_HEADING)
    const atxHeading = line.match(MD_ATX_HEADING)
    if (boldHeading) {
      blocks.push({ kind: 'heading', text: boldHeading[1].replace(/:$/, '') })
      i++
      continue
    }
    if (atxHeading) {
      blocks.push({ kind: 'heading', text: atxHeading[1].trim() })
      i++
      continue
    }

    if (MD_BULLET.test(line)) {
      const items: string[] = []
      while (i < lines.length && MD_BULLET.test(lines[i].trim())) {
        items.push(lines[i].trim().replace(MD_BULLET, ''))
        i++
      }
      blocks.push({ kind: 'ul', items })
      continue
    }

    if (MD_NUMBERED.test(line)) {
      const items: string[] = []
      while (i < lines.length && MD_NUMBERED.test(lines[i].trim())) {
        items.push(lines[i].trim().replace(MD_NUMBERED, ''))
        i++
      }
      blocks.push({ kind: 'ol', items })
      continue
    }

    const paraLines: string[] = []
    while (i < lines.length && lines[i].trim() && !isBlockStart(lines[i].trim())) {
      paraLines.push(lines[i].trim())
      i++
    }
    blocks.push({ kind: 'p', text: paraLines.join(' ') })
  }
  return blocks
}

function renderInline(text: string, keyPrefix: string) {
  // Bold (**x**) first so its ** pairs aren't mistaken for two italic (*x*) markers.
  const parts = text.split(/(\*\*[^*]+\*\*|\*[^*]+\*)/g)
  return parts.map((part, i) => {
    if (part.startsWith('**') && part.endsWith('**')) {
      return <strong key={`${keyPrefix}-${i}`}>{part.slice(2, -2)}</strong>
    }
    if (part.startsWith('*') && part.endsWith('*')) {
      return <em key={`${keyPrefix}-${i}`}>{part.slice(1, -1)}</em>
    }
    return <span key={`${keyPrefix}-${i}`}>{part}</span>
  })
}

/** Dev-only background-theme preview (see components/dev/BackgroundThemePreview) -- picks the
 *  one matching CSS suffix class, checked in the same priority order the old nested ternary used. */
function backgroundThemeClass(dark: boolean, animated: boolean, fullWidthDark: boolean, aurora: boolean): string {
  if (dark) return ` seva-chat-theme--dark${animated ? ' seva-chat-theme--animated' : ''}`
  if (fullWidthDark) return ' seva-chat-theme--fullwidth-dark'
  if (aurora) return ' seva-chat-theme--aurora'
  return ''
}

function markdownBlockSpacing(isFirst: boolean, kind: string): string {
  if (isFirst) return ''
  return kind === 'heading' ? 'mt-3' : 'mt-2'
}

function renderMarkdownish(text: string) {
  return parseMarkdownish(text).map((block, bi) => {
    const key = `b${bi}`
    const spaced = markdownBlockSpacing(bi === 0, block.kind)
    if (block.kind === 'heading') {
      return (
        <div
          key={key}
          className={`${spaced} text-[13px] font-semibold uppercase tracking-wide text-[color:var(--gov-blue)]`}
        >
          {renderInline(block.text, key)}
        </div>
      )
    }
    if (block.kind === 'ul') {
      return (
        <ul key={key} className={`${spaced} list-disc space-y-1 pl-5`}>
          {block.items.map((item, ii) => (
            <li key={`${key}-${ii}`}>{renderInline(item, `${key}-${ii}`)}</li>
          ))}
        </ul>
      )
    }
    if (block.kind === 'ol') {
      return (
        <ol key={key} className={`${spaced} list-decimal space-y-1 pl-5`}>
          {block.items.map((item, ii) => (
            <li key={`${key}-${ii}`}>{renderInline(item, `${key}-${ii}`)}</li>
          ))}
        </ol>
      )
    }
    return (
      <p key={key} className={spaced}>
        {renderInline(block.text, key)}
      </p>
    )
  })
}

function defaultPills(
  suggestedQuestions: AiChatResponse['suggestedQuestions'],
  responseType: AiChatResponse['responseType'],
  t: ReturnType<typeof useT>['t'],
): string[] {
  if (suggestedQuestions.length) return suggestedQuestions
  if (responseType === 'Clarify') return []
  return [t('pillIncome'), t('pillCaste'), t('pillRtc'), t('pillRation')]
}

function pillsFor(
  lang: Lang,
  focus: SchemeSummary | SchemeDetail | null,
  lastResult?: DiscoverResult | null,
): string[] {
  if (focus) {
    return [
      tr(lang, 'pillDocs', { name: focus.name }),
      tr(lang, 'pillEligible', { name: focus.name }),
      tr(lang, 'pillHowApply', { name: focus.name }),
      tr(lang, 'pillHowLong', { name: focus.name }),
      tr(lang, 'applyThis'),
    ]
  }
  if (lastResult?.suggestions?.length) return lastResult.suggestions.slice(0, 4)
  return [tr(lang, 'pillIncome'), tr(lang, 'pillCaste'), tr(lang, 'pillRtc'), tr(lang, 'pillTrack')]
}

// legacy: the previous `/` layout (gradient on the whole page, link cards at the bottom), served at /oldhomepage.
export function ChatPage({ legacy = false }: { legacy?: boolean }) {
  const { me } = useSession()
  const navigate = useNavigate()
  const { lang, t } = useT()
  const { loginError } = useSearch({ strict: false }) as { loginError?: string }

  // Fade the marked text in when the language changes. Animated in place (not re-keyed): re-keying
  // left the old nodes in the DOM.
  const prevLang = useRef(lang)
  useEffect(() => {
    if (prevLang.current === lang) return
    prevLang.current = lang
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
    document.querySelectorAll('[data-lang-fade]').forEach((el) =>
      el.animate(
        [{ opacity: 0, transform: 'translateY(6px)' }, { opacity: 1, transform: 'none' }],
        { duration: 350, easing: 'ease-out' },
      ),
    )
  }, [lang])

  const highlights = [
    { icon: Layers, title: t('highlight1Title'), body: t('highlight1Body') },
    { icon: Clock, title: t('highlight2Title'), body: t('highlight2Body') },
    { icon: ShieldCheck, title: t('highlight3Title'), body: t('highlight3Body') },
  ]
  const [q, setQ] = useState('')
  const [listening, setListening] = useState(false)
  const [pending, setPending] = useState(false)
  const [status, setStatus] = useState<StatusPhase>(null)
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [focusScheme, setFocusScheme] = useState<(SchemeSummary | SchemeDetail) | null>(null)
  const [aiSession, setAiSession] = useState<AiSession | null>(null)
  const [pills, setPills] = useState<string[]>([])
  const [backgroundPreviewMode, setBackgroundPreviewMode] = useState<BackgroundPreviewMode>(loadBackgroundPreviewMode)
  const inputRef = useRef<HTMLInputElement>(null)
  const threadEndRef = useRef<HTMLDivElement>(null)
  const mediaRecorderRef = useRef<MediaRecorder | null>(null)
  const audioChunksRef = useRef<Blob[]>([])
  const focusRef = useRef<(SchemeSummary | SchemeDetail) | null>(null)
  const aiSessionRef = useRef<AiSession | null>(null)

  const schemes = useQuery({
    queryKey: ['schemes', 'chat'],
    queryFn: () => api.schemes(),
  })
  // FAQ discovery matches come from the static bilingual set (src/lib/helpFaqs.ts), same
  // source as /help -- there is no /api/v1/help/faqs backend, and keeping it local means
  // discovery still surfaces FAQ answers offline.
  const faqList = useMemo(() => selectHelpFaqs(lang), [lang])
  // Ranked by real apply counts (see CatalogController.ListPopularSchemes), not an arbitrary
  // slice of `schemes` -- that query stays above only for the search-ranking fallback below.
  // The backend's own curated fallback (sp_list_top_requested_services) always returns rows
  // even with zero real usage, so it can show something sensible on a brand-new catalog -- but
  // for verifying real usage tracking end to end, a fallback-dressed "0 real applies" list
  // looks identical to a genuinely popular one. Filtering to totalApplyCount > 0 here means this
  // section is empty exactly when there's no real usage yet, with no fallback dressing it up.
  const popularQ = useQuery({
    queryKey: ['schemes', 'popular'],
    queryFn: () => api.schemesPopular(6),
  })
  const popular = (popularQ.data ?? []).filter((s) => s.totalApplyCount > 0)

  useEffect(() => {
    focusRef.current = focusScheme
  }, [focusScheme])

  useEffect(() => {
    aiSessionRef.current = aiSession
  }, [aiSession])

  useEffect(() => {
    if (!loginError) return
    flow('WSO2/BFF login failed — landed back on chat without a session cookie', { loginError })
  }, [loginError])

  useEffect(() => {
    threadEndRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' })
  }, [messages, status, pills, pending])

  async function handleApply(scheme: Pick<SchemeSummary | SchemeDetail, 'serviceCode' | 'departmentCode' | 'name'>) {
    await applyToService(scheme, me)
  }

  /** The backend is now the single source of truth for both the real apply URL and the click
   *  count for an AI-resolved service — matched by name (sevaone-rag has no code that lines up
   *  with the real catalog's own), never by opening sevaone-rag's own apply_url directly. A
   *  genuine deep-link match that needs identity gets the same login-redirect treatment as the
   *  catalogue flow (see handleApply/applyToService). A miss (sevaone-rag knows a service this
   *  catalog doesn't have yet) falls back to the URL the AI already had, same as before this
   *  endpoint existed — better an unverified link than nothing for the ~50% of AI-resolved
   *  services this catalog doesn't contain. */
  async function handleAiApply(references: string[], resolved?: AiResolvedService | null) {
    const fallbackUrl = references[0]
    if (resolved?.serviceName) {
      try {
        const outcome = await api.aiApplyClick(resolved.serviceName, resolved.departmentCode, resolved.operatorHint)
        if (outcome.matched) {
          if (outcome.requiresLogin) {
            queueLoginRedirectToast(`Sign in to apply for ${resolved.serviceName}`)
            window.location.assign(loginPath('/'))
            return
          }
          if (outcome.applyUrl) {
            window.open(ensureUrlScheme(outcome.applyUrl), '_blank', 'noopener,noreferrer')
            return
          }
        }
      } catch {
        // AI apply-click endpoint unreachable — fall through to the AI's own URL below.
      }
    }
    if (fallbackUrl) {
      window.open(ensureUrlScheme(fallbackUrl), '_blank', 'noopener,noreferrer')
    }
  }

  function push(...msgs: ChatMessage[]) {
    setMessages((prev) => [...prev, ...msgs])
  }

  function expandService(id: string) {
    setMessages((prev) =>
      prev.map((m) => (m.id === id && 'kind' in m && m.kind === 'service' ? { ...m, expanded: true } : m)),
    )
  }

  async function answerFollowUp(query: string, focus: SchemeSummary | SchemeDetail): Promise<boolean> {
    const lower = query.toLowerCase()
    const name = focus.name

    if (/fee|cost|charge|price|payment|\bpay\b/.test(lower)) {
      setStatus({ label: t('fee') })
      await sleep(450)
      setStatus(null)
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'text',
        text: tr(lang, 'chatFee', { name, fee: formatFeeInr(focus) }),
      })
      setPills(pillsFor(lang, focus))
      return true
    }

    if (/document|what do i need|papers|proof/.test(lower)) {
      setStatus({ label: t('statusDocs') })
      await sleep(550)
      let detail: SchemeDetail | null = 'documents' in focus ? (focus as SchemeDetail) : null
      if (!detail?.documents?.length) {
        try {
          detail = await api.scheme(focus.serviceCode)
          setFocusScheme(detail)
        } catch {
          /* keep summary */
        }
      }
      setStatus(null)
      const docs = detail?.documents ?? []
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'text',
        text:
          docs.length > 0
            ? tr(lang, 'chatDocsReady', {
                name,
                list: docs.map((d) => `• ${d}`).join('\n'),
              })
            : tr(lang, 'chatDocsMissing', { name }),
      })
      setPills(pillsFor(lang, focus))
      return true
    }

    if (/eligib|who can|qualify/.test(lower)) {
      setStatus({ label: t('eligibility') })
      await sleep(500)
      let detail: SchemeDetail | null = 'eligibility' in focus ? (focus as SchemeDetail) : null
      if (!detail) {
        try {
          detail = await api.scheme(focus.serviceCode)
          setFocusScheme(detail)
        } catch {
          /* keep */
        }
      }
      setStatus(null)
      const elig = detail?.eligibility?.length ? detail.eligibility : focus.eligibility
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'text',
        text: elig?.length
          ? tr(lang, 'chatEligible', {
              name,
              text: '\n' + elig.map((e) => `• ${e}`).join('\n'),
            })
          : tr(lang, 'chatEligibleFallback'),
      })
      setPills(pillsFor(lang, focus))
      return true
    }

    if (/how (do i |to )?apply|steps|process|procedure/.test(lower)) {
      setStatus({ label: t('howItWorks') })
      await sleep(600)
      let detail: SchemeDetail | null = 'steps' in focus && focus.steps?.length ? (focus as SchemeDetail) : null
      if (!detail) {
        try {
          detail = await api.scheme(focus.serviceCode)
          setFocusScheme(detail)
        } catch {
          /* keep */
        }
      }
      const steps = detail?.steps?.length
        ? detail.steps
        : [
            'Sign in on SevaOne with password + Email OTP',
            'Confirm your profile (mobile + Aadhaar)',
            'Fill the department form — details are pre-filled where possible',
            'Attach documents (or fetch from DigiLocker)',
            `Pay ${formatFeeInr(focus)} and track under Sakala (${focus.sakalaDays} days)`,
          ]
      setStatus(null)
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'text',
        text: tr(lang, 'chatSteps', {
          name,
          list: steps.map((s, i) => `${i + 1}. ${s}`).join('\n'),
        }),
      })
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'service',
        scheme: detail ?? focus,
        portal: detail?.portal || `${focus.departmentName} portal`,
        steps,
        expanded: false,
      })
      setPills(pillsFor(lang, focus))
      return true
    }

    if (/how long|sakala|timeline|days|take/.test(lower)) {
      setStatus({ label: t('sakala') })
      await sleep(450)
      setStatus(null)
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'text',
        text: tr(lang, 'chatSakala', {
          name,
          days: focus.sakalaDays,
          fee: formatFeeInr(focus),
        }),
      })
      setPills(pillsFor(lang, focus))
      return true
    }

    const wantsToApply =
      /^apply\b/.test(lower) || /apply (?:now|for this)/.test(lower) || /continue to apply/.test(lower)
    if (wantsToApply) {
      setStatus(null)
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'text',
        text: tr(lang, 'chatApplyHandoff', { name }),
      })
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'service',
        scheme: focus,
        portal: `${focus.departmentName} portal`,
        expanded: false,
      })
      setPills([tr(lang, 'pillDocs', { name }), tr(lang, 'pillTrack'), tr(lang, 'pillOther')])
      return true
    }

    return false
  }

  /** Ranks the local catalogue for `query` — used both to detect the Income/Caste carve-out and as the silent fallback if the AI backend is unreachable. Never throws; an unreachable BFF just yields an empty ranking. */
  async function fetchRankedAndPrelim(query: string) {
    let searched: SchemeSummary[] = []
    try {
      searched = await api.schemes(query)
    } catch {
      searched = []
    }
    let ranked = searched.length ? searched : (schemes.data ?? [])
    if (ranked.length) {
      try {
        const hit = await api.search(query)
        if (hit.kind === 'single' && hit.service) {
          ranked = [hit.service, ...ranked.filter((s) => s.serviceCode !== hit.service!.serviceCode)]
        } else if (hit.kind === 'clarify' && hit.clarify?.options?.length) {
          const codes = new Set(hit.clarify.options.map((o) => o.serviceCode))
          const clarified = ranked.filter((s) => codes.has(s.serviceCode))
          if (clarified.length) ranked = clarified
        } else if (hit.kind === 'none' && searched.length) {
          ranked = searched
        }
      } catch {
        /* fall back to unfiltered ranked list */
      }
    }
    const prelim = buildDiscoverResult(query, ranked, faqList, null)
    return { ranked, faqList, prelim }
  }

  /** The original catalogue-driven answer + service card, with the internal apply/handoff flow. */
  async function resolveWithCatalogue(query: string, ranked: SchemeSummary[], faqList: Faq[], prelim: DiscoverResult) {
    setAiSession(null)
    let detail: SchemeDetail | null = null
    if (prelim.primary || prelim.matches[0]) {
      const code = prelim.primary?.scheme.serviceCode ?? prelim.matches[0]!.serviceCode
      setStatus({ label: t('statusFetching'), detail: code })
      try {
        detail = await api.scheme(code)
      } catch {
        detail = null
      }
      await sleep(300)
    }

    setStatus({ label: t('statusDrafting') })
    await sleep(400)

    const result = buildDiscoverResult(query, ranked, faqList, detail)
    setStatus(null)

    push({
      id: nextId(),
      role: 'assistant',
      kind: 'text',
      text: shortAnswer(result, lang),
    })

    await sleep(450)

    if (result.primary) {
      const scheme = result.primary.scheme
      setFocusScheme(scheme)
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'service',
        scheme,
        portal: result.primary.portal,
        steps: result.primary.steps,
        expanded: false,
      })
      setPills(pillsFor(lang, scheme, result))
    } else if (result.matches.length) {
      setFocusScheme(null)
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'matches',
        schemes: result.matches.slice(0, 4),
      })
      setPills(
        result.matches
          .slice(0, 3)
          .map((s) => `Tell me more about ${s.name}`)
          .concat(['Try a different service']),
      )
    } else {
      setFocusScheme(null)
      setPills([t('pillIncome'), t('pillCaste'), t('pillRtc'), t('pillRation')])
    }
  }

  /** Renders a SevaOne AI backend response and tracks its session for follow-ups. */
  function renderAiResponse(d: AiChatResponse) {
    setFocusScheme(null)
    push({ id: nextId(), role: 'assistant', kind: 'text', text: d.message })

    if (d.responseType === 'Resolved' && d.resolvedService) {
      const alreadyShown = aiSessionRef.current?.resolvedService?.serviceId === d.resolvedService.serviceId
      if (!alreadyShown) {
        push({
          id: nextId(),
          role: 'assistant',
          kind: 'ai-service',
          resolvedService: d.resolvedService,
          references: d.references,
          serviceCode: d.serviceCode ?? d.resolvedService.serviceCode,
        })
      }
      setAiSession({
        sessionId: d.sessionId,
        resolvedService: d.resolvedService,
        references: d.references,
      })
    } else if (d.responseType === 'Fallback' && d.candidates.length) {
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'ai-candidates',
        candidates: d.candidates,
      })
      setAiSession({
        sessionId: d.sessionId,
        resolvedService: null,
        references: [],
      })
    } else if (d.responseType === 'Clarify') {
      setAiSession({
        sessionId: d.sessionId,
        resolvedService: null,
        references: [],
      })
    } else {
      // NoMatch / Greeting / Fallback-without-candidates — nothing to continue.
      setAiSession(null)
    }

    setPills(defaultPills(d.suggestedQuestions, d.responseType, t))
  }

  async function ask(raw: string) {
    const query = raw.trim()
    if (!query || pending) return
    setQ('')
    setPills([])
    setPending(true)
    push({ id: nextId(), role: 'user', text: query })

    try {
      // Random numbers / keyboard mash — ask the citizen to rephrase. Short-but-real words
      // (e.g. a bare "certificate") still go through to the AI backend below.
      if (isGarbageInput(query)) {
        setStatus({ label: t('statusThinking'), detail: t('statusReading') })
        await sleep(350)
        setStatus(null)
        setFocusScheme(null)
        setAiSession(null)
        push({
          id: nextId(),
          role: 'assistant',
          kind: 'text',
          text: unclearReply(lang),
        })
        setPills([t('pillIncome'), t('pillCaste'), t('pillRtc'), t('pillRation')])
        return
      }

      // Income/Caste Certificate follow-ups keep using the existing regex-based flow.
      const focus = focusRef.current
      if (focus) {
        setStatus({ label: t('statusThinking'), detail: t('statusFollowUp') })
        await sleep(400)
        const handled = await answerFollowUp(query, focus)
        if (handled) return
      }

      // Continuing an AI backend conversation (Clarify / Resolved follow-up) — skip the
      // catalogue re-check and stay in the same session so context isn't lost.
      const activeAi = !focus ? aiSessionRef.current : null
      if (activeAi) {
        setStatus({ label: t('statusThinking'), detail: t('statusAskingAi') })
        try {
          const d = await sendAiChatMessage({
            sessionId: activeAi.sessionId,
            queryText: query,
            language: lang === 'kn' ? 'KN' : 'EN',
          })
          setStatus(null)
          renderAiResponse(d)
          return
        } catch {
          // AI backend unreachable mid-conversation — silently fall back to the catalogue.
          setStatus({
            label: t('statusCatalogue'),
            detail: t('statusCatalogueDetail'),
          })
          const { ranked, faqList, prelim } = await fetchRankedAndPrelim(query)
          await resolveWithCatalogue(query, ranked, faqList, prelim)
          return
        }
      }

      // Fresh query: pre-fetch the local catalogue result too, purely as a fallback for if the
      // AI backend call below fails - never used to bypass the AI when it's reachable, so
      // every query (including income/caste certificates) goes through the AI first.
      setStatus({
        label: t('statusThinking'),
        detail: t('statusUnderstanding'),
      })
      await sleep(300)
      setStatus({
        label: t('statusCatalogue'),
        detail: t('statusCatalogueDetail'),
      })
      const { ranked, faqList, prelim } = await fetchRankedAndPrelim(query)

      // Everything goes to the AI backend first.
      setStatus({ label: t('statusThinking'), detail: t('statusAskingAi') })
      try {
        const d = await sendAiChatMessage({
          queryText: query,
          language: lang === 'kn' ? 'KN' : 'EN',
        })
        setStatus(null)
        renderAiResponse(d)
      } catch {
        // AI backend unreachable — silently fall back to the catalogue result already fetched above.
        await resolveWithCatalogue(query, ranked, faqList, prelim)
      }
    } catch {
      setStatus(null)
      push({
        id: nextId(),
        role: 'assistant',
        kind: 'text',
        text: t('catalogueError'),
      })
      setPills([t('pillIncome'), t('pillCaste'), t('pillRtc'), t('pillRation')])
    } finally {
      setPending(false)
      setStatus(null)
    }
  }

  /** Tap to start recording, tap again to stop -- unlike the old browser-side recognition this
   *  replaced, there's no built-in "detected silence" auto-stop here (that would need its own
   *  voice-activity detection, not just swapping the transcription backend), so the citizen
   *  explicitly ends their turn instead of it happening automatically after a pause. */
  const toggleMic = () => {
    if (listening) {
      mediaRecorderRef.current?.stop()
      return
    }
    if (!navigator.mediaDevices?.getUserMedia) {
      inputRef.current?.focus()
      return
    }
    navigator.mediaDevices
      .getUserMedia({ audio: true })
      .then((stream) => {
        const recorder = new MediaRecorder(stream)
        audioChunksRef.current = []
        recorder.ondataavailable = (e) => {
          if (e.data.size > 0) audioChunksRef.current.push(e.data)
        }
        recorder.onstop = () => {
          stream.getTracks().forEach((track) => track.stop())
          const blob = new Blob(audioChunksRef.current, {
            type: recorder.mimeType || 'audio/webm',
          })
          void (async () => {
            try {
              const result = await transcribeVoiceAudio(blob, lang)
              const transcript = result.transcript.trim()
              if (result.status === 'success' && transcript) {
                setQ(transcript)
                void ask(transcript)
              }
            } catch {
              // Same silent fallback as before (recognition unavailable) -- leave the citizen
              // free to type instead of surfacing a scary error for a best-effort feature.
            } finally {
              setListening(false)
            }
          })()
        }
        mediaRecorderRef.current = recorder
        setListening(true)
        recorder.start()
      })
      .catch(() => {
        // Mic permission denied, no device, or an insecure (non-HTTPS) context -- same silent
        // fallback as when the old browser API was simply unavailable.
        inputRef.current?.focus()
      })
  }

  const resetChat = () => {
    setMessages([])
    setFocusScheme(null)
    setAiSession(null)
    setPills([])
    setStatus(null)
    setQ('')
    setPending(false)
  }

  const hasConversation = messages.length > 0 || pending
  const darkBackgroundPreview =
    !hasConversation && (backgroundPreviewMode === 'animated-dark' || backgroundPreviewMode === 'static-dark')
  const animatedBackgroundPreview = darkBackgroundPreview && backgroundPreviewMode === 'animated-dark'
  const auroraBackgroundPreview = !hasConversation && backgroundPreviewMode === 'animated-aurora'
  const fullWidthDarkBackgroundPreview = !hasConversation && backgroundPreviewMode === 'animated-dark-full-width'
  const wholePageDarkBackgroundPreview = !hasConversation && backgroundPreviewMode === 'animated-dark-whole-page'

  const changeBackgroundPreviewMode = (mode: BackgroundPreviewMode) => {
    setBackgroundPreviewMode(mode)
    persistBackgroundPreviewMode(mode)
  }

  return (
    <div
      className={`relative flex flex-col ${hasConversation ? 'h-dvh overflow-hidden bg-background' : 'min-h-screen'}${
        wholePageDarkBackgroundPreview ? ' seva-home-theme--whole-page-dark' : ''
      }`}
      style={legacy && !hasConversation && !wholePageDarkBackgroundPreview ? { background: 'radial-gradient(70% 70% at 12% 0%, rgb(60 82 164 / 0.16) 0%, transparent 68%), radial-gradient(62% 68% at 94% 8%, rgb(0 134 144 / 0.16) 0%, transparent 70%), linear-gradient(135deg, #f2f5ff 0%, #edf8f7 100%)' } : undefined}
    >
      <FloatingRail />

      {!hasConversation ? (
        <BackgroundThemePreview mode={backgroundPreviewMode} onChange={changeBackgroundPreviewMode} />
      ) : null}

      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[520px] opacity-70"
        style={{
          background:
            'radial-gradient(60% 60% at 20% 0%, rgb(57 76 157 / 0.13) 0%, transparent 70%), radial-gradient(50% 50% at 85% 10%, rgb(0 123 133 / 0.14) 0%, transparent 70%)',
        }}
      />

      {loginError ? (
        <p className="mx-auto mt-2 w-full max-w-7xl rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800 md:pl-20">
          Sign-in did not complete ({loginError}). Use Sign in on the sidebar and finish password + Email OTP on the
          Seva login page.
        </p>
      ) : null}

      <div
        className={hasConversation || legacy ? 'contents' : 'relative flex min-h-dvh flex-col'}
        style={
          hasConversation || legacy || wholePageDarkBackgroundPreview
            ? undefined
            : {
                background:
                  'radial-gradient(60% 55% at 10% 0%, rgb(60 82 164 / 0.26) 0%, transparent 70%), radial-gradient(55% 60% at 92% 6%, rgb(0 134 144 / 0.24) 0%, transparent 70%), linear-gradient(180deg, transparent 0%, transparent 55%, var(--color-background) 100%), linear-gradient(160deg, #eef1ff 0%, #e6f4f3 100%)',
              }
        }
      >
        <header className="relative mx-auto grid w-full max-w-7xl grid-cols-3 items-center gap-3 px-4 py-3 md:grid-cols-[1fr_auto_1fr] md:pl-20 md:pr-4">
          <div className="flex min-w-0 items-center justify-center gap-3 md:justify-end md:pr-6">
            <img
              src={dks}
              alt={t('cmName')}
              width={56}
              height={56}
              className="h-12 w-12 shrink-0 rounded-full bg-white object-cover ring-2 ring-border sm:h-14 sm:w-14"
            />
            <div className="hidden w-40 min-w-0 leading-tight xl:block">
              <div className="truncate t-meta-title">{t('cmName')}</div>
              <div className="truncate t-caption">{t('cmTitle')}</div>
            </div>
          </div>

          <button type="button" onClick={resetChat} className="flex min-w-[14rem] flex-col items-center gap-1">
            <span className="flex items-center gap-2">
              <img
                src={karnatakaEmblem}
                alt={t('emblemAlt')}
                width={64}
                height={64}
                className="h-14 w-14 shrink-0 bg-transparent object-contain mix-blend-multiply sm:h-16 sm:w-16"
              />
            </span>
            <span className="t-emblem-title">{t('govKarnataka')}</span>
            {hasConversation && <BrandWordmark className="text-base font-semibold tracking-tight sm:text-lg" />}
          </button>

          <div className="flex min-w-0 items-center justify-center gap-3 md:justify-start md:pl-6">
            <div className="hidden w-36 min-w-0 text-right leading-tight xl:block">
              <div className="truncate t-meta-title">{t('ministerName')}</div>
              <div className="truncate t-caption">{t('ministerTitle')}</div>
            </div>
            <img
              src={parameshwara}
              alt={t('ministerName')}
              width={56}
              height={56}
              className="h-12 w-12 shrink-0 rounded-full bg-white object-cover ring-2 ring-border sm:h-14 sm:w-14"
            />
          </div>

          <div className="col-span-3 flex justify-end md:absolute md:right-4 md:top-1/2 md:-translate-y-1/2">
            <TopRightUserControls />
          </div>
        </header>

        {!hasConversation ? (
          <main
            className={`mx-auto flex w-full max-w-7xl flex-col items-center px-4 pt-4 md:pl-20 md:pr-4 md:pt-8 ${legacy ? '' : 'flex-1 pb-6'}`}
          >
            <div className={`flex w-full flex-col items-center ${legacy ? '' : 'flex-1 justify-center'}`}>
              <div
                className={`landing-chat-area${backgroundThemeClass(
                  darkBackgroundPreview,
                  animatedBackgroundPreview,
                  fullWidthDarkBackgroundPreview,
                  auroraBackgroundPreview,
                )}`}
              >
                <img
                  src={nammaSeveLogo}
                  alt={t('nammaSeveLogoAlt')}
                  width={120}
                  height={107}
                  className="mb-1 h-24 w-auto object-contain mix-blend-multiply sm:h-28"
                />
                <BrandWordmark className="t-brand" />

                <h1
                  data-lang-fade
                  // Fixed-height, centred box: Kannada ottaksharas need ~1.5 leading and English looks best tighter, but the
                  // box height is the same so switching language does not shift the page.
                  className={`landing-hero-heading mt-5 flex min-h-[3.2em] flex-col justify-center text-center t-display ${
                    lang === 'kn' ? 'leading-[1.5]' : 'leading-[1.2]'
                  }`}
                >
                  {t('chatHeadline1')}
                  <span className="block bg-gradient-to-r from-[color:var(--gov-blue)] to-[color:var(--gov-green)] bg-clip-text text-transparent">
                    {t('chatHeadline2')}
                  </span>
                </h1>
                <p data-lang-fade className="landing-hero-lead mt-3 min-h-[4.6rem] max-w-xl text-center t-lead">
                  {t('chatLead')}
                  {lang === 'en' ? (
                    <span className="mt-0.5 block t-caption md:text-sm">{t('chatLeadKnLine')}</span>
                  ) : null}
                </p>

                <AskForm
                  q={q}
                  setQ={setQ}
                  inputRef={inputRef}
                  listening={listening}
                  pending={pending}
                  onMic={toggleMic}
                  onSubmit={() => void ask(q)}
                  landing
                />
                {listening && (
                  <p className="mt-3 inline-flex items-center gap-2 text-xs font-medium text-[color:var(--gov-blue)]">
                    <MicWaveBars large />
                    {t('listening')}
                  </p>
                )}

                <div data-lang-fade className="mt-4 flex w-full max-w-xl flex-wrap justify-center gap-2 px-4 sm:px-6">
                  {(
                    [
                      { label: t('sugIncome'), q: SUGGESTIONS[0].q },
                      { label: t('sugCaste'), q: SUGGESTIONS[1].q },
                      { label: t('sugRtc'), q: SUGGESTIONS[2].q },
                      { label: t('sugRation'), q: SUGGESTIONS[3].q },
                      { label: t('sugDl'), q: SUGGESTIONS[4].q },
                    ] as const
                  ).map((s) => (
                    <button
                      key={s.q}
                      type="button"
                      onClick={() => void ask(s.q)}
                      className="rounded-full border border-border bg-card px-4 py-2 t-chip transition hover:border-[color:var(--gov-blue)]/40 hover:bg-secondary"
                    >
                      {s.label}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            {!legacy && <div data-lang-fade className="mt-8 grid w-full gap-3 sm:grid-cols-2 lg:grid-cols-4">
              {LINK_CARDS.map((c) => (
                <Link
                  key={c.to}
                  to={c.to}
                  className="mirror-card shine rounded-2xl p-4 backdrop-blur-md backdrop-saturate-150"
                >
                  <div className="flex items-center gap-3">
                    {c.icon ? (
                      <span
                        className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br text-white shadow-sm ${c.tile}`}
                      >
                        <c.icon className="h-6 w-6" strokeWidth={2} />
                      </span>
                    ) : (
                      <img src={digilockerSymbol} alt="" width={44} height={44} className="h-11 w-11 shrink-0 rounded-xl object-cover shadow-sm" />
                    )}
                    <div className="min-w-0">
                      <div className="t-card-title">{t(c.titleKey)}</div>
                      <p className="mt-1 line-clamp-2 t-body">{t(c.bodyKey)}</p>
                    </div>
                  </div>
                </Link>
              ))}
            </div>}
          </main>
        ) : null}
      </div>

      {!hasConversation ? (
        <>
          <main className="mx-auto flex w-full max-w-7xl flex-col items-center px-4 pb-4 pt-14 md:pl-20 md:pr-4">
            {legacy && (
            <div className="grid w-full gap-3 sm:grid-cols-3">
              {highlights.map((h) => (
                <div key={h.title} className="rounded-2xl border border-border bg-card p-5">
                  <h.icon className="h-5 w-5 text-[color:var(--gov-blue)]" />
                  <div className="mt-3 t-card-title">{h.title}</div>
                  <p className="mt-1 t-body">{h.body}</p>
                </div>
              ))}
            </div>
            )}

            {popular.length > 0 && (
              <div className="mt-14 w-full">
                <div className="mb-4 flex items-center gap-3">
                  <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-orange-500 to-rose-600 text-white shadow-sm">
                    <Flame className="h-6 w-6" strokeWidth={2} />
                  </span>
                  <h2 className="t-section-title">{t('popularThisMonth')}</h2>
                </div>
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                  {popular.map((s, i) => (
                    <button
                      key={s.serviceCode}
                      type="button"
                      onClick={() => void ask(`Apply for ${s.name}`)}
                      className="group relative flex items-center gap-3 overflow-hidden rounded-2xl border border-border bg-card p-4 text-left transition hover:-translate-y-0.5 hover:border-[color:var(--gov-blue)]/40 hover:shadow-[var(--shadow-soft)]"
                    >
                      <span className="pointer-events-none absolute -right-1 -top-3 select-none text-6xl font-black text-[color:var(--gov-blue)]/[0.07]">
                        {i + 1}
                      </span>
                      <img
                        src={deptLogoOrEmblem(s.departmentName)}
                        alt=""
                        className="h-11 w-11 shrink-0 rounded-xl bg-white object-contain p-1 shadow-sm ring-1 ring-border"
                      />
                      <div className="min-w-0 flex-1">
                        <div className="line-clamp-2 t-card-title">{svcNameKn(lang, s.serviceCode, s.name)}</div>
                        <div className="mt-1 truncate t-caption">
                          {deptNameKn(lang, s.departmentCode, s.departmentName)}
                        </div>
                      </div>
                      <ArrowRight className="h-4 w-4 shrink-0 text-muted-foreground transition group-hover:translate-x-0.5 group-hover:text-[color:var(--gov-blue)]" />
                    </button>
                  ))}
                </div>
              </div>
            )}

            {me && !legacy && (
              <div className="mt-14 w-full">
                <RecommendedServices occupation={me.occupation} onEdit={() => void navigate({ to: '/dashboard', search: { ref: undefined } })} />
              </div>
            )}

            {!legacy && (
              <section className="mt-14 w-full rounded-3xl bg-[color:var(--gov-navy)] px-5 py-10 text-white sm:px-8">
                <h2 className="text-center t-section-title !text-white">{t('whySevaOne')}</h2>
                <p className="mx-auto mt-2 max-w-2xl text-center text-sm text-white/70">{t('whySevaOneBody')}</p>
                <div className="mt-8 grid gap-4 md:grid-cols-3">
                  {(
                    [
                      ['badgeSso', 'featSsoBody'],
                      ['featUnifiedTitle', 'featUnifiedBody'],
                      ['featAiTitle', 'featAiBody'],
                      ['featTrackTitle', 'featTrackBody'],
                      ['featCertTitle', 'featCertBody'],
                      ['featGrievanceTitle', 'featGrievanceBody'],
                    ] as const
                  ).map(([titleKey, bodyKey]) => (
                    <div key={titleKey} className="rounded-2xl border border-white/10 bg-white/5 p-5">
                      <div className="flex items-center gap-2">
                        <CheckCircle2 className="h-5 w-5 shrink-0 text-[color:var(--gov-gold)]" />
                        <div className="font-semibold">{t(titleKey)}</div>
                      </div>
                      <p className="mt-2 text-sm text-white/70">{t(bodyKey)}</p>
                    </div>
                  ))}
                </div>
              </section>
            )}

            <div className="mt-14 w-full">
              <h2 className="mb-4 t-section-title">{t('exploreByCategory')}</h2>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                {SERVICE_CATEGORIES.map((c) => (
                  <Link
                    key={c.id}
                    to="/services"
                    search={{ q: undefined, department: undefined, category: c.id }}
                    className="group rounded-2xl border border-border bg-card p-4 transition hover:-translate-y-0.5 hover:border-[color:var(--gov-blue)]/40 hover:shadow-[var(--shadow-soft)]"
                  >
                    <div className={`flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br text-white shadow-sm ${CATEGORY_TILE[c.id] ?? CATEGORY_TILE.other}`}>
                      <c.icon className="h-6 w-6" strokeWidth={2} />
                    </div>
                    <div className="mt-3 t-card-title">{t(c.nameKey)}</div>
                  </Link>
                ))}
              </div>
            </div>

            <GovernmentNotifications />

            {legacy && (
              <div className="mt-14 grid w-full gap-3 sm:grid-cols-2 lg:grid-cols-4">
                {LEGACY_LINK_CARDS.map((c) => (
                  <Link
                    key={c.to}
                    to={c.to}
                    className="rounded-2xl border border-border bg-card p-4 transition hover:border-[color:var(--gov-blue)]/40"
                  >
                    <div className="t-card-title">{t(c.titleKey)}</div>
                    <p className="mt-1 t-body">{t(c.bodyKey)}</p>
                  </Link>
                ))}
              </div>
            )}

            <div className="landing-official-card mt-14 w-full rounded-2xl border border-border bg-white px-6 py-7 shadow-[var(--shadow-soft)]">
              <div className="grid items-center gap-6 sm:grid-cols-3 sm:divide-x sm:divide-border sm:gap-0">
                <div className="flex w-full justify-center">
                  <img
                    src={gokEmblem}
                    alt={t('emblemAlt')}
                    width={72}
                    height={72}
                    className="h-16 w-16 shrink-0 object-contain sm:h-[72px] sm:w-[72px]"
                  />
                </div>
                {[
                  { img: dks, name: t('cmName'), role: t('homeCmRole') },
                  {
                    img: parameshwara,
                    name: t('ministerName'),
                    role: t('chatMinisterRole'),
                  },
                ].map((p) => (
                  <div key={p.name} className="flex w-full items-center justify-center gap-3 sm:px-4">
                    <img
                      src={p.img}
                      alt={p.name}
                      loading="lazy"
                      width={56}
                      height={56}
                      className="h-14 w-14 shrink-0 rounded-full bg-white object-cover ring-2 ring-border"
                    />
                    <div className="leading-tight">
                      <div className="t-card-title">{p.name}</div>
                      <div className="t-caption">{p.role}</div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </main>

          <div className="pb-20 md:pb-0">
            <SiteFooter />
          </div>
        </>
      ) : (
        <div className="flex min-h-0 flex-1 flex-col">
          {/* Slim page header — not a modal chrome */}
          <div className="mx-auto flex w-full max-w-7xl items-center gap-3 px-4 pb-2 pt-1 md:pl-20 md:pr-4">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-navy)]">
              <Sparkles className="h-4 w-4 text-white" />
            </div>
            <div className="min-w-0 flex-1">
              <div className="text-sm font-semibold text-[color:var(--gov-navy)]">{t('sakhi')}</div>
              <div className="truncate text-xs text-muted-foreground">{t('chatSubtitle')}</div>
            </div>
            <button
              type="button"
              onClick={resetChat}
              className="shrink-0 rounded-full px-3 py-1.5 text-xs font-medium text-muted-foreground transition hover:bg-secondary hover:text-foreground"
            >
              {t('newChat')}
            </button>
          </div>

          {/* Thread */}
          <div className="mx-auto w-full max-w-7xl flex-1 overflow-y-auto hide-scrollbar px-4 pb-4 md:pl-20 md:pr-4">
            <div className="space-y-4 py-2">
              {messages.map((msg) => {
                if (msg.role === 'user') {
                  return (
                    <div key={msg.id} className="flex justify-end">
                      <div className="max-w-[80%] rounded-2xl rounded-br-md bg-[color:var(--gov-navy)] px-4 py-2.5 text-sm leading-relaxed text-white">
                        {msg.text}
                      </div>
                    </div>
                  )
                }

                if (msg.kind === 'text') {
                  return (
                    <div key={msg.id} className="flex gap-3">
                      <div className="mt-1 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-navy)]">
                        <Sparkles className="h-4 w-4 text-white" />
                      </div>
                      <div className="max-w-[85%] pt-1 text-sm leading-relaxed text-foreground">
                        {renderMarkdownish(msg.text)}
                      </div>
                    </div>
                  )
                }

                if (msg.kind === 'service') {
                  const s = msg.scheme
                  const docs = 'documents' in s ? s.documents : []
                  return (
                    <div key={msg.id} className="flex gap-3">
                      <div className="mt-1 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-navy)] opacity-0">
                        <Sparkles className="h-4 w-4 text-white" />
                      </div>
                      <div className="w-full max-w-[90%] rounded-2xl bg-[color:var(--gov-blue)]/5 p-4">
                        <div className="flex flex-wrap items-start justify-between gap-3">
                          <div className="min-w-0">
                            <div className="text-[11px] font-semibold uppercase tracking-wide text-[color:var(--gov-blue)]">
                              {t('recommendedService')}
                            </div>
                            <div className="mt-0.5 text-base font-semibold text-[color:var(--gov-navy)]">
                              {svcNameKn(lang, s.serviceCode, s.name)}
                            </div>
                            <div className="text-xs text-muted-foreground">
                              {deptNameKn(lang, s.departmentCode, s.departmentName)} · {msg.portal}
                            </div>
                          </div>
                          <button
                            type="button"
                            onClick={() => void handleApply(s)}
                            className="inline-flex items-center gap-1.5 rounded-full bg-[color:var(--gov-blue)] px-4 py-2 text-sm font-medium text-white transition hover:opacity-90"
                          >
                            Apply now <ArrowRight className="h-4 w-4" />
                          </button>
                        </div>
                        <div className="mt-3 flex flex-wrap gap-2 text-xs">
                          <Badge variant="secondary">Fee {formatFeeInr(s)}</Badge>
                          <Badge variant="secondary">Sakala {s.sakalaDays} days</Badge>
                          <Badge variant="outline">{catKn(lang, s.category)}</Badge>
                        </div>

                        {msg.expanded ? (
                          <div className="mt-4 grid gap-4 sm:grid-cols-2">
                            <div>
                              <div className="mb-1.5 text-[11px] font-semibold uppercase text-muted-foreground">
                                {t('howToApply')}
                              </div>
                              <ol className="space-y-1.5">
                                {(msg.steps ?? []).map((step, i) => (
                                  <li key={step} className="flex gap-2 text-xs text-foreground/85">
                                    <span className="mt-[1px] flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-navy)] text-[10px] font-semibold text-white">
                                      {i + 1}
                                    </span>
                                    <span>{step}</span>
                                  </li>
                                ))}
                              </ol>
                            </div>
                            <div>
                              <div className="mb-1.5 text-[11px] font-semibold uppercase text-muted-foreground">
                                {t('documentsRequired')}
                              </div>
                              <ul className="space-y-1.5">
                                {docs.map((d) => (
                                  <li key={d} className="flex items-center gap-1.5 text-xs text-foreground/85">
                                    <FileCheck2 className="h-3.5 w-3.5 shrink-0 text-[color:var(--gov-green)]" /> {d}
                                  </li>
                                ))}
                              </ul>
                            </div>
                          </div>
                        ) : (
                          <button
                            type="button"
                            onClick={() => expandService(msg.id)}
                            className="mt-3 text-xs font-medium text-[color:var(--gov-blue)] hover:underline"
                          >
                            {t('showStepsAndDocs')}
                          </button>
                        )}
                      </div>
                    </div>
                  )
                }

                if (msg.kind === 'matches') {
                  return (
                    <div key={msg.id} className="flex gap-3">
                      <div className="mt-1 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-navy)] opacity-0">
                        <Sparkles className="h-4 w-4 text-white" />
                      </div>
                      <div className="w-full max-w-[90%] space-y-2">
                        {msg.schemes.map((s) => (
                          <div
                            key={s.serviceCode}
                            className="flex items-center justify-between gap-3 rounded-2xl bg-secondary/60 px-4 py-3"
                          >
                            <button
                              type="button"
                              className="min-w-0 flex-1 text-left"
                              onClick={() => void ask(`Tell me more about ${s.name}`)}
                            >
                              <div className="text-sm font-medium">{svcNameKn(lang, s.serviceCode, s.name)}</div>
                              <div className="mt-0.5 text-xs text-muted-foreground">
                                {deptNameKn(lang, s.departmentCode, s.departmentName)} · Sakala {s.sakalaDays}d ·{' '}
                                {formatFeeInr(s)}
                              </div>
                            </button>
                            <button
                              type="button"
                              onClick={() => void handleApply(s)}
                              className="shrink-0 rounded-full bg-[color:var(--gov-blue)] px-3 py-1.5 text-xs font-semibold text-white"
                            >
                              {t('applyCta')}
                            </button>
                          </div>
                        ))}
                      </div>
                    </div>
                  )
                }

                if (msg.kind === 'ai-service') {
                  const s = msg.resolvedService
                  const applyUrl = msg.references[0]
                  const catalogCode = resolveCatalogServiceCode(s, msg.serviceCode)
                  return (
                    <div key={msg.id} className="flex gap-3">
                      <div className="mt-1 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-navy)] opacity-0">
                        <Sparkles className="h-4 w-4 text-white" />
                      </div>
                      <div className="w-full max-w-[90%] rounded-2xl bg-[color:var(--gov-blue)]/5 p-4">
                        <div className="flex flex-wrap items-start justify-between gap-3">
                          <div className="min-w-0">
                            <div className="text-[11px] font-semibold uppercase tracking-wide text-[color:var(--gov-blue)]">
                              {t('recommendedService')}
                            </div>
                            <div className="mt-0.5 text-base font-semibold text-[color:var(--gov-navy)]">
                              {s.serviceName}
                            </div>
                            {s.kannadaServiceName ? (
                              <div className="text-xs text-muted-foreground">{s.kannadaServiceName}</div>
                            ) : null}
                            {s.departmentId ? (
                              <div className="text-xs text-muted-foreground">{s.departmentId}</div>
                            ) : null}
                            {catalogCode ? (
                              <div className="mt-1 text-[11px] text-muted-foreground">
                                {t('serviceCode')}: <span className="font-medium text-foreground">{catalogCode}</span>
                              </div>
                            ) : null}
                          </div>
                          <div className="flex shrink-0 flex-wrap items-center gap-2">
                            {applyUrl ? (
                              <button
                                type="button"
                                onClick={() => void handleAiApply(msg.references, s)}
                                title={applyUrl}
                                className="inline-flex items-center gap-1.5 rounded-full bg-[color:var(--gov-blue)] px-4 py-2 text-sm font-medium text-white transition hover:opacity-90"
                              >
                                {t('aiApplyOnPortal')} <ExternalLink className="h-4 w-4" />
                              </button>
                            ) : null}
                          </div>
                        </div>
                      </div>
                    </div>
                  )
                }

                if (msg.kind === 'ai-candidates') {
                  return (
                    <div key={msg.id} className="flex gap-3">
                      <div className="mt-1 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-navy)] opacity-0">
                        <Sparkles className="h-4 w-4 text-white" />
                      </div>
                      <div className="w-full max-w-[90%] space-y-2">
                        {msg.candidates.map((c) => (
                          <button
                            key={c.serviceId}
                            type="button"
                            className="block w-full rounded-2xl bg-secondary/60 px-4 py-3 text-left"
                            onClick={() => void ask(`Tell me more about ${c.serviceName}`)}
                          >
                            <div className="text-sm font-medium">{c.serviceName}</div>
                            {c.kannadaServiceName ? (
                              <div className="text-xs text-muted-foreground">{c.kannadaServiceName}</div>
                            ) : null}
                            {c.shortDescription ? (
                              <div className="mt-0.5 text-xs text-muted-foreground">{c.shortDescription}</div>
                            ) : null}
                          </button>
                        ))}
                      </div>
                    </div>
                  )
                }

                return null
              })}

              {status && (
                <div className="flex gap-3">
                  <div className="mt-1 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-navy)]">
                    <Loader2 className="h-4 w-4 animate-spin text-white" />
                  </div>
                  <div className="pt-1">
                    <div className="flex items-center gap-2 text-sm font-medium text-[color:var(--gov-navy)]">
                      <span className="relative flex h-2 w-2">
                        <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-[color:var(--gov-blue)] opacity-60" />
                        <span className="relative inline-flex h-2 w-2 rounded-full bg-[color:var(--gov-blue)]" />
                      </span>
                      {status.label}
                    </div>
                    {status.detail ? <p className="mt-0.5 text-xs text-muted-foreground">{status.detail}</p> : null}
                  </div>
                </div>
              )}

              {!pending && !status && pills.length > 0 && (
                <div className="flex flex-wrap gap-2 pl-12">
                  {pills.map((pill) => (
                    <button
                      key={pill}
                      type="button"
                      disabled={pending}
                      onClick={() => {
                        if (/^apply for this service$/i.test(pill) && focusScheme) {
                          void handleApply(focusScheme)
                          return
                        }
                        if (/^apply\b/i.test(pill) && aiSession?.resolvedService) {
                          void handleAiApply(aiSession.references, aiSession.resolvedService)
                          return
                        }
                        void ask(pill)
                      }}
                      className="rounded-full border border-border bg-card px-3.5 py-1.5 text-xs font-medium text-foreground/80 transition hover:border-[color:var(--gov-blue)]/40 hover:bg-secondary"
                    >
                      {pill}
                    </button>
                  ))}
                </div>
              )}

              <div ref={threadEndRef} />
            </div>
          </div>

          {/* Composer — page footer, not inside a card */}
          <div className="sticky bottom-0 bg-gradient-to-t from-background via-background to-transparent pb-20 pt-2 md:pb-6">
            <div className="mx-auto w-full max-w-7xl px-4 md:pl-20 md:pr-4">
              {listening && (
                <p className="mb-2 inline-flex items-center gap-2 text-xs font-medium text-[color:var(--gov-blue)]">
                  <MicWaveBars large />
                  {t('listening')}
                </p>
              )}
              <AskForm
                q={q}
                setQ={setQ}
                inputRef={inputRef}
                listening={listening}
                pending={pending}
                onMic={toggleMic}
                onSubmit={() => void ask(q)}
              />
              <p className="mt-2 text-center text-[10px] text-muted-foreground">{t('chatDisclaimer')}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function MicWaveBars({ large = false }: { readonly large?: boolean }) {
  return (
    <span className={`mic-wave ${large ? 'mic-wave-lg' : ''}`} aria-hidden>
      <span />
      <span />
      <span />
      <span />
      <span />
    </span>
  )
}

function AskForm({
  q,
  setQ,
  inputRef,
  listening,
  pending,
  onMic,
  onSubmit,
  landing = false,
}: {
  readonly q: string
  readonly setQ: (v: string) => void
  readonly inputRef: React.RefObject<HTMLInputElement | null>
  readonly listening: boolean
  readonly pending: boolean
  readonly onMic: () => void
  readonly onSubmit: () => void
  readonly landing?: boolean
}) {
  const { t } = useT()
  if (landing) {
    return (
      <form
        onSubmit={(e) => {
          e.preventDefault()
          onSubmit()
        }}
        className="mt-6 w-full"
      >
        <div className="flex items-center gap-2 rounded-full border border-border bg-card px-3 py-1.5 transition focus-within:border-[color:var(--gov-blue)]/50 md:px-4 md:py-2">
          <Sparkles className="h-4 w-4 shrink-0 text-[color:var(--gov-blue)]" aria-hidden />
          <input
            ref={inputRef}
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder={t('askPlaceholderLanding')}
            aria-label={t('askAriaLabel')}
            className="min-w-0 flex-1 bg-transparent text-sm outline-none placeholder:text-muted-foreground/70 md:text-base"
          />
          <button
            type="button"
            onClick={onMic}
            aria-label={listening ? t('micStopAria') : t('micSpeakAria')}
            aria-pressed={listening}
            className={`inline-flex h-8 min-w-8 shrink-0 items-center justify-center rounded-full border px-1.5 transition ${
              listening
                ? 'border-[color:var(--gov-blue)] bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]'
                : 'border-border text-muted-foreground hover:bg-secondary'
            }`}
          >
            {listening ? <MicWaveBars /> : <Mic className="h-4 w-4" />}
          </button>
          <button
            type="submit"
            aria-label={t('search')}
            disabled={!q.trim() || pending}
            className="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-blue)] text-white transition disabled:opacity-30"
          >
            {pending ? <Loader2 className="h-4 w-4 animate-spin" /> : <ArrowUp className="h-4 w-4" />}
          </button>
        </div>
      </form>
    )
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault()
        onSubmit()
      }}
      className="flex gap-2"
    >
      <div className="flex flex-1 items-center rounded-2xl border border-border bg-card px-3 shadow-[var(--shadow-soft)] transition-colors focus-within:border-[color:var(--gov-blue)]/40">
        <Sparkles className="h-4 w-4 shrink-0 text-[color:var(--gov-blue)]" />
        <input
          ref={inputRef}
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder={t('askPlaceholderFollowup')}
          disabled={pending}
          className="min-w-0 flex-1 bg-transparent px-2 py-3 text-sm outline-none disabled:opacity-60"
        />
        <button
          type="button"
          onClick={onMic}
          disabled={pending}
          aria-label={listening ? 'Stop voice input' : 'Start voice input'}
          aria-pressed={listening}
          className={`flex h-8 min-w-8 shrink-0 items-center justify-center rounded-full px-1.5 transition ${
            listening
              ? 'bg-[color:var(--gov-navy)] text-white'
              : 'bg-secondary text-muted-foreground hover:text-foreground'
          }`}
        >
          {listening ? <MicWaveBars /> : <Mic className="h-4 w-4" />}
        </button>
      </div>
      <Button
        type="submit"
        disabled={pending || !q.trim()}
        className="h-auto rounded-2xl bg-[color:var(--gov-navy)] px-4 hover:bg-[color:var(--gov-navy)]/90"
      >
        {pending ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          <span className="inline-flex items-center gap-1.5">
            <Sparkles className="h-3.5 w-3.5" /> Ask AI
          </span>
        )}
      </Button>
    </form>
  )
}
