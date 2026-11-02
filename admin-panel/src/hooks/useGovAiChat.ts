import { useEffect, useState } from 'react'
import { govAdminApi } from '../api'
import type { GovAiAssistantResponse } from '../types'

export interface GovAiChatMessage {
  id: string
  role: 'user' | 'assistant'
  text: string
  sources?: GovAiAssistantResponse['sources']
  scopeNote?: string
  isError?: boolean
}

// Module-scoped (not component-scoped) so the floating widget and the full /ai/assistant page
// share one conversation instead of each keeping its own -- opening the widget mid-page should
// show the same thread you'd see on the full page, and vice versa.
let messages: GovAiChatMessage[] = []
const listeners = new Set<() => void>()
function setMessages(next: GovAiChatMessage[]) {
  messages = next
  for (const listener of listeners) listener()
}

let nextId = 0
function makeId() {
  nextId += 1
  return `gov-ai-${nextId}`
}

export function useGovAiChat() {
  const [, setTick] = useState(0)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    const listener = () => setTick((tick) => tick + 1)
    listeners.add(listener)
    return () => { listeners.delete(listener) }
  }, [])

  async function ask(question: string) {
    const trimmed = question.trim()
    if (!trimmed || busy) return
    setMessages([...messages, { id: makeId(), role: 'user', text: trimmed }])
    setBusy(true)
    try {
      const result = await govAdminApi.askGovAi(trimmed)
      setMessages([...messages, { id: makeId(), role: 'assistant', text: result.reply, sources: result.sources, scopeNote: result.scopeNote }])
    } catch (error) {
      setMessages([...messages, { id: makeId(), role: 'assistant', text: error instanceof Error ? error.message : 'Could not reach the live assistant.', isError: true }])
    } finally {
      setBusy(false)
    }
  }

  return { messages, ask, busy }
}
