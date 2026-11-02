# SevaOne Mobile — Rebuild Plan (v1, 2026-09-21)

Status: **owner approved the plan on 2026-09-21. M0 (app foundation) is DONE; Track B (Codex) can start (see [`TRACK_B_CODEX_PLAN.md`](./TRACK_B_CODEX_PLAN.md)).** Running state is in [`SESSION_HANDOFF.md`](./SESSION_HANDOFF.md) (this folder);
durable rules for agents are in `apps/Seva-Sindhu-One/mobile/AGENTS.md`.

Current auth decision (2026-10-02): ServicePlus credentials are the sole mobile first-sign-in method. The Python backend issues SevaOne JWTs and creates or reuses the citizen row. DigiLocker is an optional, separate link from Home for document access; the OTP-first-sign-in section below is historical. English is now the default language.

## 1. Goal and ground rules

Rebuild the citizen mobile app from scratch, **one feature at a time**, for **Android and iOS** from one
codebase, in `apps/Seva-Sindhu-One/mobile`.

- `apps/Seva-One/sevaone-mobile-app-legacy` is a **read-only reference** (Expo SDK 51, all mock data).
- Layout is **60-70% like the reference** (four tabs, Home composition, status stepper idioms); the visual
  language (colour, type, radius, shadows) comes from the **SevaOne web frontend** (`apps/Seva-One/frontend`),
  not Seva Sindhu's frontend.
- English default (`en`), with Kannada (`kn`) available.
- Package / bundle id: **`in.gov.karnataka.sevaonesevasindhu`** (Android package and iOS bundle identifier).
- **The app never connects to the .NET backend, its gateway or WSO2 in any manner.** Every API the app uses is served by the Python backend (`apps/Seva-Sindhu-One/backend`) from the Postgres database `sevasindhu_mobile`. If a feature seems to need something that exists only in the .NET stack, build the equivalent in Python; .NET code may be read as a reference for behaviour only.
- **No WSO2 anywhere.** ServicePlus verifies the initial credentials through the Python backend, which issues our JWT; DigiLocker (via CeG) can then be linked for documents.
- **Nothing leaves the app:** every external page (DigiLocker, department apply URLs) opens in an in-app
  WebView with no address bar. No Chrome / Safari hand-off.
- Plan first, then implement; two parallel agent tracks; handoff updated every session.

## 2. Stack

| Concern | Choice |
|---|---|
| Framework | **Expo (managed)**, latest stable SDK (npm shows `expo` 57), TypeScript strict. Dev builds only if a native module forces it. |
| Routing | Expo Router (file-based; mirrors the web routes `/services`, `/track`, `/help`...) |
| Server state / client state | TanStack Query / zustand (same as web frontend) |
| i18n | i18next + `expo-localization` **plus an `Intl.PluralRules` polyfill** (the reference app shows raw plural keys on Android without it) |
| Styling | Plain `StyleSheet` + `theme/tokens.ts` |
| Fonts | DM Sans, Space Grotesk, Noto Sans Kannada (`@expo-google-fonts/*`) |
| In-app browser | `react-native-webview` in a custom full-screen screen (section 7) |
| Tokens at rest | `expo-secure-store` (Keychain / Keystore). Never AsyncStorage. |
| Quality | `tsc --noEmit`, `expo lint` (ESLint with `eslint-config-expo`, the Expo-supported toolchain), Jest (`jest-expo`) + `@testing-library/react-native` |

**Expo vs bare React Native:** Expo. `expo prebuild` is the escape hatch if a native module is ever needed.

## 3. Design tokens (from the SevaOne web frontend, oklch converted to sRGB hex)

primary `#5553D2`, primaryDark `#4137A6`, navy `#1A1C37`, green `#00AD82`, gold `#EFB146`, saffron `#F67D51`,
background `#FDFDFF`, foreground `#131524`, secondary/muted `#F2F3FC`/`#F3F4FA`, mutedForeground `#5F6275`,
accent `#CAF5F2`, destructive `#D73240`, border `#E5E6EC`, input `#EAEBF1`. Radius base 16, soft layered shadows,
Space Grotesk headings, DM Sans / Noto Sans Kannada body. Conversions are approximate; eyeball on device.

