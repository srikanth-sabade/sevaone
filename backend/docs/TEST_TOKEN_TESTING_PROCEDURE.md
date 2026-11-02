# Test-Token Testing Procedure — KSDC Server

A step-by-step procedure to verify the department integration pipeline end-to-end on the live
KSDC server (`172.18.60.104`), using the `test-token` self-serve endpoint instead of a real
citizen. Every request/response shown below was actually run against that server — this isn't
a hypothetical script.

> **Outdated auth section, not yet re-verified against KSDC**: the `ApiKey` + Basic-auth
> (`Client ID:Client Secret`) credential pair shown below has been replaced by `ApiKey` +
> a WSO2 IS Bearer token — see `DEPARTMENT_INTEGRATION_GUIDE.md`, Section 1, verified live
> end-to-end against this project's home dev VM. The steps and response shapes below are still
> accurate; only how you authenticate has changed. This doc hasn't been re-run against KSDC
> with the new mechanism yet — do that before trusting the credential values below again.

## What this proves

Each step exercises a different hop: minting a token, redeeming it, reporting an outcome,
reporting a status change, reporting a payment. A pass on all six steps means the whole
`/api/v1/dept/*` pipeline — nginx → WSO2 APIM → bff → Postgres — is genuinely working, not just
reachable.

---

## Prerequisites

