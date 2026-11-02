# E2E checklist — assisted mode

## Prerequisites

- Flyway through **V8** (`email` / `wso2_subject` on `operator_master`) + seed operator row.
- WSO2 provisioned operator **or** BFF demo fallback: `operator.blr047@seva.local` / `Operator@123`.
- SPA `:5174`, BFF `:5080`, Nadakacheri UI `:4500`, revenue mock `:5091`.

## Operator auth

- [ ] Assisted tab shows **email + password only** (no channel / Operator ID / centre / PIN).
- [ ] A1: login succeeds → `/operator` → wallet and centre from profile.
- [ ] A2–A5: bad password / citizen email / missing row / inactive → denied.
- [ ] A6: second login invalidates first `seva.opr` session.
- [ ] Non-operator cannot open console (`/operator/me` 403).

## Citizen bind (no citizen cookie)

- [ ] Lookup `9876543210` (after that citizen exists) or register a new mobile.
- [ ] Challenge OTP appears in BFF logs (and MailHog when email known).
- [ ] Bind sets active citizen; unbind clears it.
- [ ] Incomplete profile prompts assisted profile completion.

## Console + handoff

- [ ] Stepper: Bind → Consent → Service/fee → Submit.
- [ ] Revenue SVC001: collect fee → open applyUrl → Nadakacheri shows assisted banner + operator id.
- [ ] Non-revenue: collect → `POST /operator/applications` ack.
- [ ] Printable ack modal works.
- [ ] `GET /operator/me/txns` lists shift collections.

## Money

- [ ] Collect is atomic (deduct + ledger).
- [ ] Forced mint failure path reverses fee.
- [ ] Insufficient balance returns conflict and clear UX.

## Shift polish

- [ ] Idle ~15 minutes ends session.
- [ ] End shift shows shift report toast / `shift-report` payload.
- [ ] Bound citizen can apply for a second service without re-bind.

## Regression

- [ ] Citizen login (Email OTP → `seva.sid`) unchanged.
- [ ] Self-service apply-token still requires citizen session when `assistedMode=false`.
