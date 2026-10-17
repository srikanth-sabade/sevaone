# Using WSO2 Identity Server to Its Full Potential

Where SevaOne stands today with WSO2 IS, and what's left on the table. Written against this
project's actual usage (`WSO2_IS_APIM_SETUP.md`, `TOKEN_FLOW.md`), not a generic WSO2 feature
list — every recommendation below ties to a real gap or pain point already hit in this project.

---

## 1. Where we actually are today

WSO2 IS is used for exactly one thing: **minting the citizen's session token, after identity is
already proven elsewhere.**

- Identity proof (DigiLocker consent / mobile OTP) happens with **zero WSO2 involvement**.
- Once proven, SevaOne does a SCIM2 find-or-create against WSO2 IS, then a JWT-bearer exchange to
  get a real access/refresh token pair, stored as `HttpOnly` cookies. That token pair *is* the
  citizen's session — the old app-issued `seva.sid` cookie is gone.
- Departments don't touch WSO2 IS **at all** — the old `client_credentials` OAuth2 step was
  dropped; department trust is now `ApiKey` (APIM) + `DeptMachineAuth` (a static Client ID/Secret
  table SevaOne owns itself), nothing WSO2-issued.

Everything else WSO2 IS ships with — adaptive auth, MFA, social/federated login,
self-registration and recovery, consent management, IS Analytics, acting as an IdP for *other*
service providers — is installed and running, unused.

---

## 2. Opportunities, ranked by how directly they fix a problem we already have

### 2.1 Turn WSO2 IS into the real broker for department OIDC — not just a token shop

**The problem this fixes:** we just sent KAIC a document (`KAIC_INTEGRATION_OPEN_ITEMS.md`) asking
two things — do they support OAuth2/OIDC, and can they build us a bespoke "is this person a
government officer" endpoint. Both are things WSO2 IS is *built* to do generically, for every
department, not just KAIC:

- Register each department that answers "yes" to OIDC as a **Service Provider** in WSO2 IS. WSO2
  becomes the IdP they federate against, instead of each department building and us consuming a
  one-off bespoke integration.
- The government-officer distinction (`sevaone_user.kgid` in our own schema, currently written by
  nothing) becomes a **SCIM2 group or user attribute** in WSO2 IS instead of a database column no
  process populates. A department checks group membership via a standard SCIM2/OIDC claim instead
  of us building — and every department separately building — a bespoke officer-check API. This
  turns "ask KAIC to build us an endpoint" into "read a claim WSO2 already gives you."

This is the highest-leverage item on this list because it replaces N bespoke bilateral
integrations (one per department, starting with KAIC) with one platform capability.

### 2.2 Adaptive / risk-based authentication (step-up MFA) for high-value transactions

**The problem this fixes:** every citizen action today — checking application status or
registering a property sale deed (SVC104, real money, real legal effect) — goes through the exact
same OTP-only login. WSO2 IS's adaptive authentication engine can require a second factor
*conditionally*: only for scheme categories like Property Registration, Mutation, or high
`department_fee` transactions, not for a citizen just checking a certificate's status. This is
configured entirely in WSO2 IS (adaptive auth scripts), no SevaOne backend change needed beyond
handling a slightly longer login step for those flows.

### 2.3 SCIM2 groups/roles instead of an implicit three-way split

**The problem this fixes:** SevaOne today has three separate, hand-rolled identity surfaces —
citizen (`AuthController`, WSO2-backed), operator (`OperatorController`, its own
username/password login, no WSO2 at all), and the not-yet-real government-officer distinction
(`kgid`). WSO2 IS's SCIM2 group model can represent all three as roles/groups on the *same* user
store — Citizen, Operator, GovtOfficer — instead of operator identity living entirely outside
WSO2 in a separate table. This is a real consolidation, not just a nice-to-have: one identity
store to audit instead of three.

### 2.4 Self-service password recovery for Operators

**The problem this fixes:** `OperatorController.Login` authenticates with a plain
`OperatorCode`/`Password` pair that SevaOne owns end-to-end — no path exists today for a kiosk
operator who forgets their password except an internal admin resetting it manually. If operators
move onto WSO2 IS as real users (see 2.3), WSO2's built-in self-service password recovery
(email/SMS-based) removes that manual-reset burden entirely, out of the box.

### 2.5 IS Analytics / login audit trail

**The problem this fixes:** a government citizen-services platform is exactly the kind of system
that eventually gets asked "who logged in as whom, when, from where" — for an audit, a grievance,
or a security incident. WSO2 IS Analytics gives that for free once wired up (login success/fail
history, session activity, anomaly flags) instead of SevaOne having to build its own audit
pipeline from scratch. `portal.audit_log` already exists in our own schema for
app-level actions; IS Analytics would cover the *authentication* layer specifically, which
`audit_log` currently doesn't capture in WSO2-native form.

### 2.6 The statewide-IdP story the docs already gesture at

`WSO2_IS_APIM_SETUP.md` already frames the current citizen-token design as groundwork for WSO2 IS
to become **Karnataka's statewide identity provider** — a citizen who verified via DigiLocker on
SevaOne carrying that identity to other government systems without redoing consent. Today that's
aspirational; nothing outside SevaOne actually trusts this token yet. Item 2.1 above (department
Service Providers) is the concrete first step that would make this real instead of dormant.

---

## 3. What's deliberately *not* recommended

- **Social login** (Google/Facebook-style federated IdPs) — Karnataka citizen identity already has
  a stronger, purpose-built proof (DigiLocker/Aadhaar-linked), so consumer social login adds
  attack surface without adding trust.
- **Replacing the department `ApiKey` + `DeptMachineAuth` model wholesale** — most onboarded
  departments are `stub`/non-OIDC integrations today; forcing all 11 onto WSO2 IS immediately
  would be a bigger migration than the current problem justifies. Item 2.1 should be opt-in, per
  department, starting with whichever department actually answers "yes" to the OIDC question.

---

## 4. Suggested order of attack

| Priority | Item | Why this order |
|---|---|---|
| 1 | 2.1 — Department Service Providers + SCIM2 officer-check | Directly answers the open KAIC question already in flight; highest reuse across future departments |
| 2 | 2.3 — SCIM2 groups for Citizen/Operator/GovtOfficer | Foundational — 2.1's officer claim needs a group model to exist first |
| 3 | 2.2 — Adaptive auth for high-value transactions | Real risk reduction, config-only change once the group model (or even without it) is in place |
| 4 | 2.4 — Operator self-service password recovery | Falls out naturally once operators are real WSO2 IS users under 2.3 |
| 5 | 2.5 — IS Analytics | Lower urgency, but cheap once the rest of the identity model has stabilized |
| — | 2.6 — Statewide IdP | Not a project of its own — it's the natural end-state once 2.1 has been proven with a couple of real departments |
