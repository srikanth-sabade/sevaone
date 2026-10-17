# Authentication APIs

Three separate identity types exist in SevaOne, each with its own controller, its own
session/credential mechanism, and its own auth scheme. They don't share a login system —
a citizen credential and an operator session can coexist on the same browser at once (see
`Wso2CitizenAuthMiddleware`/`OperatorSessionMiddleware`, both add a `ClaimsIdentity` onto the
same `context.User` rather than replacing it).

---

## 1. Citizen auth — `AuthController`

Route base: `/api/v1/auth/*` (plus `/api/v1/me`, `/api/v1/me/profile`). All of it — including
the two "start the login" endpoints below — goes through a dedicated APIM gateway now
(`sevaone-citizen`); a direct call to the backend's own port is rejected outright. Full
mechanics: `TOKEN_FLOW.md` → **Citizen authentication**; gateway setup: `WSO2_IS_APIM_SETUP.md`.

**Credential: `seva.wso2.at` / `seva.wso2.rt` — real WSO2 IS access/refresh tokens, held in two
`HttpOnly` cookies.** There is no SevaOne-issued session object anymore (the old `seva.sid` /
`SevaSession` / `ISevaSessionStore` citizen-session machinery has been deleted, not just
retired). `Wso2CitizenAuthMiddleware` validates `seva.wso2.at` on every request via a
Redis-cached WSO2 introspection call (`IWso2TokenIntrospectionCache` — ask WSO2 once, trust the
cache for up to 60 seconds) and loads the citizen from Postgres off the token's `username`
claim (`sevaone-{SevaOneUserId}`). Auth scheme: `Seva`.

