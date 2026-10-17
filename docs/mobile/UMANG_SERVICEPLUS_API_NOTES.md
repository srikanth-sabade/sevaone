# UMANG ↔ ServicePlus API Spec — what it is, and whether it helps SevaOne

Source: `Testables/SP Team Api Doc 1.8 (1).pdf` (NeGD, "API Specification Documents — UMANG Services - Service Plus API", v1.8, last edited 14 Aug 2023).

## What the document actually describes

It is **not** a citizen-facing ServicePlus API and not related to Apply links, OTP, or the Sakala catalogue we already use. It documents the integration ServicePlus's *own backend team* uses to **publish their services into UMANG** — India's national "one app for all government services" super-app run by NeGD (separate from SevaOne, separate from ServicePlus's own site). Two APIs, both POST, both **writes**:

1. **Service Launch API** (`.../spLaunchApi`) — pushes one service's metadata (name, description, category, FAQ, a tiny-URL that points at the individual service, a secret key) into UMANG so it becomes listed and launchable from the UMANG app.
2. **Service Plus Suspension API** (`.../spSuspensionApi`) — marks a previously-launched service inactive on UMANG.

Both require ServicePlus-owned credentials (`Authorization: Bearer ...` / `x-api-key: ...`) sent by the *service provider* (ServicePlus), not by a consuming app like ours.

There is also a read-only **UMANG Category API** (`GET https://apigw.umangapp.in/SPApi/3.0/category`) that lists UMANG's category taxonomy (Education, Health, Farmers, Transport, etc.) — this one needs no credentials.

## What I tested

Only the read-only Category API (the other two are live write operations against a production national platform, using someone else's credentials — not something to invoke for a test). It responded successfully with real, current data:

```
GET https://apigw.umangapp.in/SPApi/3.0/category → HTTP 200
{"rs":"SU","rc":"API0052","rd":"Successfully fetched ", "pd": {"primaryCategoryList": [...]}}
```

Confirms the integration is live and current, not a dead/retired spec.

## Is it useful for SevaOne?

**Not for anything we're building.** SevaOne mobile is a consumer of the Sakala/ServicePlus catalogue (our own read-only Postgres copy) and opens department Apply links — it never pushes data anywhere. This document is the opposite direction: it's how ServicePlus registers *itself* into a *different* citizen app (UMANG) that SevaOne has nothing to do with. We don't own ServicePlus's UMANG credentials, and there's no scenario in the current mobile app roadmap where SevaOne would call `spLaunchApi`/`spSuspensionApi`.

The only mildly relevant detail: the LGD state-code table and `spCat` category codes are standard-issue Government of India reference data (also used elsewhere in gov integrations), but we don't currently need either.

**Conclusion:** informational only — filed here for reference in case it resurfaces, no action needed against the SevaOne codebase.
