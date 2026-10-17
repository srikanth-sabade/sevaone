import { useEffect, useMemo, useRef, useState } from 'react'
import { MapPinned, Users, FileStack, Layers, LoaderCircle } from 'lucide-react'
import type { DistrictRow } from '../types'
import { useKarnatakaGeo } from '../hooks/useKarnatakaGeo'
import { normalizeDistrictName } from '../lib/districtNames'
import { Card } from './ui/card'
import { formatNumber } from './AnalyticsPrimitives'

type Metric = 'citizens' | 'applications' | 'status'

const METRICS: { id: Metric; label: string; icon: typeof Users }[] = [
  { id: 'citizens', label: 'Citizens', icon: Users },
  { id: 'applications', label: 'Applications', icon: FileStack },
  { id: 'status', label: 'Delivery status', icon: Layers },
]

// Sequential 5-step blue scale for the citizens/applications heat modes.
const HEAT_SCALE = ['#e3ecf7', '#b9cfec', '#88abdb', '#5480c2', '#1f4e8c']

function bucketColor(value: number, max: number): string {
  if (max <= 0 || value <= 0) return '#eef1f5'
  const ratio = value / max
  const index = Math.min(HEAT_SCALE.length - 1, Math.floor(ratio * HEAT_SCALE.length))
  return HEAT_SCALE[index]
}

function polarToCartesian(cx: number, cy: number, r: number, angle: number) {
  return { x: cx + r * Math.sin(angle), y: cy - r * Math.cos(angle) }
}

function pieSlicePath(cx: number, cy: number, r: number, startAngle: number, endAngle: number): string {
  if (endAngle - startAngle >= Math.PI * 2 - 1e-6) endAngle = startAngle + Math.PI * 2 - 1e-6
  const start = polarToCartesian(cx, cy, r, startAngle)
  const end = polarToCartesian(cx, cy, r, endAngle)
  const largeArc = endAngle - startAngle > Math.PI ? 1 : 0
  return `M ${cx} ${cy} L ${start.x} ${start.y} A ${r} ${r} 0 ${largeArc} 1 ${end.x} ${end.y} Z`
}

