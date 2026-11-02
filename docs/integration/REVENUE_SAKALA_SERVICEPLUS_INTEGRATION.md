# Revenue / Sakala Token-Based Integration: What It Is, and How to Bring It Into SevaOne

## Source

`Revenue_Services_Integration_Documentation.docx`, provided by the department side. It documents
an existing, live integration between **Service Plus** (a kiosk / assisted-mode front end used by
Common Service Centres and similar operators) and the **legacy SevaSindhu `.NET` application**
(`sevasindhu.karnataka.gov.in`). No SevaOne-side code or design is described in the source
document — it is a description of the *current, pre-SevaOne* flow.

## What the documented flow actually does

1. **Service Plus → SevaSindhu redirect.** Service Plus sends the citizen's browser to
   `NK5RedirectPage.aspx` with `UserId` and `RaisedLoc` (the location/context the request was
   raised from) as query parameters.
2. **Payment.** SevaSindhu's own `PaymentPage.aspx` handles the fee payment, keyed by `UserId` and
   a `Token`.
3. **Post-payment return.** SevaSindhu redirects the citizen back to a department-configured
   return URL.
4. **Sakala token generation.** For Sakala Act services, a separate step calls
   `sakalatokenAPI_Live/api/token_generation` (`POST`, body `{Assisted_Mode, Service_Id,
   User_Id}`) and gets back `{statusCode, Token}`.
5. **Token-based redirect to CSG.** The citizen's browser is sent to
   `TokenBased_CSG/STBRedirectToCSG.aspx` (and a near-identical
   `SakalaTokenRedirection_Live/STBRedirectToDepartment.aspx`) with `User_Id`, `Token`,
   `KioskType`, and `ServiceID` as query parameters.

The `Assisted_Mode: "Y"` and `KioskType=Y` fields are the important detail: **this is the
kiosk/assisted-mode channel**, not a citizen using a self-service web app directly. An operator at
a CSC (Common Service Centre) or Karnataka One kiosk is submitting on the citizen's behalf.

## How this compares to what SevaOne already has

SevaOne does not currently have a version of this flow, but it already has two related — and
architecturally quite different — pieces for Revenue services specifically:

- **`backend/SevaOne.Infrastructure/Connectors/RevenueDepartmentConnector.cs`** — a pure
  server-to-server REST integration with Nadakacheri: `sessions` → `applications` → `payments` →
  `esign`, all authenticated with a bearer token, no citizen-facing redirects at all. Payment is a
  `POST payments` call whose response names `"Khajane-II"` as the gateway, but the current
  implementation is a mock stub (`PaymentResponse("PAY-REV", "Khajane-II", "Success", ...)`) — no
  real Khajane-II integration exists in this codebase yet, on either side.
- **`backend/docs/DEPARTMENT_INTEGRATION_GUIDE.md`** — SevaOne's own token-based handoff protocol,
  but running in the *opposite* direction from Service Plus → SevaSindhu: SevaOne sends the
  citizen **out** to a department portal with a one-time token, the department confirms it via
  API, and sends the citizen back with a new token. This is the same shape as the Sakala flow
  (token in the URL, confirm server-side, redirect back) but for the SevaOne self-service web
  channel, not the kiosk channel.

So SevaOne already replaced SevaSindhu's role for citizens using SevaOne's own web app. It has
**not** replaced SevaSindhu's role for the kiosk/assisted-mode channel described in this document,
and it's not yet clear that it should — see the open questions below.

## Two different ways to "integrate this," depending on intent

### Option A — Sakala compliance only (no redirect, no kiosk channel)

If the actual requirement is just "every Revenue application must be registered with the state
Sakala tracking system for its SLA guarantee, regardless of which channel it came through," this
does not require touching the citizen-facing flow at all:

- Add a call to the real Sakala Token Generation API (`Assisted_Mode`, `Service_Id`, `User_Id`)
  from inside `RevenueDepartmentConnector.SubmitAsync`, right after (or as part of) the existing
  `applications` call, and store the returned `Token` alongside the SevaOne application record
  (e.g. a new column on `ServiceApplication`, or a new small table if other departments end up
  needing the same field).
- No new citizen-facing pages or redirects. `Assisted_Mode` would presumably be `"N"` for SevaOne's
  self-service traffic, since the citizen never leaves SevaOne.
- Smallest, lowest-risk change of the three options — but only correct if Sakala registration is
  genuinely channel-independent, which needs confirming (see below).

### Option B — SevaOne becomes the kiosk-channel destination

If the actual requirement is "Service Plus (or its successor) should redirect into SevaOne instead
of SevaSindhu," this is a materially bigger build:

- A new citizen-facing landing endpoint that accepts `UserId`/`RaisedLoc` (or their SevaOne
  equivalents) the way `NK5RedirectPage.aspx` does today — most naturally modeled as a new route
  alongside `PortalHandoffPage`/`ApplyPage` in the frontend, backed by a new controller action next
  to `AuthController`/`ApplicationsController`.
- A real payment integration (Khajane-II or whatever SevaOne ends up using), since this channel
  can't assume the citizen already has a SevaOne login/session the way the web app does — `Token`
  in this flow is the only credential carried across redirects, not a WSO2-issued session.
- SevaOne's own Sakala Token Generation call and `STBRedirectToCSG.aspx`-equivalent redirect, using
  SevaOne's own domain in place of `sevasindhu.karnataka.gov.in`.
- This would need Service Plus (or whichever system currently points at SevaSindhu) to be
  reconfigured with SevaOne's URLs — a change outside this codebase, coordinated with the
  department/CSC side.

### Option C — Do nothing yet

It's also possible this document was shared purely as background/reference and no channel change
is planned in the near term. Worth ruling in or out explicitly before spending effort on A or B.

## Open questions (need the department/product owner, not guessable from the document)

1. **Is Sakala token generation a compliance requirement independent of channel**, or is it
   specifically tied to the kiosk/assisted-mode flow? This decides between Option A and Option B
   outright.
2. **Who owns the Sakala Token Generation API** (`sakalatokenAPI_Live`) — is it SevaSindhu's own
   endpoint, or the state Sakala Mission's shared API that any integrated system (including
   SevaOne) is entitled to call directly? If it's shared infrastructure, SevaOne calling it
   directly is straightforward; if it's SevaSindhu-owned, that's a different conversation with a
   different owner.
3. **Is there an actual plan to retire Service Plus → SevaSindhu for Revenue services**, and on
   what timeline? Building Option B before that's confirmed would be speculative work.
4. **Does SevaOne need a real Khajane-II integration at all**, independent of this document?
   `RevenueDepartmentConnector`'s payment step is currently a mock on both the SevaOne and
   Nadakacheri sides — this document doesn't create that gap, but it does make it relevant sooner
   if Option B is in scope.
5. **Are `UserId` values shared/mappable between SevaSindhu and SevaOne**, or would a citizen have
   two disconnected identities across the two systems during any transition period?
6. The sample URLs and payloads in the source document are explicitly marked as needing validation
   against the current deployment configuration — worth getting current, confirmed values (or a
   sandbox/test credential set) before building against any of them.
