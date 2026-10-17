# Department Auth Redesign — WSO2 Key Manager + Opaque Apply-Tokens

A design proposal, compared against the current live setup. Written to settle two real problems
raised during department integration:

1. A government official's requirement: the apply-token redirect URL must not carry
   `departmentId`/`serviceId`/other fields in plain query params, and whatever it does carry must
   not be readable/decodable.
2. An operational gap: today's department credentials are frozen in SevaOne's own process memory
   at startup (`IOptions<JwsOptions>`) — revoking a compromised department requires editing
   config and restarting the `bff` container. There is no live "kill switch."

Everything below is grounded in this project's actual code and its own WSO2 troubleshooting
history — not generic WSO2 capability claims.

---

## 1. Current setup (live today)

### 1.1 What exists

- **`JwsTokenService`** (`backend/SevaOne.Infrastructure/Security/JwsTokenService.cs`) mints the
  citizen's apply-token as an `HS256`-**signed** JWT (`JwtSecurityTokenHandler`). Claims —
  `dept_id`, `service_id`, `sub` (citizen), `assisted_mode`, `operator_id` — sit in the token as
  plain base64url JSON. Signed means tamper-proof, **not encrypted**: anyone can decode the
  payload without the signing key, they just can't forge a new one.
- The redirect URL carries this token **and** `DepartmentID`, `ServiceID`, `SevaOneUserID` as
  separate plaintext query params.
- Department calls to `/dept/*` are gated by three layers:
  1. **`ApiKey`** — checked by WSO2 APIM at the gateway.
  2. **`DeptMachineAuth`** (`backend/SevaOne.Services/Auth/DeptMachineAuth.cs`) — Client
     ID/Secret, checked by SevaOne itself against `Jws:DeptClients`, a static list bound via
     `IOptions<JwsOptions>`.
  3. **`DeptGatewayAuthMiddleware`** — a stamped secret header proving the request actually came
     through APIM, not straight to the bff container.
- Single-use enforcement lives in SevaOne's own `portal.sevaone_token` table (hashed token +
  expiry), checked on every redemption.

### 1.2 The two problems, precisely

- **Requirement 1 fails today**, on both counts: `DepartmentID`/`ServiceID`/`SevaOneUserID` are
  plaintext query params, and the token itself — despite being colloquially called "encrypted" —
  is only signed, so its own payload is also plaintext-readable.
- **The revoke problem**: `DeptController` injects `IOptions<JwsOptions>`, which is bound once at
  process start and never re-read. Rotating or killing a department's `ClientSecret` requires
  editing config and restarting the container — confirmed by inspecting the actual DI wiring, not
  assumed.

### 1.3 One relevant precedent from this project's own history

The `/dept/*` API's `securityScheme` in APIM was originally
`["oauth_basic_auth_api_key_mandatory", "oauth2"]` — requiring OAuth2 *and* ApiKey together. That
combination produced live `401 Missing Credentials` errors and had to be simplified down to
`["api_key"]` alone during this project's actual onboarding work. This matters directly to the
proposal below: it's evidence that mixing security schemes on this specific APIM install is
fragile, not a hypothetical risk.

---

## 2. Proposed setup

### 2.1 What changes, and what deliberately doesn't

**Stays exactly as-is:** `ApiKey`, checked by APIM at the gateway. Given the precedent in §1.3,
touching the gateway's security scheme again is the highest-risk part of any change here — so
this design leaves it alone entirely.

**Gets replaced:** `DeptMachineAuth`'s static Client ID/Secret list. Instead of a row in
`Jws:DeptClients`, each department becomes a registered OAuth2 application inside **WSO2 IS**,
acting as APIM's **Key Manager**. Its Client ID/Secret is generated and owned by WSO2, not
SevaOne's config.

**Gets replaced:** the citizen apply-token's construction. Instead of `JwsTokenService` signing a
JWT with embedded claims, SevaOne asks **WSO2 IS's token-exchange grant** (RFC 8693) — trading the
citizen's existing session token for a new, fully **opaque** token (a bare random string, no
embedded claims at all), scoped `dept:<id> svc:<id>`, tied to the citizen as `sub`.

### 2.2 Full flow

**Setup, once per department:**
1. Department registered as an OAuth2 application in WSO2 IS (`client_credentials` grant). WSO2
   generates its Client ID/Secret.

**Per citizen apply:**
2. Citizen logs in (unchanged — DigiLocker/OTP, then the existing WSO2 IS session-token exchange).
3. Citizen clicks Apply. SevaOne calls WSO2 IS's token-exchange grant → gets back an opaque token,
   no embedded claims, ~30 min TTL, single-use.
4. Citizen's browser is redirected to `https://<dept-site>/apply/<opaque-token>` — one unlabeled
   value, nothing else in the URL.

**Per department redemption:**
5. Department calls WSO2 IS's token endpoint with its own Client ID/Secret (`client_credentials`)
   → gets a short-lived access pass.
6. Department calls `POST /dept/user-details` through APIM, sending:
   - `ApiKey` header — unchanged, checked by APIM exactly as today.
   - `Authorization: Bearer <access pass>` — the department's WSO2-issued identity proof.
   - the opaque citizen token, in the request body, same shape as today's `Token` field.
7. **APIM's native check, no custom mediation:** validates `ApiKey` (as today) and the Bearer pass
   against Key Manager. Fails either → rejected at the gateway, SevaOne never touched.
8. **What APIM does *not* check natively:** the opaque citizen token, because it's a body field,
   not a header — APIM's declarative security only inspects headers. Making the gateway also
   inspect and introspect an arbitrary body field would mean writing custom mediation logic —
   exactly the class of gateway-side business logic this project already removed once (the WSO2
   MI "operator orchestration" ESB) and separately declined twice more this session (a
   Redis-backed lookup, and a proposal to have WSO2 return user details directly). This design
   deliberately does not do that.
