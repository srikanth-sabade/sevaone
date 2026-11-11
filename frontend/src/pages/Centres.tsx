import { useMutation, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Building2, LoaderCircle, LocateFixed, Navigation } from 'lucide-react'
import { api } from '../api'
import { Breadcrumbs } from '../components/Breadcrumbs'

const getPosition = () =>
  new Promise<GeolocationPosition>((resolve, reject) => {
    if (!navigator.geolocation) reject(new Error('This browser cannot share your location.'))
    else navigator.geolocation.getCurrentPosition(resolve, reject, { enableHighAccuracy: false, timeout: 15000 })
  })

// The source data is mostly upper case.
const title = (v: string) => v.toLowerCase().replace(/(^|[\s(-])([a-z])/g, (_, a: string, b: string) => a + b.toUpperCase())

function errorText(e: unknown): string {
  if (e instanceof GeolocationPositionError) {
    return e.code === e.PERMISSION_DENIED
      ? 'Location access was blocked. Allow it for this site in your browser and try again.'
      : 'Your location could not be found. Try again.'
  }
  return e instanceof Error ? e.message : 'Something went wrong. Try again.'
}

export function CentresPage() {
  const find = useMutation({
    mutationFn: async () => {
      const { coords } = await getPosition()
      return api.nearestGramaOne(coords.latitude, coords.longitude, 6)
    },
  })

  const [district, setDistrict] = useState('')
  const [place, setPlace] = useState('')
  const districts = useQuery({ queryKey: ['gramaone-districts'], queryFn: api.gramaOneDistricts, staleTime: Infinity })
  const search = useMutation({ mutationFn: () => api.searchGramaOne(district, place.trim()) })
  const result = search.data ?? find.data

  return (
    <div className="mx-auto w-full max-w-4xl px-4 py-8">
      <Breadcrumbs />
      <div className="mt-4 flex items-center gap-4">
        <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-gradient-to-br from-orange-400 to-amber-500 text-white shadow-sm">
          <Building2 className="h-7 w-7" />
        </span>
        <div>
          <h1 className="t-section-title">Find a service centre</h1>
          <p className="t-body">The Grama One centres closest to you.</p>
        </div>
      </div>

      <button
        type="button"
        onClick={() => {
          search.reset()
          find.mutate()
        }}
        disabled={find.isPending}
        className="mt-8 inline-flex items-center gap-2 rounded-full bg-[color:var(--gov-blue)] px-6 py-3 text-sm font-semibold text-white disabled:opacity-60"
      >
        {find.isPending ? <LoaderCircle className="h-4 w-4 animate-spin" /> : <LocateFixed className="h-4 w-4" />}
        Use my location
      </button>
      <p className="mt-2 t-caption">Your location is used only to look up centres and is not stored.</p>

      <div className="mt-6 rounded-2xl border border-border bg-card p-4">
        <div className="text-sm font-semibold">Or search by district</div>
        <form
          className="mt-3 flex flex-wrap gap-2"
          onSubmit={(e) => {
            e.preventDefault()
            if (!district) return
            find.reset()
            search.mutate()
          }}
        >
          <select
            value={district}
            onChange={(e) => setDistrict(e.target.value)}
            aria-label="District"
            className="min-w-[200px] rounded-full border border-border bg-background px-4 py-2 text-sm"
          >
            <option value="">Choose a district</option>
            {(districts.data ?? []).map((d) => (
              <option key={d} value={d}>{d}</option>
            ))}
          </select>
          <input
            value={place}
            onChange={(e) => setPlace(e.target.value)}
            placeholder="Village, gram panchayat or taluk (optional)"
            className="min-w-[220px] flex-1 rounded-full border border-border bg-background px-4 py-2 text-sm"
          />
          <button type="submit" disabled={!district || search.isPending} className="rounded-full border border-border px-5 py-2 text-sm font-semibold hover:bg-secondary disabled:opacity-50">
            Search
          </button>
        </form>
      </div>

      {find.isError && <p className="mt-6 rounded-2xl border border-border bg-card p-4 text-sm">{errorText(find.error)}</p>}

      {search.data?.length === 0 && <p className="mt-6 text-sm">No centres match.</p>}
      {result && (
        <ol className="mt-6 grid gap-3 sm:grid-cols-2">
          {result.map((c, i) => (
            <li key={c.id} className="rounded-2xl border border-border bg-card p-4">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <div className="t-caption">{c.distanceKm == null ? ' ' : i === 0 ? 'Nearest' : `#${i + 1}`}</div>
                  <div className="t-card-title">{title(c.village || c.gramPanchayat || c.taluk || c.district)}</div>
                  <div className="mt-0.5 t-caption">
                    {[c.gramPanchayat !== c.village ? c.gramPanchayat : '', c.taluk, c.district].filter(Boolean).map(title).join(' · ')}
                  </div>
                </div>
                {c.distanceKm != null && <div className="shrink-0 rounded-full bg-secondary px-3 py-1 text-sm font-semibold">{c.distanceKm} km</div>}
              </div>
              <a
                href={`https://www.google.com/maps/dir/?api=1&destination=${c.latitude},${c.longitude}`}
                target="_blank"
                rel="noreferrer"
                className="mt-3 inline-flex items-center gap-1.5 text-sm font-semibold text-[color:var(--gov-blue)] hover:underline"
              >
                <Navigation className="h-4 w-4" /> Directions
              </a>
            </li>
          ))}
        </ol>
      )}
      <p className="mt-8 t-caption">
        Centre list from the Open City dataset (2023). Positions are as recorded there; a centre may have moved or closed.
      </p>
    </div>
  )
}
