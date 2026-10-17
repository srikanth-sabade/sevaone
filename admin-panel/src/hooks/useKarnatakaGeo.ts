import { useEffect, useState } from 'react'

export interface KarnatakaDistrictShape {
  name: string
  code: string
  path: string
  cx: number
  cy: number
}

export interface KarnatakaGeo {
  viewBox: string
  districts: KarnatakaDistrictShape[]
}

// Real district boundary traces, extracted once (offline, not at runtime) from a Karnataka
// district map SVG the user supplied -- see admin-panel/public/karnataka-districts.json. Fetched
// as a static asset (146KB) rather than bundled into the JS chunk, and cached at module scope so
// remounting the Districts page doesn't refetch it every time.
let cached: KarnatakaGeo | null = null
let inflight: Promise<KarnatakaGeo> | null = null

export function useKarnatakaGeo() {
  const [geo, setGeo] = useState<KarnatakaGeo | null>(cached)
  const [error, setError] = useState(false)

  useEffect(() => {
    if (cached) return
    if (!inflight) {
      inflight = fetch('/karnataka-districts.json').then((r) => {
        if (!r.ok) throw new Error('Failed to load Karnataka district geometry')
        return r.json() as Promise<KarnatakaGeo>
      })
    }
    let cancelled = false
    inflight
      .then((data) => {
        cached = data
        if (!cancelled) setGeo(data)
      })
      .catch(() => {
        if (!cancelled) setError(true)
      })
    return () => {
      cancelled = true
    }
  }, [])

  return { geo, isLoading: !geo && !error, isError: error }
}
