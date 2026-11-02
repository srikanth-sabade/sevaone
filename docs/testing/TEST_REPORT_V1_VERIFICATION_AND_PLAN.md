# SevaOne_Test_Report_V1 — Verification & Remediation Plan

Source: `~/Downloads/SevaOne_Test_Report_V1.docx` (dated Sep 2025).
Verified against branch `notsure` (`ec90d17`) on 2026-09-07.

## Additional fix — "Could not open this service right now" on Apply (2026-09-07)

Seen on the deployed server (`100.91.197.105`) clicking **Apply now** for Income Certificate
in the AI chat.

**Root cause:** the Chat page's Income/Caste carve-out was hardcoded to the old
**seed-lookup codes `SVC001` / `SVC002`** (`frontend/src/lib/aiDiscover.ts` —
`CATALOGUE_ONLY_SERVICE_CODES`, `CATALOGUE_ONLY_KEYWORDS`, `HARDCODED_CATALOGUE_SERVICES`).
Those codes do not exist in the DB-backed Catalog 2.0 (`sevaone_service_master.service_id`
is `11` / `NK-48` / …). So a "income certificate" query hit `resolveCatalogueOnlyService('SVC001')`
→ `/api/v1/schemes/SVC001` 404'd → it fell back to the hardcoded fixture (which *rendered*
fine: ₹15, Nadakacheri) → **Apply → `/apply-token` with `revenue`/`SVC001` → 404 →**
`applyFlow.ts` catch → "Could not open this service right now." Verified against the DB:
`sp_get_scheme_by_department_and_service_code('revenue','SVC001')` → 0 rows;
`('revenue','11')` → Income Certificate.

**Fix:** removed the stale `SVC*` machinery. The carve-out is now a routing hint only:
- `isIncomeOrCasteQuery(query)` / `isIncomeOrCasteService(name)` in `aiDiscover.ts`.
- `Chat.tsx` `ask()` routes those queries through the normal `resolveWithCatalogue` path,
  which uses `api.schemes()` → **real** service codes → working `/apply-token` handoff.
  `resolveCatalogueOnlyService` + `HARDCODED_CATALOGUE_SERVICES` deleted.
- Verified: `buildDiscoverResult("apply for an income certificate", …)` → primary
  `serviceCode "11"`, `departmentCode "revenue"` (real, resolvable); `tsc` + `oxlint` +
  `vite build` clean.

Needs a frontend rebuild on any deployed environment.

## Additional — "Frequently requested this month" ranking (2026-09-07)

**Verdict: the ranking is correctly implemented (by real Apply-click counts over a rolling
30-day window, `apply_count DESC`), NOT random.** Verified by seeding test clicks against
`discovery.sp_list_top_requested_services` — services returned in exact descending count order.

It looked broken because:
- `discovery.service_apply_track` has **0 rows ever** — no Apply click recorded on this DB.
  With every count at 0, the SP's name tiebreak made it pure alphabetical → surfaced obscure
  "1st Anaesthesia and Operation Theatre Technology"-type entries.
- The repeatable function `R__sp_list_top_requested_services` was **not in the DB** (added on
  this branch; applied manually).

**Curated fallback (done):** `backend/database/migrations/functions/discovery/R__sp_list_top_requested_services.sql`
now orders by `apply_count DESC, curated_rank NULLS LAST, service_name`. A 10-row curated
`VALUES` list (Income / Caste / Residence / RTC / Encumbrance / Mutation / Ration card / Birth
certificate / Gruha Lakshmi / Driving License, matched by department code + exact service
name via LEFT JOIN) fills zero-demand slots with sensible services instead of alphabetical
junk. Real apply demand always outranks the curated seed; unmatched curated names degrade to
alphabetical for that slot rather than erroring. Verified: empty-data list is now the curated
set in order; 30 clicks on a non-curated service still take rank 1.