## 4. Data: the Postgres database (DONE 2026-09-21)

The offline SQL Server dump (`apps/Seva-Sindhu-One/backend/db_dump/*.json`, 30 tables, gitignored because it holds PII)
is loaded into a **new database `sevasindhu_mobile`** on the existing docker Postgres (`sevaone-postgres-1`, network
`sevaone-net`). Only that database was created; `sevaone`, `platform`, `postgres` and every `wso2*` database, all roles and all
settings are untouched. Loader: `apps/Seva-Sindhu-One/backend/scripts/import_dump_to_postgres.py` (re-runnable with `--recreate`,
which only ever drops `sevasindhu_mobile`). Every table's row count was checked against the dump.

- Schema `sakala` = faithful read-only copy of the source tables, lower-cased names (`sakala.sakala_master_service`,
  `sakala.nodal_service_master`, ...). Three were renamed: `_` to `legacy_service_raw`, `bak` to `legacy_service_bak`,
  "Nodal officer details-Department - Master service list" to `nodal_service_master`.
- Schema `app` (to be created by the backend's own migrations, milestone B2) = tables we own: citizens, OTP challenges,
  refresh tokens, audit.
- **The dump has no citizen table.** `login_otp` and the `*_users` tables are officer/enumerator data. So citizens
  start empty; existing SevaOne citizens (`sevaone.portal.sevaone_user`) are deliberately not copied.

**Refreshing the data:** the dump is a point-in-time snapshot. To refresh it, run `apps/Seva-Sindhu-One/backend/scripts/dump_db.py` on a network that reaches the office SQL Server, then `python3 apps/Seva-Sindhu-One/backend/scripts/import_dump_to_postgres.py --recreate`. Column names are stable, so the API needs no change. One caveat: the catalogue queries are written in Postgres SQL, so reading the original SQL Server *directly* would need those queries translated; re-importing keeps them working as they are.

### Catalogue hierarchy (verified against the loaded data)

Sub-departments **do exist**, but only in `sakala.nodal_service_master` (2,410 service rows: `department`,
`sub_department_name`, `service_name`, `url`, `url_type`, `dept_code`, `service_id`). `sakala_master_service` has
services and rich detail but no sub-department column.

| Fact | Number |
|---|---|
| Department names in the table | 50 raw, **44 real** (case variants such as `HOME DEPARTMENT` / `Home Department` / `Home department` are the same department) |
| ...with more than one real sub-department | 35 (before the case merge) |
| ...with none/one (skip the sub-department level) | 15 (before the case merge) |
| `dept_code` values | 161 (one department appears under several codes: **`dept_code` is not a department key**) |
| Service rows | 2,410 (2,356 active, 54 with null flag) |
| Rows with an apply URL | 2,120 (290 empty) |

Rules the catalogue API must apply:
0. **Group departments by `lower(trim(department))`, never by `dept_code`.** Display the most frequent variant that is not ALL CAPS.
   The department `id` in the API is the slug of that display name (`revenue-department`). Sub-departments are also grouped case-insensitively.
1. **Hierarchy source** = `nodal_service_master`. Department page lists the 44 departments with a search bar; tapping one
   shows its sub-departments; tapping a sub-department shows its services; tapping a service shows detail + **Apply**.
   If a department has 0 or 1 real sub-department, skip straight to its services.
2. `sub_department_name = 'NULL'` (43 rows, the literal string) means no sub-department.
3. **Kannada department names: exact English-name match only** (`sakala_master_department.department_name_en` = the department name, case/whitespace-insensitive). **Never join on `dept_code`**: it identifies an
   *agency under* a department (`IGR` = Stamps and Registration, `SSLR` = Survey and Land Records), so a dept_code join gave the Revenue Department the name of one of its agencies (found and fixed during B1).
   **Do not use `sakala.sevaone_departments.department_name_kn`** either: it holds *service* names. Result: **13 of 44 departments have a verified Kannada name; the other 31 fall back to English** until the Kannada content team supplies them.
4. Service Kannada names and detail (definition, eligibility, documents, procedure, fee, delivery) come from
   `sakala_master_service` joined on `dept_code` + `service_code`. Only about 918 of the joined services have a Kannada name, so fall back to English.
5. **URLs are stored without a scheme** (`sevasindhuservices.karnataka.gov.in/directApply.do?...`; only 42 start with `https://`).
   Normalise to `https://` before use. Empty URL means the Apply button is disabled with "Not available online".
6. `url_type` says what the link is: `Sevasindhu` (777), `Department (deeplink)` (930), `Department (homepage)` (381), none (322). Label
   homepage links honestly ("Opens the department website").

## 5. Backend (the Python FastAPI service in `apps/Seva-Sindhu-One/backend`)

The owner has directed that this service is extended for mobile. Changes are **additive**: new modules under a
`/api/v1/mobile/*` prefix, a Postgres connection alongside the existing SQL Server one, and minimal edits to `main.py` to mount routers.
Existing routes keep working. SQL Server access stays as-is (unreachable from home, and no longer needed for mobile).

Proposed catalogue contract (Track A builds, Track B consumes with fixtures):

| Route | Returns |
|---|---|
| `GET /api/v1/mobile/catalog/departments?q=` | `[{id, name, nameKn (mostly null), subDepartmentCount, serviceCount}]` |
| `GET /api/v1/mobile/catalog/departments/{id}/subdepartments` | `[{name, serviceCount}]`. Empty means skip the level. May end with `{name: "_other"}` = services with no sub-department (show a localised "Other services"; pass `sub=_other` to the services route) |
| `GET /api/v1/mobile/health` | `{ok, services, departments}` |
| `GET /api/v1/mobile/catalog/departments/{id}/services?sub=&q=` | `[{id, name, nameKn, subDepartmentName, hasApplyUrl}]` |
| `GET /api/v1/mobile/catalog/services/{id}` | detail fields plus `applyUrl` (already `https://`-normalised), `urlType` |

## 6. Login (owner's design, with my review) — JWT, no WSO2

Flow, copied from the .NET `PortalAuthService` (`StartMobileLoginAsync` / `VerifyMobileLoginAsync`) minus WSO2:

1. `POST /api/v1/mobile/auth/otp/start {mobile}` -> 6-digit OTP, sent by SMS (Mobile Seva gateway, same as .NET), returns `{challengeId}`.
2. `POST /api/v1/mobile/auth/otp/verify {challengeId, otp}`:
   - citizen exists **and is DigiLocker-linked** -> `{found:true, accessToken, refreshToken}` -> dashboard;
   - otherwise -> `{found:false, digiLockerPendingId}` -> DigiLocker step.
3. DigiLocker (CeG gateway, as .NET `DigiLockerClient`): `POST .../auth/digilocker/start {pendingId}` returns a redirect URL; the app opens it
   **in the in-app WebView**, intercepts the navigation to the registered callback URL (carrying `status`, `txnid`, `appparam`), does
   not load that page, and posts those to `POST .../auth/digilocker/complete`. The backend fetches the profile, upserts the citizen, returns tokens.
4. Session = short-lived access JWT (15 min) + rotating refresh token (hashed in DB, revocable), both in `expo-secure-store`.

Hardening required beyond the .NET copy (the .NET code has gaps we must not port):
- .NET **logs the OTP in plaintext**; we log nothing. A dev-only flag may return a fixed OTP when SMS is not configured; it must be off outside dev.
- .NET stores an unsalted SHA-256 of a 6-digit OTP (trivially reversible if leaked). We store an **HMAC with a server secret**, add an **attempt limit (5)**, a resend limit per mobile, single use, 10-minute expiry, and constant-time compare.
- The .NET SMS sender's request-signing algorithm is documented there as an **unverified guess**. Real SMS from Python needs the gateway's confirmed algorithm; until then use the dev OTP flag.
- JWT secret from environment (never committed); reject `alg=none`; short access lifetime; refresh rotation with reuse detection.
- Never attach our JWT to a WebView page or a third-party request; department sites get no credentials from us.

### 6b. What was built and verified (B2)
Backend `apps/Seva-Sindhu-One/backend`: `mobile_auth.py` (routes below `/api/v1/mobile/auth`), `security.py`, `sms.py`, `digilocker.py`, `migrate.py` + `migrations/001_app_schema.sql` (tables `app.citizen`, `otp_challenge`, `digilocker_pending`, `refresh_token`, `audit_log`; the Aadhaar number and its hash are never stored, only `XXXX-XXXX-1234`). 44 backend tests (unit + end-to-end against the real database with CeG stubbed) pass; the DigiLocker request signing reproduces CeG's own documentation sample. App: `src/api/authApi.ts`, `src/lib/auth.ts` (refresh token in the keychain, single-flight refresh because a replayed rotated token ends the session), 78 app tests pass.
Verified live on the emulator: OTP start/verify, wrong-code attempt counter, the **real CeG gateway returning the real DigiLocker sign-in page, opened inside the app**, and the callback interception (a non-success redirect was captured, never loaded, and the backend refused it). **Not yet verified: a successful DigiLocker login** (needs a real Aadhaar/DigiLocker account; the owner should try it at `/dev-login`), and **real SMS delivery** (local stack runs in dev-OTP mode; set `SEVASINDHU_OTP_DEV_MODE=0` in `docker-deploy/.env` and recreate the backend to send real SMS).
**CeG's redirect goes through its own plain-http gateway.** DigiLocker's `redirect_uri` is `http://103.138.197.55/.../GetTokenAPI` (CeG is the OAuth client) and CeG then redirects on to our callback. So the in-app browser allows that one exact prefix over http (the backend names it in `cleartextAllowedPrefixes`; everything else http stays blocked). For release builds: iOS App Transport Security is documented as not applying to raw IP addresses (confirm on the iPhone); **Android release builds need a network-security exception for `103.138.197.55`** (Expo Go already permits cleartext). Ask CeG for an https endpoint. This is the same exposure the SevaOne web portal has today.

## 7. In-app browser (DigiLocker and department apply pages)

One reusable `InAppBrowser` screen built on `react-native-webview`: full-screen, custom header (title, back, close, reload, progress bar),
**no address bar**. Rules: same-window for `target=_blank`/`window.open`; allow only `http(s)`; block `intent:`/`file:`/custom schemes;
Android back button goes back in page history, then closes; loading, offline and error states in our styling.
**My recommendation (your call):** keep the URL hidden, but let a tap on a small lock icon show the read-only domain. Without any way to see the domain, a
citizen cannot tell a genuine government page from a look-alike, which matters most on the DigiLocker sign-in page.
**Built and verified (M2):** https-only policy, new windows stay in the view, DigiLocker-style redirect interception, lock shows only the domain, blocked-link notice, offline/load/certificate error states, Android back. Try it by hand at route `/dev-browser` (dev builds only).

**ServicePlus "get our mobile app" page -- root cause and fix (superseding an earlier, wrong diagnosis):** department Apply links (`sevaone.karnataka.gov.in/services/...`) showed a "download our mobile app" promo instead of the real form. Several hypotheses were tried and disproven in turn -- User-Agent string (curl with a desktop vs. mobile UA got byte-identical responses), session-cookie continuity across ServicePlus's own GET-then-JS-auto-POST redirect chain (curl proved cookies alone were sufficient), Chromium's separate User-Agent Client Hints API, stale WebView disk cache. The screen was changed to non-`incognito` plus a desktop-UA/Client-Hints spoof chasing these, none of which fixed it. Chrome DevTools remote debugging of the on-device WebView (`WebView.setWebContentsDebuggingEnabled`) finally caught the real cause: Android's WebView adds an `X-Requested-With: in.gov.karnataka.sevaonesevasindhu` header to every request, invisible from React Native, and ServicePlus checks specifically for that header -- confirmed with curl (identical request, only that one header added, same promo page back every time). Android's only API to suppress it (`WebSettingsCompat.setRequestedWithHeaderOriginAllowList`) is dead: Google's current docs say the origin trial behind it has ended and the method "no longer does anything." **Fix:** `lib/browser.ts` now opens any Apply-style request (no `intercept` rule) in a Chrome Custom Tab (`expo-web-browser`, `createTask: false` so it stays in the app's own task) instead of the in-app WebView -- a Custom Tab runs in Chrome's own process and never sends that header. The in-app WebView (`app/browser.tsx`) is now used only for DigiLocker's `intercept`-based sign-in flow, which needs to inspect and cancel a navigation before it loads (something a Custom Tab cannot do); it was reverted to `incognito` (every DigiLocker visit starts clean) and the `@preeternal/react-native-cookie-manager` cookie-clearing added while chasing the wrong fix was removed as unnecessary. The desktop-UA/Client-Hints spoof in `browserPolicy.ts` is kept (harmless, and DigiLocker or a future WebView-loaded page may still benefit from a desktop layout) but its comments now say plainly that it did not fix ServicePlus.
Also still true from the earlier investigation: **the Seva Sindhu apply links (783 of the 2,120 catalogue URLs, the largest group, host `sevasindhuservices.karnataka.gov.in`) present a TLS certificate for `sevaone.karnataka.gov.in` only** (seen identically from three DNS resolvers) -- this is a separate, server-side certificate mismatch unrelated to the X-Requested-With issue, unaffected by the Custom Tab fix (Chrome will show its own certificate-error page for these, same as any browser would), and previously an owner decision to ignore since nothing can be fixed in this app's code for it.
**Two things found while testing on the emulator:** (1) react-native-webview applies `originWhitelist` *before* our policy and hands any URL that fails it to `Linking.openURL`, so a redirect to an http:// page opened Chrome. The fix is `originWhitelist={["*"]}` so our policy decides every URL; a test locks it in. (2) **The Seva Sindhu apply links (783 of the 2,120 catalogue URLs, the largest group, host `sevasindhuservices.karnataka.gov.in`) cannot load: that server presents a TLS certificate for `sevaone.karnataka.gov.in` only**, seen identically from three DNS resolvers. The app correctly refuses it and shows "security certificate is not valid". We never bypass certificate checks. **Owner decision: ignore.** The dump is a snapshot; the latest links come with the original DB and its column names are unchanged, so nothing to do in code.
**Known risks still to test:** file uploads and certificate PDF downloads inside WKWebView (iOS) are weaker than Android; some department sites
have poor mobile layouts or break in embedded WebViews; the CeG DigiLocker page must be verified to work embedded on both platforms.

