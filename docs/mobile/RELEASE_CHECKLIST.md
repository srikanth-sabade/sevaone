# SevaOne Mobile: release checklist (M7)

Status on 2026-09-21. Items marked **DONE** are built and tested; the rest need an owner decision, an account, or a real device. Nothing here has been
released. Details of each area are in [`MOBILE_PLAN.md`](./MOBILE_PLAN.md).

## A. Must be fixed before any public release

| # | Item | Why it matters | Status |
|---|---|---|---|
| 1 | **Serve the API over HTTPS.** Set `EXPO_PUBLIC_API_BASE_URL=https://...` for release builds. | Release builds block plain http (except the single CeG exception below); the local `http://<PC>:5094` only works in development. | To do |
| 2 | **Turn dev OTP mode off.** `OTP_DEV_MODE` must be unset and `APP_ENV=production` (the backend refuses to start OTP with both dev mode and production). Test real SMS with a real number: set `SEVASINDHU_OTP_DEV_MODE=0` locally first. | With dev mode on, every OTP is `123456`. | To do |
| 3 | **New secrets per environment.** Generate a fresh `JWT_SECRET` and `OTP_HMAC_SECRET` (at least 32 random characters each); never reuse the local ones. Rotating them signs everyone out. | The local secrets are on this PC only. | To do |
| 4 | **Least-privilege database role.** The backend connects as the `sevaone` superuser. Create a role that can read schema `sakala` and read/write schema `app` only. | A leaked backend credential must not expose the other databases. | To do (deliberately not done locally: the Postgres was to stay untouched) |
| 5 | **Client address behind a proxy.** Set `FORWARDED_ALLOW_IPS` (uvicorn) to the reverse proxy's address. | Today every request appears to come from one address (the Docker gateway), so the per-address limits (20 OTPs an hour, 5 feedback messages an hour) would apply to *all users together*. | To do |
| 6 | **Confirm the DigiLocker redirect address with CeG.** The backend sends `http://localhost:5080/api/v1/auth/digilocker/callback` (the .NET dev value) as `RedirectURL`; the app captures it and never loads it. Ask CeG which redirect URL is registered for the production department key. | A mismatch could make DigiLocker refuse to complete. | To do |
| 7 | **Test a successful DigiLocker login** (new citizen, then the same citizen again by OTP alone) with a real account. | Only the failure/redirect path has been exercised live. | To do (owner) |
| 8 | **Privacy policy URL** and the store privacy forms (Apple "App Privacy", Google "Data safety"). Use section E as the source of facts. | Both stores require them. | To do (owner/legal) |
| 9 | **In-app account deletion** (Apple rule 5.1.1(v)). | Required for apps with sign-up. | **DONE** (`DELETE /auth/me`; the settings screen button is Codex's B-7) |
| 10 | **AI traffic to the GPU box is plain http** (`http://101.53.138.83:8000`, no authentication). Questions and voice clips cross the network unencrypted between our backend and that server. | Citizen questions can contain personal details. Terminate TLS on the GPU box or use a government-controlled private link. Not a third-party VPN service. | To do (owner) |
| 11 | **The old public AI routes are still open.** `/api/v1/ai-chat/message` and `/api/v1/language/asr` in `main.py` need no login because the Seva Sindhu web site (another team's) uses them. Anyone who can reach the backend can still spend the GPU server's time through them. The mobile app uses the new signed-in-only routes. Rate-limit or protect the old routes at the proxy in production. | To do (owner) |

## B. Build and store steps

- **Android:** Play Console account; signing key; build an app bundle; raise `versionCode` for every upload (currently 1). **DONE:** the network security config allows plain http for exactly one address, CeG's DigiLocker gateway `103.138.197.55` (`plugins/withNetworkSecurityConfig.js`, skipped when `APP_VARIANT=development`, so set that variable for development builds); permissions are down to INTERNET, RECORD_AUDIO, MODIFY_AUDIO_SETTINGS and VIBRATE (storage, overlay and foreground-service permissions are blocked). Ask CeG for an https endpoint and delete the plugin when they have one.
- **iOS:** Apple Developer account (USD 99 a year); a Mac for local builds, or Expo's cloud build service (uploads source to Expo; decide whether that is acceptable for a government project); TestFlight for testers. **DONE:** `ITSAppUsesNonExemptEncryption=false` (standard TLS only), microphone purpose text, `buildNumber` 1. **Verify on a real iPhone build:** Apple documents App Transport Security as not applying to raw IP addresses, so CeG's http hop should load; Expo Go cannot prove this.
- **Icons and splash:** the files in `assets/images/` are the Expo template placeholders. Replace them with the official artwork (icon 1024x1024, Android adaptive foreground/background, splash).
- **App name:** `SevaOne` today; decide on the Kannada display name and add localised store listings.

## C. Quality still to do

- **Kannada review** (files in [`kannada-review/`](./kannada-review/)): 137 interface strings are machine-drafted; **31 of 44 departments have no Kannada name** (they hold 1,342 of the 2,410 services); **only 856 of the 2,410 service names have a Kannada name**. Department and service *descriptions* are English-only in the source data.
- **Real devices:** iPhone SE (2020, 375x667) and a low-end Android phone; Kannada text at large font sizes; slow and offline networks; a screen reader (TalkBack, VoiceOver).
- **Voice:** speak real Kannada and English questions; accuracy has not been tested (the emulator microphone is silent).
- **In-app browser limits to test on department sites:** file uploads, and certificate PDF downloads (WKWebView on iOS is weaker than Android).
- **Development pages:** the temporary gallery, browser, login and assistant routes have been removed. Catalogue fixtures (about 120 KB) remain bundled for tests and offline UI checks.
- **No crash reporting or analytics is installed.** If you add either, the privacy policy and store forms change.

## D. Known data limits

- Catalogue data is a snapshot of the Sakala SQL Server dump. Links (783 of them use a host whose TLS certificate is for a different name) refresh when the original DB is connected; the column names stay the same. To refresh: run `backend/scripts/dump_db.py` on the office network, then `backend/scripts/import_dump_to_postgres.py --recreate`.
- Applications status, grievances and notifications are not built (owner decisions); the Applications tab is a placeholder. The Sakala tracking-number design is in plan section 8b.

## E. Data inventory (facts for the privacy policy and store forms)

**Stored on our server** (Postgres database `sevasindhu_mobile`, schema `app`):

| Data | Where | Notes |
|---|---|---|
| Mobile number | `citizen` | Verified by OTP. Used to sign in. |
| DigiLocker id, name, date of birth, gender, address parts, district, taluk, village, state, Kannada name/address | `citizen` | From DigiLocker and the eAadhaar record, once at sign-up. |
| Aadhaar: **last 4 digits only** (`XXXX-XXXX-1234`) | `citizen` | The full number, its hash and the photo are **never stored**. |
| Email, preferred language | `citizen` | Optional / chosen by the citizen. |
| OTP challenges | `otp_challenge` | Only a keyed hash of the code, valid 10 minutes; kept until deleted. |
| Refresh tokens | `refresh_token` | Only a hash; expire after 30 days. |
| Feedback (category, subject, text, contact details) | `feedback` | Guests' contact details are stored as given; signed-in citizens' details are copied from their profile. |
| AI assistant usage counters (who, what kind, when; no question text) | `usage_event` | Used only to cap use per hour; purged after 2 days and deleted with the account. |
| Sign-in and profile events | `audit_log` | Action and time; mobile numbers only as the last 4 digits for OTP events. |

**Deleting an account** removes the citizen row, tokens, OTP challenges and pending sign-ins; feedback text stays with all personal fields cleared; audit rows lose their identifiers.

**Sent to other parties:** the mobile number and OTP text to the Mobile Seva SMS gateway (http); sign-in requests and identity data with CeG's DigiLocker gateway (http, server to server); AI questions and voice clips to our AI service on the GPU server (http) and, for speech, onward to the speech-to-text provider the AI service uses (retention there must be confirmed with the AI team).

**On the device:** the refresh token and a cached copy of the profile, in the phone's secure storage (Keychain / Keystore); language and settings. No analytics, advertising or crash-reporting libraries.