**`flyway migrate` — NOT run, and should not be, with `notsure` checked out.** The local
`sevaone` DB was migrated from `develop`/`deployment-dev`: it has `V55__sub_department_hierarchy`
(commit `93b0cb5`, not in `notsure`) and its portal-operator migrations sit at V56–V59 with
`service_apply_track` at V54. `notsure` renumbered these (portal operator → V54–57,
`service_apply_track` → V62) and never got `93b0cb5`. So `flyway migrate` against `notsure`'s
migrations fails validation on V54–V59 checksum mismatches, and forcing past that would try to
`CREATE TABLE discovery.service_apply_track` (already exists) → hard failure. The DB is
effectively **ahead of `notsure`**. Resolve the branch/migration divergence separately (rebase
`notsure` onto `develop`, or rebuild the dev DB) — the popular-services feature needs nothing
from a migrate: `service_apply_track` exists and the function is applied.

## Additional fix — Recent Activity showed "apply homepage_fallback" (2026-09-07)

Separate from the report. The dashboard "Recent activity" feed rendered the raw audit action
(`apply homepage_fallback`) with no department/service, because `friendlyAudit`
(`frontend/src/lib/auditLabels.ts`) had no case for `apply.homepage_fallback` (nor `.assisted`
/ `.test`, nor `apply.token.issued.test`, `apply.ajsk_redirect.assisted`,
`apply.sakala_token_redirect.assisted`) — all fell through to a default that prints
`action.replace(/\./g,' ')`. The `Resource` those rows carried was the machine key
`deptCode/serviceCode`, and `describeApplyResource` only knew the seeded `SVC001-004` codes,
so even the mapped `apply.token.issued` case dropped the service name for catalog-2.0 codes.

- **Backend** `backend/SevaOne.Business/Portal/Services/PortalApplyService.cs`: the four apply
  audit writes now store `Resource = "{scheme.Name} · {department.Name}"` (a display string)
  instead of `"{department.Code}/{scheme.ServiceCode}"`. `Resource` is opaque audit metadata —
  nothing in the backend parses it; the machine codes still live on the JWS and in
  `discovery.service_apply_track`.
- **Frontend** `frontend/src/lib/auditLabels.ts`: added `case`s for all six apply actions
  (deep-link token, homepage fallback, AJSK / Sakala-token redirect, test variants) → "Started
  an application" / "Application started at a Seva centre" with the service · department
  subtitle. `describeApplyResource` now returns any whitespace-containing `Resource` as-is
  (new rows) and, for legacy `code/code` rows, shows the raw service code rather than dropping
  it. `department.session.open` (bare dept code) still localises correctly.

**Follow-up (2026-09-07): resolve legacy rows to names server-side.** Existing rows still hold
`deptCode/serviceCode`, so the feed showed e.g. `REVENUE-59 · Revenue department` — a code, not
a name. `PortalAuthService.GetMyAuditAsync` now resolves those: for any `apply.*` (except
`apply.return.*`) or `department.session.open` row whose `Resource` has no whitespace, it looks
up `GetSchemeByDepartmentAndServiceCodeAsync` + the department and rewrites `Resource` to
`"{ServiceName} · {DepartmentName}"` before returning (deduped per distinct resource, ≤20 rows).
Works whether or not the frontend `auditLabels.ts` change is deployed — the old
`describeApplyResource` also passes a no-slash string straight through.
- Verified: backend `SevaOne.Business` builds clean; `friendlyAudit` checked against all
  cases (new + legacy rows) via a bundled runtime harness; `tsc` + `oxlint` + `vite build` clean.

## Summary

The report is an AI-generated QA audit. Its bulk — a 765-service redirection matrix and
a 65-case test matrix, all "100% PASS" — is not real test evidence and can be ignored.
The actionable content is the 8-item defect list (§1) and the usability suggestions (§10).

Several defects describe code that does not exist in this repo (React Router, a grievance
file uploader, API fields `query`/`session_id`), which means the tester was looking at an
older prototype or the standalone `100.96.248.6` deployment, not the current `frontend/`.
Each item was still checked against the real code below.