9. SevaOne's backend, once APIM has already confirmed the department is genuine, does what only it
   ever did: introspects the opaque citizen token against WSO2 IS itself (a plain outbound call,
   same category of work as today's local JWT-signature check, just delegated), looks up the
   citizen and service records in Postgres, and responds.
10. SevaOne immediately revokes the opaque token at WSO2 IS on successful redemption — closing
    replay at the gateway (step 7's neighbor, effectively) rather than relying solely on SevaOne's
    own single-use bookkeeping.

### 2.3 What this buys, concretely

- **Requirement 1, fully met:** the URL carries one opaque value. Nothing to decode because
  nothing is embedded.
- **The revoke problem, fully fixed:** disabling a department's WSO2 IS application is a live
  WSO2 admin action. The next `client_credentials` call fails immediately; no SevaOne config
  edit, no redeploy, no restart.

---

## 3. Side-by-side

| | Current setup | Proposed setup |
|---|---|---|
| Apply-token construction | Self-issued `HS256`-signed JWT (`JwsTokenService`) | WSO2 IS token-exchange, opaque, no embedded claims |
| Apply-token URL | `?DepartmentID=...&ServiceID=...&SevaOneUserID=...&Token=...` (all plaintext) | `/apply/<opaque-token>` — one unlabeled value |
| Department identity check | `ApiKey` (APIM) **+** static `DeptMachineAuth` list (SevaOne, in-process) | `ApiKey` (APIM, unchanged) **+** WSO2-IS-issued Bearer pass (Key Manager, checked by APIM) |
| Revoking a department | Edit `appsettings.json`/user-secrets, redeploy, restart `bff` | Disable the application in WSO2 IS — instant, no SevaOne change |
| Single-use enforcement | SevaOne's own hashed-token table (`portal.sevaone_token`) | Same table **+** WSO2 IS revoke-on-redeem as a second, gateway-level backstop |
| Citizen-token validity check | Local signature verification inside SevaOne (fast, in-process) | Outbound introspection call to WSO2 IS on every redemption (network round-trip) |
| Gateway security scheme | `["api_key"]` only (already simplified once after a live 401 incident) | Unchanged — this design deliberately avoids touching it again |
| New infra dependency for the apply flow | None — self-contained, works even if WSO2 IS has a bad moment (login already depends on it, apply/redemption currently doesn't) | Apply-token minting *and* every department redemption now require WSO2 IS to be reachable |

---

## 4. Pros and cons

### Current setup

**Pros**
- Fully self-contained for the apply/redemption path — no live dependency on WSO2 IS beyond
  login (which is already load-bearing today).
- Simple, already built, already onboarded against by 11 real departments, already documented
  (`SERVER_DEPARTMENT_INTEGRATION_GUIDE.md`, `DEPARTMENT_INTEGRATION_GUIDE.md`).
- Fast — no extra network hop to validate a token; it's a local signature check.

**Cons**
- Fails the government official's stated requirement outright — plaintext params, and a token
  that only *looks* encrypted.
- No live revoke — a compromised department credential requires a container restart to kill.
- Token generation quality depends entirely on SevaOne's own code being correct (though this is
  fixable on its own, without WSO2, by switching to a CSPRNG-generated opaque value).

### Proposed setup

**Pros**
- Meets the government official's requirement completely and directly.
- Turns department-credential revocation into a live, no-deploy action — the actual operational
  problem that started this whole design conversation.
- Reuses WSO2 IS for what it's structurally built for (token issuance, validation, revocation)
  instead of leaving it running underused, as flagged in `WSO2_IS_ROADMAP.md`.
- Rejects a dead/expired/revoked department credential at the gateway, before SevaOne spends a
  database call on it.

**Cons**
- New hard dependency: both apply-token minting and every department redemption now require WSO2
  IS to be reachable. A WSO2 IS outage today only blocks login; under this design it also blocks
  every in-flight apply and every department call, even for citizens already signed in.
- Untested against this project's actual WSO2 IS 7.3.0 install. Every WSO2 feature touched this
  session — SCIM2 provisioning, the citizen JWT-bearer exchange, `securityScheme`,
  `generateApiKey` — surfaced at least one live gotcha only found by running it for real. Token
  exchange should be assumed to carry the same risk until proven otherwise.
- Real migration cost: `PortalApplyService.MintApplyTokenAsync`, `JwsTokenService`,
  `DeptTokenGate`/`DeptMachineAuth`, and both integration guides all need to change; 11 already-
  onboarded departments would need new Client ID/Secret pairs issued through WSO2 IS instead of
  the ones already sent out.
- Adds a network round-trip (introspection) to every department redemption where today there's
  only a local, fast signature check.

---

## 5. Recommendation

Two independent changes, not one bundled migration:

1. **Fix requirement 1 immediately, without WSO2:** replace `JwsTokenService`'s signed-JWT output
   with a SevaOne-generated opaque token (CSPRNG, no embedded claims), stored server-side in
   `portal.sevaone_token`. Small, contained, ships fast, satisfies the government official's ask
   completely on its own.
2. **Treat the Key Manager migration (department identity + citizen-token issuance via WSO2) as a
   separate, deliberate hardening project** — real value, but it should get the same live testing
   treatment every other WSO2 feature in this project has needed, on its own timeline, not bundled
   under deadline pressure from item 1.

Keep `ApiKey` at the gateway in both cases — the one part of this design that stays fixed no
matter which path is taken, given §1.3's precedent.
