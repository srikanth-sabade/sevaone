#!/usr/bin/env node
/**
 * Guards the Kannada translation so new UI text can't silently ship untranslated.
 *
 * Two checks:
 *   1. PARITY  — every `en` key has a `kn` counterpart, and no `kn` value is a
 *                verbatim copy of its English (a placeholder someone forgot).
 *   2. LITERALS— no user-visible English string is hardcoded in JSX. Anything a
 *                citizen can read must come from `t('key')`.
 *
 * Run: `npm run lint:i18n` (also part of `npm run lint`). Exits 1 on findings.
 *
 * Escape hatch: add `{/* i18n-ignore *\/}` on the line above, or extend
 * ALLOWED_LITERALS / IGNORED_FILES below when a string is genuinely language-neutral.
 */
import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join, relative } from 'node:path'

const SRC = new URL('../src/', import.meta.url).pathname
const I18N = join(SRC, 'i18n.ts')

/** Files whose English text is intentional (dev-only tools, unrouted dead code). */
const IGNORED_FILES = [/\/components\/dev\//, /\/FloatingRail\.sidebar1\.tsx$/, /\/pages\/Landing\.tsx$/]

/** Language-neutral strings: brand marks, codes, symbols, proper nouns rendered the same in both. */
const ALLOWED_LITERALS = new Set([
  'SevaOne', 'Seva', 'One', 'SEVA ONE', 'API', 'Stub', 'KPI', 'IT/BT', 'LLR',
  'DigiLocker', 'Aadhaar', 'Sakala', 'e-KYC', 'SSO', 'iPGRS', 'SVK', 'OTP', 'PDF',
  // Code, credentials and format hints — must stay verbatim in every language.
  'window.print()', 'operator.blr047@seva.local', 'Operator@123',
  'sevasindhu@karnataka.gov.in', 'SVK2026…', 'Citizen mobile (try 9876543210)',
])

/** Keys whose Kannada is deliberately identical to the English (e.g. the "English" label). */
const SAME_IN_BOTH = new Set(['english', 'deptsStub', 'deptsLive'])

/** JSX attributes that reach the user (screen readers included). */
const USER_FACING_ATTRS = ['aria-label', 'placeholder', 'title', 'alt', 'aria-description']

const findings = []

function walk(dir) {
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry)
    if (statSync(full).isDirectory()) walk(full)
    else if (/\.tsx$/.test(full)) checkFile(full)
  }
}

/** Source code that happens to sit between a ">" and a "<" (arrow bodies, ternary arms). */
function looksLikeCode(t) {
  if (/^(return|const|let|var|if|else|case|default)\b/.test(t)) return true
  if (/^[)}\]]/.test(t)) return true          // ") : result ? ("
  if (/[({[]\s*$/.test(t)) return true        // ends opening a block
  if (/=>|;\s*$/.test(t)) return true
  return false
}

function isEnglishish(s) {
  const t = s.trim()
  if (t.length < 3) return false
  if (looksLikeCode(t)) return false
  if (ALLOWED_LITERALS.has(t)) return false
  if (/[ಀ-೿]/.test(t)) return false          // already Kannada
  if (!/[A-Za-z]{2,}/.test(t)) return false            // needs a real word
  if (/^[A-Z0-9_]+$/.test(t)) return false             // SCREAMING_CASE constant
  if (/^[\d\s.,:%+/-]+$/.test(t)) return false         // pure numeric/punctuation
  return true
}

function checkFile(file) {
  if (IGNORED_FILES.some((re) => re.test(file))) return
  const rel = relative(SRC, file)
  const source = readFileSync(file, 'utf8')
  const lines = source.split('\n')

  // Multi-line JSX text, e.g.
  //   <Link to="/x">
  //     All services
  //   </Link>
  // Scanned over the whole file because a line-by-line pass never sees these.
  for (const m of source.matchAll(/>\s*\n\s*([^<>{}\n][^<>{}\n]*?)\s*\n\s*</g)) {
    const text = m[1].trim()
    const lineNo = source.slice(0, m.index).split('\n').length + 1
    if (/i18n-ignore/.test(lines[lineNo - 2] ?? '')) continue
    if (isEnglishish(text)) {
      findings.push({ file: rel, line: lineNo, kind: 'jsx-text', text })
    }
  }

  lines.forEach((line, i) => {
    if (/i18n-ignore/.test(line) || /i18n-ignore/.test(lines[i - 1] ?? '')) return
    if (/^\s*(\/\/|\*|\/\*)/.test(line)) return        // comment
    if (/^\s*import\s/.test(line)) return

    // 1. JSX text nodes:  >Some text<
    for (const m of line.matchAll(/>\s*([^<>{}\n][^<>{}\n]*?)\s*</g)) {
      // Skip TypeScript generics that look like JSX text:
      //   `() => Promise<void>`  — the ">" is an arrow, not a tag close
      //   `<'div'> & VariantProps<…>` — a type intersection, not text
      if (line[m.index - 1] === '=') continue
      if (/^[&|]/.test(m[1]) || /\btypeof\b/.test(m[1])) continue
      if (isEnglishish(m[1])) {
        findings.push({ file: rel, line: i + 1, kind: 'jsx-text', text: m[1].trim() })
      }
    }

    // 2. User-facing attributes with a literal (not {t('…')}) value
    for (const attr of USER_FACING_ATTRS) {
      const re = new RegExp(`\\b${attr}\\s*=\\s*"([^"]+)"`, 'g')
      for (const m of line.matchAll(re)) {
        if (isEnglishish(m[1])) {
          findings.push({ file: rel, line: i + 1, kind: attr, text: m[1] })
        }
      }
    }
  })
}

function checkParity() {
  const src = readFileSync(I18N, 'utf8')
  const en = src.split('en: {')[1].split('kn: {')[0]
  const kn = src.split('kn: {')[1]
  const pat = /^\s{4}(\w+):\s*(?:'((?:[^'\\]|\\.)*)'|"((?:[^"\\]|\\.)*)")/gm
  const grab = (block) => {
    const out = {}
    for (const m of block.matchAll(pat)) out[m[1]] = m[2] ?? m[3]
    return out
  }
  const E = grab(en)
  const K = grab(kn)

  for (const key of Object.keys(E)) {
    if (!(key in K)) {
      findings.push({ file: 'i18n.ts', line: 0, kind: 'missing-kn', text: `${key} has no Kannada` })
    } else if (E[key] === K[key] && isEnglishish(E[key]) && !SAME_IN_BOTH.has(key)) {
      findings.push({ file: 'i18n.ts', line: 0, kind: 'untranslated', text: `${key} = ${JSON.stringify(E[key])}` })
    }
  }
  return Object.keys(E).length
}