## Verdict per defect

| ID | Report claim | Verdict | Evidence |
|----|--------------|---------|----------|
| **DEF-001** | External Google Fonts `<link>` blocks page load in offline/KSWAN networks | **PARTLY REAL** | `frontend/index.html:12` render-blocking-loads Inter + Noto Sans Kannada from `fonts.googleapis.com`. DM Sans + Space Grotesk are *already* self-hosted (`frontend/public/fonts/`, `@font-face` in `frontend/src/styles.css:3`). Inter is loaded but referenced nowhere (dead). Noto Sans Kannada — the Kannada UI font — is still CDN-only and render-blocking. Real, narrower than described. |
| **DEF-002** | Nginx needs `try_files … /index.html` for SPA deep-refresh | **NOT REAL** | `frontend/nginx.conf:15` already has `try_files $uri $uri/ /index.html;`. `Docker-deploy/nginx/nginx.conf` proxies `/` to that container. Deep refresh on `/grievance`, `/track` works today. |
| **DEF-003** | FastAPI returns raw `{"detail":[…]}` 422 on empty body | **REAL, low impact** | `ai_backend/app/main.py:63-64` registers handlers for `SevaOneException` and `Exception` only — no `RequestValidationError` handler, so `/api/v1/ai-chat/message` returns FastAPI's default 422. But the citizen path goes through the bff (`backend/SevaOne.Services/Controllers/AiChatController.cs` + `AiChatMessageRequestValidator`), which already returns clean envelopes, and `ai_backend` is deliberately not client-reachable (the `/ai-api/` nginx proxy was removed). Report's payload shape is also wrong: real schema is `queryText`/`sessionId` (`ai_backend/app/schemas/chat.py:39`). |
| **DEF-004** | Some services redirect to portal root, not a deep apply URL | **REAL (data completeness)** | `frontend/src/lib/catalogFromCsv.generated.ts` — many entries point at portal landing pages (`ssp.postmatric.karnataka.gov.in/`, `nadakacheri.karnataka.gov.in/ajsk`, `wcd.karnataka.gov.in/en`, `mybmtc.karnataka.gov.in/en`, …); only ~22 use `sevasindhuservices.karnataka.gov.in/directApply.do?serviceId=`. Matches the known partial `deeplink_citizen_url` backfill. Note this branch's catalog is the ~236-row CSV mock, not the DB-backed 765-service catalog the report assumes. |
| **DEF-005** | No `aria-live` on catalog search filter results | **REAL (minor a11y)** | `frontend/src/pages/Catalog.tsx:118` renders a live result count with no `role="status"` / `aria-live`. `AuthBusyOverlay.tsx` is the only component in the app using `aria-live`. |
| **DEF-006** | Grievance file upload lacks pre-upload size check | **NOT REAL / N/A** | `frontend/src/pages/Grievance.tsx` has no file input at all — fields are name, mobile, department, details. The described `.pdf/.jpg/.png ≤ 5MB` uploader does not exist. |
| **DEF-007** | Mobile sticky bottom bar overlaps the OTP submit button | **NOT REPRODUCED** | `frontend/src/components/Shell.tsx:68` — `<main>` has `pb-24 md:pb-0`, a 96px mobile bottom gutter. The `FloatingRail` bottom bar (`FloatingRail.tsx:347`) is `md:hidden` with `env(safe-area-inset-bottom)` padding. The login card renders inside the padded main; no occlusion in the current layout. |
| **DEF-008** | No PWA / service worker offline caching for kiosks | **REAL (strategic)** | No `vite-plugin-pwa`, no service worker, no web-app manifest anywhere in `frontend/`. |

### Extra finding (not in the report)

**No branded 404.** `frontend/src/router.tsx` creates the router with no
`defaultNotFoundComponent`, so an unknown multi-segment path (e.g. `/a/b/c`) falls through
to TanStack Router's bare built-in default. The single-segment `/$serviceCode` catch-all
only handles one segment and only for known service codes. The report's "Page 9: Graceful
404 Fallback" does not exist as a global handler.

