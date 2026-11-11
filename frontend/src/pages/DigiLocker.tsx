import { BadgeCheck, FileText, Link2, Search, ShieldCheck, X } from 'lucide-react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import { api } from '../api'
import type { DigiLockerDocument } from '../types'
import digilockerSymbol from '../assets/digilocker-symbol.webp'
import { AadhaarCards, parseAadhaar } from '../components/AadhaarCards'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { loginPath } from '../applyFlow'
import { useSession } from '../store'

const fileUrl = (id: string, format: string) => `/api/v1/me/digilocker/documents/${id}/file?format=${format}`

export function DigiLockerPage() {
  const { me } = useSession()
  const [q, setQ] = useState('')
  const [open, setOpen] = useState<DigiLockerDocument | null>(null)
  const [syncFailed, setSyncFailed] = useState(false)
  const queryClient = useQueryClient()
  const list = useQuery({ queryKey: ['digilocker-documents'], queryFn: api.digilockerDocuments, enabled: !!me })

  // DigiLocker serves content only while the sign-in session is valid, so copy it into SevaOne's storage whenever the page opens.
  useEffect(() => {
    if (!me) return
    api
      .digilockerSync()
      .then((docs) => queryClient.setQueryData(['digilocker-documents'], docs))
      .catch(() => setSyncFailed(true))
  }, [me, queryClient])

  // A document with no PDF (the Aadhaar) is shown from its signed XML instead.
  const xmlView = useQuery({
    queryKey: ['digilocker-xml', open?.id],
    enabled: !!open && !open.storedFormats.includes('pdf') && open.storedFormats.includes('xml'),
    queryFn: async () => {
      const r = await fetch(fileUrl(open!.id, 'xml'), { credentials: 'include' })
      return parseAadhaar(await r.text())
    },
  })

  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(null)
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [open])

  const docs = useMemo(
    () =>
      (list.data ?? []).filter((d) => `${d.name} ${d.issuer}`.toLowerCase().includes(q.trim().toLowerCase())),
    [q, list.data],
  )

  const details: [string, string][] = [
    ['Name', me?.name ?? '—'],
    ['Mobile', me?.mobile ?? '—'],
    ['Aadhaar', me?.aadhaarMasked ?? 'XXXX XXXX 4821'],
    ['Date of birth', me?.dateOfBirth ?? '—'],
    ['Gender', me?.gender ?? '—'],
    ['Email', me?.email ?? '—'],
  ]

  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-8">
      <Breadcrumbs />
      <div className="mt-4 flex flex-wrap items-center gap-4">
        <img src={digilockerSymbol} alt="" width={56} height={56} className="h-14 w-14 rounded-2xl object-cover shadow-sm" />
        <div className="min-w-0 flex-1">
          <h1 className="t-section-title">DigiLocker</h1>
          <p className="t-body">Your verified documents, in one place.</p>
        </div>
      </div>

      {!me ? (
        <div className="mt-8 rounded-3xl border border-border bg-card p-8 text-center">
          <ShieldCheck className="mx-auto h-10 w-10 text-[color:var(--gov-blue)]" />
          <h2 className="mt-3 t-card-title">Sign in to see your DigiLocker</h2>
          <p className="mt-1 t-body">Log in to SevaOne to view your account and issued documents.</p>
          <a
            href={loginPath('/digilocker')}
            className="mt-5 inline-flex rounded-full bg-[color:var(--gov-blue)] px-6 py-2.5 text-sm font-semibold text-white"
          >
            Log in
          </a>
        </div>
      ) : (
        <div className="mt-8 grid gap-6 lg:grid-cols-[320px_1fr]">
          <aside className="h-fit rounded-3xl border border-border bg-card p-5">
            <div className="flex items-center gap-2 text-sm font-semibold text-emerald-700">
              <Link2 className="h-4 w-4" /> Account linked
            </div>
            <dl className="mt-4 space-y-3">
              {details.map(([k, v]) => (
                <div key={k}>
                  <dt className="t-caption">{k}</dt>
                  <dd className="break-words text-sm font-medium">{v}</dd>
                </div>
              ))}
            </dl>
            <div className="mt-5 flex items-center gap-2 rounded-xl bg-secondary px-3 py-2 text-xs text-muted-foreground">
              <BadgeCheck className="h-4 w-4 shrink-0 text-[color:var(--gov-blue)]" />
              Verified through Aadhaar e-KYC
            </div>
          </aside>

          <section>
            <div className="flex flex-wrap items-center gap-3">
              <div className="relative min-w-[200px] flex-1">
                <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <input
                  value={q}
                  onChange={(e) => setQ(e.target.value)}
                  placeholder="Search your documents"
                  className="w-full rounded-full border border-border bg-card py-2.5 pl-9 pr-4 text-sm outline-none focus:border-[color:var(--gov-blue)]"
                />
              </div>
            </div>

            <div className="mt-4 grid gap-3 sm:grid-cols-2">
              {docs.map((d) => (
                <button
                  key={d.id}
                  type="button"
                  onClick={() => setOpen(d)}
                  className="flex items-start gap-3 rounded-2xl border border-border bg-card p-4 text-left transition hover:-translate-y-0.5 hover:border-[color:var(--gov-blue)]/40 hover:shadow-[var(--shadow-soft)]"
                >
                  <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600 text-white shadow-sm">
                    <FileText className="h-6 w-6" />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block t-card-title">{d.name}</span>
                    <span className="mt-0.5 block truncate t-caption">{d.issuer}</span>
                    <span className="mt-2 flex items-center gap-2 text-xs text-muted-foreground">
                      <span className="inline-flex items-center gap-1 font-medium text-emerald-700">
                        <BadgeCheck className="h-3.5 w-3.5" /> Issued
                      </span>
                      {d.uploadDate && <>· {d.uploadDate}</>}
                    </span>
                  </span>
                </button>
              ))}
              {list.isLoading && <p className="t-body sm:col-span-2">Loading your documents…</p>}
              {!list.isLoading && docs.length === 0 && (
                <p className="t-body sm:col-span-2">{q ? 'No documents match.' : 'No documents saved yet.'}</p>
              )}
              {syncFailed && (
                <p className="t-caption sm:col-span-2">
                  DigiLocker could not be reached, so saved copies are shown. Sign in with DigiLocker again to refresh.
                </p>
              )}
            </div>
          </section>
        </div>
      )}

      {open && (
        <div
          role="dialog"
          aria-modal="true"
          aria-label={open.name}
          className="fixed inset-0 z-[100] flex items-center justify-center bg-[color:var(--gov-navy)]/55 p-4 backdrop-blur-sm"
          onMouseDown={(e) => e.target === e.currentTarget && setOpen(null)}
        >
          <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl">
            <div className="flex items-start justify-between gap-4">
              <h2 className="t-section-title">{open.name}</h2>
              <button type="button" onClick={() => setOpen(null)} aria-label="Close" className="rounded-full p-2 text-slate-400 hover:bg-slate-100">
                <X className="h-5 w-5" />
              </button>
            </div>
            <dl className="mt-4 space-y-2 text-sm">
              <div className="flex justify-between gap-4"><dt className="text-muted-foreground">Issuer</dt><dd className="text-right font-medium">{open.issuer || '—'}</dd></div>
              {open.uploadDate && (
                <div className="flex justify-between gap-4"><dt className="text-muted-foreground">Issued on</dt><dd className="font-medium">{open.uploadDate}</dd></div>
              )}
            </dl>
            {open.storedFormats.includes('pdf') ? (
              <a
                href={fileUrl(open.id, 'pdf')}
                target="_blank"
                rel="noreferrer"
                className="mt-5 inline-flex rounded-full bg-[color:var(--gov-blue)] px-5 py-2 text-sm font-semibold text-white"
              >
                Download PDF
              </a>
            ) : xmlView.data ? (
              <div className="mt-5">
                <AadhaarCards data={xmlView.data} />
              </div>
            ) : (
              <p className="mt-5 t-caption">
                {xmlView.isLoading ? 'Loading…' : 'This document could not be saved from DigiLocker. Sign in with DigiLocker again.'}
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