/**
 * Catalog content (service/department names, categories) lives in the DB, seeded by
 * R__seed_lookup.sql, and is localized by code in src/lib/catalogKn.ts. This makes adding
 * a service to the seed without its Kannada a build failure, the same as any UI string.
 */
function checkCatalog() {
  const seed = join(SRC, '../../backend/database/migrations/R__seed_lookup.sql')
  const kn = join(SRC, 'lib/catalogKn.ts')
  let seedSrc, knSrc
  try {
    seedSrc = readFileSync(seed, 'utf8')
    knSrc = readFileSync(kn, 'utf8')
  } catch {
    return // backend not checked out alongside — skip rather than fail
  }
  // Keys are one-per-line; anchor on line start to avoid a greedy match spilling into values.
  const keysIn = (re) => {
    const block = knSrc.match(re)?.[0] ?? ''
    return new Set(
      block
        .split('\n')
        .map((l) => l.match(/^\s*'?([A-Za-z0-9 &/-]+?)'?:\s*'/)?.[1])
        .filter(Boolean),
    )
  }
  const svcKn = keysIn(/SERVICE_KN\s*[:=][\s\S]*?\n\}/)
  const deptKn = keysIn(/DEPARTMENT_KN\s*[:=][\s\S]*?\n\}/)
  const catKn = keysIn(/CATEGORY_KN\s*[:=][\s\S]*?\n\}/)

  // Service rows:  ('SVC001', 'revenue', 'Income Certificate', 'Certificates', …)
  for (const m of seedSrc.matchAll(/\(\s*'(SVC\d+)'\s*,\s*'[^']*'\s*,\s*'([^']*)'\s*,\s*'([^']*)'/g)) {
    const [, code, , category] = m
    if (!svcKn.has(code)) findings.push({ file: 'lib/catalogKn.ts', line: 0, kind: 'catalog-service', text: `${code} seeded but no Kannada in SERVICE_KN` })
    if (!catKn.has(category)) findings.push({ file: 'lib/catalogKn.ts', line: 0, kind: 'catalog-category', text: `category ${JSON.stringify(category)} seeded but no Kannada in CATEGORY_KN` })
  }
  // Department rows:  (gen_random_uuid(), 'revenue', 'Revenue Department', …)
  const deptBlock = seedSrc.split('INSERT INTO discovery.department_master')[1]?.split('INSERT INTO')[0] ?? ''
  for (const m of deptBlock.matchAll(/,\s*'([a-z-]+)'\s*,\s*'[^']*'/g)) {
    if (!deptKn.has(m[1])) findings.push({ file: 'lib/catalogKn.ts', line: 0, kind: 'catalog-department', text: `department ${JSON.stringify(m[1])} seeded but no Kannada in DEPARTMENT_KN` })
  }
}

const keyCount = checkParity()
checkCatalog()
walk(SRC)

if (findings.length === 0) {
  console.log(`i18n OK — ${keyCount} keys, en/kn in parity, no hardcoded UI strings.`)
  process.exit(0)
}

const byFile = {}
for (const f of findings) (byFile[f.file] ??= []).push(f)
console.error(`\ni18n check failed — ${findings.length} issue(s):\n`)
for (const [file, items] of Object.entries(byFile)) {
  console.error(`  ${file}`)
  for (const it of items) {
    const loc = it.line ? `:${it.line}` : ''
    console.error(`    ${loc.padEnd(6)} [${it.kind}] ${JSON.stringify(it.text)}`)
  }
  console.error('')
}
console.error(`Fix: move the string into src/i18n.ts (both en and kn) and render it with t('key').`)
console.error(`If it is genuinely language-neutral, add it to ALLOWED_LITERALS in scripts/check-i18n.mjs`)
console.error(`or put an {/* i18n-ignore */} comment on the line above.\n`)
process.exit(1)
