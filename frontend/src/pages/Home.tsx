import { Link } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import {
  ArrowRight,
  Briefcase,
  CheckCircle2,
  FileText,
  GraduationCap,
  HeartHandshake,
  IdCard,
  ScrollText,
  Stethoscope,
  Zap,
} from 'lucide-react'
import { api } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { Badge } from '../components/ui/badge'
import dks from '../assets/dks.webp'
import gokEmblem from '../assets/gok-emblem.webp'
import parameshwara from '../assets/parameshwara.webp'
import { useT } from '../hooks/useT'
import { serviceName, departmentName } from '../lib/catalogKn'
import { formatFeeInr } from '../lib/fee'
import type { MessageKey } from '../i18n'

const STATS: { labelKey: MessageKey; value: string }[] = [
  { labelKey: 'statTotalServices', value: '1,150+' },
  { labelKey: 'statDeptsOnboarded', value: '112' },
  { labelKey: 'statAppsProcessed', value: '18.4 Cr' },
  { labelKey: 'statRegisteredCitizens', value: '3.9 Cr' },
]

const CATEGORIES: { nameKey: MessageKey; icon: typeof FileText; q: string }[] = [
  { nameKey: 'catCertificates', icon: FileText, q: 'certificate' },
  { nameKey: 'catLicenses', icon: IdCard, q: 'licence' },
  { nameKey: 'catPensions', icon: HeartHandshake, q: 'pension' },
  { nameKey: 'catUtility', icon: Zap, q: 'ration' },
  { nameKey: 'catBusiness', icon: Briefcase, q: 'trade' },
  { nameKey: 'catEducation', icon: GraduationCap, q: 'scholarship' },
  { nameKey: 'catHealth', icon: Stethoscope, q: 'birth' },
  { nameKey: 'catEmployment', icon: ScrollText, q: 'employment' },
]

const LINK_CARDS: { to: '/track' | '/departments' | '/help' | '/grievance'; titleKey: MessageKey; bodyKey: MessageKey }[] = [
  { to: '/track', titleKey: 'trackApp', bodyKey: 'linkTrackBody' },
  { to: '/departments', titleKey: 'departments', bodyKey: 'linkDeptsBody' },
  { to: '/help', titleKey: 'helpCentre', bodyKey: 'linkHelpBody' },
  { to: '/grievance', titleKey: 'grievanceIpgrs', bodyKey: 'linkGrievanceBody' },
]

const ANNOUNCEMENTS: { titleKey: MessageKey; date: string; tagKey: MessageKey }[] = [
  { titleKey: 'annPhase1', date: '24 Jul 2026', tagKey: 'tagNew' },
  { titleKey: 'annGruha', date: '18 Jul 2026', tagKey: 'tagUpdate' },
  { titleKey: 'annSakala', date: '11 Jul 2026', tagKey: 'tagNotice' },
  { titleKey: 'annDigilocker', date: '02 Jul 2026', tagKey: 'tagNew' },
]

