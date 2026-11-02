# WSO2 Data Model — What SevaOne Actually Stores There

This isn't shaped like `DATABASE_SCHEMA.md` because it can't be. Postgres is a database SevaOne
owns — every table there has a migration file SevaOne's own team wrote. WSO2 Identity Server and
WSO2 API Manager are products SevaOne *uses*; each one owns and manages its own internal
database, and SevaOne has no migration, no direct SQL access, and no documented dependency on
either product's internal table names. Everything below is what SevaOne creates and reads
through each product's own REST APIs — that's the actual interface, not raw tables.

In this environment, both WSO2 IS and WSO2 APIM run on their own default embedded H2 database
(the product's out-of-the-box store), not Postgres — confirmed by the absence of any external
database override in `deploy/wso2/local-overrides.toml`. SevaOne's own citizen/token/application
data always stays in SevaOne's own Postgres, described in `DATABASE_SCHEMA.md`; WSO2 never sees
any of it beyond what's listed below.

---

## Part 1 — WSO2 Identity Server: one SCIM2 user per citizen

WSO2 IS is used for exactly one thing: issuing a real, introspectable access/refresh token pair
for a citizen SevaOne has already verified (DigiLocker or mobile OTP). SevaOne provisions the
citizen into WSO2's own user store via SCIM2, then exchanges for tokens via an RFC 7523
JWT-bearer grant — the citizen never sets or enters a WSO2 password.

### The SCIM2 user resource SevaOne creates

`POST /scim2/Users`, one resource per citizen, created lazily the first time that citizen needs
a WSO2-backed session (not at SevaOne registration time):

| SCIM2 attribute | Value | Source |
|---|---|---|
| `userName` | `sevaone-{sevaOneUserId}` | SevaOne's own Postgres `sevaone_user.sevaone_user_id` |
| `password` | random 24-byte value, generated once | Never reused, never known again — required by SCIM2 user creation only |
| `name.formatted`, `.givenName`, `.familyName` | split from SevaOne's single free-text `name` field, on the last space | `SplitName()` — a single-word name is used for both given and family |
| `emails[0].value`, `.primary` | citizen's email, if on file | omitted entirely if none |
| `phoneNumbers[0].value`, `.type=mobile` | citizen's mobile, if on file | omitted entirely if none |
| `addresses[0].type=home`, `.country` | fixed `"India"` | no per-citizen country field exists in SevaOne to source this from |

That's the complete attribute set. No custom SCIM2 claims are set — nothing SevaOne-specific
(role, department history, Aadhaar status) is pushed into WSO2's user store; WSO2 only ever
knows a citizen by this SCIM2 identity, never anything from their application history.

### How SevaOne authenticates as that user

Never a password. SevaOne mints a short-lived (5 minute), self-signed JWT asserting
`sub={scimUsername}`, signed with its own RS256 key, and exchanges it at WSO2's token endpoint
using the `urn:ietf:params:oauth:grant-type:jwt-bearer` grant against a Trusted Token Issuer
identity provider registered for SevaOne inside WSO2 IS. The resulting access/refresh token pair
is what actually becomes the citizen's live session (held in an HttpOnly cookie) — WSO2 IS is a
real, introspectable identity provider from that point on, not a rubber stamp.

Two WSO2-side identifiers matter here and are easy to confuse: the Trusted Token Issuer's
**name** (`Issuer`, must match what WSO2 resolves the JWT's `iss` claim against) and its
**tokenEndpoint alias** (`Audience`, must match the JWT's `aud` claim) — WSO2 validates these
against two different fields on the same identity provider record, not the same one twice.

### Where this actually lives inside WSO2

WSO2 IS's own Identity/Carbon DB — standard WSO2 tables such as `UM_USER`,
`UM_USER_ATTRIBUTE`, `IDN_OAUTH2_ACCESS_TOKEN`, and so on. These are WSO2's product internals,
not SevaOne's — they could change shape between WSO2 versions without SevaOne needing to change
anything, because SevaOne only ever talks to the stable SCIM2 and OAuth2 REST surface, never the
tables directly.

---

## Part 2 — WSO2 API Manager: the department gateway

APIM is used only for department-facing traffic (`/api/v1/dept/*`) — never citizen login. Two
different kinds of thing are provisioned here, at two different times.

### Provisioned once, at platform setup (not per-department, not per-request)

- **One API Product** — `SevaOne-API`, context `/sevaone`, version `1.0.0` — imported directly
  from SevaOne's own live OpenAPI spec, published, and deployed as a gateway revision
  (`deploy/wso2-apim/provision.sh`).
- **One external Key Manager registration** — `WSO2-IS-KM` — points APIM at the same WSO2 IS
  instance used for citizen login, sharing its OAuth2 token/introspection/JWKS endpoints. This
  registration exists for APIM's own admin/DevPortal login and general OAuth2 support; department
  traffic itself does not authenticate through this key manager (see `ERROR_CODES.md` and
  `DEPARTMENT_INTEGRATION_GUIDE.md` — department calls use `ApiKey`, not an OAuth2 bearer token).

### Provisioned once per department, at onboarding time

- **One APIM Application** per department (e.g. "SevaOne-Dept-Revenue"), created via the
  Devportal REST API.
- **One API key** generated for that Application against the deployed `SevaOne-API`
  (`/applications/{id}/api-keys/PRODUCTION/generate`) — a long-lived, self-verifying signed JWT,
  not a short-lived OAuth2 access token. This is the exact value a department sends as the
  `ApiKey` header on every call, documented in `DEPARTMENT_INTEGRATION_GUIDE.md` §1.
- **A rate-limit tier** attached to the Application's subscription (Bronze in this deployment —
  1000 requests/minute per department).

### Where this actually lives inside APIM

APIM's own internal database — tables such as `AM_APPLICATION`, `AM_SUBSCRIPTION`,
`AM_APPLICATION_KEY_MAPPING`, `AM_API`. Same rule as WSO2 IS: SevaOne never touches these
directly, only the Publisher/Devportal/Admin REST APIs that `provision.sh` and the onboarding
process use.

---

## Part 3 — What actually crosses back into SevaOne's own Postgres

Exactly one column links a SevaOne citizen row to anything WSO2-side:
`portal.sevaone_user.wso2_subject` (see `DATABASE_SCHEMA.md`) stores the SCIM2 `userName` —
`sevaone-{sevaOneUserId}` — as the pointer back to that citizen's WSO2 IS identity, or
`digilocker:<id>` for a citizen who hasn't gone through the WSO2 exchange. Nothing else about
either product's data model is duplicated into SevaOne's schema: department Applications, API
keys, and client credentials are tracked in SevaOne's own configuration
(`SevaOne.Services/appsettings.json`'s `DeptClients` section plus each department's own stored
`ApiKey`), independently of anything inside APIM's tables.
