import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/** Prepend "https://" to a URL missing a scheme entirely - confirmed live, a government
 *  apply-link value like "ajsk.karnataka.gov.in/nk5_online/Login/Login_Public" (no
 *  "http(s)://" at all) opened as-is via window.open()/<a href> resolves as a RELATIVE path
 *  against the current page, silently sending the citizen to
 *  "https://<this-app's-own-host>/ajsk.karnataka.gov.in/..." instead of the real government
 *  site. Fixed at the source too (sevaone-rag's ServiceRecord now normalizes this on every
 *  ingestion), but kept here as well as a defensive second layer for any other backend/data
 *  source this frontend ever renders a link from. */
export function ensureUrlScheme(url: string): string {
  const trimmed = url.trim()
  return /^https?:\/\//i.test(trimmed) ? trimmed : `https://${trimmed}`
}
