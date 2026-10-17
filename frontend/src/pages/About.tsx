import { Breadcrumbs } from '../components/Breadcrumbs'
import { useT } from '../hooks/useT'

export function AboutPage() {
  const { t } = useT()
  const layers = [
    ['User Experience', 'React + Vite citizen portal'],
    ['SSO', 'WSO2 Identity Server · OAuth2 + PKCE'],
    ['BFF', '.NET Clean Architecture + CQRS'],
    ['Connectors', 'IDepartmentConnector per department'],
    ['Gateway', 'YARP stand-in for WSO2 APIM / EI'],
    ['Data', 'PostgreSQL + Redis sessions + Kafka outbox'],
  ]
  return (
    <div className="mx-auto max-w-4xl px-4 py-10">
      <h1 className="text-3xl font-bold text-[color:var(--gov-navy)]">{t('aboutTitle')}</h1>
      <Breadcrumbs />
      <p className="mt-3 text-muted-foreground">
        Citizens interact with one portal. Departments keep their own backends. The browser never holds WSO2 tokens and never
        calls department APIs.
      </p>
      <div className="mt-6 grid gap-3 md:grid-cols-2">
        {layers.map(([t, d]) => (
          <div key={t} className="rounded-2xl border border-border bg-card p-4 shadow-sm">
            <div className="font-semibold text-[color:var(--gov-navy)]">{t}</div>
            <p className="mt-1 text-sm text-muted-foreground">{d}</p>
          </div>
        ))}
      </div>
    </div>
  )
}
