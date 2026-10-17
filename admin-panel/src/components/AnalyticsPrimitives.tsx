import type { ReactNode } from 'react'
import { AlertTriangle, LoaderCircle } from 'lucide-react'
import { Card } from './ui/card'

export function PageIntro({ eyebrow, title, description, action }: { readonly eyebrow: string; readonly title: string; readonly description: string; readonly action?: ReactNode }) {
  return <div className="mb-7 flex flex-wrap items-end justify-between gap-4"><div><p className="text-xs font-bold uppercase tracking-[0.18em] text-[color:var(--gov-blue)]">{eyebrow}</p><h1 className="mt-2 text-3xl text-[color:var(--gov-navy)] sm:text-4xl">{title}</h1><p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">{description}</p></div>{action}</div>
}

export function MetricCard({ label, value, detail, tone = 'blue' }: { readonly label: string; readonly value: string; readonly detail: string; readonly tone?: 'blue' | 'green' | 'saffron' | 'navy' }) {
  const tones = { blue: 'bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]', green: 'bg-[color:var(--gov-green)]/12 text-[color:var(--gov-green)]', saffron: 'bg-[color:var(--saffron)]/15 text-[color:var(--saffron)]', navy: 'bg-[color:var(--gov-navy)]/10 text-[color:var(--gov-navy)]' }
  return <Card className="relative overflow-hidden rounded-2xl p-5"><div className={`absolute right-4 top-4 h-2 w-2 rounded-full ${tones[tone].split(' ')[0]}`} /><p className="text-xs font-semibold text-muted-foreground">{label}</p><p className="mt-3 text-3xl font-semibold tracking-tight text-[color:var(--gov-navy)]">{value}</p><p className="mt-2 text-xs leading-5 text-muted-foreground">{detail}</p></Card>
}

export function ChartCard({ title, description, children, className = '' }: { readonly title: string; readonly description: string; readonly children: ReactNode; readonly className?: string }) {
  return <Card className={`rounded-2xl p-5 ${className}`}><div><h2 className="text-base font-semibold text-[color:var(--gov-navy)]">{title}</h2><p className="mt-1 text-xs text-muted-foreground">{description}</p></div><div className="mt-5 h-72">{children}</div></Card>
}

export function QueryState({ isLoading, isError, children }: { readonly isLoading: boolean; readonly isError: boolean; readonly children: ReactNode }) {
  if (isLoading) return <div className="flex min-h-72 items-center justify-center text-sm text-muted-foreground"><LoaderCircle className="mr-2 h-4 w-4 animate-spin text-[color:var(--gov-blue)]" />Loading live analytics...</div>
  if (isError) return <Card className="flex min-h-56 flex-col items-center justify-center rounded-2xl p-6 text-center"><AlertTriangle className="h-8 w-8 text-[color:var(--saffron)]" /><h2 className="mt-3 text-base font-semibold text-[color:var(--gov-navy)]">Analytics unavailable</h2><p className="mt-1 max-w-sm text-sm text-muted-foreground">The service did not return this report. Please try again or contact the platform administrator.</p></Card>
  return children
}

export function formatNumber(value: number) { return new Intl.NumberFormat('en-IN').format(value) }