| Method | Path | Auth required? | Purpose |
|---|---|---|---|
| POST | `/api/v1/auth/digilocker/start` | No | CeG DigiLockerSignInWithcodeChallange; returns `redirectUrl`. Rate-limited (`auth-start`, 5/10 min per IP) |
| GET | `/api/v1/auth/digilocker/callback` | No | Browser return from DigiLocker (`status`, `txnid`, `appparam`). GetDigilockerIDFromTxnID + GetEAadharDetails, then cookies and 302 to the frontend |
| GET | `/api/v1/me/digilocker/documents` | **Yes** (`Seva`) | CeG GetIssuedDocList for the logged-in citizen |
| POST | `/api/v1/auth/mobile/start` | No | Sends an OTP (SevaOne's own, not DigiLocker's) to the given mobile via `ISmsOtpSender` — returns a `challengeId`, doesn't reveal whether the number is already registered. Rate-limited (`auth-start`, 5/10 min per IP) |
| POST | `/api/v1/auth/mobile/verify` | No | Verifies the OTP; `found:true` on a match against an existing citizen's *verified* mobile provisions/refreshes their WSO2 identity and sets the token cookies, `found:false` means no citizen has this mobile yet — the frontend proceeds to the DigiLocker flow next. Rate-limited (`auth-credential`, 5/5 min per IP) |
| POST | `/api/v1/auth/refresh` | No (needs a valid `seva.wso2.rt` cookie instead) | Exchanges the refresh-token cookie for a fresh access/refresh pair via WSO2's `refresh_token` grant — the one place SevaOne actively manages this credential's lifetime, since the browser can't rotate it itself. 401 + clears both cookies if the refresh token is dead |
| POST | `/api/v1/auth/logout` | No (reads the cookie if present) | Revokes the token against WSO2, deletes its introspection-cache entry immediately (don't wait out the 60s TTL), closes any open department sessions, clears both cookies |
| GET | `/api/v1/me` | **Yes** (`Seva`) | Current citizen's profile |
| PATCH | `/api/v1/me/profile` | **Yes** (`Seva`) | Updates the profile; sets `ProfileCompleted = true` |

**Retired and deleted — now a plain `404 Not Found`:** `GET /api/v1/auth/login`,
`GET /api/v1/auth/callback`, `POST /api/v1/auth/password`, `POST /api/v1/auth/otp` (the WSO2
OIDC redirect and password-grant citizen login paths). They never set
`AadhaarVerified`/`MobileVerified`, so none of the identity safeguards built this session
(duplicate-Aadhaar detection, mobile verification tracking, real-Aadhaar backfill) ever covered
them — a citizen could register through them with a self-typed, unverified mobile and a
fabricated Aadhaar placeholder, invisible to the `aadhaar_hash` unique-index dedup entirely.
The controller actions themselves have since been deleted outright, along with everything they
used to call: `Wso2OidcClient`, `Wso2Options`, `PasswordLoginAsync`/`CompletePasswordOtpAsync`,
the demo-user fallback — none of it exists in the codebase anymore, so the routes 404 like any
other unmapped path.

**Both remaining login paths — DigiLocker and mobile OTP — funnel through the same
`ICitizenSessionService.EstablishAsync`**, which bootstraps a new `portal.sevaone_user` row on
first login (or updates fields on return), writes the `auth.login` audit entry, and provisions
the citizen's WSO2 identity. Neither duplicates that logic separately.

Mobile OTP is deliberately login-only, not registration — `VerifyMobileLoginAsync` only ever
calls `EstablishAsync` for a mobile that already matches an existing, verified citizen row
(via `GetCitizenByMobileAsync`, which only matches `mobile_verified_at IS NOT NULL`). A new
citizen typing an unregistered number gets `found:false` and is routed to DigiLocker instead —
DigiLocker is what actually proves identity and creates the account; mobile OTP just proves
the citizen still owns a number SevaOne already trusts.

**`EstablishAsync` provisions a real WSO2 IS identity for the citizen and exchanges it for the
citizen's actual credential** — SCIM2 find-or-create (keyed on `sevaone-{SevaOneUserId}`) then
a JWT-bearer exchange (RFC 7523) for a real access/refresh token pair, set as the two `HttpOnly`
cookies above. This is now **required**, not soft-fail: if WSO2 is unreachable or the exchange
fails, the login itself fails — there's no fallback token anymore, since the WSO2 token *is*
the credential. Full detail: `TOKEN_FLOW.md` → **Citizen authentication**,
`WSO2_IS_APIM_SETUP.md` → **Citizen authentication**.

---

## 2. Operator auth — `OperatorController`

Route base: `/api/v1/operator/*`.
Session: cookie `seva.op.sid`, Redis-backed (`IOperatorSessionStore`), 8-hour TTL.
Auth scheme: `SevaOperator`.
Credential: `portal.operator_master.password_hash` — PBKDF2-SHA256, owned entirely by
SevaOne. Operators have no WSO2 IS account; there's no external IdP for internal
government-staff/kiosk accounts.

| Method | Path | Auth required? | Purpose |
|---|---|---|---|
| POST | `/api/v1/operator/login` | No | Operator code + password. Rate-limited (`auth-credential`, 5/5 min per IP — shared with citizen `mobile/verify`) |
| POST | `/api/v1/operator/logout` | No (reads the cookie if present) | Ends the operator session |
| POST | `/api/v1/operator/apply-token` | **Yes** (`SevaOperator`) | Mints an Assisted-Mode apply-token (`AssistedMode: true`, real `OperatorId`) for a citizen the operator specifies by `SevaOneUserId` |

`OperatorAuthService.LoginAsync` runs the password verification against a fixed dummy hash
even when the operator code doesn't exist, so a nonexistent code doesn't return faster than
a real one with a wrong password — avoids a timing side-channel for operator-code
enumeration.

---

## 3. Department (machine) auth — `DeptController`

Route base: `/api/v1/dept/*`.
No session cookie — this is server-to-server. **Two credentials, checked by two different
layers:** `ApiKey` (a per-department key from an APIM Application, checked by **APIM itself**
before the request reaches this controller at all) and `DeptMachineAuth` — HTTP Basic
(`client_id:client_secret`) or the equivalent `X-SevaOne-Dept-Client`/`X-SevaOne-Dept-Secret`
headers, checked against `Jws:DeptClients` config, by **SevaOne**. Full request-level detail:
`DEPARTMENT_INTEGRATION_GUIDE.md`.

| Method | Path | Purpose |
|---|---|---|
| POST | `/api/v1/dept/token-validate` | Redeems an apply-token for a citizen the department already knows locally — 30-minute validity, one-time use |
| POST | `/api/v1/dept/user-details` | Redeems an apply-token for a citizen new to the department — same 30-minute/one-time rule, returns a minimized profile (never the Aadhaar hash) |
| POST | `/api/v1/dept/get-acknowledgement` | Mints a second, independent, 10-minute one-time token once the department confirms completion, for the `GET /api/v1/apply/return` redirect trip. Also creates the `orchestration.applications` tracking row (deduped by a unique index, not just an app-level check) if one doesn't already exist for this citizen+scheme+department |

**`POST /api/v1/dept/payment-status` (`DeptController.PaymentStatus`) is orphaned, unreachable
code** — the controller action still physically exists, but it was removed from the department
APIM gateway's operation list along with the rest of the payment surface. There's no route
configured for it at APIM, and direct port-5080 access is rejected outright by
`DeptGatewayAuthMiddleware` regardless — no department can reach it through any path today.

A separate, simpler-auth department push exists outside this table:
`POST /api/v1/internal/webhooks/status` — a plain shared-secret header (`X-Seva-Webhook-Secret`
against `Seva:WebhookSecret` config), not `DeptMachineAuth`/APIM. Updates an application's
status by `departmentCode` + `departmentReference`; only works once a row already exists
(created via `get-acknowledgement` above).

**Two independent auth layers stack here, not one — but neither one is WSO2 anymore.** Every
`/api/v1/dept/*` call passes through APIM's own gate first: a per-department `ApiKey`
(`authType: "Application"`, `securityScheme: ["api_key"]`), issued through an APIM Application
and checked by **APIM itself** — a bad or missing key is rejected at the gateway with WSO2's own
error shape (`code`/`message`/`description`), and this controller never runs. Once that passes,
APIM stamps its own gateway secret onto the forwarded request, and `DeptMachineAuth` runs
underneath, checked by SevaOne, resolving *which* department is calling — a different question
than "is this application allowed to call this gateway at all," which `ApiKey` already answered.
There is **no WSO2 IS involvement anywhere in this chain** — departments used to also fetch a
WSO2 `client_credentials` bearer for APIM to introspect, but that layer was dropped as a
simplification and `ApiKey` (a pure-APIM construct) took its place as the real gateway-level
check. Full detail: `TOKEN_FLOW.md` → WSO2 layer, `WSO2_IS_APIM_SETUP.md` → Department
onboarding. Citizen traffic goes through its *own* separate APIM gateway too (see §1) — that one
works differently: APIM doesn't validate the citizen's token itself (it can't; the token lives
in a cookie, not a header), it just enforces "only I can reach the backend" plus a shared-secret
stamp, and SevaOne's backend does the actual token check. Operator auth is the only one of the
three that never touches APIM at all — it was never a WSO2 identity to begin with.

Department `ApiKey`s are rate-limited: `Bronze` subscription tier per department (1000
requests/minute), plus a shared 20,000 requests/minute flood cap across the whole gateway.

Every redeem here also enforces a **one-time-use guarantee**: `IDeptTokenGate.TryRedeemAsync`
is an atomic `UPDATE portal.sevaone_token SET revoked_at = ... WHERE revoked_at IS NULL`
(`portal.sp_try_redeem_token`) — a second call with the same token is rejected, confirmed live
via the `test-login-flow.ps1` script's replay step.

---

## Summary

| | Citizen | Operator | Department |
|---|---|---|---|
| Controller | `AuthController` | `OperatorController` | `DeptController` |
| Credential | `seva.wso2.at`/`seva.wso2.rt` `HttpOnly` cookies — real WSO2 IS tokens, no SevaOne session object | `seva.op.sid` cookie (Redis) | `ApiKey` (APIM Application) + `DeptMachineAuth` (`Jws:DeptClients`) — two, not one |
| Who validates the credential | SevaOne's backend, via Redis-cached WSO2 introspection | SevaOne's backend, Redis session lookup | `ApiKey`: **APIM itself**. `DeptMachineAuth`: SevaOne's backend |
| Auth scheme | `Seva` | `SevaOperator` | `ApiKey` (APIM) + `DeptMachineAuth` (SevaOne) — zero WSO2 involvement |
| Identity source | DigiLocker, or mobile OTP (existing citizens only) — WSO2 IS login/password grant deleted | SevaOne-owned password (PBKDF2) | APIM-issued `ApiKey` (which application) + shared secret (`Jws:DeptClients`, which department) |
| Brute-force protection | Rate-limited (`auth-credential`/`auth-start`, per IP) | Rate-limited (`auth-credential`, shared bucket with citizen mobile-verify) | `Bronze` tier per department (1000 req/min) + 20,000 req/min gateway-wide cap |
| Routed through APIM? | Yes (`sevaone-citizen` gateway — perimeter + shared secret only, not token validation) | No | Yes (`sevaone` gateway — real `ApiKey` validation at APIM, no WSO2/OAuth2 anywhere) |
