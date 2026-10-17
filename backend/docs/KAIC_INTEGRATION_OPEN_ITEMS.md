# KAIC Integration — Open Items (Update to Server Integration Guide)

**Status:** KAIC is already provisioned on the SevaOne KSDC server — `departmentId: "kaic"`,
with an ApiKey and Client ID/Secret issued (see `SERVER_DEPARTMENT_INTEGRATION_GUIDE.md`). The
standard call flow in `DEPARTMENT_INTEGRATION_GUIDE.md` (User Details → Acknowledgement →
Status Update, using the ApiKey + Client ID/Secret credentials already sent) applies to KAIC the
same as every other department.

Two things are specific to KAIC, though, and can't be finalized until their technical team
responds. This document is meant to be sent to KAIC as-is — it states what we need from them and
why, not just an internal note.

---

## 1. Does KAIC support OAuth2 / OpenID Connect (OIDC)?

**Question for KAIC:** Does your platform support OAuth2 / OIDC as an authentication mechanism —
either as an Identity Provider (issuing tokens) or a Relying Party (accepting tokens from an
external IdP)?

**Why we're asking:** every other department integrated with SevaOne so far only needs the
standard `ApiKey` + Client ID/Secret model — a static credential pair, no token exchange, no
identity federation (see Section 1 of `DEPARTMENT_INTEGRATION_GUIDE.md`). If KAIC's own systems
already speak OAuth2/OIDC, that changes how the two systems should trust each other for
Item 2 below — a real token exchange is a stronger, more standard integration than a bespoke
lookup call, and worth using if it's actually available on KAIC's side.

**What we need back from KAIC:**
- A yes/no on OAuth2/OIDC support.
- If yes: which role (IdP, Relying Party, or both), the discovery/well-known endpoint if
  available, supported grant types, and who owns client registration on their side.
- If no: confirmation so we proceed on the plain ApiKey + Client ID/Secret model already in
  place, and treat Item 2 below as a standalone REST call instead of a token claim.

---

## 2. A government-officer lookup endpoint from KAIC

**What we need:** a simple API that SevaOne's backend can call, given an identifier for the
person currently on SevaOne, that tells us whether that person is a government officer (as
opposed to an ordinary citizen) — **before** we redirect them into KAIC's service.

**Why this matters:** SevaOne already has a notion of a government-officer account, distinct from
a citizen — `sevaone_user.kgid` (Karnataka Government ID) in our own schema — but nothing
populates it from KAIC's side today, and SevaOne alone can't tell whether a given person is a
government officer *for KAIC's purposes* specifically. We need KAIC to be the source of truth for
that check, so we can decide which path to send the citizen down (a government-officer flow vs.
the ordinary citizen flow) before the redirect happens, not after.

**Proposed shape** (starting point for KAIC to react to — happy to adjust to whatever's easiest
on their side):

```
GET https://<kaic-host>/api/officer-check?identifier=<value>
Headers: ApiKey / whatever auth KAIC requires (see Item 1)

Response 200:
{
  "isGovtOfficer": true,
  "kgid": "KA-GOV-1234"        // optional, if KAIC can return it
}
```

`identifier` would be whichever value KAIC can actually match against their own records — mobile
number, Aadhaar-linked ID, or an email, whatever they already hold. We'll adapt to what's
realistic on their end rather than dictate the identifier type.

**What we need back from KAIC:**
- Confirmation they can build this, and a rough timeline.
- The identifier type they can key the lookup on.
- The actual endpoint URL, auth mechanism (ties back to Item 1), and response shape once built.

---

## What's not blocked

Everything else about KAIC's integration is already working and doesn't need their input:
- Credentials are issued and live (ApiKey, Client ID `kaic_portal`, Client Secret).
- `POST /api/v1/dept/user-details`, `get-acknowledgement`, and `status-update` all work against
  the live server today, the same as for every other department.
- Testing procedure: `backend/docs/TEST_TOKEN_TESTING_PROCEDURE.md` (currently only runnable
  against `SVC001`/revenue until a KAIC service is seeded with a working deeplink — same
  data-completeness gap noted there, not specific to KAIC).

## Next steps

1. Send this document to KAIC's technical team.
2. Get their answer on Item 1 (OAuth2/OIDC) and Item 2 (officer-check endpoint + timeline).
3. Once both are answered, update `SERVER_DEPARTMENT_INTEGRATION_GUIDE.md` with KAIC's actual
   integration details, and seed at least one real KAIC service with a working deeplink so the
   full flow is testable end-to-end.