export function KarnatakaMap({ rows, focusDistrictName, focusTalukLabel }: { readonly rows: DistrictRow[]; readonly focusDistrictName?: string; readonly focusTalukLabel?: string | null }) {
  const { geo, isLoading, isError } = useKarnatakaGeo()
  const [metric, setMetric] = useState<Metric>('citizens')
  const [selected, setSelected] = useState<string | null>(focusDistrictName ?? null)
  const focusPathRef = useRef<SVGPathElement | null>(null)
  const [croppedViewBox, setCroppedViewBox] = useState<string | null>(null)

  const normalizedFocus = focusDistrictName ? normalizeDistrictName(focusDistrictName) : null

  useEffect(() => {
    if (!normalizedFocus || !focusPathRef.current) {
      setCroppedViewBox(null)
      return
    }
    const bbox = focusPathRef.current.getBBox()
    const pad = Math.max(bbox.width, bbox.height) * 0.15
    setCroppedViewBox(`${bbox.x - pad} ${bbox.y - pad} ${bbox.width + pad * 2} ${bbox.height + pad * 2}`)
  }, [normalizedFocus, geo])

  const byName = useMemo(() => {
    const map = new Map<string, DistrictRow>()
    for (const row of rows) map.set(normalizeDistrictName(row.district), row)
    return map
  }, [rows])

  const maxCitizens = useMemo(() => Math.max(1, ...rows.map((r) => r.citizenCount)), [rows])
  const maxApplications = useMemo(() => Math.max(1, ...rows.map((r) => r.applicationCount)), [rows])

  const selectedRow = selected ? byName.get(normalizeDistrictName(selected)) : undefined

  return (
    <Card className="overflow-hidden rounded-2xl p-0">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border p-5">
        <div className="flex items-center gap-2">
          <MapPinned className="h-4 w-4 text-[color:var(--gov-blue)]" />
          <div>
            <h2 className="text-base font-semibold text-[color:var(--gov-navy)]">Karnataka district map</h2>
            <p className="text-xs text-muted-foreground">Real district boundaries, colored by the selected metric.</p>
          </div>
        </div>
        <div className="flex flex-wrap gap-1.5 rounded-full bg-secondary/70 p-1">
          {METRICS.map(({ id, label, icon: Icon }) => (
            <button
              key={id}
              type="button"
              onClick={() => setMetric(id)}
              className={`flex items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-semibold transition ${
                metric === id ? 'bg-[color:var(--gov-blue)] text-white shadow-sm' : 'text-muted-foreground hover:text-[color:var(--gov-navy)]'
              }`}
            >
              <Icon className="h-3.5 w-3.5" />
              {label}
            </button>
          ))}
        </div>
      </div>

      <div className="grid gap-5 p-5 lg:grid-cols-[1.35fr_1fr]">
        <div>
          {isLoading && (
            <div className="flex min-h-72 items-center justify-center text-sm text-muted-foreground">
              <LoaderCircle className="mr-2 h-4 w-4 animate-spin text-[color:var(--gov-blue)]" />
              Loading map geometry...
            </div>
          )}
          {isError && (
            <div className="flex min-h-72 items-center justify-center text-sm text-muted-foreground">Map geometry unavailable.</div>
          )}
          {normalizedFocus && (
            <div className="mb-3 rounded-xl bg-[color:var(--gov-blue)]/10 px-4 py-2 text-xs font-semibold text-[color:var(--gov-navy)]">
              Showing: {focusDistrictName} district{focusTalukLabel ? ` — focus taluk: ${focusTalukLabel}` : ''}
            </div>
          )}
          {geo && (
            <svg viewBox={croppedViewBox ?? geo.viewBox} className="h-auto w-full" role="img" aria-label="Karnataka districts">
              {geo.districts.map((district) => {
                const row = byName.get(normalizeDistrictName(district.name))
                const isSelected = selected === district.name
                const isFocusDistrict = normalizedFocus ? normalizeDistrictName(district.name) === normalizedFocus : true
                const fill =
                  metric === 'citizens'
                    ? bucketColor(row?.citizenCount ?? 0, maxCitizens)
                    : metric === 'applications'
                      ? bucketColor(row?.applicationCount ?? 0, maxApplications)
                      : '#eef1f5'
                return (
                  <g
                    key={district.name}
                    onClick={() => setSelected(district.name)}
                    className="cursor-pointer"
                    role="button"
                    aria-label={district.name}
                    opacity={isFocusDistrict ? 1 : 0.25}
                  >
                    <path
                      ref={isFocusDistrict && normalizedFocus ? focusPathRef : undefined}
                      d={district.path}
                      fill={fill}
                      stroke={isSelected ? 'var(--gov-navy)' : 'rgba(15,23,42,0.35)'}
                      strokeWidth={isSelected ? 7 : 2}
                      strokeLinejoin="round"
                    />
                    <title>
                      {district.name}
                      {row ? ` — ${formatNumber(row.citizenCount)} citizens, ${formatNumber(row.applicationCount)} applications` : ' — no reported data'}
                    </title>
                  </g>
                )
              })}
              {metric === 'status' &&
                geo.districts.map((district) => {
                  const row = byName.get(normalizeDistrictName(district.name))
                  const total = (row?.submittedCount ?? 0) + (row?.inProgressCount ?? 0) + (row?.completedCount ?? 0)
                  if (!row || total === 0) return null
                  const submittedAngle = (row.submittedCount / total) * Math.PI * 2
                  const inProgressAngle = (row.inProgressCount / total) * Math.PI * 2
                  const r = 20
                  const pieCy = district.cy - 26
                  return (
                    <g key={`pie-${district.name}`} className="pointer-events-none">
                      <circle cx={district.cx} cy={pieCy} r={r + 3} fill="white" fillOpacity={0.9} />
                      <path d={pieSlicePath(district.cx, pieCy, r, 0, submittedAngle)} fill="var(--saffron)" />
                      <path d={pieSlicePath(district.cx, pieCy, r, submittedAngle, submittedAngle + inProgressAngle)} fill="var(--gov-blue)" />
                      <path d={pieSlicePath(district.cx, pieCy, r, submittedAngle + inProgressAngle, Math.PI * 2)} fill="var(--gov-green)" />
                      <circle cx={district.cx} cy={pieCy} r={r + 3} fill="none" stroke="rgba(15,23,42,0.4)" strokeWidth={1.5} />
                    </g>
                  )
                })}
              {geo.districts.map((district) => (
                <text
                  key={`label-${district.name}`}
                  x={district.cx}
                  y={district.cy + 8}
                  textAnchor="middle"
                  fontSize={26}
                  fontWeight={700}
                  fill="rgba(15,23,42,0.85)"
                  stroke="rgba(255,255,255,0.85)"
                  strokeWidth={5}
                  paintOrder="stroke"
                  className="pointer-events-none select-none"
                >
                  {district.code}
                </text>
              ))}
            </svg>
          )}
          <div className="mt-4 flex flex-wrap items-center gap-4 text-[11px] text-muted-foreground">
            {metric === 'status' ? (
              <>
                <LegendDot color="var(--saffron)" label="Just submitted" />
                <LegendDot color="var(--gov-blue)" label="In progress" />
                <LegendDot color="var(--gov-green)" label="Completed" />
              </>
            ) : (
              <>
                <span className="font-semibold uppercase tracking-wide">Low</span>
                <div className="flex overflow-hidden rounded-full">
                  {HEAT_SCALE.map((color) => (
                    <span key={color} className="h-2.5 w-8" style={{ backgroundColor: color }} />
                  ))}
                </div>
                <span className="font-semibold uppercase tracking-wide">High</span>
              </>
            )}
          </div>
        </div>

        <div className="rounded-2xl border border-dashed border-border p-5">
          {selected ? (
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.14em] text-[color:var(--gov-blue)]">{selected}</p>
              <p className="mt-1 text-xs text-muted-foreground">
                {selectedRow ? (selectedRow.taluk ? `Reporting taluk: ${selectedRow.taluk}` : 'District-level aggregate') : 'No reported activity for this district yet'}
              </p>
              <div className="mt-4 grid grid-cols-2 gap-3 text-sm">
                <Stat label="Citizens" value={formatNumber(selectedRow?.citizenCount ?? 0)} />
                <Stat label="Applications" value={formatNumber(selectedRow?.applicationCount ?? 0)} />
                <Stat label="Completed" value={formatNumber(selectedRow?.completedCount ?? 0)} tone="text-[color:var(--gov-green)]" />
                <Stat label="In progress" value={formatNumber(selectedRow?.inProgressCount ?? 0)} tone="text-[color:var(--gov-blue)]" />
                <Stat label="Just submitted" value={formatNumber(selectedRow?.submittedCount ?? 0)} tone="text-[color:var(--saffron)]" />
                <Stat label="Rejected" value={formatNumber(selectedRow?.rejectedCount ?? 0)} />
              </div>
            </div>
          ) : (
            <div className="flex h-full min-h-48 flex-col items-center justify-center text-center text-sm text-muted-foreground">
              <MapPinned className="mb-3 h-6 w-6 text-[color:var(--gov-blue)]" />
              Select a district on the map to see its citizen, application, and delivery breakdown.
            </div>
          )}
        </div>
      </div>
    </Card>
  )
}

function LegendDot({ color, label }: { readonly color: string; readonly label: string }) {
  return (
    <span className="flex items-center gap-1.5">
      <span className="h-2.5 w-2.5 rounded-sm" style={{ backgroundColor: color }} />
      {label}
    </span>
  )
}

function Stat({ label, value, tone }: { readonly label: string; readonly value: string; readonly tone?: string }) {
  return (
    <div className="rounded-xl bg-secondary/60 p-3">
      <p className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className={`mt-1 text-lg font-semibold ${tone ?? 'text-[color:var(--gov-navy)]'}`}>{value}</p>
    </div>
  )
}
