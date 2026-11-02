import { create } from 'zustand'
import { api } from './api'
import { flow } from './flowLog'
import type { Me } from './types'
import type { Lang } from './i18n'

export type AuthBusy = null | 'signing-in' | 'signing-out'

const LANG_KEY = 'seva.lang'

function readStoredLang(): Lang {
  try {
    const v = localStorage.getItem(LANG_KEY)
    if (v === 'kn' || v === 'en') return v
  } catch {
    /* ignore */
  }
  return 'en'
}

type SessionState = {
  me: Me | null
  loaded: boolean
  authBusy: AuthBusy
  lang: Lang
  fontScale: number
  refresh: () => Promise<void>
  logout: () => Promise<void>
  setAuthBusy: (busy: AuthBusy) => void
  setLang: (lang: Lang) => void
  bumpFont: (delta: number) => void
}

export const useSession = create<SessionState>((set, get) => ({
  me: null,
  loaded: false,
  authBusy: null,
  lang: readStoredLang(),
  fontScale: 1,
  refresh: async () => {
    try {
      const me = await api.me()
      flow('session cookie valid — citizen is signed in on SEVA', {
        name: me.name,
        sevaId: me.sevaId,
      })
      set({ me, loaded: true })
    } catch {
      flow('no SEVA session (GET /me 401) — Apply Now will send you to Seva login')
      set({ me: null, loaded: true })
    }
  },
  logout: async () => {
    flow('logout clicked')
    await api.logout()
    set({ me: null })
  },
  setAuthBusy: (authBusy) => set({ authBusy }),
  setLang: (lang) => {
    try {
      localStorage.setItem(LANG_KEY, lang)
    } catch {
      /* ignore */
    }
    document.documentElement.lang = lang === 'kn' ? 'kn' : 'en'
    set({ lang })
  },
  bumpFont: (delta) => {
    const next = Math.min(1.3, Math.max(0.85, get().fontScale + delta))
    document.documentElement.style.setProperty('--seva-font-size', `${16 * next}px`)
    set({ fontScale: next })
  },
}))
