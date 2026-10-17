import { CheckCircle2, Clock3, ExternalLink, FileText, Globe, Info, ListChecks, MapPin, Tag } from 'lucide-react'
import { useT } from '../hooks/useT'
import { categoryLabel } from '../lib/catalogKn'
import { localizeJurisdiction, localizeMode, type MockService } from '../lib/catalogMock'
import { Button } from './ui/button'

/** Renders inline inside the /catalog tree layout (see pages/Catalog.tsx) — no page
 *  chrome of its own, so it sits in the right-hand panel next to the sidebar. The
 *  department/sub-department breadcrumb is rendered by the caller, not here. */
export function CatalogServiceDetailPanel({ service }: { readonly service: MockService }) {
  const { lang, t } = useT()
  const name = lang === 'kn' ? service.nameKannada : service.name
  const shortDesc = lang === 'kn' ? service.shortDescriptionKannada : service.shortDescription
  const desc = lang === 'kn' ? service.descriptionKannada : service.description
  const totalFee = service.departmentFee + service.serviceCharge
  const documents = lang === 'kn' ? service.documentsKannada : service.documents
  const eligibility = lang === 'kn' ? service.eligibilityKannada : service.eligibility
  const steps = lang === 'kn' ? service.stepsKannada : service.steps
  const remarks = lang === 'kn' ? service.remarksKannada : service.remarks
  const applyUrl = service.applyUrl?.trim()

  return (
    <div className="rounded-2xl border border-border bg-card p-6 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <h1 className="text-2xl font-bold text-[color:var(--gov-navy)]">{name}</h1>
          <p className="mt-1 text-xs text-muted-foreground">
            {t('serviceCode')}: <span className="font-medium text-foreground">{service.code}</span>
          </p>
        </div>
        {applyUrl ? (
          <Button asChild className="shrink-0 bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90">
            <a href={applyUrl} target="_blank" rel="noopener noreferrer">
              {t('apply')}
              <ExternalLink className="ml-1.5 h-4 w-4" />
            </a>
          </Button>
        ) : null}
      </div>

      <p className="mt-4 text-sm leading-relaxed text-foreground/80">{shortDesc}</p>

      <div className="mt-4 flex flex-wrap gap-2 text-xs">
        <span className="inline-flex items-center gap-1 rounded-md bg-secondary px-2 py-1 font-medium text-foreground/80">
          <Clock3 className="h-3.5 w-3.5" /> {t('sakala')}: {service.sakalaDays}d
        </span>
        <span className="inline-flex items-center gap-1 rounded-md bg-secondary px-2 py-1 font-medium text-foreground/80">
          <Tag className="h-3.5 w-3.5" /> {categoryLabel(lang, service.category)}
        </span>
        <span className="inline-flex items-center gap-1 rounded-md bg-secondary px-2 py-1 font-medium text-foreground/80">
          <MapPin className="h-3.5 w-3.5" /> {localizeJurisdiction(lang, service.jurisdiction)}
        </span>
        <span className="inline-flex items-center gap-1 rounded-md bg-secondary px-2 py-1 font-medium text-foreground/80">
          <Globe className="h-3.5 w-3.5" /> {localizeMode(lang, service.modeOfService)}
        </span>
        {service.isMandatory && (
          <span className="inline-flex items-center gap-1 rounded-md border border-amber-200 bg-amber-50 px-2 py-1 font-medium text-amber-900">
            {t('catalogMandatory')}
          </span>
        )}
      </div>

      <div className="mt-6 grid gap-6 sm:grid-cols-2">
        <section>
          <h2 className="flex items-center gap-1.5 text-sm font-semibold text-[color:var(--gov-navy)]">
            <Info className="h-4 w-4" /> {t('catalogAbout')}
          </h2>
          <p className="mt-2 text-sm leading-relaxed text-foreground/80">{desc}</p>
          {remarks && (
            <p className="mt-3 rounded-lg border border-border bg-secondary/40 p-3 text-xs text-muted-foreground">
              {remarks}
            </p>
          )}
        </section>

        <section>
          <h2 className="flex items-center gap-1.5 text-sm font-semibold text-[color:var(--gov-navy)]">
            <Tag className="h-4 w-4" /> {t('catalogFees')}
          </h2>
          <dl className="mt-2 space-y-1.5 text-sm">
            <div className="flex justify-between">
              <dt className="text-muted-foreground">{t('catalogDeptFee')}</dt>
              <dd className="font-medium text-foreground">₹{service.departmentFee}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted-foreground">{t('catalogServiceCharge')}</dt>
              <dd className="font-medium text-foreground">₹{service.serviceCharge}</dd>
            </div>
            <div className="flex justify-between border-t border-border pt-1.5">
              <dt className="font-medium text-foreground">{t('catalogTotalFee')}</dt>
              <dd className="font-semibold text-[color:var(--gov-navy)]">₹{totalFee}</dd>
            </div>
          </dl>
          {service.portal && (
            <p className="mt-3 break-all text-xs text-muted-foreground">
              {t('catalogPortal')}:{' '}
              <span className="font-medium text-foreground">{service.portal}</span>
            </p>
          )}
        </section>

        <section>
          <h2 className="flex items-center gap-1.5 text-sm font-semibold text-[color:var(--gov-navy)]">
            <CheckCircle2 className="h-4 w-4" /> {t('eligibility')}
          </h2>
          <ul className="mt-2 space-y-1.5 text-sm text-foreground/80">
            {eligibility.map((item) => (
              <li key={item} className="flex items-start gap-2">
                <CheckCircle2 className="mt-0.5 h-3.5 w-3.5 shrink-0 text-emerald-600" />
                {item}
              </li>
            ))}
          </ul>
        </section>

        <section>
          <h2 className="flex items-center gap-1.5 text-sm font-semibold text-[color:var(--gov-navy)]">
            <FileText className="h-4 w-4" /> {t('documents')}
          </h2>
          <ul className="mt-2 space-y-1.5 text-sm text-foreground/80">
            {documents.map((item) => (
              <li key={item} className="flex items-start gap-2">
                <FileText className="mt-0.5 h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                {item}
              </li>
            ))}
          </ul>
        </section>

        <section className="sm:col-span-2">
          <h2 className="flex items-center gap-1.5 text-sm font-semibold text-[color:var(--gov-navy)]">
            <ListChecks className="h-4 w-4" /> {t('catalogHowToApply')}
          </h2>
          <ol className="mt-2 space-y-2 text-sm text-foreground/80">
            {steps.map((step, i) => (
              <li key={step} className="flex items-start gap-2.5">
                <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-[color:var(--gov-blue)]/10 text-[11px] font-semibold text-[color:var(--gov-blue)]">
                  {i + 1}
                </span>
                {step}
              </li>
            ))}
          </ol>
          {applyUrl ? (
            <div className="mt-4">
              <Button asChild className="bg-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/90">
                <a href={applyUrl} target="_blank" rel="noopener noreferrer">
                  {t('apply')}
                  <ExternalLink className="ml-1.5 h-4 w-4" />
                </a>
              </Button>
            </div>
          ) : null}
        </section>

        {service.aliases.length > 0 && (
          <section className="sm:col-span-2">
            <h2 className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
              {t('catalogAlsoKnownAs')}
            </h2>
            <div className="mt-2 flex flex-wrap gap-1.5">
              {service.aliases.map((alias) => (
                <span key={alias} className="rounded-full bg-secondary px-2.5 py-1 text-xs text-muted-foreground">
                  {alias}
                </span>
              ))}
            </div>
          </section>
        )}
      </div>
    </div>
  )
}
