/** Shared status → badge color mapping, used by Dashboard's application list and Track's result card. */
export function statusTone(status: string) {
  const s = status.toLowerCase()
  if (s.includes('approved') || s.includes('issued') || s.includes('completed')) {
    return 'border-emerald-200 bg-emerald-50 text-emerald-800'
  }
  if (s.includes('reject') || s.includes('fail')) {
    return 'border-red-200 bg-red-50 text-red-800'
  }
  if (s.includes('pending') || s.includes('process') || s.includes('review')) {
    return 'border-amber-200 bg-amber-50 text-amber-900'
  }
  return 'border-[color:var(--gov-blue)]/20 bg-[color:var(--gov-blue)]/8 text-[color:var(--gov-navy)]'
}

/** Buckets a raw ApplicationStatus string into the three groups the dashboard summarizes. */
export function statusBucket(status: string): 'pending' | 'completed' | 'rejected' {
  const s = status.toLowerCase()
  if (s.includes('approved') || s.includes('issued') || s.includes('completed')) return 'completed'
  if (s.includes('reject') || s.includes('fail')) return 'rejected'
  return 'pending'
}
