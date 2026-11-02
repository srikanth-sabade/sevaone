# ServicePlus (eserviceplus-master) — API inventory

Reverse-engineered from the Flutter app source at `apps/eserviceplus-master` (2026-09-28), to understand
what the citizen mobile app SevaOne redirects into today, ahead of asking the ServicePlus/CDAC platform
team about a real SSO integration. Every call in this app funnels through two files:
`lib/services/api_service.dart` (13 endpoints) and `lib/services/common_service.dart` (1 endpoint, login).
No screen calls the network directly.

## Base URLs

- **National instance registry**: `https://serviceonline.gov.in/liveprod/` (`lib/utils/Constants.dart:11`).
  This is the only hard-coded host in the app; it serves the tenant directory.
- **Per-tenant base**: every other call is prefixed with `instance.url`, one per state/department
  "instance" returned by `getAllStateUrl` below (e.g. a Karnataka tenant, a district tenant, etc.).
  Nothing else in the client is Karnataka-specific — this is a shared multi-tenant platform.

Both HTTP helpers the app uses (`Utility.httpPost`, `Utility.postData` in `lib/utils/Utility.dart`)
construct their own `HttpClient`/`Dio` adapter with `badCertificateCallback = (cert, host, port) => true`,
i.e. **TLS certificate validation is disabled app-wide**, not just for one host.

## Session mechanism

There is no cookie-based session. After login, three values are cached locally (`GetStorage`) and reused
everywhere:

- `user_token` — a UUID v5 derived from the device id, generated on-device (`Utility.generateToken`, not
  server-issued)
- `device_token` — the raw device id
- `signNo` — returned by the server at login (`LoginResponse.signNo`)

Every subsequent `httpPost` call appends `device_token=&user_token=&user={signNo}` as query params, and
every in-app WebView page load additionally sends them as headers (`getHeaders()`,
`api_service.dart:369-379`): `DEVICE_TOKEN`, `USER_TOKEN`, `USER_SIGN`, plus `X-Requested-With:
XMLHttpRequest`. There's no refresh/expiry call anywhere in the app — a session is only ever replaced by
logging in again.

## Endpoints