## 8. Milestones

| # | Milestone | Track |
|---|---|---|
| **M0** | **DONE 2026-09-21.** App foundation in `apps/Seva-Sindhu-One/mobile`: Expo SDK 57 scaffold, tokens, fonts, i18n + plural polyfill, tab shell, API client, catalogue API layer with fixtures, lint/test | A |
| **B1** | **DONE 2026-09-21.** Backend: Postgres connection + catalogue API (section 5), 16 tests, live in the docker stack on :5094; the app now defaults to the live API | A |
| **M1** | Catalogue screens: departments (search) -> sub-departments -> services -> detail + Apply | B (fixtures first, then B1) |
| **M2** | **DONE 2026-09-21.** In-app browser (`src/app/browser.tsx`, `lib/browser*.ts`) verified on the emulator; see section 7 | A |
| **B2** | **DONE 2026-09-21** (success path awaits a real DigiLocker login by the owner). Backend auth: `app` schema migrations, OTP, JWT, refresh, DigiLocker start/complete; app-side `@/lib/auth` | A |
| **M3** | Login screens (mobile, OTP, DigiLocker-in-WebView) + secure token storage + dashboard shell | B screens, A wiring |
| **M4** | Home dashboard (greeting, search, carousel, categories, quick services) | B |
| **M5** | **DONE 2026-09-21** (real spoken-word accuracy untested: the emulator microphone records silence). AI assistant + voice: `@/lib/assistant`, `@/lib/voice`, `@/lib/chatMarkdown`, catalogue lookup endpoint. Screens are Track B tasks B-8/B-9 | A |
| **M6** | Profile update, feedback and Sakala application tracking endpoints; grievance and notifications were parked here and are now planned as M10 and M12 (section 8c) | A, then B (B-7) |
| **M7** | iOS distribution path, accessibility, Kannada review, hardening (push delivery moved to M10) | both |
| **M8** | Personal library: favourites, recent services synced to the account, Applications v2 (persistent, uncapped, multi-source). Section 8c, phase P1 | A backend/api/lib, B screens (B-17 to B-19) |
| **M9** | Nearby service centres: table, API, screens, 2 sample centres (real data seeded later). Phase P2 | A, B (B-23) |
| **M10** | Notifications: in-app inbox, device tokens, push adapter (direct FCM/APNs, no relay), one native rebuild. Phase P3 | A, B (B-20) |
| **M11** | DigiLocker documents: storage adapter, MinIO, document cache, stub/live gateway (CeG call swapped in later). Phase P4 | A, B (B-21) |
| **M12** | Grievances: form, storage, attachments, status history, gateway adapter (external submission later). Phase P5 | A, B (B-22) |
| **M13** | **Later:** NIC ServicePlus login/SSO and ServicePlus application tracking; real CPGRAMS/Karnataka grievance submission; real centre data; live CeG document download; FCM/APNs credentials | A |

