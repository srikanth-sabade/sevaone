# API flow — citizen login through to landing on the department's service

Companion to `AUTHENTICATION_APIS.md` (which groups APIs by *who* authenticates) and
`USER_JOURNEY.md` (prose walkthrough + sequence diagrams). This one lists the APIs in the
**order a real journey actually calls them**, login through to the citizen landing on the
department's own site.

Two paths are shown: citizen self-service, and operator-assisted. They're identical from
Step 3 onward except for which controller mints the apply-token and what flag it carries.

---

## Path A — Citizen self-service

```
1.  Login                     (pick one -- the WSO2 redirect/password-grant paths that used
                                to live here, GET /auth/login, GET /auth/callback,
                                POST /auth/password, POST /auth/otp, are RETIRED (410 Gone).
                                They never set AadhaarVerified/MobileVerified, so none of the
                                identity safeguards below ever covered them.)
    POST /api/v1/auth/mobile/start               -> challengeId (OTP sent via ISmsOtpSender)
                                                     Rate-limited: 5 requests / 10 min per IP
    POST /api/v1/auth/mobile/verify              -> found:true  -> session established
                                                     found:false -> no citizen has this
                                                     VERIFIED mobile yet; frontend falls
                                                     back to the DigiLocker start step below
                                                     to prove identity and create the account
                                                     Rate-limited: 5 requests / 5 min per IP
      -- or --
    POST /api/v1/auth/digilocker/start          -> sessionId
                                                    Rate-limited: 5 requests / 10 min per IP
    [real DigiLocker consent happens in DigiLocker's own widget -- not an API call]
    POST /api/v1/auth/digilocker/complete       -> 200 session established
                                                    -> or 409 if this Aadhaar already belongs
                                                       to a DIFFERENT SevaOne account (citizen
                                                       tried a new mobile number but they're
                                                       already registered under a different
                                                       one) -- no duplicate account created,
                                                       response tells them which number to use
                                                       instead. Also fetches the citizen's real
                                                       Aadhaar document (number, address,
                                                       district, taluk, state) alongside the
                                                       basic profile -- see
                                                       COMPLETE_FLOW_WITH_PAYLOADS.md for the
                                                       full two-call breakdown and merge rules

    => seva.sid cookie set. Both paths above funnel through the same
       ICitizenSessionService.EstablishAsync (see AUTHENTICATION_APIS.md).

2.  Check identity / finish registration
    GET   /api/v1/me                            -> citizen profile, incl. aadhaarVerified --
                                                    true only for a real DigiLocker-sourced
                                                    Aadhaar; frontend should hide/pre-fill the
                                                    Aadhaar-last-4 field below when true instead
                                                    of asking again
    PATCH /api/v1/me/profile                     (only if profileCompleted:false --
                                                    aadhaarLast4 is ignored server-side if
                                                    aadhaarVerified is already true)

3.  Browse the catalog
    GET  /api/v1/departments                    -> list departments
    GET  /api/v1/schemes?departmentId=...        -> list schemes (optionally filtered)
    GET  /api/v1/schemes/{code}                  -> one scheme's detail
    GET  /api/v1/search?q=...                    -> keyword/alias search across schemes

4.  Apply
    POST /api/v1/schemes/{code}/apply-token     -> { applyUrl, expiresAt }
      -- mints a SevaOne JWS, ENCRYPTS it (IApplyRedirectProtector), embeds it in
         applyUrl's Token= query param, alongside DepartmentID/ServiceID/SevaOneUserID

5.  Browser redirects to applyUrl
    -- this is the DEPARTMENT'S OWN URL, not a SevaOne API. SevaOne's job ends here;
       everything from this point is the department's site until it calls back in Step 6.

6.  Department validates the handoff (server-to-server, via APIM -- see below)
    POST /api/v1/dept/token-validate             (citizen already known to this department)
      -- or --
    POST /api/v1/dept/user-details               (citizen new to this department)
    => one-time redeem (portal.sp_try_redeem_token) -- the SAME token can't be used twice

7.  Citizen is now on the department's own portal, applies for the actual service there.
    (What happens on the department's own site from here is outside SevaOne's API surface.)

8.  Department finishes -- sends the citizen's browser back to SevaOne
    POST /api/v1/dept/get-acknowledgement        -> { linked, returnUrl, expiresAt }
      -- mints a SECOND, short-lived (10 min), one-time token distinct from the original
         apply-token (which was already redeemed in Step 6) -- its only job is proving,
         on the way back, that the department itself confirmed completion for this citizen.
      -- linked:false means this department has no SevaOne relationship for this citizen at
         all (e.g. they logged into the department directly, never via an apply-token
         handoff) -- NOT an error. The department just keeps the citizen on its own portal;
         there's nothing for SevaOne to redirect back to.
      -- if linked:true, department redirects the browser to {SevaOneOrigin}{returnUrl}

    GET /api/v1/apply/return?token=...            -> SevaOne decrypts + verifies + atomically
                                                      redeems the completion token, cross-checks
                                                      it against the citizen's CURRENT session
                                                      (if any), then redirects to the frontend
      -- no active session on this browser -> redirect to /login?returnTo=... (their SevaOne
         session may have simply expired while they were on the department's site)
      -- session exists but belongs to a DIFFERENT citizen than the token -> hard reject,
         audit-logged as apply.return.citizen_mismatch (the real attack case, distinct from
         the merely-expired-session case above)
      -- session matches -> redirect to the frontend path from Step 4's mint (default
         /dashboard)

9.  Meanwhile -- department can also push status/payment updates independently
    POST /api/v1/internal/webhooks/status        -> application status update
    POST /api/v1/dept/payment-status              -> fee-completion status (midnight-cutoff
                                                      validity, not the 30-min token rule)

10. Citizen checks progress (any time after Step 4)
    GET /api/v1/applications                     -> citizen's own applications
    GET /api/v1/applications/{reference}          -> one application's detail
    GET /api/v1/track/{reference}                 -> public tracking by reference (no auth)
```

