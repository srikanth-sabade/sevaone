# Full platform journey — assisted mode

Assisted (counter) flow is separate from citizen self-service. Operators use cookie `seva.opr`; citizens use `seva.sid`. A kiosk should complete assisted apply **without** a citizen cookie.

## Operator login (email + password only)

1. SPA Assisted tab → `POST /api/v1/operator/login` `{ email, password }`.
2. BFF: WSO2 password grant + userinfo (demo fallback when WSO2 unreachable).
3. Require linked active `portal.operator_master` row (email / `wso2_subject`). No operator OTP.
4. Redis `OperatorSession` (8h) + Set-Cookie `seva.opr`. Re-login revokes the previous operator session.
5. `/operator/me` returns provisioned centre, channel (`operator_type`), operatorId, wallet, optional `activeCitizenId`.

Demo: `operator.blr047@seva.local` / `Operator@123` → OPR1042 · BLR-047 · K1.

## Citizen at counter (bind)

| Step | API |
| --- | --- |
| Lookup | `POST /operator/citizens/lookup` `{ mobile }` |
| Presence OTP (citizen, not operator) | `POST /operator/citizens/challenge` → SMTP + BFF log |
| Bind | `POST /operator/citizens/bind` `{ challengeId, otp }` → `ActiveCitizenId` |
| Register | `POST /operator/citizens/register` |
| Profile completion | `POST /operator/citizens/profile` |
| Unbind | `POST /operator/citizens/unbind` |

## Fee → handoff / submit

1. `POST /operator/wallet/collect` — single UoW deduct + `operator_wallet_txn`.
2. Revenue: `POST /schemes/{code}/apply-token` `{ assistedMode: true }` uses **ActiveCitizenId** (not `ICurrentCitizen`); SPA opens `applyUrl`.
3. Non-revenue: `POST /operator/applications` (same submit path; citizen from ActiveCitizenId).
4. Mid-flow failure after fee → `POST /operator/wallet/reverse`.
5. Shift txns: `GET /operator/me/txns`; end-shift summary: `GET /operator/me/shift-report`.

Nadakacheri shows an assisted banner when validate / URL carries `assistedMode` + `OperatorID`.

## Scenario matrix (assisted)

| ID | Case | Expect |
| --- | --- | --- |
| A1 | Happy email/password | `seva.opr` + console |
| A2 | Bad password | 403 |
| A3 | Non-operator email (no master row) | 403 |
| A4 | Missing master row | 403 |
| A5 | Inactive operator | 403 |
| A6 | Re-login | Previous session revoked |
| B1 | Bind known mobile + OTP | ActiveCitizenId set |
| B2 | Register + bind | New citizen, no `seva.sid` |
| C1 | Assisted SVC001 | Fee → applyUrl → Nadakacheri assisted banner |
| C2 | Fee OK, mint fails | Auto reverse |
| C3 | Insufficient wallet | 409 UX |
| D1 | Idle 15m | Console logout |
| D2 | Multi-service while bound | Second service without re-bind |
| E1 | Citizen self-service | Unchanged Email OTP → `seva.sid` |

Audits: `operator.login` / `login.failed` / `logout` / `wallet.collect` / `wallet.reverse`.