Each milestone ends with typecheck + lint + tests green, run on the Android emulator, and verified on the iPhone for iOS-affecting changes.

## 8b. M6: scope decided by the owner (2026-09-21)

**Python only, nothing calls .NET.** New routes live under `/api/v1/mobile/`, new tables in schema `app`, protected by the same access token (`current_citizen`).

| Area | Decision | What we build |
|---|---|---|
| Guest access | **Decided: guests browse; sign-in for the assistant** | Enforced server-side: `POST /api/v1/mobile/assistant/message` and `/asr` need a session and are capped per citizen per hour (`app.usage_event`, migration 003). The old public `/api/v1/ai-chat/message` and `/api/v1/language/asr` stay for the Seva Sindhu web site. Codex's B-11 changes the app to match. |
| Profile and settings | **Build now** (small) | `GET /auth/me` (exists), `PATCH /auth/me` (preferred language, email). Name, date of birth, gender and Aadhaar last-4 stay read-only: DigiLocker verified them. |
| Feedback | **Build now** (Codex's B-7 form needs it; the old `/api/feedback` writes to the unreachable SQL Server) | `POST /feedback` into new table `app.feedback` (migration 002); attaches the signed-in citizen automatically. |
| Applications and status | **Build now** (owner request 2026-09-22); **v2 planned in M8** | Guests can track a 15-digit Sakala GSC number through Python; signed-in citizens can save the resolved record to My Applications (latest five today; M8 removes the cap, stores a status snapshot, adds delete and a `source` column so ServicePlus applications can be tied to the user later). |
| Grievance | **Planned in M12: built, external submission stubbed** | Form fields and the logged-in user are stored in our database and shown in the app; only the call to the external grievance system (CPGRAMS or a Karnataka one, undecided) is left for later. |
| Notifications | **Planned in M10: inbox now, push provider pending** | In-app inbox with unread badge and preferences; device tokens stored; push sent directly from our backend once an FCM project (Android) / Apple key (iOS) exists. |

## 8c. Wave 2 (planned 2026-09-27): gaps found against UMANG

A government contact compared us with UMANG (favourites, recently used, nearby centres, push, grievances, DigiLocker documents). Owner's rule: **build everything now; where real data or an API is missing, keep dummy data in the database and show it in the app; only the external API call is left for later.** ServicePlus login/SSO is postponed (M13).

| Feature | Milestone | What is built now | What stays for later |
|---|---|---|---|
| Favourites | M8 | `app.favourite_service`; heart on service detail; Favourites screen; local for guests, merged into the account at login | none |
| Recent services | M8 | `app.recent_service` (server keeps 20, app shows 5); local store kept as guest fallback; cleared on sign-out | none |
| Application tracking | M8 | `app.application` gains `source`, `external_ref`, `service_name`, `status`, `status_snapshot`, `last_checked_at`; no cap; delete; pull-to-refresh | ServicePlus tracking API (`source='serviceplus'`) |
| Nearby centres | M9 | `app.service_centre` (lat/lng, type, district/taluk, phone, hours, `is_sample`); nearby and list APIs; 2 sample rows; screen with distance, filters, Call; no map link in v1 | real dataset; a self-hosted map |
| Notifications | M10 | `device_token`, `notification`, `notification_pref`; inbox, unread badge, mark read; `/notifications/test` and an admin endpoint; triggers for application status change, grievance status, document ready | FCM project / APNs key and switching `PUSH_PROVIDER` on |
| DigiLocker documents | M11 | `storage.py` adapter (MinIO/local/memory), private bucket, server-proxied download with cache, `document_gateway.py` with `stub` (sample docs for the demo citizen) and `live` modes, structured JSON/XML viewer | CeG enabling `GetDocumentFromDigiLocker` (currently `-2`); PDF viewer (native dependency, flagged off) |

**ServicePlus citizen DigiLocker linking (2026-10-02):** Home Quick Access opens `/digilocker`. A signed-in citizen with `digilockerLinked=true` sees `GET /api/v1/mobile/auth/me/digilocker/documents`; an unlinked citizen explicitly starts a CeG sign-in inside the app. `POST /api/v1/mobile/auth/me/digilocker/link/start` creates a short-lived, one-use pending link bound to the current SevaOne JWT citizen and returns `pendingId`, `redirectUrl`, `callbackUrl` and `cleartextAllowedPrefixes`. The app intercepts the callback and posts `{pendingId,status,txnid,appparam}` to `/link/complete` with the same citizen's JWT. The backend resolves the DigiLocker ID from CeG, refuses cross-account links, updates that existing citizen row, and returns the refreshed `AuthCitizen`; it does not issue another SevaOne session or create a second citizen. A guest is sent through the normal ServicePlus sign-in first. The document-list API remains separate from file-content availability (`DIGILOCKER_DOCUMENT_MODE`).
| Grievances | M12 | `app.grievance` (+attachments, status history), submit-once via idempotency key, form with the citizen attached, list/detail with honest "forwarding pending" status, attachments in storage | `grievance_gateway` for the chosen system (`GRIEVANCE_GATEWAY`), officer status feed |

**Cross-cutting rules for wave 2:** push payloads carry no personal data (generic title plus `{notificationId, kind, route}`; the app fetches the text from the inbox); documents and attachments are served only through the backend (no presigned URLs, `Cache-Control: no-store`, JWT checked on every download); deep links from notifications use a client-side route whitelist, never a raw URL; account deletion (section 8b) now also deletes the citizen's grievances and purges their storage prefixes `documents/{id}/` and `grievances/{id}/`, retrying failures through `app.storage_purge`; new native packages (`expo-notifications`, `expo-location`) are added in one rebuild during M10.

**Stubs to swap later:** `DIGILOCKER_DOCUMENT_MODE=stub|live`, `GRIEVANCE_GATEWAY=stub|cpgrams|karnataka`, `PUSH_PROVIDER=none|fcm|apns`, `STORAGE_BACKEND=minio|local|memory`, the sample rows from `scripts/seed_sample_centres.py`, and the `source='serviceplus'` application path.

**Applications implementation:** Sakala offers a public tracking API, `POST https://sakala.kar.nic.in/ack_api/api/Gscmobile/GetSakalaackdata` with `{gscno}` (no authentication; an unknown number returns HTTP 500 with a database error text, which the Python API maps to "not found"). The Python routes are `POST /api/v1/mobile/applications/track` for guest lookup, `POST /api/v1/mobile/applications` for an authenticated citizen to save a resolved GSC, and `GET /api/v1/mobile/applications` for the latest five saved numbers. The mobile Applications tab provides the GSC form and renders the mapped Sakala applicant, service, department, dates, pending days and status. The .NET `SakalaClient.cs` remains a behaviour reference only; the mobile app never calls .NET or Sakala directly.

**Release requirement to remember (M7):** Apple requires an in-app "delete my account" for apps that let people create an account (App Store guideline 5.1.1(v)). **Built:** `DELETE /auth/me` removes the citizen, their tokens and their personal data (feedback stays anonymous); the settings screen (B-7) exposes it.

## 9. Tracks and working agreement

| | **Track A: Claude** (backend + plumbing) | **Track B: Codex** (mobile UI) |
|---|---|---|
| Owns | `backend/**` additions, `mobile/src/api/**`, `mobile/src/lib/**`, InAppBrowser, auth wiring, root layout | `mobile/src/components/**`, `mobile/src/theme/**`, screen routes, i18n text |
| i18n keys | `auth.*`, `browser.*`, `error.*` | `home.*`, `catalogue.*`, `common.*`, `login.*`, and for wave 2 `favourites.*`, `notifications.*`, `grievance.*`, `centres.*`, `documents.*` |

- **M0 is Track A alone**; Track B starts after it lands.
- Contract-first: shared types in `mobile/src/api/types.ts` (A owns); change contracts in this plan first.
- **One shared working folder, no per-track branches** (the mobile folder is not committed yet, so worktrees would not contain it, and switching branches would change files under the other agent). Safety comes from strict path ownership. Never run `git checkout`, `switch`, `stash`, `reset`, `clean` or `commit`: the owner commits. Don't edit the other track's paths; ask via the handoff.
- Append-only `en.json`/`kn.json` within your own key prefix. Kannada is machine-drafted until a native-speaker review (M7).
- No secrets in the repo or handoff. `EXPO_PUBLIC_*` values are public by design, so never put a secret in one.
- End every session by updating [`SESSION_HANDOFF.md`](./SESSION_HANDOFF.md).

## 10. Owner decisions and consequences

| # | Decision (2026-09-21) | Consequence |
|---|---|---|
| D1 | Login = our own JWT + OTP + DigiLocker on the Python backend and the new database; no WSO2. Catalogue from the imported dump. | Backend gets Postgres + auth modules (sections 5, 6). All users start via DigiLocker sign-up because no citizens were copied. |
| D2 | Catalogue shown as department -> sub-department -> services -> detail -> Apply, all in-app. | Section 4 rules; Apply uses `InAppBrowser`. |
| D3 | Test on an iPhone SE (2020). | See section 11. |
| D4 | Folder `apps/Seva-Sindhu-One/mobile`; id `in.gov.karnataka.sevaonesevasindhu`; docs in `docs/mobile`. | Adopted throughout. |
| D5 | (2026-09-27) A citizen's personal library (favourites, recent services) lives on the account; guests keep it locally and it is merged at login and cleared from the device at sign-out. | Migration 005; merge endpoints; `lib/librarySync.ts`. |
| D6 | Documents are fetched by the backend, cached in private object storage and served only through the backend (no presigned URLs). The CeG call sits behind a `stub`/`live` gateway. | Migration 009; `storage.py`; `document_gateway.py`. |
| D7 | Notifications are inbox-first; push is sent directly from our Python backend (FCM, later APNs), never through Expo's relay, and carries no personal data. | Migration 008; `push_sender.py`. |
| D8 | Grievances are stored in our database and forwarded through an adapter; the external system is undecided, and the app says honestly when forwarding is pending. | Migration 010; `grievance_gateway.py`. |
| D9 | Service centres live in our own table with a sample flag. No map link in v1 (opening a map site would tell a third party which centre a citizen looked at); a self-hosted map can follow. | Migration 007. |
| D10 | Applications are multi-source (`sakala` now, `serviceplus` later) with a stored status snapshot, no cap. | Migration 006. |
| D11 | Object storage is MinIO running as a binary under supervisord on the GPU server (no Docker there), Docker MinIO for development, behind a storage adapter so the host is a config change. `STORAGE_BACKEND=local` is the fallback. | `docker-deploy` compose files for dev; env `S3_*`. |
| D12 | Delete account stays (store requirement) and now also removes grievances and stored files. | Section 8b; purge in `delete_account`. |

## 11. Devices and local environment

- **iPhone SE (2020, 2nd gen)**: A13 chip, 4.7-inch, 375x667 pt, home button, no notch. It is fine for Expo Go. Its small screen is
  the design constraint: every screen must work at 375x667 with Kannada text (which is taller than Latin), so we test on a small AVD as well as the phone.
  The phone needs iOS new enough for the current Expo Go (I will confirm the exact minimum when scaffolding; check Settings > General > About).
  It reaches this PC over Wi-Fi, so the API base URL must be this PC's LAN IP, not `10.0.2.2` (the Android emulator alias). Expo's dev server uses port 8081 and the backend 5094.
- Android: Android Studio 2026.1.4 in `~/Applications/android-studio`; SDK in `~/Android/Sdk`; AVD `Medium_Phone`; KVM works.
- Node 24.19.0. No system JDK (Studio's bundled JBR is Java 25); Expo Go needs none. Local native builds would need JDK 17/21 (`sudo apt install openjdk-17-jdk`).
- Release path for iOS (TestFlight/App Store) needs an Apple Developer account and either a Mac or Expo's cloud build service (uploads source to Expo). Deferred to M7.
