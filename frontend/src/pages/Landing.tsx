import { Link, useNavigate, useSearch } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import { ArrowUp, Clock, Layers, Mic, ShieldCheck, Sparkles } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import dks from '../assets/dks.webp'
import gokEmblem from '../assets/gok-emblem.webp'
import parameshwara from '../assets/parameshwara.webp'
import { api } from '../api'
import { FloatingRail } from '../components/FloatingRail'
import { flow } from '../flowLog'

const SUGGESTIONS = [
  { en: 'Income certificate', kn: 'ಆದಾಯ ಪ್ರಮಾಣಪತ್ರ', q: 'income certificate' },
  { en: 'Caste certificate', kn: 'ಜಾತಿ ಪ್ರಮಾಣಪತ್ರ', q: 'caste certificate' },
  { en: 'Land record (RTC)', kn: 'ಪಹಣಿ / ಆರ್‌ಟಿಸಿ', q: 'RTC land record' },
  { en: 'Ration card', kn: 'ಪಡಿತರ ಚೀಟಿ', q: 'ration card' },
  { en: 'Driving licence', kn: 'ಚಾಲನಾ ಪರವಾನಗಿ', q: 'driving licence' },
]

const HIGHLIGHTS = [
  { icon: Layers, title: '1,150+ services, one search', body: '112 departments behind a single question box.' },
  { icon: Clock, title: 'Minutes, not counters', body: 'Pre-filled forms, saved documents, instant status.' },
  { icon: ShieldCheck, title: 'Verified end to end', body: 'Aadhaar-linked identity, e-sign and DigiLocker built in.' },
]

