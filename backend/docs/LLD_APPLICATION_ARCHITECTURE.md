# SevaOne — Low-Level Design (Application Architecture)

Companion to `HLD_APPLICATION_ARCHITECTURE.md`. Where that document shows components and flows,
this one shows classes, methods, schemas, and contracts — verified directly against the current
codebase, including live testing done against the running services and database this session.

---

## Contents

1. [.NET backend — module and class map](#1-net-backend--module-and-class-map)
2. [.NET backend — middleware pipeline](#2-net-backend--middleware-pipeline)
3. [.NET backend — controller/endpoint contracts](#3-net-backend--controllerendpoint-contracts)
4. [.NET backend — core service internals](#4-net-backend--core-service-internals)
5. [.NET backend — error code catalog](#5-net-backend--error-code-catalog)
6. [Postgres schema — full detail](#6-postgres-schema--full-detail)
7. [WSO2 integration internals](#7-wso2-integration-internals)
8. [AI backend — module map and internals](#8-ai-backend--module-map-and-internals)
9. [AI backend — SQLite schema](#9-ai-backend--sqlite-schema)
10. [AI backend — API contract](#10-ai-backend--api-contract)
11. [Frontend — routes and API clients](#11-frontend--routes-and-api-clients)
12. [Department reference implementation](#12-department-reference-implementation)

---

## 1. .NET backend — module and class map

```
SevaOne.Domain/
  Models/            Citizen, DeptUserRef, SevaOneToken, ServiceApplication, DeptGatewayAuthOptions, ...
  Enums/             OperatorType (G1|K1|B1), ApplicationStatus, PaymentPostStatus
  Common/
    SevaErrorCodes.cs   static classes: Gateway, Auth, Request, Dept — see §5
    Exceptions.cs        DomainException(Code?), NotFoundException, ConflictException, ForbiddenException

SevaOne.Business/
  Portal/DTOs/           GetUserDetailsRequest, UserDetailsResult, PostPaymentStatusRequest/Result,
                          CompleteApplicationRequest/Result
  Portal/Services/       PortalApplyService, PortalDeptFacingService, CitizenSessionService
  Portal/Validators/     DeptFacingValidators.cs, Validators.cs (FluentValidation, auto-registered
                          via AddValidatorsFromAssembly)
  Portal/Mappings/       PortalProfile.cs (AutoMapper, ForCtorParam for token-sourced fields)

SevaOne.Infrastructure/
  Repositories/          Discovery/, Portal/, Orchestration/, Status/, Audit/ — Dapper, one
                          function call per method, no inline SQL
  ExternalAuth/           Wso2CitizenIdentityClient, IWso2CitizenIdentityClient,
                          Wso2CitizenIssuerOptions, IDigiLockerClient, Wso2TokenIntrospectionCache
  Security/               IJwsTokenService (sign/verify/hash), IApplyRedirectProtector
                          (encrypt/decrypt the redirect URL's token), IPayloadProtector

SevaOne.Services/
  Controllers/            AuthController, DeptController, CatalogController, ApplicationsController,
                          SevaControllerBase (shared ValidateAsync helper)
  Middleware/              CitizenGatewayAuthMiddleware, DeptGatewayAuthMiddleware,
                          Wso2CitizenAuthMiddleware, OperatorSessionMiddleware
  Auth/                    DeptMachineAuth (static TryAuthorize)
  HealthChecks/            Wso2HealthCheck
  Program.cs               composition root — DI registration, middleware order, exception mapping
```

Dependency direction is strictly one-way: `Services → Business → Infrastructure → Domain`.
`Domain` has zero dependencies on anything above it.

---

## 2. .NET backend — middleware pipeline

Registered in `Program.cs`, in this exact order:

```
1. CitizenGatewayAuthMiddleware   -- /api/* : rejects anything not stamped by the citizen APIM product
2. DeptGatewayAuthMiddleware       -- /api/v1/dept/* : X-SevaOne-Dept-Gateway-Secret, FixedTimeEquals,
                                      fail-closed outside Development even if unconfigured
3. Wso2CitizenAuthMiddleware       -- resolves the citizen session from the WSO2 access-token cookie,
                                      Redis-cached introspection
4. OperatorSessionMiddleware       -- resolves an assisted-mode operator session, if present
   [controller action runs]
   Global exception handler        -- catches DomainException family, maps .Code -> JSON {code, title};
                                      FluentValidation's ValidationException -> SEVA-REQUEST-4001
```

A request to `/api/v1/dept/*` that bypasses APIM entirely (hits port 5080 directly) is rejected
by step 2 before DeptMachineAuth or any business logic ever runs — verified live this session:
direct calls return `401 SEVA-GATEWAY-4001`; the identical call through APIM without an `ApiKey`
returns APIM's own `900902` first, never reaching step 2 at all.

---

## 3. .NET backend — controller/endpoint contracts

### AuthController (citizen-facing, via `sevaone-citizen` gateway)

| Method | Path | Purpose |
|---|---|---|
| POST | `/auth/mobile/start` | Send OTP to a mobile number |
| POST | `/auth/mobile/verify` | Verify OTP; `found: true` = login (sets WSO2 cookies), `found: false` = signup needed |
| POST | `/auth/digilocker/start` | Begin DigiLocker/CeG redirect flow |
| GET | `/auth/digilocker/callback` | DigiLocker return leg — parses signed eAadhaar, provisions WSO2 |
| PATCH | `/me/profile` | Complete/update citizen profile post-signup |

### DeptController (department-facing, via `sevaone` gateway, `/api/v1/dept`)

All three actions call `TryResolveDepartment()` first (machine-auth), then `ValidateAsync(request, ct)`
(FluentValidation), then the service.

**`POST /user-details`** — body `{ departmentId?: string, token: string }`.
Response `200`:
```json
{
  "sevaOneUserId": 123,
  "name": "Ramesh Kumar",
  "mobile": "9876500000",
  "email": "ramesh@example.com",
  "aadhaarMasked": "XXXX-XXXX-3210",
  "dateOfBirth": "1990-04-12",
  "gender": "Male",
  "stateName": "Karnataka",
  "districtName": "Bengaluru Urban",
  "taluk": "Bengaluru North",
  "villageName": null,
  "address": "12, MG Road, Shivajinagar",
  "serviceId": "SVC001",
  "assistedMode": false,
  "operatorId": null,
  "expiresAt": "2026-08-14T18:15:00Z",
  "returnUrl": "/dashboard"
}
```
One call, used for both a citizen the department already knows and a first-time citizen — the
response shape is identical either way (merged from what were previously two separate endpoints,
`token-validate` and `user-details`, this session).

**`POST /get-acknowledgement`** — body `{ departmentId?, sevaOneUserId: long, serviceId: string, status: string, acknowledgementId?: string }`.
Response `200`: `{ linked: bool, returnUrl: string|null, expiresAt: datetime|null }`.
`linked: false` (both fields null) is the normal outcome for a citizen with no SevaOne relationship
to return to — not an error.

**`POST /payment-status`** — body `{ departmentId?, token, paymentStatus, amount?, transactionId? }`.
Response `200`: `{ accepted: bool, status: PaymentPostStatus }`. Token rule here is different from
the other two — valid until midnight of the issue day, not the 30-minute apply-token window.

### CatalogController / ApplicationsController

`GET /schemes/search?q=` → discovery repo full-text-ish search. `POST /applications` → the
direct-integration ("connector") submission path, separate from the redirect/token journey —
used by departments with a real `IDepartmentConnector` implementation rather than the handoff API.

---

## 4. .NET backend — core service internals

**`PortalApplyService.MintApplyTokenAsync(citizen, serviceCode, assistedMode, operatorGuid, operatorCode, ct)`**
1. Look up scheme + department, verify both active.
2. Pick a deep link for the audience: `assistedMode ? (DeeplinkOperatorUrl ?? DeeplinkCitizenUrl) : DeeplinkCitizenUrl`.
3. If that's blank: no deep-link integration for this department/service yet. If `HomepageUrl` is
   also blank, throw `ConflictException`. Otherwise write an `apply.homepage_fallback[.assisted]`
   audit entry and return `ApplyTokenDto(HomepageUrl, ExpiresAt: null, IsHomepageFallback: true)`
   directly — no token minted, since there's no department endpoint that would ever redeem one.
4. Otherwise, proceed to mint a token: `expireAt = now.AddMinutes(30)`; `midnightValidUntil` =
   end of day UTC (IST timezone decision still pending — flagged in code).
5. `_jws.Mint(JwsClaims(...))` → signed JWS + hash.
6. Insert `portal.sevaone_token` row (`ExpireAt`, `MidnightValidUntil`, `ReturnUrl = "/dashboard"`
   fixed for now).
7. Write an `apply.token.issued[.assisted]` `audit.audit_logs` entry.
8. `_redirectProtector.Protect(tokenValue)` — encrypts before embedding in the URL; the DB row
   stores the plaintext JWS (hashed for lookup), the URL never carries it.
9. Build `applyUrl = {deeplinkUrl}?DepartmentID=...&ServiceID=...&SevaOneUserID=...&Token=...`,
   return `ApplyTokenDto(applyUrl, expireAt, IsHomepageFallback: false)`.

**`PortalDeptFacingService.GetUserDetailsAsync(request, ct)`**
1. `_gate.ResolveAsync` — decrypt, hash-lookup, existence check → `SEVA-DEPT-4001` on failure.
2. Expiry check → `SEVA-DEPT-4002`.
3. `_gate.TryRedeemAsync` — atomic `UPDATE ... WHERE revoked_at IS NULL` → `SEVA-DEPT-4003` if
   already redeemed (lost the race or genuine replay).
4. Fetch scheme (`SEVA-DEPT-4005` if missing), fetch operator code if `AssistedMode`.
5. Fetch citizen (`SEVA-DEPT-4004` if missing — internal inconsistency).
6. `_orchestration.UpsertDeptUserRefAsync` — records "this department now knows this citizen."
7. Map via AutoMapper, then `with { ServiceId, AssistedMode, OperatorId, ExpiresAt, ReturnUrl }`.

**`PortalDeptFacingService.CompleteApplicationAsync(request, ct)`**
1. Look up citizen by `SevaOneUserId` — if none, return `linked: false` (not an exception; a
   citizen with no SevaOne relationship is an expected case, not a failure).
2. Look up scheme (`SEVA-DEPT-4005`) and department (`SEVA-DEPT-4006`).
3. Check for an existing `orchestration.applications` row keyed on
   `(citizen_id, scheme_id, department_id, department_reference)` — insert only if absent, so a
   repeat `get-acknowledgement` call never duplicates a row.
4. `sakala_due_at = submitted_at + scheme.SakalaDays`.
5. Mint a **second**, distinct completion token (10 min, no midnight window), insert, encrypt,
   return `{ linked: true, returnUrl, expiresAt }`.

**`DeptMachineAuth.TryAuthorize(request, jwsOptions, out departmentId, out reason)`** — checks
HTTP Basic or the two `X-SevaOne-Dept-*` headers against `appsettings.json` → `Jws:DeptClients[]`
(`{ ClientId, ClientSecret, DepartmentId }` triples). Returns a `reason` string
(`no_dept_clients_configured` | `missing_credentials` | invalid) mapped to a specific error code
by the controller, not a generic 401.

---

## 5. .NET backend — error code catalog

Format `SEVA-<AREA>-<NNNN>`. Permanent once shipped — never renumbered or reused.

| Code | HTTP | Area | Meaning |
|---|---|---|---|
| `SEVA-GATEWAY-4001` | 401 | Gateway | Request didn't come through APIM's department gateway |
| `SEVA-AUTH-4001` | 401 | Auth | Department client credentials missing |
| `SEVA-AUTH-4002` | 401 | Auth | Department client credentials invalid |
| `SEVA-AUTH-4003` | 403 | Auth | Body `departmentId` doesn't match authenticated client |
| `SEVA-AUTH-4004` | 401 | Auth | No department clients configured server-side |
| `SEVA-REQUEST-4001` | 400 | Request | Field present but invalid (FluentValidation) |
| `SEVA-DEPT-4001` | 401 | Dept | Token invalid / wrong department |
| `SEVA-DEPT-4002` | 401 | Dept | Token expired |
| `SEVA-DEPT-4003` | 401 | Dept | Token already used |
| `SEVA-DEPT-4004` | 404 | Dept | Citizen not found for token (internal inconsistency) |
| `SEVA-DEPT-4005` | 404 | Dept | Scheme/service not found |
| `SEVA-DEPT-4006` | 404 | Dept | Department not found |
| `SEVA-DEPT-4007` | 400 | Dept | Unrecognized payment status value |
| `SEVA-DEPT-4008` | 401 | Dept | Token has no payment-status validity window |
| `SEVA-DEPT-4009` | 401 | Dept | Past payment-status midnight validity window |

Plus two shapes with **no** `SEVA-*` code: APIM's own numeric faults (`900901` invalid key,
`900902` missing key, `900800` rate limit) and ASP.NET's default model-binding shape (a field
missing from the JSON entirely, caught before FluentValidation ever runs) —
`{ type, title, status, errors, traceId }`.

---

## 6. Postgres schema — full detail

Six schemas, one database, every table `uuid`-keyed with FKs to `uuid` (never business codes),
every column `timestamptz`/`boolean` (never naive datetime or Y/N), every read/write through a
named Postgres function (`database/migrations/functions/<schema>/R__sp_*.sql`).

**`discovery`** — `department_master` (business `department_id`, `portal_domain`,
`connector_type`, `is_integrated`, self-referencing `parent_department_id` +
`hierarchy_level` 1/2 for a department's own sub-departments — collapsed from a separate
`sub_department_master` table, which had zero live rows and one caller-less function) ·
`department_onboarding` (1:1 with department — contact info, nodal-officer nomination,
`ai_enabled`, and an ordered `onboarding_stage`: `NotStarted`/`ApiImplemented`/`Live`) ·
`sevaone_service_master`
(business `service_id` + department's own `department_service_id`, `sakala_days`,
`department_fee`/`service_charge`, `jurisdiction`, `mode_of_service`, JSON arrays for
documents/eligibility/aliases/steps) · `sevaone_service_document` (per-service docs, English +
Kannada name, `is_mandatory`) · view `v_sevaone_service_document_flat` (denormalized join for
ad-hoc access).

**`portal`** — `sevaone_user` (`id` uuid PK, `sevaone_user_id` bigint public identity,
`wso2_subject`, English + Kannada name/address/district/state/village, `mobile_verified_at`,
`aadhaar_masked`/`aadhaar_hash` (HMAC-SHA256, unique where present)) · `operator_master`
(`operator_type` CHECK `G1|K1|B1`, PBKDF2 password hash, `wallet_id` — this operator's identifier
on the external wallet provider; no local balance column, balance is checked there in real time
once that integration exists) · `sevaone_token` (one table, two kinds
of row — apply-token 30 min with `midnight_valid_until`, completion-token 10 min without;
`revoked_at`/`is_active` drive atomic one-time redeem; rows never deleted).

**`orchestration`** — `applications` (`seva_reference` `SVK{year}{6-digit}`, `sakala_due_at`,
unique on `citizen_id, scheme_id, department_id, department_reference`) ·
`department_sessions` (connector-mode only, encrypted payload) · `dept_user_ref` (unique per
`department_id, citizen_id` — "this department now knows this citizen") ·
`operator_wallet_txn` · `outbox_messages`.

**`status`** — `application_events` (FK `applications`, cascade delete) · `payment_status_post`
(idempotency key unique where present).

**`audit`** — `audit_logs` (action, resource, ip_address, indexed on `at` alone and on
`(citizen_id, at DESC)` for the citizen-scoped read path) · `db_errors`
(written by every function's `EXCEPTION WHEN OTHERS` handler, logged **and** re-raised).

**`notification`** — `notifications` (channel default `'log'` — mostly unused in the redirect
journey today).

---

## 7. WSO2 integration internals

**SCIM2 user resource** created per citizen (lazily, first WSO2-backed session):

| Attribute | Value |
|---|---|
| `userName` | `sevaone-{sevaOneUserId}` |
| `password` | random 24 bytes, generated once, never reused |
| `name.formatted/.givenName/.familyName` | split on last space |
| `emails[0]` / `phoneNumbers[0]` | only if present on the citizen |
| `addresses[0].country` | fixed `"India"` |

Authentication is never a WSO2 password — SevaOne mints a 5-minute self-signed JWT
(`sub = scimUsername`), exchanged via RFC 7523 `urn:ietf:params:oauth:grant-type:jwt-bearer`
against a Trusted Token Issuer identity provider (matched on **name**, not alias, for `iss`; on
**tokenEndpoint alias**, not URL, for `aud` — two different IdP fields).

**APIM provisioning** — one-time: `SevaOne-API` product (`/sevaone`, `1.0.0`) imported from the
live OpenAPI spec, published, deployed; `WSO2-IS-KM` key manager registered pointing at the same
WSO2 IS instance (shared OAuth endpoints for APIM's own admin use — department traffic doesn't
use this key manager, it uses `ApiKey`). Per-department: one Application, one long-lived
self-verifying API key (`/applications/{id}/api-keys/PRODUCTION/generate`), one Bronze-tier
rate-limit subscription (1000 req/min).

Both WSO2 IS and APIM run on their own default embedded H2 database in this environment — no
external DB override configured. SevaOne never queries either product's internal tables
directly, only their REST APIs.

---

## 8. AI backend — module map and internals

```
ai_backend/app/
  main.py                 FastAPI app, lifespan: init_db -> load embedder -> load FAISS indexes
  config.py                pydantic Settings (.env-driven); ACTIVE_KNOWLEDGE_SOURCE switch
  api/routes/
    health.py, ingestion.py, chat.py    -- POST /api/v1/ai-chat/message, /message/stream (SSE)
  api/dependencies.py      process-global singletons (embedder, 2x FAISS store); per-request DI
                            for RAGRetriever/Matcher/Clarifier/Answerer/SessionManager/Orchestrator
  db/
    base.py                 async SQLAlchemy engine/session
    models.py                see §9
  embeddings/bge_m3.py      BGEM3Embedder — BAAI/bge-m3, 1024-dim, CPU, fp16
  vectorstore/faiss_store.py FAISSVectorStore — IndexFlatIP, load/save to data/index/
  llm/
    factory.py               get_llm_provider() -> ollama_provider | openai_provider
  rag/
    detector.py               greeting/intent detection
    retriever.py               embed query -> FAISS search (service_store + passage_store)
    matcher.py, entity_match.py  confidence scoring / entity-level matching
    clarifier.py               below-threshold -> follow-up question generation
    answerer.py                grounded LLM answer generation
    greeting.py                 canned/greeting-flow handling
    orchestrator.py            RAGOrchestrator — ties all of the above together, per turn
  session/manager.py          SessionManager — 30-min inactivity timeout, turn recording
  ingestion/                  3 independent pipelines: fetcher -> normalizer -> pipeline, for
                              sevasindhu (live API), dept_tracker (xlsx), unique_services (xlsx)
```

**`RAGOrchestrator.process_message(request, db_session)`** (the per-turn entry point):
1. `SessionManager.get_or_create_session` — existing session ID + still within 30-min inactivity
   window → reuse; otherwise mint a new `uuid4` session.
2. Greeting/intent `detector` short-circuits trivial turns.
3. `RAGRetriever` embeds the query and searches both FAISS stores.
4. `ConfidenceMatcher`/`entity_match` scores candidates; below threshold routes to
   `ClarificationEngine` (`responseType: Clarify`, increments `clarify_round_count`), above
   threshold routes to `GroundedAnswerer` (`responseType: Resolved`, calls the configured LLM).
5. `SessionManager.update_session_state` + `record_turn` persist the outcome.
`process_message_stream` is the same pipeline, yielding SSE chunks instead of one response.

**Ingestion** — `ACTIVE_KNOWLEDGE_SOURCE` (`sevasindhu` | `dept_tracker` | `unique_services`)
selects which pipeline populates the vector stores; each writes its own index-file prefix
(`service_matching[_source]`, `passage[_source]`) so switching sources doesn't require
re-ingesting. None of the three pipelines reads from SevaOne's own Postgres `discovery` schema —
`sevasindhu` hits the live public site (`SEVASINDHU_API_URL`), the other two parse Excel trackers
committed into the repo (`data_dept/*.xlsx`, `Date_0.1/*.xlsx`).

---

## 9. AI backend — SQLite schema

| Table | Key columns | Notes |
|---|---|---|
| `knowledge_source_documents` | `id` (int PK), `department_id`, `service_id` (indexed), `source_url`, `document_type` default `API_RECORD`, `language` default `EN`, `parse_status`, `last_ingested_date` | Document-level ingestion metadata |
| `embedding_chunks` | `id` (int PK), `source_type` default `PASSAGE`, `source_id`, `department_id`, `service_id` (indexed), `chunk_text`, `language`, `vector_ref` (indexed) | `vector_ref` points into the FAISS index, not a Postgres FK |
| `sevasindhu_unique_services_raw` | `id` (int PK), `row_number`, 20 verbatim source columns (dept/service codes, English + Kannada name/definition, fees, URLs, `is_active`, `integration_services`), `source_file`, `ingested_at` | Lossless 1:1 mirror of the source XLSX; full delete-and-reload scoped by `source_file` on re-ingest, since nothing in the sheet is a reliable unique key |
| `conversation_sessions` | `id` (str PK, uuid), `sevaone_user_id`, `language` default `EN`, `resolved_service_id`, `clarify_round_count` default `0`, `status` (`ACTIVE`/`EXPIRED`), `created_at`, `last_activity_at` | 30-min inactivity timeout enforced in `SessionManager`, not the DB |
| `conversation_turns` | `id` (int PK), `session_id` (FK, cascade delete), `turn_index`, `input_mode` (`Text`/`Voice`), `query_text`, `response_type`, `response_message`, `created_at` | One row per turn, ordered by `turn_index` |

No foreign key, shared identifier, or sync job connects any of these to Postgres.

---

## 10. AI backend — API contract

**`POST /api/v1/ai-chat/message`**

Request:
```json
{
  "sessionId": null,
  "inputMode": "Text",
  "language": "EN",
  "queryText": "how do I get an income certificate",
  "sevaOneUserId": null
}
```

Response:
```json
{
  "sessionId": "8f2c1e40-....",
  "responseType": "Resolved",
  "message": "...",
  "resolvedService": {
    "departmentId": "revenue",
    "serviceId": "SVC001",
    "serviceName": "Income Certificate",
    "kannadaServiceName": "...",
    "serviceCode": "111"
  },
  "candidates": [],
  "references": [],
  "suggestedQuestions": ["..."],
  "serviceCode": "111"
}
```

`responseType` is one of `Clarify | Resolved | Fallback | NoMatch | Greeting`. `/message/stream`
returns the same information as newline-delimited SSE (`data: {...}\n\n`) events instead of one
JSON body.

---

## 11. Frontend — routes and API clients

**Routes** (`router.tsx`, TanStack Router, all children of one `Shell` root):
`/` (Chat — default landing), `/home`, `/login` (`returnTo`, `loginError` search params),
`/register` (`next`), `/services` (`q`, `department`), `/services/$code`, `/apply/$code`,
`/dashboard` (`ref`), `/track`, `/departments`, `/catalog` (`dept`, `sub`, `service`),
`/catalog/$code`, `/handoff/$dept` (`service`), `/portal/$dept`, `/operator`, `/kpi`, `/help`,
`/about`, `/grievance`, and a catch-all `/$serviceCode` deep-link (registered last, so named
routes take priority).

**API clients** — `api.ts` calls the `.NET` backend exclusively via `/api` → APIM citizen gateway
(`sevaone-citizen/1.0.0`), never the backend port directly, in dev or prod. `aiChat.ts` calls the
AI backend via `/ai-api`, bypassing both APIM and the `.NET` backend entirely; 35-second timeout,
any non-2xx/timeout throws and the caller falls back to a bundled static catalogue
(`catalogMock.ts` / `catalogFromCsv.generated.ts`, generated at build time from a checked-in CSV).

---

## 12. Department reference implementation

`Seva.Mocks.Nadakacheri` (.NET, port 5091) — `RedeemHandoffAsync` calls the real APIM gateway
with a real per-department `ApiKey` plus `X-SevaOne-Dept-Client`/`-Secret`; local `_userMaster`
dictionary is the department's own "do I already know this citizen" bookkeeping, entirely
separate from which SevaOne endpoint gets called (there's only one now).
`Mocks/nadakacheri` (React) — `lib/sso.ts`: `readHandoffFromUrl` parses the redirect query
string; the app's bootstrap effect verifies with SevaOne exactly once per new session (restoring
an existing local session skips re-verification entirely), matching the "SSO applies once, at
the launch page" pattern.
