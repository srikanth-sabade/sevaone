/**
 * Kannada labels for catalog content that lives in the database, not in i18n.ts.
 *
 * Service and department names come from the API (seeded by
 * backend/database/migrations/R__seed_lookup.sql) and are English-only there.
 * The catalog is a fixed, curated list keyed by stable codes, so we localize it
 * here by code rather than adding _kn columns to the schema.
 *
 * Keyed by the codes/strings the API returns:
 *   SERVICE_KN    — by serviceCode  (SVC001…)
 *   DEPARTMENT_KN — by departmentCode (revenue, transport…)
 *   CATEGORY_KN   — by the exact English category string (no code exists)
 *
 * Anything without an entry falls back to the English the API gave us, so a
 * newly-seeded service still renders — just in English until its Kannada lands.
 * `scripts/check-i18n.mjs` reads the seed file and fails the build when a seeded
 * code has no entry here, so that gap can't ship silently.
 *
 * Digits stay Western (₹2000, 200 Units) to match the rest of the UI.
 */
import type { Lang } from '../i18n'

export const SERVICE_KN: Record<string, string> = {
  SVC001: 'ಆದಾಯ ಪ್ರಮಾಣಪತ್ರ',
  SVC002: 'ಜಾತಿ ಪ್ರಮಾಣಪತ್ರ',
  SVC003: 'ವಾಸಸ್ಥಳ / ಮೂಲನಿವಾಸಿ ಪ್ರಮಾಣಪತ್ರ',
  SVC004: 'ಜನನ ಪ್ರಮಾಣಪತ್ರ',
  SVC005: 'ಮರಣ ಪ್ರಮಾಣಪತ್ರ',
  SVC006: 'ಕಲಿಕಾ ಪರವಾನಗಿ (LLR)',
  SVC007: 'ಚಾಲನಾ ಪರವಾನಗಿ (DL)',
  SVC008: 'ವಾಹನ ನೋಂದಣಿ (RC)',
  SVC009: 'ಪಡಿತರ ಚೀಟಿ - ಹೊಸ / ಮಾರ್ಪಾಡು',
  SVC010: 'ಗೃಹ ಜ್ಯೋತಿ (200 ಯೂನಿಟ್ ಉಚಿತ ವಿದ್ಯುತ್)',
  SVC011: 'ಗೃಹ ಲಕ್ಷ್ಮಿ (ಮನೆಯ ಮಹಿಳಾ ಯಜಮಾನಿಗೆ ತಿಂಗಳಿಗೆ ₹2000)',
  SVC012: 'ಯುವ ನಿಧಿ (ನಿರುದ್ಯೋಗ ಭತ್ಯೆ)',
  SVC013: 'ಅನ್ನ ಭಾಗ್ಯ (ಉಚಿತ ಅಕ್ಕಿ / ಡಿಬಿಟಿ)',
  SVC014: 'ಶಕ್ತಿ ಯೋಜನೆ (ಮಹಿಳೆಯರಿಗೆ ಉಚಿತ ಬಸ್ ಪ್ರಯಾಣ)',
  SVC015: 'ವೃದ್ಧಾಪ್ಯ ವೇತನ (ಸಂಧ್ಯಾ ಸುರಕ್ಷಾ)',
  SVC016: 'ವಿಧವಾ ವೇತನ',
  SVC017: 'ವ್ಯಾಪಾರ ಪರವಾನಗಿ (ಪೌರಸಭೆ)',
  SVC018: 'ಖಾತಾ ನೋಂದಣಿ / ವರ್ಗಾವಣೆ (ಬಿಬಿಎಂಪಿ)',
  SVC019: 'ಮೆಟ್ರಿಕ್ ನಂತರದ ವಿದ್ಯಾರ್ಥಿವೇತನ',
  SVC020: 'ವಿದ್ಯಾಸಿರಿ ವಸತಿನಿಲಯ ಪ್ರವೇಶ',
  SVC021: 'ಇ-ಸ್ವತ್ತು ಆಸ್ತಿ ದಾಖಲೆ',
  SVC022: 'ವಿವಾಹ ನೋಂದಣಿ',
  SVC023: 'ಪೊಲೀಸ್ ನಿರಪೇಕ್ಷಣಾ ಪ್ರಮಾಣಪತ್ರ',
  SVC024: 'ಅಂಗಡಿ ಮತ್ತು ಸಂಸ್ಥೆ ನೋಂದಣಿ',
  SVC101: 'ಭೂ ಹಿಡುವಳಿ ಪ್ರಮಾಣಪತ್ರ (ಆರ್‌ಟಿಸಿ ಸಾರ)',
  SVC102: 'ಭೂಮಿ ಮ್ಯುಟೇಶನ್ (ಖಾತಾ ವರ್ಗಾವಣೆ)',
  SVC103: 'ಋಣಭಾರ ಪ್ರಮಾಣಪತ್ರ (ಇಸಿ)',
  SVC104: 'ಆಸ್ತಿ ನೋಂದಣಿ (ಕ್ರಯಪತ್ರ)',
  SVC105: 'ಕೃಷಿ ಕುಟುಂಬ ಸದಸ್ಯ ಪ್ರಮಾಣಪತ್ರ',
  SVC106: 'ಸಣ್ಣ / ಅತಿ ಸಣ್ಣ ರೈತ ಪ್ರಮಾಣಪತ್ರ',
  SVC107: 'ಸಾಲ್ವೆನ್ಸಿ (ದ್ರವ್ಯಶಕ್ತಿ) ಪ್ರಮಾಣಪತ್ರ',
  SVC108: 'ಗೇಣಿ ಇಲ್ಲದಿರುವ ಪ್ರಮಾಣಪತ್ರ',
  SVC109: 'ವಿಧವಾ ಪ್ರಮಾಣಪತ್ರ',
  SVC110: 'ನಿರುದ್ಯೋಗ ಪ್ರಮಾಣಪತ್ರ',
  SVC111: 'ಜೀವಂತ / ಉಳಿದಿರುವ ಕುಟುಂಬ ಸದಸ್ಯ ಪ್ರಮಾಣಪತ್ರ',
  SVC112: 'ಒಬಿಸಿ / ವರ್ಗ ಆದಾಯ ಮತ್ತು ಜಾತಿ ಪ್ರಮಾಣಪತ್ರ (ನಮೂನೆ ಡಿ)',
  SVC113: 'ಭೂ ಪರಿವರ್ತನೆ (ಕೃಷಿಯಿಂದ ಕೃಷಿಯೇತರ)',
  SVC114: 'ಪೋಡಿ (ಭೂ ಉಪ-ವಿಭಜನೆ) ಅರ್ಜಿ',
  SVC115: 'ಕಂದಾಯ ದಾಖಲೆಗಳ ದೃಢೀಕೃತ ಪ್ರತಿ',
  SVC116: 'ನೈಸರ್ಗಿಕ ವಿಕೋಪ / ಬೆಳೆ ನಷ್ಟ ಪರಿಹಾರ',
}

