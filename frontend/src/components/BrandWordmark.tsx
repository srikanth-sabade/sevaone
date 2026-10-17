/**
 * The "SevaOne" wordmark with its accented second half, language-aware.
 * en → Seva·One, kn → ಸೇವಾ·ಒನ್. Shared so the header and footer never drift.
 */
import { useT } from '../hooks/useT'

export function BrandWordmark({ className }: { readonly className?: string }) {
  const { lang } = useT()
  const [first, second] = lang === 'kn' ? ['ಸೇವಾ', 'ಒನ್'] : ['Seva', 'One']
  return (
    <span className={className}>
      {first}
      <span className="text-[color:var(--gov-blue)]">{second}</span>
    </span>
  )
}
