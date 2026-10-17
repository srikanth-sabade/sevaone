import { Link } from '@tanstack/react-router'
import { useT } from '../hooks/useT'

/**
 * Branded catch-all for unknown routes. Wired as the router's `defaultNotFoundComponent`
 * (see router.tsx) so any unmatched path -- including multi-segment ones the `/$serviceCode`
 * route can't reach -- lands here instead of TanStack Router's bare built-in fallback.
 */
export function NotFoundPage() {
  const { t } = useT()
  return (
    <div className="mx-auto max-w-lg px-4 py-24 text-center">
      <p className="text-5xl font-bold tracking-tight text-[color:var(--gov-blue)]">404</p>
      <h1 className="mt-4 text-xl font-bold text-[color:var(--gov-navy)]">{t('notFoundTitle')}</h1>
      <p className="mt-2 text-sm text-muted-foreground">{t('notFoundBody')}</p>
      <div className="mt-8 flex flex-wrap justify-center gap-3">
        <Link
          to="/"
          search={{ loginError: undefined }}
          className="rounded-lg bg-[color:var(--gov-blue)] px-4 py-2 text-sm font-medium text-white hover:bg-[color:var(--gov-blue-dark)]"
        >
          {t('notFoundHome')}
        </Link>
        <Link
          to="/catalog"
          search={{ dept: undefined, sub: undefined, service: undefined }}
          className="rounded-lg border border-border px-4 py-2 text-sm font-medium text-[color:var(--gov-navy)] hover:border-[color:var(--gov-blue)]"
        >
          {t('notFoundSearch')}
        </Link>
      </div>
    </div>
  )
}