## Verdict — §10 usability suggestions

| Item | Verdict |
|------|---------|
| §10.1 `/help` FAQ page is sparse | **REAL** — `frontend/src/pages/Help.tsx` is 32 lines: a search box + `<details>` list from `/api/v1/help/faqs`. No categories, no 1902/112/108 helpline cards, no counter locator. Content depends entirely on backend faq rows. |
| §10.2 AI search autocomplete / "did you mean" | Enhancement, not a defect. |
| §10.2 Track by mobile number + PDF receipt | Enhancement. `frontend/src/pages/Track.tsx` has no track-by-mobile today. |
| §10.2 Grievance file-size meter + save draft | Depends on DEF-006 (there is no uploader yet). |
| §10.2 Catalog "Top 10 most availed" carousel | Enhancement. |
| §10.2 WhatsApp OTP delivery | Enhancement (backend SMS/OTP provider work). |
| §10.2 Search box on the 404 page | Folds into the "no branded 404" finding above. |

## Remediation plan

Only the items that verified. Grouped by real priority, not the report's.

### Phase 1 — Real defects, low effort (~half a day) — DONE 2026-09-07

Commit: font self-hosting + catalog `aria-live` + branded 404.
- `frontend/public/fonts/noto-sans-kannada.woff2` added (Kannada subset, ~89 KB); `@font-face`
  in `frontend/src/styles.css` with the Kannada `unicode-range`.
- `frontend/index.html` — removed the Google Fonts `<link>` + both `preconnect`s (Inter dropped, was unused).
- CSP tightened in `Docker-deploy/nginx/nginx.conf` (citizen `location /`) and
  `backend/SevaOne.Services/Program.cs` — `style-src`/`font-src` no longer allow the font CDN.
  The `/dashboard/` block keeps its allowance (dashboard-kar still uses the CDN).
- `frontend/src/pages/Catalog.tsx` — visually-hidden `role="status" aria-live="polite"` regions
  on both the landing search and the department-level filter, mirroring the visible result count.
- `frontend/src/pages/NotFound.tsx` + `defaultNotFoundComponent` in `frontend/src/router.tsx`;
  i18n keys `notFound*` (en + kn).
- Verified: dev server loads zero `fonts.gstatic`/`googleapis` requests, Kannada `@font-face`
  resolves from the local woff2, live region updates on search, `/x/y/z` renders the branded 404,
  `npm run build` + `tsc` + `oxlint` clean (pre-existing `navAiDiscoveryBetaShort` i18n-parity
  failure is unrelated).

Original plan for reference:

**1. DEF-001 — remove the render-blocking font CDN dependency**
- Add `noto-sans-kannada.woff2` (weights 400, 600) to `frontend/public/fonts/`.
- Add a `@font-face` for it in `frontend/src/styles.css` alongside the existing two, `font-display: swap`.
- Delete the `<link href="https://fonts.googleapis.com/…">` and the two `preconnect`
  lines from `frontend/index.html`. Inter is unused — drop it, don't re-host it.
- Drop `https://fonts.googleapis.com` / `https://fonts.gstatic.com` from the `style-src`
  and `font-src` of `Docker-deploy/nginx/nginx.conf` (and the `/dashboard/` block) once
  nothing external is referenced.
- Verify: load the app with devtools "offline" after first paint blocked — no 30s stall;
  Kannada toggle still renders correctly.

**2. DEF-005 — announce catalog search results to screen readers**
- Wrap the results-count line in `frontend/src/pages/Catalog.tsx` (~L118) in a
  `role="status" aria-live="polite" aria-atomic="true"` container, or add a visually-hidden
  live region that updates with `catalogSearchResultsCount`.
- Do the same for the department-level filter (~L285) and the zero-results state.
- Verify with VoiceOver: typing in the filter announces "N services found".

