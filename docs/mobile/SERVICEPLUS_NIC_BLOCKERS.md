# SevaOne–ServicePlus Integration: Open Blockers

**To:** NIC / ServicePlus Platform Team
**From:** SevaOne Mobile App Team
**Date:** 2026-09-30
**Re:** Karnataka tenant integration (`https://sevaone.karnataka.gov.in/services/`)

All findings below were verified live against our own citizen account, using the same request contract as the ServicePlus reference Android app.

---

### 1. Service catalogue and application APIs return empty for a valid, logged-in citizen

`validateUser` succeeds and returns a valid session (`signNo`, roles, `citizenState`). Using that session, `getServiceJson`, `getAllApplRefNoByUser`, `getIncomleteApplList`, and `getAllRevalidatePaymentApplication` all return HTTP 200 with empty results.

**Ask:** Confirm the correct `states`/`dept`/`instance_state` values for a Karnataka citizen, and share one sample non-empty response for us to validate our parsing against.

### 2. No supported path to the Apply form without a manual CAPTCHA login

`directApply.do` (creates the application/`applId`) requires a human to complete a separate, CAPTCHA-protected web login — distinct from the mobile `validateUser` session. `renderApplicationForm.do`, called with a valid `serviceId` but no browser session, returns "Session Invalidated — Relogin," confirming it needs its own server-side session that `validateUser`'s tokens do not establish.

**Ask:** Confirm whether a server-to-server session-establishment method exists for an already-verified citizen, or whether the CAPTCHA-gated web login is the only supported route by design.

### 3. Embedded WebView is blocked

ServicePlus detects Android's `X-Requested-With` header and serves a "download the app" page instead of the form. We currently route around this using a Chrome Custom Tab.

**Ask:** Written approval to recognize our app for this flow, rather than a client-side workaround.

### 4. `getAllStateUrl` misresolves the Karnataka tenant

The registry entry for Karnataka points back to `serviceonline.gov.in` instead of the tenant URL NIC separately confirmed for us. Flagging as a likely defect on your side.

---

**For awareness (not blocking us):** `sevasindhuservices.karnataka.gov.in` (783 of 2,120 catalogue URLs) presents a TLS certificate issued for `sevaone.karnataka.gov.in`, confirmed from three independent resolvers.

---

*Related: `SERVICEPLUS_REGISTRATION_API_QUERY.md` (registration API / SSO ask), `SERVICEPLUS_NIC_URL_HANDOFF.md` (full contract checklist).*
