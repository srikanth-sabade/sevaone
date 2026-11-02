# ServicePlus Integration — Full Investigation Log

**Scope:** every distinct thing tried while investigating the ServicePlus (Karnataka tenant) integration for SevaOne, in the order it happened, with the exact method and result for each. Companion to `SERVICEPLUS_API_INVENTORY.md` (endpoint reference + security concerns), `SERVICEPLUS_NIC_BLOCKERS.md` (blockers for NIC), `SERVICEPLUS_NIC_URL_HANDOFF.md` (full contract checklist), and `serviceplus_full_session.postman_collection.json` (runnable collection of every request below).

**Test account:** our own real citizen ServicePlus account, Karnataka tenant, `https://sevaone.karnataka.gov.in/services/`.

---

## 1. Reference app setup

- Installed the Flutter SDK and got `apps/eserviceplus-master` (NIC's own reference Android app) building and running on an emulator.
- Fixed a JDK version conflict: Gradle 8.10.2 is incompatible with the JDK 25 bundled with Android Studio; pinned to JDK 17 (`~/.gradle/jdks/eclipse_adoptium-17-amd64-linux.2`) via `flutter config --jdk-dir`.

## 2. API surface documented

- Read every network call in the app source (`lib/services/api_service.dart`, `lib/services/common_service.dart`, plus WebView-loaded URLs in several screens) and produced a complete endpoint inventory: 16 JSON APIs + 5 HTML pages loaded in-app, with exact URL templates, parameters, and auth requirements. Result: `SERVICEPLUS_API_INVENTORY.md`.
- Traced the session mechanism: `device_token` (device id), `user_token` (`uuid.v5(NAMESPACE_URL, device_token)`, client-generated, not server-issued), `signNo` (server-issued at login). Confirmed via `Utility.generateToken()`.
- Confirmed no API anywhere in the client creates an `applId` — it is only ever read back from existing records (`RevalidatePaymentApplication`, `IncompleteApplications`, `UserApplications`) or passed into calls that operate on an existing application.
- Confirmed `EService.dart` (the `getServiceJson` model) has no `applId` field — only `serviceId`, `deptName`, `stateName`, `label`, etc. `serviceId` identifies *which service*; `applId` identifies *one specific application instance*, and they are never interchangeable.

## 3. NIC outreach drafted

- Drafted a message requesting a citizen registration API, server-side login confirmation, and WebView approval. Result: `SERVICEPLUS_REGISTRATION_API_QUERY.md` (includes a sequence diagram of the proposed flow). Not yet sent.

## 4. Live verification of the confirmed base URL

- NIC confirmed `https://sevaone.karnataka.gov.in/services/` as the Karnataka tenant base URL (superseding the national registry's self-referencing entry, see §12).
- Verified live with curl: `GET /services/` (200, real ServicePlus app), `GET /services/citizenRegistration.html` (200, real registration page), `POST /services/validateUser` with a real account (200, genuine `LoginResponse` JSON — `userStatus:true`, real `signNo`, roles, `citizenState`).
- Confirmed the path is case-sensitive (`validateUser` works, `validateuser` 404s).

## 5. Empty-catalogue investigation (`getServiceJson` and family)

- Swept 11+ parameter combinations for `dept`/`states`/`instance_state` (including `dept=0`, values read from local-storage-equivalent fields, and the citizen's own `citizenState`) — result identical every time: empty `serviceJosn`.
- Ran a full sweep of every cataloged API against the real account in one session (`test_all_apis.sh`): confirmed `getAllApplRefNoByUser`, `getIncomleteApplList`, `getAllRevalidatePaymentApplication` are also empty, while role-gated endpoints (`getServiceList`, `getTaskList`) correctly return `userStatus:false`/denied for a Citizen-role session — proving auth and role-checking work correctly elsewhere on the same session, so the catalogue itself is what's empty, not the session.
- Identified the likely root cause: the reference Flutter client hard-codes `dept=0` with its own author's comment admitting it's a placeholder (`String instanceDept = "0"; //localStorage.read("instanceDept");`) — the real per-tenant department id was never wired up, and Karnataka's own `getAllStateUrl` doesn't supply one either (§12).
- **Definitive confirmation:** rebuilt NIC's own official reference APK (see §9) and logged into it with the real account on a real emulator — the official app's own Services screen is also empty. Rules out any mistake in how we replicated the request ourselves.

## 6. Apply-flow / applId investigation

- Traced the exact Flutter code path for the "Apply" button (`services_screen.dart`): constructs `renderApplicationForm.do?serviceId=&UUID={user_token}&OWASP_CSRFTOKEN={hard-coded literal}&applLimitFlag=N`, loaded in an in-app WebView.
- Tested `renderApplicationForm.do` with a real `serviceId`, fake session values → generic uncaught Java exception page (Error Code 2547182).
- Tested `renderApplicationForm.do` with a real `serviceId` and **zero session at all** → clean 302 redirect to `/services/error.do`, which reads: **"Session Invalidated. Either you have logged in from another system or your session has been expired... Relogin."** This pinpoints the actual requirement precisely: a server-side `JSESSIONID`-backed HTTP session, established only by a real browser login — not the JSON `validateUser` session's tokens.
- Tested whether the JSON login's session (`signNo`/`device_token`/`user_token`, and its `JSESSIONID` cookie) authenticates the HTML flow at all: reused the real cookie from a successful `validateUser` call against `directApply.do`/`renderApplicationForm.do` — still showed the login screen / session-invalidated response. Confirmed: **the two login worlds are separate; nothing bridges them.**
- Loaded `directApply.do?serviceId=1363` in a real browser: redirects to a genuine HTML login form (`loginWindow.do`) with a CAPTCHA. Inspected its DOM directly (read-only): fields are `id="userName" name="signNo"`, `id="password" name="passwd"`, `id="captchaAnswer" name="captchaAnswer"`; the page loads `forge.all.min.js` and `md5.js`, suggesting client-side JS encryption on submit (a separate wire format from `validateUser`'s AES-ECB scheme).
- Tested the two remaining WebView-loaded pages for completeness: `editApplicationForm.do` (dummy `applicationRefNo`) → HTTP 302, empty body (redirect target not followed); `mExportToPdfAck` (dummy `applId`/`spdiCurrentProcId`/`templId`) → HTTP 200, empty body. Both inconclusive without a real application behind them, but consistent with "no real data, no crash."
- Tested the manager's suggestion directly: pulled real `serviceId` values from our own catalogue database (not the empty `getServiceJson`) and hit `directApply.do`/`renderApplicationForm.do` with them. Result: identical to every prior test — both endpoints check for a valid session *before* looking at which service was requested, so no `serviceId`, however obtained, changes the outcome. Confirms the blocker is the missing session/`applId`, never the catalogue data.

## 7. WebView-embedding block

- Used Chrome remote debugging on the on-device WebView to find the true cause of a "download our mobile app" page appearing instead of real Apply forms: Android's WebView silently adds `X-Requested-With: <our package name>` to every request; ServicePlus checks for it and blocks unrecognized values.
- Confirmed live with curl: identical request, only that header differing, reproduces the block/bypass reliably. `X-Requested-With: XMLHttpRequest` is accepted.
- Found the reference Flutter app's own `webview_screen.dart` already sets this exact override via `InAppWebViewSettings`/request headers — the official client works around its own platform's anti-embedding check.
- Confirmed separately (for our own SevaOne mobile app, `react-native-webview`) that the one relevant Android API to control this header, `WebSettingsCompat.setRequestedWithHeaderOriginAllowList`, is dead — Google ended the origin trial, it is a documented no-op — hence SevaOne's Apply links use a Chrome Custom Tab instead of an embedded WebView.

## 8. Visible-prefill POC (CAPTCHA login)

- Explored a proposal to pre-fill the real login form's username/password fields (visible autofill, human still solves the CAPTCHA and submits) versus a scripted/background form submission with only the CAPTCHA relayed by a human — concluded the latter is a CAPTCHA-bypass-via-human-relay pattern regardless of how the credential is encrypted at rest, and is not something to build; the visible-autofill version remains valid and unbuilt pending a decision on where it should live (mobile WebView vs. the already-documented `X-Requested-With` block, §7).
- Confirmed the real login form's exact field selectors for this purpose (see §6): `#userName`/`#password`/`#captchaAnswer`.

## 9. Independent confirmation via the real reference app

- Patched the reference app directly: fixed a dead `Constants.baseUrl` path (`.../liveprod/`, confirmed 404 live) and hard-coded `common_service.dart`'s broken instance-dropdown to the NIC-confirmed Karnataka URL, since the national registry's own entry for Karnataka self-references (§12).
- Rebuilt the debug APK, installed on the emulator, logged in with the real account (real password typed by the account owner). Result: real login succeeds; the Services screen is empty — the same result as every API-level test, now confirmed inside NIC's own unmodified client logic.

## 10. Platform nature and comparison

- Confirmed via the live page footer that ServicePlus is a Government of India / NIC (MeitY, eSDT) platform, not proprietary commercial software.
- Checked Meghalaya's live ServicePlus deployment for comparison: same backend, same generic exception-page format, different frontend skin — confirms this is a shared, multi-tenant platform, not Karnataka-specific software.
- Searched the Play Store for an official NIC/Meghalaya ServicePlus app under several names and the exact package id — not found publicly listed.
- Searched for a Swagger/API documentation page on the platform — none found.

## 11. Public catalogue backend (separate discovery)

- Found a second, entirely separate backend at the bare domain root (`sevaone.karnataka.gov.in`, no `/services/` path) — a modern Vite/React SPA with its own public, unauthenticated `/api/` backend.
- Verified live: `GET /api/departments` → 200, 140 departments; full catalogue drill-down confirmed at ~1,789 services total. `GET /api/health` → 200, discloses backing SQL Server hostname/DB name/login (`EP0000_Cursor_Test` / `edcstestdb` / `CCS-DB-VP03`) — flagged as an information-disclosure item, not something to rely on.
- This is the real source of SevaOne's own service catalogue import — unrelated to, and unaffected by, the empty `getServiceJson` problem (§5), since that endpoint is citizen-specific application data, not a public catalogue browse.

## 12. Platform defects found and reported (not ours to fix)

- `getAllStateUrl`, called against Karnataka's own tenant, returns an entry that self-references back to `serviceonline.gov.in` instead of resolving to itself — reported to NIC (`SERVICEPLUS_NIC_BLOCKERS.md` item 4).
- `sevasindhuservices.karnataka.gov.in` (783 of the 2,120 URLs in our full catalogue, the single largest group) presents a TLS certificate issued for `sevaone.karnataka.gov.in`, confirmed identically from three independent DNS resolvers — flagged to NIC as an observation, not a blocker.

## 13. Security review of the reference app itself

Read the shipped source end-to-end and confirmed seven concrete issues, ranked by impact (full detail and file:line citations in `SERVICEPLUS_API_INVENTORY.md`):

1. TLS certificate validation disabled app-wide (`Utility.dart:28`), sitting under a committed `//TODO: Enable SSL in Production`.
2. The real ServicePlus password is persisted **unencrypted** on-device (`User.toJson()` includes plaintext `password`; written to GetStorage's plain JSON file via `common_service.dart:100`).
3. Login credentials are only obfuscated (AES-128-ECB, key hard-coded in the client), not really encrypted, before transmission.
4. The CSRF token sent with Apply-flow requests is a hard-coded literal, identical for every install.
5. Session tokens and full API responses (including application/PII data) are printed via `print()` throughout the app, not gated to debug builds.
6. `getCertificate()` has a real-looking sample credential set hard-coded in source, unrelated to the current session.
7. The app overrides `X-Requested-With` to defeat ServicePlus's own anti-embedding detection (§7).

## 14. Deliverables produced

- `SERVICEPLUS_API_INVENTORY.md` — full endpoint reference + security concerns.
- `SERVICEPLUS_REGISTRATION_API_QUERY.md` — draft NIC outreach for registration/SSO/WebView approval.
- `SERVICEPLUS_NIC_URL_HANDOFF.md` — full contract/URL tracking checklist for NIC.
- `SERVICEPLUS_NIC_BLOCKERS.md` — short, evidence-backed blockers summary for NIC.
- `serviceplus_full_session.postman_collection.json` — runnable collection of every request tested (23 requests, 6 folders, self-contained login).
- Rebuilt NIC reference APK with the Karnataka base URL forced, for independent on-device verification.

## Bottom line

The service catalogue and application-tracking data are genuinely empty for a real account — confirmed three independent ways (parameter sweep, full API sweep, NIC's own unmodified reference app). The Apply flow cannot be reached without a human completing ServicePlus's CAPTCHA-protected web login, because that flow requires its own server-side session that the mobile JSON login never establishes, and no API exists anywhere to create an `applId` without going through it. Neither limitation is fixable from our side; both require NIC's input (tracked in `SERVICEPLUS_NIC_BLOCKERS.md`).