| # | Name | Method / transport | URL template | Params | Auth | Purpose | Source |
|---|---|---|---|---|---|---|---|
| 1 | `getAllStateUrl` | POST, empty body | `{baseUrl}getAllStateUrl` | none | none | Returns the tenant directory: one `Instance` per state/dept (`url`, `url_alias`, `state_id`, `dept_id`, `login_with_mobile`, `login_with_email`, `third_party_registration`) | `api_service.dart:25` |
| 2 | `validateUser` | POST, multipart form | `{instance.url}validateUser` | `userName`, `password` (both AES‑128‑ECB encrypted client-side, see below), `user_token`, `device_token`, `otpGenerationSessionId` | none (this is the login) | Authenticates; returns `LoginResponse` (`userStatus`, `roleIdList`, `signNo`, `citizenState`) | `common_service.dart:33` |
| 3 | `forgotPassword` | POST | `{instance.url}forgotPassword?login_id={email}` | `login_id` | none | Triggers a password reset | `api_service.dart:36` |
| 4 | `getServiceJson` | POST | `{instance.url}getServiceJson?states=&dept=&instance_state=&device_token=&user_token=&user=` | session tokens + state/dept | session | Service catalogue for the citizen's state | `api_service.dart:57` |
| 5 | `getAllApplRefNoByUser` | POST | `{instance.url}getAllApplRefNoByUser?states=&dept=&instance_state=&device_token=&user_token=&user=` | session tokens + state/dept | session | List of the citizen's own application reference numbers | `api_service.dart:87` |
| 6 | `applTrackStatus` | POST | `{instance.url}applTrackStatus?...&applrefno=` | session tokens + `applrefno` | session | Status/tracking detail for one application | `api_service.dart:112` |
| 7 | `getCertificate` | POST | `{instance.url}getCertificate?applRefNo=&token=&user=&device_token=&user_token=` | session tokens + `applRefNo`, `token` | session | Downloads the issued certificate (base64 + file extension) | `api_service.dart:135` — **see note below, URL is currently hard-coded to a sample, not parameterized** |
| 8 | `getIncomleteApplList` *(sic, server-side typo)* | POST | `{instance.url}getIncomleteApplList?...` | session tokens + state/dept | session | Incomplete/draft applications | `api_service.dart:164` |
| 9 | `getAllRevalidatePaymentApplication` | POST | `{instance.url}getAllRevalidatePaymentApplication?device_token=&user_token=&user=` | session tokens | session | Applications with a stuck/failed payment eligible for retry | `api_service.dart:186` |
| 10 | `revalidatePayment` | POST | `{instance.url}revalidatePayment?device_token=&user_token=&user=&currentProcessId=&serviceId=&applId=` | session tokens + ids | session | Re-attempts a payment | `api_service.dart:217` |
| 11 | `getServiceList` | POST | `{instance.url}getServiceList?device_token=&user_token=&user=` | session tokens | session | Official/DEO's assigned service list (Work Flow Player role) | `api_service.dart:241` |
| 12 | `getTaskList` | POST | `{instance.url}getTaskList?device_token=&user_token=&user=&serviceId=` | session tokens + `serviceId` | session | Queued tasks for an official under one service | `api_service.dart:266` |
| 13 | `searchApplication` | POST | `{instance.url}searchApplication?device_token=&user_token=&userName=&serviceId=&taskId=&holdApplType=N` | session tokens + ids | session | Officials' worklist search | `api_service.dart:289` |
| 14 | `getDashboardData` | POST | `{instance.url}getDashboardData?device_token=&user_token=&userName=` | session tokens | session | Officials' dashboard counts | `api_service.dart:314` |
| 15 | `pullApplication` | POST | `{instance.url}pullApplication?applId=&currentProcId=&userSignNo=&downloadEnclFlag=&device_token=&user_token=` | session tokens + ids | session | Official pulls/claims an application to work on | `api_service.dart:341` |
| 16 | `extractBarCodeContent` | POST | `{instance.url}extractBarCodeContent?encBCode=` | `encBCode` (RSA-decrypted client-side first, see below) | none observed | Resolves a scanned application QR/barcode | `api_service.dart:359` |

## Portal web pages (not JSON APIs)

