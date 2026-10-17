# Stage Engine spike

A working proof of the custom process model for **native** services (the ones whose form, fee and
approval live in SevaOne, as opposed to the token-based department hand-off). It answers one
question: can a small, custom, testable state machine express multi-stage services where each
stage has its own fee and its own officer approval, and stage N+1 stays locked until stage N is
approved?

**Status:** spike. Pure logic plus in-memory stores, 46 unit tests passing. Not registered in
dependency injection, no HTTP surface, no database. Nothing in the running application calls it.

## What is here

| Path | What it is |
|---|---|
| `SevaOne.Business/Stages/Packages/` | `ServicePackage` records and `ServicePackageParser` (strict JSON, unknown properties rejected) |
| `SevaOne.Business/Stages/Runtime/` | `StageEngine`, the pure state machine, plus the case model and events |
| `SevaOne.Business/Stages/Service/` | `StageCaseService` (load, step, save), store interfaces, in-memory stores |
| `SevaOne.Business/Stages/Samples/PilotPackages.cs` | The pilot: a two-stage income certificate |
| `tests/SevaOne.UnitTests/Stages/` | Tests for the parser, every engine rule, and the service |

## The model

A **service package** is versioned JSON: an ordered list of stages. Per stage, form, fee and
approval are each optional.

```json
{ "id": "application", "name": "Application and fee", "formId": "income-cert-application",
  "fee": { "amount": 30, "gateway": "paygov" },
  "approval": { "role": "tahsildar", "slaDays": 7 },
  "expiresAfterDays": 30 }
```

The pilot values (fee, gateway, roles, SLA) are **placeholders**, not department rules.

Per stage the flow is `Open -> submit -> [FeeDue -> paid] -> [AwaitingApproval -> approve] -> Approved`.
Approval unlocks the next stage; approving the last stage issues the case.

```
Open --submit--> FeeDue --paid--> AwaitingApproval --approve--> Approved --> next stage Open / case Issued
 ^                  | failed          |  return                    (no fee: submit goes straight to approval)
 |                  v                 v                            (no approval: paid goes straight to Approved)
 |              FeeFailed --retry--> FeeDue     Returned --resubmit--> (approval, fee not charged again)
 |                                    reject --> Rejected (terminal)
 +-- applicant window lapses (Open / Returned / FeeDue / FeeFailed) --> Expired (terminal)
```

## Rules the tests pin down

- **Pure engine.** Every step takes the package, the case and "now", and returns a new case plus
  events. No I/O, no clock. A no-op returns the *same* case instance.
- **Version pinning.** A case runs only against the package version it started on; any other
  version is refused. Publishing a new version never changes cases in flight.
- **Waits are events, never polled.** Fee wait ends on a gateway callback, approval wait on an
  officer's decision, expiry on a timer calling `Expire`.
- **Idempotent payments.** Fee references are deterministic (`<caseId>-<stage>-<attempt>`).
  A repeated "paid" is a no-op; a stale "failed" after payment is ignored; a late success for an
  earlier attempt is honoured.
- **Money with nowhere to go is reported, not lost.** A second different payment emits
  `DuplicatePayment`; a payment for an expired stage emits `OrphanedPayment`. State is untouched,
  and the events reach the outbox so a refund can be raised.
- **Officer authority.** Deciding needs the stage's approval role (case-insensitive). Returning or
  rejecting needs a written reason.
- **Applicant window.** `expiresAfterDays` runs only while the applicant owes an action. A stage
  with an officer never expires, and a return grants a fresh window.
- **Optimistic concurrency.** Every change bumps `Revision`; a save against a stale revision throws.

## Defaults chosen (change any of these)

1. A fee already paid for a stage is **not charged again** when an officer returns it for correction.
2. One approval role per stage. No multi-approver or parallel approval.
3. Roles are passed in as input. Mapping WSO2 IS groups to roles is not done.
4. `FormJson` is opaque. The engine does not validate form data against a schema.
5. Expiry is for the applicant only. SLA overdue is reported (`IsApprovalOverdue`) but triggers no escalation.

## Not built yet (suggested next increment)

- Postgres stores and a migration. Sketch: `stages.service_package(service_code, version, json)`,
  `stages.case(id, service_code, package_version, status, current_stage, revision)`,
  `stages.case_stage(case_id, idx, ...)`, and a `stages.fee_attempt` table for refund reconciliation.
  Write case, stage rows and outbox rows in one transaction and guard the update with
  `WHERE revision = @expected`. Reuse the existing outbox table and dispatcher.
- A controller for applicant actions, an officer worklist query, and a payment-callback endpoint
  behind the gateway-secret pattern already used for `/dept/*`.
- A real gateway adapter (one of PayGov, SBI ePay, CSC eWallet, eTreasury) behind the fee request event.
- Timers: a job calling `Expire` for applicant windows and flagging overdue approvals.
- Conditional stages (skip a stage based on form answers) and multi-approver stages, if a real service needs them.

## Running the tests

There is no local .NET SDK on this machine; the tests were run in the SDK container, on a copy so
nothing is written into the repo:

```bash
cd apps/Seva-One/backend
docker run --rm --user "$(id -u):$(id -g)" -e HOME=/tmp -e DOTNET_CLI_HOME=/tmp -e NUGET_PACKAGES=/tmp/nuget \
  -v "$PWD":/src:ro mcr.microsoft.com/dotnet/sdk:10.0 bash -c \
  'mkdir -p /tmp/work && cd /src && tar cf - --exclude=bin --exclude=obj . | (cd /tmp/work && tar xf -) \
   && cd /tmp/work && dotnet test tests/SevaOne.UnitTests/SevaOne.UnitTests.csproj'
```

With a local SDK 10 it is simply `dotnet test tests/SevaOne.UnitTests`.
