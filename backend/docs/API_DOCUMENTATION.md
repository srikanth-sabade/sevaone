# SevaOne API Documentation

Every HTTP API exposed by the backend (`SevaOne.Services`), grouped by the flow each one belongs
to. One line per API: what it does and where it sits in that flow. Base path for all citizen,
catalog, applications, and operator APIs is `/api/v1`; department-facing APIs are under
`/api/v1/dept`.

---

## 1. Citizen Auth flow

How a citizen signs in and manages their own session. Two sign-in paths (DigiLocker or mobile
OTP), both land the citizen in the same authenticated state.

| Method & Path | Crisp definition |
|---|---|
| `POST /auth/digilocker/start` | Starts DigiLocker sign-in; returns the CeG redirect URL to send the citizen's browser to. |
| `GET /auth/digilocker/callback` | CeG/DigiLocker redirects back here after consent; sets session cookies and redirects into the frontend. |
| `POST /auth/mobile/start` | Sends an OTP to the given mobile number; returns a `challengeId`. |
| `POST /auth/mobile/verify` | Verifies the OTP for a `challengeId`; on success, sets session cookies and signs the citizen in. |
| `POST /auth/refresh` | Silently rotates an expired access token using the HttpOnly refresh-token cookie. |
| `POST /auth/logout` | Clears the citizen's session cookies. |
| `GET /me` | Returns the signed-in citizen's own profile. *(Authenticated)* |
| `PATCH /me/profile` | Updates the signed-in citizen's own profile fields. *(Authenticated)* |
| `GET /me/digilocker/documents` | Lists documents already fetched into the citizen's DigiLocker. *(Authenticated)* |

---

## 2. Catalog (browse) flow

Public, unauthenticated browsing of what SevaOne offers — no citizen login needed to look around.

| Method & Path | Crisp definition |
|---|---|
| `GET /departments` | Lists all onboarded departments. |
| `GET /schemes` | Lists schemes/services, optionally filtered by department, category, or a text query. |
| `GET /search` | Free-text search across services (e.g. "income certificate"). |
| `GET /schemes/{code}` | Full detail for one scheme/service by its code. |
| `POST /schemes/{code}/apply-token` | Mints a one-time apply-token for a signed-in citizen to start applying for this scheme — the first step of the **Apply flow** below. *(Authenticated)* |

---

## 3. Citizen Apply flow

The citizen's own application lifecycle, from applying through tracking status. The apply-token
minted above (`POST /schemes/{code}/apply-token`) is what redirects the citizen out to the
department's own site; the department then calls back into SevaOne via Section 5 below.

| Method & Path | Crisp definition |
|---|---|
| `POST /departments/{code}/sessions` | Opens a session with a department before applying (department-specific handshake). *(Authenticated)* |
| `POST /applications` | Submits an application directly through SevaOne (non-redirect departments). *(Authenticated)* |
| `GET /applications` | Lists the signed-in citizen's own applications. *(Authenticated)* |
| `GET /applications/{reference}` | Full detail for one of the citizen's own applications. *(Authenticated)* |
| `GET /track/{reference}` | Public status lookup for an application by reference — no login needed. |
| `GET /apply/return` | The citizen's browser lands here when the department redirects them back after applying; validates the return-token and forwards into the frontend. |

---

## 4. Operator (kiosk/counter) flow

A separate identity from the citizen — a counter operator applying **on behalf of** a citizen at
a physical kiosk. Own login, own session cookie, distinct from citizen auth entirely.

| Method & Path | Crisp definition |
|---|---|
| `POST /operator/login` | Operator signs in with their operator code and password; sets the operator session cookie. |
| `POST /operator/logout` | Clears the operator's session cookie. |
| `POST /operator/apply-token` | Mints an apply-token on behalf of a specific citizen (assisted mode) — same downstream flow as a citizen's own apply-token. *(Authenticated as operator)* |

---

## 5. Department Integration flow

Called by department systems, not citizens or operators — authenticated with ApiKey (via WSO2
APIM) + Client ID/Secret (`DeptMachineAuth`), never a session cookie. Full detail and real
request/response examples: `DEPARTMENT_INTEGRATION_GUIDE.md`.

| Method & Path | Crisp definition |
|---|---|
| `POST /dept/user-details` | Department redeems the citizen's apply-token; gets back the citizen's profile to pre-fill their form. Single-use. |
| `POST /dept/get-acknowledgement` | Department reports the application outcome (submitted/approved/etc.) and gets a one-time return-token to send the citizen's browser back to SevaOne with. |
| `POST /dept/status-update` | Department reports a later status change on an application already acknowledged — keyed on their own `acknowledgementId`, callable any number of times, no token needed. |
| `POST /dept/payment-status` | Department reports a payment outcome (Pending/Success/Failed/Cancelled) tied to a token; idempotent on `transactionId`. |
| `POST /dept/test-token` | **Integration testing only.** Mints an apply-token for a fixed seeded test citizen, so a department can self-serve their own User Details → Acknowledgement → Payment Status testing without a real citizen or SevaOne triggering it. Same auth, same response shape as a real apply-token. |

---

## 6. Operations / internal

| Method & Path | Crisp definition |
|---|---|
| `GET /health` | Liveness check — is the process up. |
| `GET /ready` | Readiness check — are dependencies (DB, etc.) actually reachable. |

*(`/dev/*` OTP-inbox routes exist only in non-production environments for manual testing without
a real SMS gateway — not part of the production API surface, omitted here.)*

---

## How the flows connect

```
Citizen Auth  ──▶  Catalog (browse)  ──▶  Apply (apply-token minted)
                                              │
                                    citizen's browser redirected
                                    to department's own site
                                              │
                                              ▼
                                   Department Integration flow
                          (user-details → get-acknowledgement →
                                status-update / payment-status)
                                              │
                                    citizen's browser redirected
                                    back via /apply/return
                                              │
                                              ▼
                                  Citizen Apply flow (tracking)

Operator flow runs the same Apply → Department Integration path,
just minting the apply-token on the citizen's behalf instead of the citizen doing it themselves.
```