**3. Branded 404 (extra finding)**
- Add a `NotFound` component (reuse the `ServiceCodePage` not-found card style) and pass it
  as `defaultNotFoundComponent` to `createRouter` in `frontend/src/router.tsx`.
- Include a "Return to Home" link and a search box that routes to `/catalog?…` (covers the
  §10.2 "404 search box" suggestion).

### Phase 2 — Catalog data quality (scope depends on catalog source)

**4. DEF-004 — deep apply URLs**
- This branch still ships the CSV mock (`catalogFromCsv.generated.ts`). Confirm with the
  team whether `notsure` is meant to move to the DB-backed catalog first — if so this work
  belongs in the ingestion pipeline (`deeplink_citizen_url` / `deeplink_citizen_url_kannada`),
  not the generated file.
- For the CSV path: extend `frontend/"Sakala & Sevasindhu Services - 28aug redirect urls.csv"`
  so every row has a `Sevasindu URL` / direct-apply value; regenerate
  `catalogFromCsv.generated.ts` via `frontend/scripts/`.
- Where a department has no per-service deep link (portal genuinely only has a landing
  page), keep the landing URL but surface it in the UI as "Opens the department portal"
  rather than "Apply now", so the citizen isn't surprised.
- Verify: count of entries whose URL is a bare origin drops to only those departments that
  truly lack deep links.

### Phase 3 — Help content (§10.1) — DONE 2026-09-07

