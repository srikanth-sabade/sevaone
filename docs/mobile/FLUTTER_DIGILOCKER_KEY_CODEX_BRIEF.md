# Flutter: DigiLocker without a backend login (brief for Codex)

App `apps/eserviceplus-master`, backend `apps/Seva-Sindhu-One/backend` (FastAPI, GPU server `101.53.138.83:8002`, DB `sevasindhu_mobile`). Branch `flutter-modernize`.

## Owner decisions (final, 2026-10-06)
- **No second login.** After the NIC login the app must NOT call `signInWithServicePlus` / `/api/v1/mobile/auth/serviceplus/login` or any other backend session creation. The app must be fast: NIC calls stay direct.
- **DigiLocker uses a per-phone "DigiLocker key"**, issued once when the user links DigiLocker on that phone (the DigiLocker OAuth approval is the identity proof). The backend stores `digilocker_id` in `app.citizen` already; documents are fetched by looking up the key → citizen → `digilocker_id` → CeG.
- **The key is kept after logout** (per phone). Guard against a different person on the same phone: store the key together with the NIC `signNo` that linked it; the app only uses the key when the signed-in `signNo` matches, otherwise it shows "Link DigiLocker".
- **Notifications: hide for now** (bell, unread count, Home entry, notification screen routes). Don't delete the code or backend routes.
- **Assistant and Feedback work with no session at all.**
- Never put CeG department keys/secrets in the app. The backend stays a thin pass-through.

## Backend
1. **Key issuance at link.** Change the existing DigiLocker link flow (`mobile_digilocker_link.py`, `digilocker.start_sign_in` / `get_profile`) so it works without a SevaOne citizen session:
   - `POST /api/v1/mobile/digilocker/link/start` — no auth; body `{ signNo, deviceId }`; returns the DigiLocker sign-in URL + state (as today).
   - `POST /api/v1/mobile/digilocker/link/complete` — no auth; completes OAuth with CeG (existing code), finds or creates the `app.citizen` row for `signNo` (reuse existing linking/conflict logic: one `digilocker_id` per citizen), saves `digilocker_id`, and issues a **random 256-bit key**. Store only its SHA-256 hash in a new table `app.digilocker_device_key(id, citizen_id FK ON DELETE CASCADE, key_hash UNIQUE, device_label, created_at, last_used_at, revoked_at)` — new numbered migration. Return the key once.
2. **Key-auth dependency** `current_digilocker_citizen`: header `X-DigiLocker-Key`; hash, look up active row, update `last_used_at`; 401 if missing/revoked. Rate-limit failures.
3. Routes using it: `GET /api/v1/mobile/digilocker/documents` (same response as today's `/me/digilocker/documents`, same retryable 502 on CeG failure) and `GET /api/v1/mobile/documents/{documentId}/preview` (must check the document belongs to that citizen). `POST /api/v1/mobile/digilocker/unlink` revokes the key (optional UI).
4. **Assistant and Feedback**: make their routes work without a bearer token (rate-limit by IP/device). Keep accepting a token if sent.
5. Keep the old JWT routes working for now (other clients); don't delete them in this change. Tests for: link → key; documents with key; wrong/revoked key → 401; preview of another citizen's document → 404; assistant/feedback anonymous.
6. Deploy: back up changed files on the GPU server first, run the migration only on `sevasindhu_mobile`, restart `sevaone-sevasindhu-backend` via supervisorctl, verify. Also confirm whether the Step-0 timing-only `serviceplus_catalog.py` instrumentation is still deployed and remove it if so.

## App
1. Remove the background `establishSessionWithRetry` / `signInWithServicePlus` call from `login_screen.dart` and every "Connecting SevaOne services…" wait tied to it.
2. `DigiLockerKeyStore` (flutter_secure_storage): `{ key, signNo }`. Not cleared on logout.
3. DigiLocker screen: no key for current signNo → "Link DigiLocker" (opens link/start URL in the existing in-app DigiLocker WebView, then link/complete, store key). Has key → load documents with `X-DigiLocker-Key`. 401 → clear key, show "Link DigiLocker". 502 → automatic retry (existing policy) then inline retry.
4. Home DigiLocker preview: same key; reuse the DigiLocker screen's result where possible instead of a second call; no preview card if not linked (no error).
5. Hide Notifications everywhere in the UI.
6. Assistant and Feedback: call without any token.
7. Never send the NIC password or NIC tokens to our backend.

## Rules
Never remove/change ServicePlus APIs or WebView certificate/PDF behaviour. Don't enter credentials, link DigiLocker or submit anything yourself — ask the owner to do it on the Poco (`adb -s 1ba0f1cd`, USB).

## Verify
`flutter analyze` clean, `flutter test` and backend tests pass, release installed on the Poco (`install -r`, keeps data). Owner checks: login → Home appears instantly with no backend login in logcat; link DigiLocker once; documents load; logout/login → documents load without linking again; different NIC user on same phone → "Link DigiLocker"; Assistant and Feedback work; no Notifications visible. Dated entry in `docs/mobile/SESSION_HANDOFF.md`; push the branch; don't merge to main.