export const DEPARTMENT_KN: Record<string, string> = {
  'commercial-taxes': 'ವಾಣಿಜ್ಯ ತೆರಿಗೆ',
  education: 'ಶಿಕ್ಷಣ ಇಲಾಖೆ',
  food: 'ಆಹಾರ ಮತ್ತು ನಾಗರಿಕ ಸರಬರಾಜು',
  health: 'ಆರೋಗ್ಯ ಮತ್ತು ಕುಟುಂಬ ಕಲ್ಯಾಣ',
  home: 'ಗೃಹ ಇಲಾಖೆ',
  labour: 'ಕಾರ್ಮಿಕ ಇಲಾಖೆ',
  rdpr: 'ಗ್ರಾಮೀಣಾಭಿವೃದ್ಧಿ ಮತ್ತು ಪಂಚಾಯತ್ ರಾಜ್',
  revenue: 'ಕಂದಾಯ ಇಲಾಖೆ',
  'social-welfare': 'ಸಮಾಜ ಕಲ್ಯಾಣ',
  transport: 'ಸಾರಿಗೆ ಇಲಾಖೆ',
  urban: 'ನಗರಾಭಿವೃದ್ಧಿ',
  wcd: 'ಮಹಿಳಾ ಮತ್ತು ಮಕ್ಕಳ ಅಭಿವೃದ್ಧಿ',
}

export const CATEGORY_KN: Record<string, string> = {
  'Business & Trade': 'ವ್ಯಾಪಾರ ಮತ್ತು ವಾಣಿಜ್ಯ',
  Certificates: 'ಪ್ರಮಾಣಪತ್ರಗಳು',
  'Education & Scholarships': 'ಶಿಕ್ಷಣ ಮತ್ತು ವಿದ್ಯಾರ್ಥಿವೇತನ',
  Employment: 'ಉದ್ಯೋಗ',
  Licenses: 'ಪರವಾನಗಿಗಳು',
  'Pensions & Welfare': 'ಪಿಂಚಣಿ ಮತ್ತು ಕಲ್ಯಾಣ',
  'Utility Services': 'ಸೌಲಭ್ಯ ಸೇವೆಗಳು',
}

/** Localized service name, falling back to the API's English when no Kannada exists. */
export function serviceName(lang: Lang, code: string, english: string): string {
  return lang === 'kn' ? (SERVICE_KN[code] ?? english) : english
}

/** Localized department name, falling back to the API's English. */
export function departmentName(lang: Lang, code: string, english: string): string {
  return lang === 'kn' ? (DEPARTMENT_KN[code] ?? english) : english
}

/** Localized category label, keyed by the English string the API returns. */
export function categoryLabel(lang: Lang, english: string): string {
  return lang === 'kn' ? (CATEGORY_KN[english] ?? english) : english
}