Source of truth: **static bilingual set in the frontend**, not a DB-backed endpoint. There is
no `/api/v1/help/faqs` backend on this branch (the old `api.faqs` just 502'd), and keeping
~18 slow-changing, translation-reviewed Q&As local means `/help` works on an offline
Grama One kiosk. `selectHelpFaqs()` is the single swap point if a FAQ admin is ever built.

- New `frontend/src/lib/helpFaqs.ts` — 18 FAQs across 7 categories (certificates, Sakala,
  fees, tracking, login, grievances, helplines), each with en + kn question/answer, optional
  related service codes and hidden search keywords; `selectHelpFaqs(lang, query)` localises
  and filters.
- `frontend/src/pages/Help.tsx` rebuilt — 1902 / 112 / 108 helpline tiles (`tel:` links),
  an in-person / counter-hours note, category-grouped `<details>` accordions, a search box
  with an `aria-live` no-results announcement, and related-service codes per answer.
- `frontend/src/pages/Chat.tsx` — FAQ discovery matches now come from `selectHelpFaqs(lang)`
  instead of `api.faqs()`; dead `api.faqs` + its `Faq` import removed from `frontend/src/api.ts`.
- i18n: `helpTitle` / `helpHint` / `helpSearch` Kannada upgraded from stubs; added
  `helpFaqsTitle`, `helpNoResults`, `helpRelatedServices`, `helpHelplines*`, `helpCounters*`
  (en + kn).
- Verified: `/help` renders populated categories + helpline tiles in EN and KN (Kannada via
  the self-hosted font, no clipping), search narrows across categories, `/` (Chat) still
  renders. `npm run build` + `tsc` + `oxlint` clean; i18n parity clean apart from the
  pre-existing `navAiDiscoveryBetaShort` failure.

Original plan for reference:

**5. Populate `/help`**
- Decide the source of truth: seed rows behind `/api/v1/help/faqs` (backend), or a static
  bilingual JSON in `frontend/src/lib/`. Given the catalog already went DB-backed, prefer
  backend seed rows with a `category` column.
- Rebuild `Help.tsx`: categorised accordions (Certificates, Sakala timelines, Fees,
  Helplines, Counters), the 1902 / 112 / 108 helpline tiles, and a Grama One / Karnataka One
  counter note. Use the 5 Q&As drafted in the report §10.1 as the starting set, expand to ~25.
- Verify: `/help` renders populated categories in EN and KN with working accordion toggle.

### Phase 4 — Defense-in-depth & strategic — DONE 2026-09-07

**DEF-003 — validation envelope in ai_backend**
- `ai_backend/app/core/exceptions.py` — new `validation_exception_handler` for
  `RequestValidationError`, registered in `ai_backend/app/main.py`. Returns the same
  `{"detail": <first field error as a string>, "status": "error", "errors": [<full list>]}`
  shape the other handlers use, at 422, run through `jsonable_encoder`.
- `ai_backend/tests/test_validation_envelope.py` — isolated test (no RAG stack import needed);
  3 cases pass (missing body, blank field, valid pass-through).
- Still low urgency in practice: the bff already validates the only client-reachable path.

**DEF-008 — offline resilience for kiosks**
- `vite-plugin-pwa` (generateSW / Workbox) added to `frontend/vite.config.ts`:
  - Precache: app shell (`index.html`, hashed JS/CSS), the three self-hosted `woff2`, icons,
    manifest — 17 entries, verified in `dist/sw.js`.
  - Runtime: **only** `/api/v1/catalog/*` → `StaleWhileRevalidate` (1-day, 200 entries, 200-only).
    Every other `/api/*` path has no rule, so the SW always goes to network — no auth,
    citizen, application, track, grievance or operator response is ever cached.
  - `navigateFallback: /index.html` with a denylist of `/api/`, `/rag-api/`, `/ai-api/`,
    `/dashboard/` so those never get the SPA shell.
  - `injectRegister: 'script'` → external `/registerSW.js` (the citizen CSP has `script-src
    'self'` with no `'unsafe-inline'`); `devOptions.enabled: false`.
- `frontend/public/manifest.webmanifest` generated (name, short_name, `#164c93` theme,
  `#fafafa` bg, standalone, 192 + 512 icons — `icon-192.png` added via `sips` from the 512).
- CSP: added `worker-src 'self'; manifest-src 'self'` to the citizen policy in
  `Docker-deploy/nginx/nginx.conf` and `backend/SevaOne.Services/Program.cs`.
- Verified: `npm run build` emits `sw.js` + `workbox-*.js` + `manifest.webmanifest` +
  `registerSW.js`; precache list contains no API/sensitive entries; `vite preview` serves
  `/sw.js` at 200 `text/javascript`. **Not verified: live SW activation** — the in-app
  browser sandbox blocks `serviceWorker.register`. Confirm on a real Chrome via DevTools →
  Application → Service Workers and a Lighthouse PWA audit.

Original plan for reference:

**6. DEF-003 — validation envelope in ai_backend**
- Add `@app.exception_handler(RequestValidationError)` in `ai_backend/app/main.py` returning
  `{"detail": "...", "status": "error"}` to match the existing handlers' shape.
- Low urgency: the bff already validates the only path clients can reach.

**7. DEF-008 — offline resilience for kiosks**
- Add `vite-plugin-pwa` with a Workbox config: precache the app shell + fonts, runtime-cache
  the catalog API responses (`/api/v1/catalog/*`) stale-while-revalidate, and a web-app
  manifest (name, icons — `public/icon-512.png` exists, theme colour).
- Scope carefully: the citizen-facing app behind WSO2 APIM must not cache authenticated
  responses. Only cache the public catalog/help/static assets.
- Add an offline fallback route. Verify with Lighthouse PWA audit + devtools offline.

### Explicitly out of scope (verified not real)
- DEF-002 (try_files) — already present.
- DEF-006 (grievance upload pre-check) — no uploader exists; only relevant if one is added.
- DEF-007 (mobile bar occlusion) — `pb-24` gutter already handles it.
- The 765-service and 65-case "PASS" matrices — not real evidence.

## Suggested effort

| Phase | Items | Estimate |
|-------|-------|----------|
| 1 | DEF-001, DEF-005, branded 404 | ~0.5 day |
| 2 | DEF-004 catalog URLs | 1–3 days (data-dependent) |
| 3 | §10.1 Help content | 1–2 days (needs content sign-off) |
| 4 | DEF-003, DEF-008 | 1–2 days |