---

## Path B — Operator-assisted

Same shape, different first two steps and who mints the apply-token:

```
1.  Operator login (separate identity, separate cookie -- see AUTHENTICATION_APIS.md)
    POST /api/v1/operator/login                 -> seva.op.sid cookie
                                                    Rate-limited: 5 requests / 5 min per IP
                                                    (shared bucket with citizen mobile/verify)

2.  Operator applies on behalf of a citizen they already have the SevaOneUserId for
    POST /api/v1/operator/apply-token           -> { applyUrl, expiresAt }
      -- mints with AssistedMode:true, real OperatorId (not the citizen's own session)

3.  Steps 5-9 from Path A are identical -- the department can't tell from the
    token-validate response alone that this was operator-assisted, except that
    assistedMode:true and operatorId are populated in the response.
```

---

## Where WSO2 APIM sits in this flow

**Steps 1-5, 7, and 10 never touch APIM.** Citizen login (including mobile OTP), browsing,
and apply-token minting all go straight to the backend (`SevaOne.Services`, port 5080
locally); `GET /api/v1/apply/return` in Step 8 is a citizen-browser call and also bypasses
APIM the same way.

**Every department-server call routes through APIM instead: `POST /api/v1/dept/token-validate`
/ `/user-details` (Step 6), `POST /api/v1/dept/get-acknowledgement` (Step 8), and
`POST /api/v1/dept/payment-status` (Step 9).** The department calls
`https://<apim-gateway>/sevaone/1.0.0/api/v1/dept/*`, not the backend directly. APIM requires
its own OAuth2 `client_credentials` token (issued by WSO2 IS) before it will even forward the
request; `DeptMachineAuth` (Basic/`X-SevaOne-Dept-*` headers) still runs underneath that,
unchanged. See `AUTHENTICATION_APIS.md` §3 and `POSTGRES_FUNCTIONS_VS_PROCEDURES.md`-adjacent
`WSO2_IS_APIM_SETUP.md` for the full mechanics.

---

## Full request sequence, condensed

| # | Caller | Method + Path | Through APIM? |
|---|---|---|---|
| 1 | Citizen browser | Login (mobile OTP / DigiLocker — WSO2 redirect/password retired) — see §1 above | No |
| 2 | Citizen browser | `GET /api/v1/me`, `PATCH /api/v1/me/profile` | No |
| 3 | Citizen browser | `GET /api/v1/departments`, `/schemes`, `/schemes/{code}`, `/search` | No |
| 4 | Citizen browser | `POST /api/v1/schemes/{code}/apply-token` | No |
| 5 | Citizen browser | *(redirects to department's own site)* | — |
| 6 | Department server | `POST /api/v1/dept/token-validate` or `/user-details` | **Yes** |
| 7 | — | *(citizen interacts with department's own portal)* | — |
| 8 | Department server, then citizen browser | `POST /api/v1/dept/get-acknowledgement`, then `GET /api/v1/apply/return?token=...` | get-acknowledgement: **Yes** · apply/return: No |
| 9 | Department server | `POST /api/v1/internal/webhooks/status`, `POST /api/v1/dept/payment-status` | webhooks: No · payment-status: **Yes** |
| 10 | Citizen browser | `GET /api/v1/applications`, `/applications/{reference}`, `/track/{reference}` | No |
