# ServicePlus / NIC URL Handoff

**Purpose:** record every URL and contract item SevaOne needs from NIC/ServicePlus, and the exact place to configure each item when NIC supplies it.

**Status:** NIC has confirmed the Karnataka tenant base URL (below). The backend now has live login, ServicePlus-only catalogue and application-tracking adapters that follow the Flutter-observed wire contract, but NIC approval of the server-side integration, token/session semantics, response guarantees and WebView launch policy is still pending. Deployment remains explicitly opt-in through backend environment variables. This document is a handoff checklist, not a record of a fully approved production contract yet.

**Last updated:** 2026-09-29

## Confirmed by NIC (2026-09-29)

NIC told us to use our own instance rather than the national registry: **`https://sevaone.karnataka.gov.in/services/`** is the Karnataka tenant base URL, e.g. `https://sevaone.karnataka.gov.in/services/validateUser`.

Verified live the same day, from this machine, with `curl` (read-only checks — no real citizen credentials used):

- `GET https://sevaone.karnataka.gov.in/services/` → 200, serves the actual ServicePlus (Service+) J2EE app — same old-style markup as `serviceonline.gov.in`, not the modern SPA that sits at the bare domain root (`sevaone.karnataka.gov.in/` with no path serves a *different*, unrelated React app — don't confuse the two).
- `GET https://sevaone.karnataka.gov.in/services/citizenRegistration.html` → 200, the real registration page.
- `POST https://sevaone.karnataka.gov.in/services/validateUser` sent as multipart form-data (`userName`, `password`, `user_token`, `device_token`, `otpGenerationSessionId` — matching the Flutter app's exact request shape) → 200, with a genuine `LoginResponse`-shaped JSON body: `{"userName":null,"signNo":null,"mobileNo":null,"userStatus":false,"message":null,"roleIdList":[],"stateList":[],"roleList":{},"serviceList":[],"taskList":{},"citizenState":0}`. Test values were rejected correctly (`userStatus:false`), which is the expected behavior for invalid credentials, not an error — the endpoint is live and working.
- The path is case-sensitive: `validateUser` works, `validateuser` (lowercase) 404s.
- This tenant's own `getAllStateUrl` returns a single, seemingly stale entry pointing back at `serviceonline.gov.in` — don't rely on it for anything Karnataka-specific; use the NIC-confirmed base URL directly instead of resolving it through the registry.

This unblocks the "Karnataka tenant base URL" row below — everything else in the contract (encryption scheme, field rules, session/expiry semantics, WebView approval) is still unconfirmed and should not be assumed from this alone.

## Open problem: getServiceJson returns an empty catalogue (2026-09-29)

Live-tested end-to-end against a real citizen account (own credentials, real ServicePlus login, real backend session on the GPU deployment): `validateUser` succeeds and returns full profile/role data, but three different follow-up calls using that same session all come back `200` with an empty payload:

- `getServiceJson` (service catalogue) → empty `serviceJosn`/`serviceList`
- `getAllApplRefNoByUser` (applications) → empty
- `getIncomleteApplList` (drafts) → empty

Ruled out: this is not the stub fallback (stub mode returns non-empty fixture rows, not `[]`), and it is not a `device_token`/`user_token` mismatch — the adapter was changed to derive `user_token` from `device_token` via `uuid.v5(NAMESPACE_URL, device_token)`, exactly like the Flutter client's `Utility.generateToken()`, redeployed, and retested: still empty.

Suspected root cause, not yet confirmed: our adapter's `_application_params` (`serviceplus.py`) sends `dept=0` on every call, copied verbatim from the Flutter reference client's `api_service.dart`, whose own author left a comment admitting this is a placeholder: `String instanceDept = "0"; //localStorage.read("instanceDept");` — i.e. the real value was meant to come from the citizen's selected `instance.dept_id`, which the reference app never actually wired up either. Karnataka's own `getAllStateUrl` (see above) doesn't return a usable per-tenant `dept_id` for us to substitute. It's plausible the reference app itself never got real catalogue data with `dept=0`, and this bug is inherited from it rather than introduced here.

**Needs from NIC, not guessable from our side:** the exact `states`/`dept`/`instance_state` values a Karnataka citizen account should send to `getServiceJson` to see the real service catalogue — plus, ideally, a raw sample success response so we can confirm our parsing (`serviceJosn` row shape: `serviceId`/`label`/`serviceDesc`/`deptName`) matches what a populated response actually looks like. Add this to the pending NIC conversation alongside the registration/SSO/WebView asks already drafted in `SERVICEPLUS_REGISTRATION_API_QUERY.md`.

## The important boundary

The mobile app must continue to call only the SevaOne backend. NIC/ServicePlus URLs, credentials, encryption keys and provider tokens must stay on the backend. They must never be placed in `EXPO_PUBLIC_*` variables, the mobile bundle, a WebView URL, or a client-side request.

The existing mobile entry point remains:

```text
POST {EXPO_PUBLIC_API_BASE_URL}/api/v1/mobile/auth/serviceplus/login
Body: { "username": "...", "password": "..." }
```

The current GPU development value is `EXPO_PUBLIC_API_BASE_URL=https://101.53.138.83:8002`. That is our SevaOne backend URL, not a NIC URL. The backend returns SevaOne access/refresh tokens to the app; it must not return ServicePlus `device_token`, `user_token` or `signNo` to the app.

## URL inventory

| URL/config item | Current value or template | Where it belongs | NIC confirmation needed |
|---|---|---|---|
| SevaOne mobile backend | `EXPO_PUBLIC_API_BASE_URL` — currently `https://101.53.138.83:8002` | Mobile build environment only | No; change only when our backend moves |
| ServicePlus national instance registry | `https://serviceonline.gov.in/liveprod/` from the Flutter app | Backend secret/config, if NIC confirms it is still the supported registry | Confirm UAT and production hosts, TLS, and whether Karnataka may use a fixed tenant URL instead |
| Registry lookup | `{registryBase}getAllStateUrl` | Backend ServicePlus adapter | Confirm method, response shape, Karnataka state/dept selector, caching and refresh rules |
| Karnataka tenant base URL | **Confirmed by NIC:** `https://sevaone.karnataka.gov.in/services/` | Backend config | Still need: is this the same URL for UAT and production, or is there a separate UAT host? |
| Login | `{karnatakaBase}validateUser` — **verified live**, returns real `LoginResponse` JSON for a multipart form-data POST | Backend adapter | **Required:** the encryption scheme for `userName`/`password` (the Flutter app's hard-coded AES-ECB key is not something to assume is approved for our server-to-server use), exact expiry/session behavior |
| Forgot password | `{karnatakaBase}forgotPassword?login_id={urlEncodedLoginId}` — **verified live**, using the Flutter client's JSON `null` body and `application/json` headers | Backend adapter route `/api/v1/mobile/auth/serviceplus/forgot-password`; the mobile UI calls SevaOne only | Confirm rate limits and production error semantics |
| Citizen registration page | Current default: `https://sevaone.karnataka.gov.in/services/citizenRegistration.html` | Backend config used when opening the registration browser | Confirm this is the approved Karnataka registration URL and whether a return URL is supported |
| Citizen registration API | Not available in current code | Backend only | **Required if we want invisible provisioning:** API URL, payload, idempotency key, response and password rules |
| Service catalogue | `{karnatakaBase}getServiceJson?...` | Backend adapter `/api/v1/mobile/serviceplus/catalog`; live mode is implemented, stub remains disabled for this catalogue | Confirm state/dept/instance parameters and response contract |
| Citizen applications | `{karnatakaBase}getAllApplRefNoByUser?...` | Backend adapter; live mode is implemented, stub remains the local default | Confirm parameters and application response fields |
| Application tracking | `{karnatakaBase}applTrackStatus?...&applrefno={ref}` | Backend adapter; live mode is implemented, stub remains the local default | Confirm status values, polling limits and error behavior |
| Incomplete applications | `{karnatakaBase}getIncomleteApplList?...` | Backend adapter; live mode is implemented, stub remains the local default | Confirm the server-side spelling and response contract |
| Payment retry list | `{karnatakaBase}getAllRevalidatePaymentApplication?...` | Backend adapter; live mode is implemented, stub remains the local default | Confirm whether this is needed for Karnataka citizens |
| Payment retry | `{karnatakaBase}revalidatePayment?...` | Backend adapter; live mode is implemented, stub remains the local default | Confirm required process/service/application identifiers |
| Issued certificate | `{karnatakaBase}getCertificate?...` | Backend adapter | Confirm the parameterized URL; the Flutter source contains a hard-coded sample URL that must not be copied |
| Apply form | `{karnatakaBase}renderApplicationForm.do?serviceId={id}&UUID={sessionUuid}&OWASP_CSRFTOKEN={csrf}&applLimitFlag=N` | Server-created launch URL / approved in-app browser flow | **Required:** approved launch method, CSRF strategy, session hand-off, return URL and WebView policy |

Do not guess a URL from the public website. A page URL can load successfully while its mobile API, tenant routing or session contract is different.

## What we already learned from the Flutter app

The Flutter project uses a two-level URL model:

1. It calls the national registry `getAllStateUrl`.
2. It selects an `instance.url` for a state/department.
3. It appends the endpoint path to that tenant URL.

The observed citizen API paths are:

```text
POST {instance.url}validateUser
POST {instance.url}forgotPassword?login_id={email-or-login-id}
POST {instance.url}getServiceJson?states=&dept=&instance_state=&device_token=&user_token=&user=
POST {instance.url}getAllApplRefNoByUser?states=&dept=&instance_state=&device_token=&user_token=&user=
POST {instance.url}applTrackStatus?...&applrefno=
POST {instance.url}getIncomleteApplList?states=&dept=&instance_state=&device_token=&user_token=&user=
POST {instance.url}getAllRevalidatePaymentApplication?device_token=&user_token=&user=
POST {instance.url}revalidatePayment?...&device_token=&user_token=&user=
POST {instance.url}getCertificate?applRefNo=&token=&user=&device_token=&user_token=
```

For forgot password, the Flutter client sends `POST {instance.url}forgotPassword?login_id=...` with an `application/json` request whose body is JSON `null`. The SevaOne backend now preserves that wire contract while keeping the NIC URL and provider response off the mobile client.

The Flutter login observations are useful leads, not production approval. NIC must confirm the supported server-to-server contract before we implement them.

## Contract details NIC must provide with the URLs

Ask NIC for one complete UAT packet and one complete production packet containing:

1. **Hosts and tenant routing**
   - Registry URL, Karnataka tenant URL, registration URL and any separate application/payment host.
   - UAT/sandbox versus production values.
   - Whether every URL must use HTTPS and the required certificate chain.
   - Exact Karnataka state, department and instance values.

2. **Login request**
   - HTTP method and content type: JSON, form-url-encoded or multipart.
   - Exact field names and whether `userName` and `password` are encrypted.
   - The approved encryption/hash algorithm, mode, padding, key exchange and key rotation process.
   - Meaning and generation rules for `device_token`, `user_token` and `otpGenerationSessionId`.
   - Required headers, API keys, client IDs, timestamps, nonce and signatures.

3. **Login response**
   - Exact success and failure examples with HTTP status codes.
   - `userStatus`, `signNo`, `citizenState`, `roleIdList`, user identity fields and any expiry values.
   - Whether `signNo` is a session credential, how long it lives, how it is revoked and whether it is bound to the device/user token.
   - Whether NIC authorizes SevaOne’s backend to perform this call on behalf of a citizen.

4. **Forgot password**
   - Exact URL and parameter name (`login_id` must be confirmed).
   - Whether the identifier is username, email, mobile number or all three.
   - Success response, invalid-user response, lockout/rate limits and language behavior.

5. **Registration**
   - Whether the HTML page is the only supported route or a server-to-server registration API exists.
   - If an API exists: idempotency behavior, fields, password policy, duplicate-user response, verification requirements and returned subject/account ID.
   - Whether registration may be initiated from SevaOne after SevaOne OTP/DigiLocker verification.

6. **Application launch and WebView approval**
   - The supported way to mint a short-lived launch session for a selected service.
   - Whether headers `DEVICE_TOKEN`, `USER_TOKEN`, `USER_SIGN` and `X-Requested-With: XMLHttpRequest` are required.
   - Written approval for SevaOne’s application package/origin to load the form in an embedded browser.
   - CSRF token generation and lifetime; the Flutter source’s hard-coded sample token is not acceptable.
   - Return/callback URL after submit, how to obtain the application reference number, and what happens if the citizen closes the form.

7. **Service and tracking APIs**
   - Karnataka-only service catalogue filtering.
   - Application list, tracking, incomplete application and certificate response schemas.
   - Status vocabulary and polling limits.
   - Payment and document upload requirements, if they are part of the approved integration.

## How to install NIC values when received

### 1. Record the contract first

Fill the URL table above with separate UAT and production values. Record the date, NIC contact, document/version, certificate requirements and a redacted request/response example. Do not paste passwords, API keys, encryption keys, OTPs or live tokens into this document.

### 2. Put provider values on the backend

The deployment file is `docker-deploy/.env` on the GPU server. The mobile `.env` should keep only our backend URL. The following names are the backend configuration shape; enable live mode only after the owner/NIC approves the server-side contract and the migrations are applied:

```dotenv
# Our backend mode switch
SERVICEPLUS_AUTH_MODE=live
SERVICEPLUS_ALLOW_STUB_LOGIN=0
SERVICEPLUS_APPLICATIONS_MODE=live
SERVICEPLUS_TOKEN_ENCRYPTION_KEY=<strong deployment secret>

# NIC/ServicePlus URLs — backend only
SERVICEPLUS_KARNATAKA_BASE_URL=https://sevaone.karnataka.gov.in/services/    # confirmed by NIC 2026-09-29; ask whether UAT differs
SERVICEPLUS_REGISTER_URL=https://sevaone.karnataka.gov.in/services/citizenRegistration.html
SERVICEPLUS_APPLICATION_BASE_URL=https://sevaone.karnataka.gov.in/services/

# Only if NIC supplies them; keep secrets in the deployment secret store
SERVICEPLUS_CLIENT_ID=<redacted>
SERVICEPLUS_CLIENT_SECRET=<redacted>
SERVICEPLUS_API_KEY=<redacted>
SERVICEPLUS_ENCRYPTION_KEY=<redacted>
```

These configuration names are implemented in the backend. They are not permission to invent a NIC contract: if NIC supplies different names or separate hosts, keep the values centralized in `apps/Seva-Sindhu-One/backend/config.py` and the GPU deployment environment rather than scattering literals through screens.

### 3. Keep provider credentials and tokens server-side

The current migration already reserves backend-only fields in `app.serviceplus_account`:

- `login_name`
- one-way `password_hash`
- encrypted `device_token_ciphertext`
- encrypted `user_token_ciphertext`
- encrypted `sign_no_ciphertext`
- `token_expires_at`

The approved adapter should encrypt provider values at rest, refresh or re-mint them on the backend when required, and never send them to the mobile app. The phone stores only the SevaOne session: access token in memory and rotating refresh token in SecureStore.

### 4. Keep the mobile API stable

The app should continue calling:

```text
POST /api/v1/mobile/auth/serviceplus/login
```

Only the backend adapter changes between stub and live NIC calls. The mobile response remains the existing SevaOne `SignedInResponse` shape, so no login-screen change is needed; application tracking likewise keeps the existing mobile routes and response types.

The later service-click flow should also be backend-mediated:

```text
Mobile -> SevaOne backend: open service
Backend -> NIC: resolve tenant / mint or reuse provider session
Backend -> Mobile: short-lived approved launch instruction
Mobile -> approved ServicePlus form: open in-app browser
ServicePlus -> SevaOne callback/backend: return application reference/status
```

The exact launch endpoint and callback must wait for NIC’s written approval.

### 5. Deploy and smoke-test in UAT

After the adapter exists and the UAT values are filled:

1. Update the GPU server’s `docker-deploy/.env`; do not put NIC values in the mobile `.env`.
2. Keep `APP_ENV=development` or UAT mode while testing and keep the stub account available as a fallback only if the owner wants it.
3. Restart/rebuild only the SevaOne mobile backend using the project’s deployment procedure.
4. Test registry resolution, Karnataka login, wrong-password handling, forgot password, service catalogue, application list/tracking and the approved apply-form launch.
5. Confirm logs contain no password, API key, provider token, full mobile number or CSRF value.
6. Test expiry/re-login and provider outage behavior.
7. Only after NIC and owner sign-off, set `SERVICEPLUS_AUTH_MODE=live` in production and disable stub login.
8. Build the mobile APK only after the backend UAT smoke test passes; the app should still point to the SevaOne backend URL.

## Do not do these things

- Do not put the NIC base URL, username/password, API key or encryption key in `EXPO_PUBLIC_*` variables.
- Do not return `device_token`, `user_token` or `signNo` to the mobile app merely because the Flutter app stores them locally.
- Do not copy the Flutter app’s hard-coded certificate URL, CSRF token or sample credentials.
- Do not assume the public website URL is the API tenant URL.
- Do not spoof `X-Requested-With` or bypass an embedded-browser restriction before NIC gives written approval.
- Do not switch the production backend to live mode until the login, session, application launch and failure contracts are tested end-to-end.

## Related project references

- `docs/mobile/SERVICEPLUS_API_INVENTORY.md` — Flutter-derived endpoint inventory and session observations.
- `docs/mobile/SERVICEPLUS_REGISTRATION_API_QUERY.md` — draft NIC request for registration, server-side login and WebView approval.
- `apps/Seva-Sindhu-One/backend/config.py` — current backend configuration and stub-mode defaults.
- `apps/Seva-Sindhu-One/backend/migrations/011_serviceplus_auth.sql` — provider selector and backend-only credential/token columns.
- `apps/Seva-Sindhu-One/mobile/src/api/authApi.ts` — stable mobile-to-SevaOne login route.
