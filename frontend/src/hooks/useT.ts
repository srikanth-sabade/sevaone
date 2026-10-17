import { t, type MessageKey } from '../i18n'
import { useSession } from '../store'

/** Current language dictionary accessor — survives refresh via localStorage. */
export function useT() {
  const lang = useSession((s) => s.lang)
  return {
    lang,
    t: (key: MessageKey, vars?: Record<string, string | number>) => t(lang, key, vars),
  }
}
