# Flutter app: load every screen on the first try (brief for Codex)

App `apps/eserviceplus-master`, branch `flutter-modernize`. Backend: `apps/Seva-Sindhu-One/backend` (FastAPI), deployed on the GPU server `101.53.138.83`, port 8002 (`SEVAONE_BACKEND_URL`).

## Problem (owner-observed on Poco F1 and emulator)
After login, DigiLocker and Services often show "Try again"; pressing it 2–3 times eventually works. Home sections are inconsistent. **Applications always loads** — it calls ServicePlus directly via `ServicePlusClient`. Everything that fails goes through `core/sevaone/sevaone_mobile_api.dart` → our backend.

## Likely causes (confirm in Step 0 before fixing)
1. Backend builds the catalogue from a live ServicePlus `getServiceJson` call, cached only briefly per citizen. First call after login is slow; the client gives up at 15 s (`_requestData` default). Retries hit the warm cache → works on try 2–3.
2. Home fires trending, departments, recents, unread count, DigiLocker documents and profile at once, all racing the same cold fetch.
3. `login_screen.dart`: if `SevaOneMobileApi.signInWithServicePlus` fails, login continues with `gpuBackendSessionReady=false` and nothing retries — every authorized call then throws `SevaOneSessionRequiredException` until re-login.
4. DigiLocker depends on CeG; backend returns retryable HTTP 502 on transient CeG failures, but the app never retries.
5. No automatic retry anywhere; one slow call = full-screen error.
6. Verify token refresh under concurrency (`_refreshInFlight`) and expiry.

## Rules (non-negotiable)
- Never remove or change any ServicePlus API or its wire format; don't touch WebView certificate handling or the WebView/PDF behaviour.
- **Catalogue rule:** every catalogue response the backend serves to the app must come from a live `getServiceJson` result. Sharing one in-flight fetch across simultaneous requests (single-flight) and the existing brief per-citizen cache are fine; serving from a stored DB copy is not.
- **Step 5 (on-phone cache of the last catalogue/DigiLocker list) needs the owner's explicit OK before you build it — ask first.**
- Backend changes: back up files on the GPU server before deploying (pattern in `docs/mobile/SESSION_HANDOFF.md`), deploy only the files you changed, restart via `supervisorctl`, verify.
- Don't enter credentials, register, submit applications or pay — ask the owner to log in on the device.

## Steps (commit each; app builds and `flutter analyze` is clean after each)

**0. Measure.** Debug-only (`kDebugMode`) timing log in `_requestData`: path, ms, status/exception type. Owner does 5 fresh logins on the Poco (`adb -s 1ba0f1cd`, USB); collect logcat. Check GPU backend logs/timings for the same window, especially cold `getServiceJson`. Write the findings into the handoff before fixing.

**1. Reliable session hand-off.** Retry `signInWithServicePlus` up to 3× with backoff (1 s, 2 s) while the login loader shows. If still failing, continue login, keep retrying in the background, and expose a `sessionReady` future/notifier that backend-dependent screens await (with a "Connecting SevaOne services…" state) instead of failing immediately.

**2. Backend warm-up + single-flight.** After the session is ready, trigger one catalogue fetch (existing trending call or a small warm endpoint). On the backend, make concurrent catalogue requests for the same citizen share one in-flight `getServiceJson` call.

**3. Shared retry policy in `SevaOneMobileApi`.** Per-call timeouts (catalogue/trending/search/departments 40 s; others 15 s). Automatic retry ×2 with backoff + jitter on timeout, socket/handshake errors, HTTP 502/503/504. Never retry 4xx. Make refresh-token handling safe under concurrency.

**4. Staged loading + per-section errors.** Home loads profile/search first, then trending, then DigiLocker and recents. Each Home section shows its own inline "Couldn't load · Tap to retry" card and auto-retries once; no full-screen errors for a single section. Services and DigiLocker screens: full-screen error only after all retries fail.

**5. (Owner approval required.)** Stale-while-revalidate on the phone for catalogue and DigiLocker list: show last good data instantly, refresh live in the background, live always replaces it.

**6. Verify.** Poco + emulator, 10 fresh logins in a row, plus one on mobile data: Services, DigiLocker and Home load first time without pressing Try again. Re-run Step 0 timings and report before/after numbers.

## Done
`flutter analyze` clean, `flutter test` passes, release build installed on the Poco over USB (`adb -s 1ba0f1cd install -r`, keeps data). Dated entry in `docs/mobile/SESSION_HANDOFF.md` with measurements, changes, backend deploy details and what's unverified. Push the branch; don't merge to `main`.
