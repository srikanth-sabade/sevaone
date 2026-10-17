# Department Gateway: Documented Design vs. What's Actually Provisioned

## Summary

The department-facing APIM gateway (`SevaOne-API`, context `/sevaone`) is currently provisioned
with a materially weaker security posture than `backend/docs/WSO2_IS_APIM_SETUP.md` describes and
than the `sevaone-flow` presentation tool (a separate Next.js project, not part of this app)
depicts. This is a gap in `Docker-deploy/wso2-apim/provision.sh` — the script that
auto-provisions APIM on `docker compose up` — not a documentation error. The documentation
describes the correct, intended design; the script was never finished to actually apply it.

**The code is weaker than the documentation.** The fix is to harden the script, not to soften the
docs or the flow diagram (neither was changed as part of this write-up, per instruction).

## What's documented as the intended design

From `backend/docs/WSO2_IS_APIM_SETUP.md`, "Department onboarding: provision an Application and
API Key":

- The department API's allowed subscription tiers are **`["Bronze"]`** (1000 req/min per
  department) — the doc explicitly notes "`Unlimited` was removed once real departments started
  subscribing."
- The whole API has a **20,000 req/min gateway-wide flood cap** (`apiThrottlingPolicy:
  "20KPerMin"`), a shared ceiling across all departments together, on top of each department's own
  1000/min Bronze allowance.
- Department operations use `authType: "Application"` with **`ApiKey` as the only security
  scheme** — no OAuth2 fallback.

The `sevaone-flow` diagram tool independently depicts the same design (IP whitelist, per-department
rate limiting, gateway-level request logging/schema validation, `ApiKey`-only auth) as part of its
"Department Validates" phase.

## What's actually provisioned right now

Checked directly against the live, running `SevaOne-API` via the APIM Publisher REST API:

| Setting | Documented / diagrammed | Actually provisioned |
|---|---|---|
| Allowed subscription tiers | `["Bronze"]` only | `["Unlimited"]` |
| Gateway-wide flood cap (`apiThrottlingPolicy`) | `20KPerMin` | Not set (`None`) |
| Operation `authType` | `Application` (ApiKey-only) | `Application & Application User` (OAuth2 also accepted) |
| IP whitelist | Rejects unknown government IPs at the edge | Not implemented anywhere — no such policy exists in APIM, the provisioning script, or any doc beyond the flow diagram's illustration |
| JSON schema validation at the gateway | Malformed requests get `400` before reaching SevaOne | Not configured |

## Root cause

`Docker-deploy/wso2-apim/provision.sh` imports `SevaOne-API` with `"policies": ["Unlimited"]` and
never sets `apiThrottlingPolicy` or narrows operation `authType` afterward — it only handles the
one-time import/publish/deploy, not the hardening step. This is the same class of gap already found
and fixed for the **citizen** API earlier in this project's setup work (`SevaOne-Citizen-API` now
gets `authType: "None"` and `apiThrottlingPolicy: "20KPerMin"` set programmatically as part of
provisioning) — the department half of the same script was simply never finished to match.

IP whitelisting and JSON-schema validation are not partially-done gaps like the above three — they
don't exist in any form yet. They'd be new APIM policies to design and attach, not a case of
turning on an existing but unset field.

## What should be done

1. **Harden `wso2apim-provision.sh` for the department API**, mirroring the pattern already used
   for the citizen API in the same script:
   - Set `policies: ["Bronze"]` (drop `Unlimited`) at import/update time.
   - Set `apiThrottlingPolicy: "20KPerMin"` on the API.
   - Set every operation's `authType` to `"Application"` (ApiKey-only, matching the documented
     department security model — no OAuth2 fallback).
   - Idempotent update, same shape as the existing Key Manager / citizen-API skip-if-exists checks
     already in the script.
2. **IP whitelisting** — separate, larger piece of work: design and attach an APIM policy (or a
   Synapse mediation sequence) that restricts the department API to known government IP ranges.
   Needs a real IP range list from whoever owns department onboarding before this can be built.
3. **JSON schema validation at the gateway** — separate work: attach APIM's built-in JSON Schema
   Validator policy per operation, using the schemas already present in the OpenAPI spec bff
   exposes.

Items 2 and 3 are new capability, not a fix to something half-built — worth scoping as their own
follow-up rather than bundling into the same change as item 1.

## Explicitly out of scope for this write-up

Per instruction, `sevaone-flow` (the separate Next.js flow-diagram project) was **not modified**.
Everything else checked during this comparison — all 26 backend routes, and the response/request
DTOs for mobile OTP login, `/me`, catalog browsing, apply-token minting, department
token-validate/apply-complete, and application tracking — matched the real codebase closely; the
diagram is accurate there. This gap is specific to the department gateway's throttling/auth/IP
hardening.