These are loaded whole inside an in-app `InAppWebView`, so the app has no visibility into what they call
internally server-side. `webview_screen.dart` (the app's own generic WebView) sends the session as headers
(`DEVICE_TOKEN`, `USER_TOKEN`, `USER_SIGN`, plus `X-Requested-With: XMLHttpRequest` — set explicitly via
`InAppWebViewSettings`, overriding whatever Android's WebView would otherwise inject) for every page below:

- **`citizenRegistration.html`** (`sign_up_screen.dart`) — the self-registration form. Cookies are cleared
  before loading it; nothing else is passed in.
- **`renderApplicationForm.do?serviceId=&UUID={user_token}&OWASP_CSRFTOKEN=...&applLimitFlag=N`**
  (`services_screen.dart:167`) — the actual "Apply" form a citizen fills in for a chosen service. This is
  the functional equivalent of what SevaOne currently opens via a Chrome Custom Tab for department Apply
  links. **The `OWASP_CSRFTOKEN` value is a hard-coded literal in the source, not generated per
  session/request.** Live-tested with a valid `serviceId` and no browser session: redirects to
  `/services/error.do` ("Session Invalidated... Relogin") — confirms this needs its own server-side
  `JSESSIONID` session that the JSON login's tokens don't establish.
- **`directApply.do?serviceId=` (commented out in current source, `services_screen.dart:176`)** — the real
  applId-creation flow. Redirects to a CAPTCHA-protected HTML login form (`loginWindow.do`, fields
  `signNo`/`passwd`/`captchaAnswer`) — a separate login path from `validateUser`, not just a UI skin over
  it.
- **`editApplicationForm.do?serviceId=&UUID=&OWASP_CSRFTOKEN=&applicationRefNo=`**
  (`incomplete_applications_screen.dart:166`) — resumes/edits a draft application by reference number.
- **`mExportToPdfAck?applId=&spdiCurrentProcId=&templId=`** (`webview_screen.dart`, mostly commented-out
  handler code) — exports a submitted application as a PDF.

## QR code flow

`scan_qr_code_screen.dart` scans a code, RSA-decrypts its payload locally with `AsymmetricCryptography`
(`lib/utils/AsymmetricCryptography.dart`), strips non-printable characters, then (based on the code's
purpose) is the likely caller of `extractBarCodeContent` (#16) to resolve it server-side.

## Security concerns

Ranked by impact. All confirmed by reading the shipped source, not assumed.

1. **TLS certificate validation is disabled app-wide.** `Utility.dart:28` sets
   `badCertificateCallback = (cert, host, port) => true` on every `HttpClient` the app creates — it accepts
   *any* certificate (expired, self-signed, wrong host, attacker-issued) for every request, not just one
   host. The line directly above it is a committed, unresolved `//TODO: Enable SSL in Production`. Combined
   with #2 and #3 below, anyone who can put themselves on-path (a malicious Wi-Fi AP, a compromised router)
   can intercept and modify login credentials, session tokens and all application data in transit, on the
   version of the app currently shipped.
2. **The real ServicePlus password is persisted unencrypted on the device.** `User.toJson()`
   (`models/User.dart`) serializes the plaintext `password` field, and `common_service.dart:100` calls
   `localStorage.write('user', user)` right after a successful login — persisting that plaintext password
   to `GetStorage`, which is a plain JSON file in app-private storage, **not** Android Keystore or iOS
   Keychain. It survives app restarts indefinitely and is recoverable from a device backup, a rooted
   device, or any file-level extraction — the citizen's real government-services password sitting in clear
   text on disk.
3. **Login credentials are only obfuscated, not really encrypted, before transmission.** `userName`/
   `password` are AES-128-**ECB** encrypted with a key hard-coded in the client (`thisIsASecretKey`,
   `Utility.dart`) before being POSTed. The key is extractable from the APK in minutes (we did this
   ourselves this session), and ECB mode leaks repeating plaintext patterns even without the key. This adds
   no real confidentiality on top of TLS — and TLS itself is defeated by #1.
4. **The CSRF token is a hard-coded literal, not generated per session.** The `OWASP_CSRFTOKEN` value sent
   with `renderApplicationForm.do`/`editApplicationForm.do` (`services_screen.dart`,
   `incomplete_applications_screen.dart`) is the same fixed string compiled into every install — it cannot
   protect against CSRF since it doesn't vary per user, session, or request.
5. **Session tokens and application data are printed to the device log.** `print()` calls throughout
   `api_service.dart`, `services_screen.dart`, `incomplete_applications_screen.dart` and others write
   `user_token`, `device_token`, `signNo`, and full API response bodies (application/PII data) to the
   console. These aren't gated behind a debug-only check, so they can end up in logcat on release builds
   too — anything with legacy log-read access, or a support/crash-log tool, can see live session tokens.
6. **A real-looking sample credential set is hard-coded in source.** `getCertificate()`
   (`api_service.dart:135`) has its URL and parameters hard-coded to a specific sample
   `applRefNo`/`token`/`user`(an email)/`device_token`/`user_token`, ignoring the real `instance`/session
   values already in scope. This looks like leaked internal/test data left in from debugging and shipped
   in the public APK, rather than a placeholder.
7. **The app overrides `X-Requested-With` to defeat the platform's own anti-embedding check.**
   `webview_screen.dart` explicitly sets `X-Requested-With: XMLHttpRequest` on its WebView requests,
   overriding Android's automatic header — which is exactly the value that lets an embedded WebView bypass
   ServicePlus's own "is this an approved app?" detection (see the Portal web pages section above). Whether
   or not this is sanctioned by NIC, it's the app's own client working around its platform's security
   control, and worth surfacing to NIC directly (see `SERVICEPLUS_NIC_BLOCKERS.md`, item 3).

## Notes for whoever picks this up

- `httpPostFormData` (`Utility.dart:56`) is defined but never called anywhere — dead code.
- `Instance.third_party_registration` (`Instance.dart:8`) is parsed from the `getAllStateUrl` response but
  never read anywhere in the app — a lead worth asking the platform team about, not something this client
  implements.
