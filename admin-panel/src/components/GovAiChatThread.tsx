import { useEffect, useRef, useState } from 'react'
import { BrainCircuit, Send, Sparkles } from 'lucide-react'
import { Button } from './ui/button'
import { useGovAiChat } from '../hooks/useGovAiChat'

const SUGGESTIONS = ['How many applications are in progress?', 'Compare service discovery by department.', 'How many citizens are registered?']

/** ChatGPT-style conversation thread: shared between the full /ai/assistant page and the
 * floating widget (see useGovAiChat) so both surfaces always show the same conversation. */
export function GovAiChatThread({ compact }: { readonly compact?: boolean }) {
  const { messages, ask, busy } = useGovAiChat()
  const [input, setInput] = useState('')
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' })
  }, [messages.length, busy])

  function submit(event: React.FormEvent) {
    event.preventDefault()
    const question = input.trim()
    if (!question || busy) return
    setInput('')
    void ask(question)
  }

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex-1 space-y-4 overflow-y-auto px-1 py-2">
        {messages.length === 0 && (
          <div className="flex h-full flex-col items-center justify-center px-6 py-10 text-center">
            <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]"><BrainCircuit className="h-6 w-6" /></div>
            <h3 className="mt-4 text-sm font-semibold text-[color:var(--gov-navy)]">Ask about live SevaOne reports</h3>
            <p className="mt-1 max-w-xs text-xs leading-5 text-muted-foreground">Answers come from live aggregate API calls only, scoped to your government access.</p>
            <div className="mt-5 flex w-full flex-col gap-2">
              {SUGGESTIONS.map((suggestion) => (
                <button key={suggestion} type="button" onClick={() => void ask(suggestion)} className="rounded-xl border border-border bg-secondary/40 px-3 py-2 text-left text-xs font-medium text-[color:var(--gov-navy)] transition hover:bg-[color:var(--gov-blue)]/8">
                  {suggestion}
                </button>
              ))}
            </div>
          </div>
        )}
        {messages.map((message) => (
          <div key={message.id} className={`flex ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}>
            {message.role === 'assistant' && (
              <div className="mr-2 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]"><BrainCircuit className="h-4 w-4" /></div>
            )}
            <div className={`max-w-[80%] rounded-2xl px-4 py-2.5 text-sm leading-6 ${
              message.role === 'user'
                ? 'rounded-br-md bg-[color:var(--gov-blue)] text-white'
                : message.isError
                  ? 'rounded-bl-md border border-red-200 bg-red-50 text-red-700'
                  : 'rounded-bl-md border border-border bg-white text-[color:var(--gov-navy)]'
            }`}>
              <p className="whitespace-pre-wrap">{message.text}</p>
              {message.sources && message.sources.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-1.5 border-t border-black/5 pt-2">
                  {message.sources.map((source) => (
                    <span key={source.tool} className="rounded-full bg-secondary px-2 py-0.5 text-[10px] font-medium text-muted-foreground">{source.description}</span>
                  ))}
                </div>
              )}
            </div>
          </div>
        ))}
        {busy && (
          <div className="flex justify-start">
            <div className="mr-2 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]"><BrainCircuit className="h-4 w-4" /></div>
            <div className="flex items-center gap-1 rounded-2xl rounded-bl-md border border-border bg-white px-4 py-3">
              <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-muted-foreground [animation-delay:-0.3s]" />
              <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-muted-foreground [animation-delay:-0.15s]" />
              <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-muted-foreground" />
            </div>
          </div>
        )}
        <div ref={bottomRef} />
      </div>
      <form onSubmit={submit} className="mt-2 shrink-0 border-t border-border pt-3">
        <div className={`flex items-end gap-2 rounded-2xl border border-border bg-secondary/30 p-2 ${compact ? '' : 'sm:p-2.5'}`}>
          <textarea
            value={input}
            onChange={(event) => setInput(event.target.value)}
            onKeyDown={(event) => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); submit(event) } }}
            rows={1}
            maxLength={2000}
            placeholder="Ask about a current aggregate report..."
            className="max-h-28 min-h-9 flex-1 resize-none border-0 bg-transparent px-2 py-1.5 text-sm outline-none placeholder:text-muted-foreground"
          />
          <Button type="submit" size="sm" disabled={busy || !input.trim()} className="h-9 shrink-0 rounded-xl bg-[color:var(--gov-blue)] px-3 text-white hover:bg-[color:var(--gov-blue-dark)]">
            <Send className="h-4 w-4" />
          </Button>
        </div>
        <p className="mt-2 flex items-center gap-1 text-[11px] text-muted-foreground"><Sparkles className="h-3 w-3" /> Live aggregate data only, no document search -- scoped to your government access.</p>
      </form>
    </div>
  )
}
