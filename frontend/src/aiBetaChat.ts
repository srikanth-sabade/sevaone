// Talks directly to the new AI Discovery backend the co-dev hosted separately from
// sevaone-rag (see AiBeta.tsx) -- its own FastAPI process, reached through this app's own
// nginx /ai-beta-api/ proxy (see nginx.conf) or Vite's dev proxy (see vite.config.ts), never
// through the bff or any other SevaOne service. Wire shape matches its openapi.json
// (ChatRequest/ChatResponse) exactly -- no field renaming needed, unlike aiChat.ts's mapping
// from sevaone-rag's leaner snake_case shape.
const AI_BETA_CHAT_TIMEOUT_MS = 35000

export type AiBetaResponseType = 'Clarify' | 'Resolved' | 'Fallback' | 'NoMatch' | 'Greeting' | 'List' | 'Info'

export type AiBetaResolvedService = {
  departmentId?: string | null
  departmentCode?: string | null
  serviceId: string
  serviceName: string
  kannadaServiceName?: string | null
  serviceCode?: string | null
}

export type AiBetaCandidate = {
  serviceId: string
  serviceName: string
  kannadaServiceName?: string | null
  shortDescription?: string | null
  serviceCode?: string | null
  departmentCode?: string | null
  linkType?: string | null
}

export type AiBetaChatRequest = {
  sessionId?: string | null
  queryText: string
  inputMode?: 'Text' | 'Voice'
  language?: 'EN' | 'KN'
  serviceId?: string | null
  requestId?: string | null
}

export type AiBetaChatResponse = {
  sessionId: string
  responseType: AiBetaResponseType
  message: string
  language?: string | null
  resolvedService?: AiBetaResolvedService | null
  resolvedServices: AiBetaResolvedService[]
  candidates: AiBetaCandidate[]
  references: string[]
  suggestedQuestions: string[]
  serviceCode?: string | null
  listedCategory?: string | null
  sevasindhuServiceCode?: string | null
  outcome?: string | null
  requestId?: string | null
  catalogVersion?: string | null
}

/** Throws on any non-2xx response, network error, or timeout -- caller (AiBeta.tsx) shows the error inline, there is no catalogue fallback here (this page is only for beta-testing the new backend itself). */
export async function sendAiBetaChatMessage(body: AiBetaChatRequest): Promise<AiBetaChatResponse> {
  const controller = new AbortController()
  const timer = window.setTimeout(() => controller.abort(), AI_BETA_CHAT_TIMEOUT_MS)
  try {
    const response = await fetch('/ai-beta-api/api/v1/ai-chat/message', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({
        sessionId: body.sessionId ?? null,
        queryText: body.queryText,
        inputMode: body.inputMode ?? 'Text',
        language: body.language ?? 'EN',
        serviceId: body.serviceId ?? null,
        requestId: body.requestId ?? null,
      }),
      signal: controller.signal,
    })
    if (!response.ok) {
      let detail = response.statusText
      try {
        const body2 = (await response.json()) as { detail?: unknown }
        if (body2.detail) detail = typeof body2.detail === 'string' ? body2.detail : JSON.stringify(body2.detail)
      } catch {
        /* keep statusText */
      }
      throw new Error(`AI beta backend ${response.status}: ${detail}`)
    }
    return (await response.json()) as AiBetaChatResponse
  } finally {
    window.clearTimeout(timer)
  }
}
