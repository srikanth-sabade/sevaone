import { flow } from './flowLog'
import { request, requestForm } from './api'
import type { AiChatResponse, AiCandidate, AiResolvedService } from './types'

// Calls sevaone-rag directly (its own local dev process, port 8009 - see
// sevaone-rag/run.py, proxied at /rag-api by vite.config.ts locally / nginx.conf in
// Docker), NOT the bff/ai_backend pipeline anymore. ai_backend is co-dev's own separate
// project; this app's "AI Discovery" now runs on sevaone-rag end to end, LLM included (its
// own vLLM instance on the GPU box, reached over sevaone-rag's own SSH tunnel - see
// sevaone-rag/scripts/gpu_tunnel_local_dev.sh and .env's OPENAI_BASE_URL).
const AI_CHAT_TIMEOUT_MS = 35000
const ASR_TIMEOUT_MS = 20000

export type AiChatRequest = {
  sessionId?: string
  queryText: string
  inputMode?: 'Text' | 'Voice'
  language?: 'EN' | 'KN'
}

/** sevaone-rag's own wire shape (app/schemas/agent_chat.py's AgentChatResponse) - deliberately
 *  simpler than AiChatResponse, which was shaped for ai_backend's richer deterministic
 *  orchestrator output. Mapped into AiChatResponse below so Chat.tsx's existing rendering
 *  (built against that richer shape) needs no changes of its own. */
type SevaOneRagChatResponse = {
  session_id: string
  reply: string
  apply_url: string | null
  status: 'answered' | 'clarifying'
  resolved_service: {
    service_id: string
    service_name: string
    kannada_service_name?: string | null
    department?: string | null
    department_code?: string | null
    operator_hint?: string | null
    service_code?: string | null
  } | null
  options: { service_id: string; service_name: string }[]
}

function mapResolvedService(s: NonNullable<SevaOneRagChatResponse['resolved_service']>): AiResolvedService {
  return {
    serviceId: s.service_id,
    serviceName: s.service_name,
    kannadaServiceName: s.kannada_service_name,
    departmentId: s.department,
    departmentCode: s.department_code,
    operatorHint: s.operator_hint,
    serviceCode: s.service_code,
  }
}

function mapCandidate(o: SevaOneRagChatResponse['options'][number]): AiCandidate {
  return {
    serviceId: o.service_id,
    serviceName: o.service_name,
    kannadaServiceName: null,
    shortDescription: null,
    serviceCode: null,
  }
}

/** status/resolved_service/options -> the responseType enum Chat.tsx's renderAiResponse
 *  branches on. sevaone-rag has no direct equivalent of NoMatch/Greeting (a refusal or a
 *  greeting both just come back as status="clarifying" with no options, same as an ordinary
 *  clarifying question) - mapped to "Clarify" throughout, which only affects the follow-up
 *  suggested-pills shown after the reply, not the reply text itself. */
function toAiChatResponse(json: SevaOneRagChatResponse): AiChatResponse {
  if (json.status === 'answered' && json.resolved_service) {
    return {
      sessionId: json.session_id,
      responseType: 'Resolved',
      message: json.reply,
      resolvedService: mapResolvedService(json.resolved_service),
      candidates: [],
      references: json.apply_url ? [json.apply_url] : [],
      suggestedQuestions: [],
      serviceCode: json.resolved_service.service_code,
    }
  }
  if (json.options.length >= 2) {
    return {
      sessionId: json.session_id,
      responseType: 'Fallback',
      message: json.reply,
      resolvedService: null,
      candidates: json.options.map(mapCandidate),
      references: [],
      suggestedQuestions: [],
      serviceCode: null,
    }
  }
  return {
    sessionId: json.session_id,
    responseType: 'Clarify',
    message: json.reply,
    resolvedService: null,
    candidates: [],
    references: [],
    suggestedQuestions: [],
    serviceCode: null,
  }
}

/** Throws on any non-2xx response, network error, or timeout — callers fall back to the local catalogue. */
export async function sendAiChatMessage(body: AiChatRequest): Promise<AiChatResponse> {
  flow('browser → sevaone-rag POST /chat', { queryText: body.queryText, sessionId: body.sessionId })
  try {
    const json = await request<SevaOneRagChatResponse>(
      '/rag-api/chat',
      {
        method: 'POST',
        body: JSON.stringify({
          session_id: body.sessionId ?? null,
          message: body.queryText,
          language: body.language === 'KN' ? 'kn' : 'en',
        }),
      },
      AI_CHAT_TIMEOUT_MS,
    )
    const mapped = toAiChatResponse(json)
    flow('sevaone-rag ← /chat 200', {
      responseType: mapped.responseType,
      resolvedService: mapped.resolvedService?.serviceName,
    })
    return mapped
  } catch (err) {
    flow('sevaone-rag ← /chat failed', { error: err instanceof Error ? err.message : String(err) })
    throw err
  }
}

export type AsrResult = {
  transcript: string
  language: string
  status: 'success' | 'empty' | 'error'
  message?: string | null
}

/** Used by Chat.tsx's mic button (MediaRecorder-captured audio) to transcribe server-side via
 *  ai_backend's Bhashini integration, instead of the browser's own SpeechRecognition API.
 *  Left on the bff/ai_backend path deliberately - sevaone-rag has no ASR endpoint of its own,
 *  and voice transcription is a separate concern from which backend answers the chat itself. */
export async function transcribeVoiceAudio(audioBlob: Blob, language?: string): Promise<AsrResult> {
  const form = new FormData()
  // A bare Blob (not File) has no filename, so the backend would see an extension-less upload
  // -- derive one from the blob's actual MIME type (MediaRecorder defaults vary: webm on
  // Chrome/Firefox, mp4 on Safari) so the right audio format reaches Bhashini, not a guessed one.
  const extension = audioBlob.type.split(';')[0].split('/')[1] || 'webm'
  form.append('file', audioBlob, `recording.${extension}`)
  if (language) form.append('language', language)
  flow('browser → BFF POST /language/asr')
  try {
    const json = await requestForm<AsrResult>('/api/v1/language/asr', form, ASR_TIMEOUT_MS)
    flow('BFF ← /language/asr 200', { status: json.status })
    return json
  } catch (err) {
    flow('BFF ← /language/asr failed', { error: err instanceof Error ? err.message : String(err) })
    throw err
  }
}