| Item | Value |
|---|---|
| Base URL | `http://172.18.60.104` |
| Department | Revenue (`departmentId: "revenue"`) |
| Client ID / Secret | `nadakacheri_portal` / `nadakacheri_secret` |
| ApiKey | `u4G8DT-yrd_I_zfHWwnBV93VtmikFOfSpKJfpu2ES4I` |
| Tool | `curl` (or Postman — same requests) |
| Service to use | `SVC001` only — see [Known limitation](#known-limitation) |

Full credential set for all 11 departments: `backend/docs/SERVER_DEPARTMENT_INTEGRATION_GUIDE.md`.
Every call below sends all three credentials the same way:
```
-H "ApiKey: <ApiKey>" -u "<ClientId>:<ClientSecret>"
```

---

## Step 1 — Mint a test token

No real citizen needed. This stands in for a citizen clicking "Apply" on SevaOne.

```bash
curl -s -X POST 'http://172.18.60.104/api/v1/dept/test-token' \
  -H 'ApiKey: u4G8DT-yrd_I_zfHWwnBV93VtmikFOfSpKJfpu2ES4I' \
  -u 'nadakacheri_portal:nadakacheri_secret' \
  -H 'Content-Type: application/json' \
  -d '{"departmentId":"revenue","serviceId":"SVC001"}'
```

**Expected: `200 OK`**
```json
{
  "applyUrl": "https://nadakacheri.karnataka.gov.in/apply?DepartmentID=revenue&ServiceID=SVC001&SevaOneUserID=1&Token=CfDJ8H6C...(long token, truncated here)",
  "expiresAt": "2026-09-01T11:19:55.97Z",
  "isHomepageFallback": false
}
```

**✅ Pass criteria:** `isHomepageFallback: false` and a `Token=` value present in `applyUrl`.
**Extract and keep two things** from the URL for the next steps: the `Token` value, and
`SevaOneUserID` (always `1` — the fixed seeded test citizen).

**❌ If you get a `409`** ("no Apply URL or homepage configured yet") — you used a `serviceId`
other than `SVC001`. See [Known limitation](#known-limitation).

---

## Step 2 — Redeem it: User Details

Simulates the department confirming the token and pulling the citizen's profile.

```bash
curl -s -X POST 'http://172.18.60.104/api/v1/dept/user-details' \
  -H 'ApiKey: u4G8DT-yrd_I_zfHWwnBV93VtmikFOfSpKJfpu2ES4I' \
  -u 'nadakacheri_portal:nadakacheri_secret' \
  -H 'Content-Type: application/json' \
  -d '{"departmentId":"revenue","token":"<TOKEN FROM STEP 1>"}'
```

**Expected: `200 OK`**
```json
{
  "sevaOneUserId": 1,
  "name": "Test Citizen One",
  "mobile": null,
  "serviceId": "SVC001",
  "assistedMode": false,
  "operatorId": null,
  "returnUrl": "/dashboard"
}
```
(Most personal fields are `null` — the seeded test citizen has no mobile/Aadhaar/address, that's
expected, not a bug.)

**✅ Pass criteria:** `200`, `sevaOneUserId` matches what Step 1 gave you.
**⚠️ This token is now spent.** Calling User Details again with the same token gets you
`SEVA-DEPT-4003` ("Token already used") — that's the correct, single-use behaviour, not a failure.

---

## Step 3 — Post Acknowledgement

Simulates the department reporting the citizen finished on their site.

```bash
curl -s -X POST 'http://172.18.60.104/api/v1/dept/get-acknowledgement' \
  -H 'ApiKey: u4G8DT-yrd_I_zfHWwnBV93VtmikFOfSpKJfpu2ES4I' \
  -u 'nadakacheri_portal:nadakacheri_secret' \
  -H 'Content-Type: application/json' \
  -d '{"departmentId":"revenue","sevaOneUserId":1,"serviceId":"SVC001","status":"Submitted","acknowledgementId":"TEST-ACK-0001"}'
```
*(Pick your own `acknowledgementId` each test run — anything unique, e.g. include today's date.)*

**Expected: `200 OK`**
```json
{
  "linked": true,
  "returnUrl": "/api/v1/apply/return?token=CfDJ8H6C...(a different, new token)",
  "expiresAt": "2026-09-01T11:00:19.23Z"
}
```

**✅ Pass criteria:** `linked: true` (proves SevaOne correctly tied this acknowledgement back to
the citizen from Step 1/2), and a new `token=` present in `returnUrl`.

---

## Step 4 — Post Status Update

Simulates the department reporting a later status change — no token needed here, keyed on your
own `acknowledgementId` instead.

```bash
curl -s -X POST 'http://172.18.60.104/api/v1/dept/status-update' \
  -H 'ApiKey: u4G8DT-yrd_I_zfHWwnBV93VtmikFOfSpKJfpu2ES4I' \
  -u 'nadakacheri_portal:nadakacheri_secret' \
  -H 'Content-Type: application/json' \
  -d '{"departmentId":"revenue","acknowledgementId":"TEST-ACK-0001","status":"UnderReview","note":"Document verification completed, forwarded for approval"}'
```

**Expected: `200 OK`**
```json
{ "accepted": true, "changed": true }
```

**✅ Pass criteria:** `accepted: true`, `changed: true`.
Re-run the exact same call again — `changed` should flip to `false` (SevaOne already had it at
that status; that's correct, not a failure) while `accepted` stays `true`.

---

## Step 5 — Post Payment Status

Needs its **own fresh token** — the one from Step 1 was already spent in Step 2. Mint a new one
first (repeat Step 1), then:

```bash
curl -s -X POST 'http://172.18.60.104/api/v1/dept/payment-status' \
  -H 'ApiKey: u4G8DT-yrd_I_zfHWwnBV93VtmikFOfSpKJfpu2ES4I' \
  -u 'nadakacheri_portal:nadakacheri_secret' \
  -H 'Content-Type: application/json' \
  -d '{"departmentId":"revenue","token":"<FRESH TOKEN>","paymentStatus":"Success","amount":15,"transactionId":"TEST-TXN-0001"}'
```

**Expected: `200 OK`**
```json
{ "accepted": true, "status": "Success" }
```

**✅ Pass criteria:** `200`, `accepted: true`.

---

## Step 6 — Confirm idempotent retry

Re-send the **exact same** Step 5 request (same `transactionId`, same token).

**Expected: `200 OK`**, identical body:
```json
{ "accepted": true, "status": "Success" }
```

**✅ Pass criteria:** same `200` and same body as Step 5 — proves a department can safely retry a
timed-out payment POST without creating a duplicate record.

---

## Pass/fail summary

| Step | Call | Pass looks like |
|---|---|---|
| 1 | `test-token` | `200`, `isHomepageFallback: false`, `Token=` present |
| 2 | `user-details` | `200`, correct `sevaOneUserId` |
| 3 | `get-acknowledgement` | `200`, `linked: true` |
| 4 | `status-update` | `200`, `accepted: true` |
| 5 | `payment-status` | `200`, `accepted: true` |
| 6 | `payment-status` (repeat) | `200`, identical body to Step 5 |

All six passing = the department pipeline is genuinely working end-to-end, not just "the server
responds."

---

## Known limitation

Only `SVC001` (revenue, Income Certificate) currently has a working `deeplink_citizen_url`
configured — every other one of the 40 seeded services will `409` at Step 1
("no Apply URL or homepage configured yet"). This isn't specific to this test flow; the same gap
blocks a real citizen's Apply flow for those services too. Fixing it means adding a
`deeplink_citizen_url` (or `homepage_url`) to each service in `R__seed_lookup.sql` — out of scope
for this procedure, flagged here so it isn't mistaken for a test failure.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `401` "Missing Credentials" from a generic-looking body | `ApiKey` header missing/wrong, or the request didn't reach APIM |
| `401 SEVA-GATEWAY-4001` | Request bypassed APIM (shouldn't happen via the base URL above — only if you point curl straight at bff's port) |
| `401 SEVA-DEPT-4001/4002/4003` | Token invalid / expired (30 min) / already used — mint a fresh one (Step 1) |
| `409` at Step 1 | Wrong `serviceId` — use `SVC001` (see Known limitation) |
| `500` at Step 5 | Should not happen after the Dapper enum-binding fix (deployed and verified) — if it recurs, check `docker logs sevaone-bff-1` on the server for a stack trace |

Full error code reference: `backend/docs/DEPARTMENT_INTEGRATION_GUIDE.md`, Section 4.