export function HomePage() {
  const { lang, t } = useT()
  const popularQ = useQuery({ queryKey: ['schemes', 'popular'], queryFn: () => api.schemesPopular(8) })
  const popular = popularQ.data ?? []

  return (
    <div>
      <section className="relative overflow-hidden border-b border-border bg-card">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-50"
          style={{
            backgroundImage:
              'linear-gradient(to right, color-mix(in oklab, var(--gov-navy) 7%, transparent) 1px, transparent 1px), linear-gradient(to bottom, color-mix(in oklab, var(--gov-navy) 7%, transparent) 1px, transparent 1px)',
            backgroundSize: '40px 40px',
            maskImage: 'radial-gradient(ellipse at 70% 40%, black 30%, transparent 75%)',
          }}
        />
        <div className="relative mx-auto max-w-6xl px-4 py-10">
          <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-[color:var(--gov-navy)]/15 bg-card px-2.5 py-1">
            <span className="h-1.5 w-1.5 rounded-full bg-[color:var(--gov-navy)]" />
            <span className="text-[10px] font-semibold uppercase tracking-wider text-[color:var(--gov-navy)]">
              {t('homeBadge')}
            </span>
          </div>
          <h1 className="text-3xl font-bold leading-[1.05] text-[color:var(--gov-navy)] md:text-5xl">
            {t('homeHeadline1')}
            <br />
            {t('homeHeadline2')}
          </h1>
          <Breadcrumbs />
          <p className="mt-3 max-w-xl text-sm leading-relaxed text-muted-foreground md:text-base">
            {t('homeLead')}
          </p>
          <div className="mt-5 flex flex-wrap gap-2">
            <Link
              to="/services"
              search={{ q: undefined, department: undefined, category: undefined }}
              className="inline-flex items-center gap-1.5 rounded-lg bg-[color:var(--gov-blue)] px-4 py-2 text-sm font-semibold text-white transition hover:opacity-90"
            >
              {t('homeBrowseServices')} <ArrowRight className="h-3.5 w-3.5" />
            </Link>
            <Link
              to="/login"
              search={{ returnTo: undefined, loginError: undefined, mode: undefined }}
              className="inline-flex items-center gap-1.5 rounded-lg border border-[color:var(--gov-blue)]/30 px-4 py-2 text-sm font-semibold text-[color:var(--gov-navy)] transition hover:bg-[color:var(--gov-blue)]/5"
            >
              {t('loginSignup')}
            </Link>
            <Link
              to="/dashboard"
              search={{ ref: undefined }}
              className="inline-flex items-center gap-1.5 rounded-lg border border-border px-4 py-2 text-sm font-medium text-muted-foreground transition hover:bg-secondary"
            >
              {t('dashboard')}
            </Link>
          </div>
          <div className="mt-4 flex flex-wrap items-center gap-x-4 gap-y-1.5 text-[11px] text-muted-foreground">
            {(['badgeSakalaGuaranteed', 'badgeDigilockerReady', 'badgeAadhaarEkyc', 'badgeSso'] as const).map((key) => (
              <span key={key} className="inline-flex items-center gap-1">
                <CheckCircle2 className="h-3 w-3 text-[color:var(--gov-blue)]" /> {t(key)}
              </span>
            ))}
          </div>
          <div className="mt-6 grid grid-cols-2 gap-2 sm:grid-cols-4">
            {STATS.map((s) => (
              <div key={s.labelKey} className="rounded-2xl border border-border bg-card p-3 shadow-sm">
                <div className="text-lg font-bold leading-none text-[color:var(--gov-navy)] md:text-xl">{s.value}</div>
                <div className="mt-1 text-[10px] leading-tight text-muted-foreground">{t(s.labelKey)}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="border-b border-border bg-card">
        <div className="mx-auto max-w-6xl px-4 py-8">
          <div className="mb-5 flex items-center gap-3">
            <img src={gokEmblem} alt={t('emblemAlt')} width={56} height={56} className="h-14 w-14 object-contain" />
            <div className="leading-tight">
              <div className="text-sm font-semibold text-[color:var(--gov-navy)]">{t('govKarnataka')}</div>
              <div className="text-xs text-muted-foreground">ಕರ್ನಾಟಕ ಸರ್ಕಾರ</div>
            </div>
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            {[
              {
                img: dks,
                name: t('cmName'),
                role: t('homeCmRole'),
                quote: t('homeCmQuote'),
              },
              {
                img: parameshwara,
                name: t('ministerName'),
                role: t('homeMinisterRole'),
                quote: t('homeMinisterQuote'),
              },
            ].map((p) => (
              <div key={p.name} className="flex items-center gap-4 rounded-2xl border border-border bg-card p-4 shadow-sm">
                <img
                  src={p.img}
                  alt={p.name}
                  width={80}
                  height={80}
                  className="h-20 w-20 rounded-full bg-white object-cover ring-2 ring-[color:var(--gov-gold)]"
                />
                <div>
                  <div className="font-semibold text-[color:var(--gov-navy)]">{p.name}</div>
                  <div className="text-xs text-muted-foreground">{p.role}</div>
                  <p className="mt-1 text-sm italic text-foreground/80">&ldquo;{p.quote}&rdquo;</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-4 py-10">
        <div className="mb-6 flex items-end justify-between">
          <div>
            <h2 className="text-2xl font-bold text-[color:var(--gov-navy)] md:text-3xl">{t('exploreByCategory')}</h2>
            <p className="mt-1 text-sm text-muted-foreground">{t('exploreByCategoryHint')}</p>
          </div>
          <Link
            to="/services"
            search={{ q: undefined, department: undefined, category: undefined }}
            className="hidden items-center gap-1 text-sm font-medium text-[color:var(--gov-blue)] hover:underline md:inline-flex"
          >
            {t('viewAll')} <ArrowRight className="h-4 w-4" />
          </Link>
        </div>
        <div className="grid grid-cols-2 gap-3 md:grid-cols-4 md:gap-4">
          {CATEGORIES.map((c) => (
            <Link
              key={c.nameKey}
              to="/services"
              search={{ q: c.q, department: undefined, category: undefined }}
              className="group surface-card rounded-2xl p-5 transition hover:border-[color:var(--gov-blue)]"
            >
              <div className="flex h-11 w-11 items-center justify-center rounded-lg bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)] transition group-hover:bg-[color:var(--gov-blue)] group-hover:text-white">
                <c.icon className="h-5 w-5" />
              </div>
              <div className="mt-3 text-sm font-semibold text-[color:var(--gov-navy)]">{t(c.nameKey)}</div>
            </Link>
          ))}
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-4 pb-10 grid gap-8 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <h2 className="mb-6 text-2xl font-bold text-[color:var(--gov-navy)] md:text-3xl">{t('popularServices')}</h2>
          <div className="grid gap-3 sm:grid-cols-2">
            {popular.map((s) => (
              <Link
                key={s.serviceCode}
                to="/services/$code"
                params={{ code: s.serviceCode }}
                className="rounded-2xl border border-border bg-card p-4 shadow-sm transition hover:border-[color:var(--gov-blue)]"
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="text-sm font-semibold text-[color:var(--gov-navy)]">{serviceName(lang, s.serviceCode, s.name)}</div>
                  <Badge variant="outline" className="shrink-0 text-[10px]">
                    {t('sakala')} {s.sakalaDays}d
                  </Badge>
                </div>
                <div className="mt-1 text-xs text-muted-foreground">{departmentName(lang, s.departmentCode, s.departmentName)}</div>
                <div className="mt-3 flex items-center justify-between text-xs">
                  <span className="text-foreground/70">{t('fee')}: {formatFeeInr(s)}</span>
                  <span className="inline-flex items-center gap-1 font-medium text-[color:var(--gov-blue)]">
                    {t('applyCta')} <ArrowRight className="h-3 w-3" />
                  </span>
                </div>
              </Link>
            ))}
            {!popularQ.data && (
              <p className="col-span-full text-sm text-muted-foreground">{t('loadingPopularServices')}</p>
            )}
          </div>
        </div>
        <aside className="surface-card rounded-2xl p-5">
          <h3 className="font-semibold text-[color:var(--gov-navy)]">{t('announcementsTitle')}</h3>
          <ul className="mt-4 space-y-4">
            {ANNOUNCEMENTS.map((a) => (
              <li key={a.titleKey} className="border-l-2 border-[color:var(--saffron)] pl-3">
                <div className="mb-1 flex items-center gap-2">
                  <Badge variant={a.tagKey === 'tagNew' ? 'default' : 'secondary'} className="text-[10px]">
                    {t(a.tagKey)}
                  </Badge>
                  <span className="text-xs text-muted-foreground">{a.date}</span>
                </div>
                <div className="text-sm leading-snug">{t(a.titleKey)}</div>
              </li>
            ))}
          </ul>
        </aside>
      </section>

      <section className="mx-auto max-w-6xl px-4 pb-10">
        <h2 className="mb-4 text-2xl font-bold text-[color:var(--gov-navy)]">{t('quickLinksTitle')}</h2>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {LINK_CARDS.map((c) => (
            <Link
              key={c.to}
              to={c.to}
              className="rounded-2xl border border-border bg-card p-4 shadow-sm transition hover:border-[color:var(--gov-blue)]"
            >
              <div className="font-semibold text-[color:var(--gov-navy)]">{t(c.titleKey)}</div>
              <p className="mt-1 text-sm text-muted-foreground">{t(c.bodyKey)}</p>
            </Link>
          ))}
        </div>
      </section>

      <section className="bg-[color:var(--gov-navy)] text-white">
        <div className="mx-auto max-w-6xl px-4 py-12">
          <h2 className="text-center text-2xl font-bold md:text-3xl">{t('whySevaOne')}</h2>
          <p className="mx-auto mt-2 max-w-2xl text-center text-sm text-white/70">
            {t('whySevaOneBody')}
          </p>
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
                  <CheckCircle2 className="h-5 w-5 text-[color:var(--gov-gold)]" />
                  <div className="font-semibold">{t(titleKey)}</div>
                </div>
                <p className="mt-2 text-sm text-white/70">{t(bodyKey)}</p>
              </div>
            ))}
          </div>
        </div>
      </section>
    </div>
  )
}
