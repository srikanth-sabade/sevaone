export type BackgroundPreviewMode =
  | 'default'
  | 'animated-dark'
  | 'static-dark'
  | 'animated-aurora'
  | 'animated-dark-full-width'
  | 'animated-dark-whole-page'

const STORAGE_KEY = 'seva-one:background-preview'

const OPTIONS: ReadonlyArray<{ value: BackgroundPreviewMode; label: string }> = [
  { value: 'default', label: 'Default' },
  { value: 'animated-dark-full-width', label: 'Full width' },
  { value: 'animated-dark-whole-page', label: 'Whole page' },
]

export function loadBackgroundPreviewMode(): BackgroundPreviewMode {
  if (!import.meta.env.DEV || typeof window === 'undefined') return 'default'
  const stored = window.localStorage.getItem(STORAGE_KEY)
  return OPTIONS.some((option) => option.value === stored)
    ? (stored as BackgroundPreviewMode)
    : 'default'
}

export function persistBackgroundPreviewMode(mode: BackgroundPreviewMode) {
  if (!import.meta.env.DEV || typeof window === 'undefined') return
  window.localStorage.setItem(STORAGE_KEY, mode)
}

export function BackgroundThemePreview({
  mode,
  onChange,
}: {
  readonly mode: BackgroundPreviewMode
  readonly onChange: (mode: BackgroundPreviewMode) => void
}) {
  if (!import.meta.env.DEV) return null

  return (
    <label className="fixed bottom-[calc(4.75rem+env(safe-area-inset-bottom))] right-3 z-[100] md:bottom-auto md:left-3 md:right-auto md:top-3">
      <span className="sr-only">Background theme preview</span>
      <select
        aria-label="Background theme preview"
        value={mode}
        onChange={(event) => onChange(event.target.value as BackgroundPreviewMode)}
        className="h-8 rounded-lg border border-white/15 bg-[#080b16]/90 px-2 text-[11px] font-medium text-white shadow-lg backdrop-blur-md outline-none focus-visible:ring-2 focus-visible:ring-white/70"
      >
        {OPTIONS.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  )
}