export function LandingPage() {
  const navigate = useNavigate()
  const { loginError } = useSearch({ from: '/' })
  const [q, setQ] = useState('')
  const [listening, setListening] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)
  const popularQ = useQuery({ queryKey: ['schemes', 'popular'], queryFn: () => api.schemesPopular(6) })
  const popular = popularQ.data ?? []

  useEffect(() => {
    if (loginError) {
      flow('WSO2/BFF login failed — landed back on home without a session cookie', { loginError })
    }
  }, [loginError])

  const submit = (value: string) => {
    const query = value.trim()
    if (!query) return
    void navigate({ to: '/services', search: { q: query, department: undefined, category: undefined } })
  }

  const startVoice = () => {
    const w = window as unknown as {
      SpeechRecognition?: new () => {
        lang: string
        interimResults: boolean
        onresult: ((e: { results?: ArrayLike<ArrayLike<{ transcript?: string }>> }) => void) | null
        onerror: (() => void) | null
        onend: (() => void) | null
        start: () => void
      }
      webkitSpeechRecognition?: new () => {
        lang: string
        interimResults: boolean
        onresult: ((e: { results?: ArrayLike<ArrayLike<{ transcript?: string }>> }) => void) | null
        onerror: (() => void) | null
        onend: (() => void) | null
        start: () => void
      }
    }
    const SR = w.SpeechRecognition || w.webkitSpeechRecognition
    if (!SR) {
      inputRef.current?.focus()
      return
    }
    const rec = new SR()
    rec.lang = 'en-IN'
    rec.interimResults = false
    rec.onresult = (e) => {
      const text = e.results?.[0]?.[0]?.transcript ?? ''
      setQ(text)
      setListening(false)
      submit(text)
    }
    rec.onerror = () => setListening(false)
    rec.onend = () => setListening(false)
    setListening(true)
    rec.start()
  }

  return (
    <div className="relative flex min-h-screen flex-col bg-background">
      <FloatingRail />

      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[520px] opacity-70"
        style={{
          background:
            'radial-gradient(60% 60% at 20% 0%, oklch(0.52 0.19 278 / 0.16) 0%, transparent 70%), radial-gradient(50% 50% at 85% 10%, oklch(0.66 0.14 168 / 0.18) 0%, transparent 70%)',
        }}
      />

      <div className="mx-auto flex w-full max-w-5xl items-center justify-end px-5 pt-3 md:pl-20">
        <Link
          to="/login"
          search={{ returnTo: undefined, loginError: undefined, mode: undefined }}
          className="rounded-full bg-[color:var(--gov-blue)] px-4 py-1.5 text-xs font-semibold text-primary-foreground shadow-sm transition hover:opacity-90"
        >
          Login / Signup
        </Link>
      </div>

      {loginError ? (
        <p className="mx-auto mt-2 max-w-5xl rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800 md:pl-20">
          Sign-in did not complete ({loginError}). Use Login again and finish WSO2 password + Email OTP within a minute.
        </p>
      ) : null}

      <header className="mx-auto grid w-full max-w-5xl grid-cols-3 items-center gap-3 px-5 py-3 md:pl-20">
        <div className="flex min-w-0 items-center gap-3">
          <img
            src={dks}
            alt="Shri D. K. Shivakumar"
            width={56}
            height={56}
            className="h-12 w-12 shrink-0 rounded-full bg-white object-cover ring-2 ring-border sm:h-14 sm:w-14"
          />
          <div className="hidden min-w-0 leading-tight md:block">
            <div className="truncate text-xs font-semibold">Shri D. K. Shivakumar</div>
            <div className="truncate text-[11px] text-muted-foreground">Hon&apos;ble Chief Minister</div>
          </div>
        </div>

        <Link to="/" search={{ loginError: undefined }} className="flex flex-col items-center gap-1">
          <img
            src={gokEmblem}
            alt="Government of Karnataka emblem"
            width={48}
            height={48}
            className="h-11 w-11 shrink-0 object-contain sm:h-12 sm:w-12"
          />
          <span className="text-base font-semibold tracking-tight sm:text-lg">
            Seva<span className="text-[color:var(--gov-blue)]">One</span>
          </span>
        </Link>

        <div className="flex min-w-0 items-center justify-end gap-3">
          <div className="hidden min-w-0 text-right leading-tight md:block">
            <div className="truncate text-xs font-semibold">Shri G. Parameshwara</div>
            <div className="truncate text-[11px] text-muted-foreground">Dy CM & Home Minister</div>
          </div>
          <img
            src={parameshwara}
            alt="Shri G. Parameshwara"
            width={56}
            height={56}
            className="h-12 w-12 shrink-0 rounded-full bg-white object-cover ring-2 ring-border sm:h-14 sm:w-14"
          />
        </div>
      </header>

      <main className="mx-auto flex w-full max-w-3xl flex-1 flex-col items-center px-5 pb-24 pt-4 md:pl-8 md:pt-8">
        <span className="rounded-full border border-border bg-card px-3.5 py-1 text-[11px] font-medium text-muted-foreground">
          Government of Karnataka · ಸೇವಾ ವನ್
        </span>

        <h1 className="mt-4 text-center text-3xl font-semibold leading-[1.05] tracking-tight sm:text-4xl md:text-5xl">
          Ask once.
          <span className="block bg-gradient-to-r from-[color:var(--gov-blue)] to-[color:var(--gov-green)] bg-clip-text text-transparent">
            Government handles the rest.
          </span>
        </h1>
        <p className="mt-3 max-w-xl text-center text-sm leading-relaxed text-muted-foreground md:text-base">
          Type or speak what you need — SevaOne finds the right service, form and department for you.
          <span className="mt-0.5 block text-xs md:text-sm">ಸರ್ಕಾರದ ಯಾವುದೇ ಸೇವೆಯನ್ನು ಇಲ್ಲಿ ಕೇಳಿ.</span>
        </p>

        <form
          onSubmit={(e) => {
            e.preventDefault()
            submit(q)
          }}
          className="mt-6 w-full"
        >
          <div className="flex items-center gap-2 rounded-full border border-border bg-card px-3 py-1.5 transition focus-within:border-[color:var(--gov-blue)]/50 md:px-4 md:py-2">
            <Sparkles className="h-4 w-4 shrink-0 text-[color:var(--gov-blue)]" aria-hidden />
            <input
              ref={inputRef}
              value={q}
              onChange={(e) => setQ(e.target.value)}
              placeholder="e.g. income certificate for my daughter's college"
              aria-label="Ask for a government service"
              className="min-w-0 flex-1 bg-transparent text-sm outline-none placeholder:text-muted-foreground/70 md:text-base"
            />
            <button
              type="button"
              onClick={startVoice}
              aria-label="Speak your request"
              className={`inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-full border transition ${
                listening
                  ? 'animate-pulse border-[color:var(--gov-blue)] bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]'
                  : 'border-border text-muted-foreground hover:bg-secondary'
              }`}
            >
              <Mic className="h-4 w-4" />
            </button>
            <button
              type="submit"
              aria-label="Search"
              disabled={!q.trim()}
              className="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-blue)] text-white transition disabled:opacity-30"
            >
              <ArrowUp className="h-4 w-4" />
            </button>
          </div>
        </form>

        <div className="mt-4 flex w-full max-w-xl flex-wrap justify-center gap-2 px-4 sm:px-6">
          {SUGGESTIONS.map((s) => (
            <button
              key={s.en}
              type="button"
              onClick={() => submit(s.q)}
              className="rounded-full border border-border bg-card px-4 py-2 text-sm text-foreground/80 transition hover:border-[color:var(--gov-blue)]/40 hover:bg-secondary"
            >
              {s.en}
              <span className="ml-1.5 text-xs text-muted-foreground">{s.kn}</span>
            </button>
          ))}
        </div>

        <div className="mt-14 grid w-full gap-3 sm:grid-cols-3">
          {HIGHLIGHTS.map((h) => (
            <div key={h.title} className="rounded-2xl border border-border bg-card p-5">
              <h.icon className="h-5 w-5 text-[color:var(--gov-blue)]" />
              <div className="mt-3 text-sm font-semibold">{h.title}</div>
              <p className="mt-1 text-sm text-muted-foreground">{h.body}</p>
            </div>
          ))}
        </div>

        <div className="mt-14 w-full">
          <div className="mb-3 text-xs font-medium uppercase tracking-wider text-muted-foreground">
            Frequently requested this month
          </div>
          <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-2">
            {popular.map((s) => (
              <Link
                key={s.serviceCode}
                to="/services/$code"
                params={{ code: s.serviceCode }}
                className="rounded-2xl border border-border bg-card px-4 py-3.5 text-sm transition hover:border-[color:var(--gov-blue)]/40 hover:shadow-[var(--shadow-soft)]"
              >
                <div className="font-medium leading-snug">{s.name}</div>
                <div className="mt-1 text-xs text-muted-foreground">{s.departmentName}</div>
              </Link>
            ))}
            {!popularQ.isLoading && popular.length === 0 && (
              <p className="col-span-full text-sm text-muted-foreground">Catalogue is loading… Open Services to browse.</p>
            )}
          </div>
        </div>

        <div className="mt-14 w-full rounded-2xl border border-border bg-white px-6 py-7 shadow-[var(--shadow-soft)]">
          <div className="flex flex-col items-center gap-6 sm:flex-row sm:justify-center sm:gap-10">
            <img
              src={gokEmblem}
              alt="Government of Karnataka emblem"
              width={72}
              height={72}
              className="h-16 w-16 shrink-0 object-contain sm:h-[72px] sm:w-[72px]"
            />
            {[
              { img: dks, name: 'Shri D. K. Shivakumar', role: "Hon'ble Chief Minister, Karnataka" },
              { img: parameshwara, name: 'Shri G. Parameshwara', role: 'Deputy Chief Minister & Home Minister' },
            ].map((p) => (
              <div key={p.name} className="flex items-center gap-3">
                <img
                  src={p.img}
                  alt={p.name}
                  loading="lazy"
                  decoding="async"
                  width={56}
                  height={56}
                  className="h-14 w-14 shrink-0 rounded-full bg-white object-cover ring-2 ring-border"
                />
                <div className="leading-tight">
                  <div className="text-sm font-semibold">{p.name}</div>
                  <div className="text-xs text-muted-foreground">{p.role}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </main>

      <footer className="border-t border-border pb-20 md:pb-6">
        <div className="mx-auto flex max-w-3xl flex-wrap items-center justify-center gap-x-6 gap-y-2 px-5 py-6 text-sm">
          <Link to="/track" className="text-muted-foreground hover:text-foreground">
            Track application
          </Link>
          <Link to="/help" className="text-muted-foreground hover:text-foreground">
            Help
          </Link>
          <Link to="/grievance" className="text-muted-foreground hover:text-foreground">
            Grievance
          </Link>
        </div>
        <div className="px-5 pb-6 text-center text-xs text-muted-foreground">
          PoC UI for discussion — Government of Karnataka · Directorate of EDCS
        </div>
      </footer>
    </div>
  )
}
