# Track B (Codex) — Work Plan

You are **Track B**: the mobile UI. Track A (Claude) owns the backend, the API layer and the in-app browser. This file is everything you need to work without asking
questions. The overall plan is [`MOBILE_PLAN.md`](./MOBILE_PLAN.md); running state is [`SESSION_HANDOFF.md`](./SESSION_HANDOFF.md).

## Paste-ready kickoff prompt

> You are Track B (Codex) on the SevaOne mobile app in `apps/Seva-Sindhu-One/mobile`. Read, in order: `apps/Seva-Sindhu-One/mobile/AGENTS.md`,
> `docs/mobile/MOBILE_PLAN.md`, `docs/mobile/SESSION_HANDOFF.md`, and `docs/mobile/TRACK_B_CODEX_PLAN.md`. Work in the shared folder (no branches: do not run git checkout/switch/stash/reset/clean/commit; the owner commits). Do the next unfinished task in the
> Track B plan, only inside the paths Track B owns. Before finishing, run `npm run lint`, `npm run typecheck` and `npm test` from the mobile folder, then append a dated entry under
> "Track B" in `docs/mobile/SESSION_HANDOFF.md` (what changed, what is next, what is blocked).

## State you start from (M0 is done)

Expo SDK 57 app, verified running on the Android emulator. Present and working: design tokens (`src/theme`), bundled fonts, i18n with Kannada default and a plural polyfill,
a language store (`src/lib/language.ts`), `AppText`, `ScreenPlaceholder`, four placeholder tabs, and the catalogue data layer.
**The catalogue hooks call the live backend by default** (B1 is done; the owner's emulator/phone reach it automatically). **Jest always uses fixtures** (a slice of the live API's real payloads), so your tests never touch the network. The hooks:
`useDepartments(search)`, `useSubDepartments(departmentId)`, `useServices(departmentId, sub, search)`, `useService(serviceId)` in `src/api/catalogHooks.ts`
(types in `src/api/types.ts`). Track A will later point them at the live backend without changing their signatures.
`openInAppBrowser({ url, title })` in `src/lib/browser.ts` is the only way to open a web page; today it shows a placeholder, M2 makes it real.

## What you own (edit freely) and what you must not touch

| Yours | Track A's: do not edit (leave a note in the handoff instead) |
|---|---|
| `src/components/**`, `src/theme/**`, `src/app/**` route files for the screens below, i18n text in `src/i18n/locales/*.json` under your prefixes (`home`, `catalogue`, `common`, `login`, `settings`, `helpline`) | `backend/**`, `src/api/**`, `src/lib/**`, `src/app/_layout.tsx`, `src/app/browser.tsx`, `package.json` dependencies (ask Track A; if you truly need a package, say so in the handoff) |

`AppText.tsx` and `ScreenPlaceholder.tsx` are M0 primitives written by Track A; you may extend them (add variants), but keep their current props working.

## Hard rules (every task)

1. **Design = SevaOne web frontend look**: use only tokens from `src/theme` (colour, spacing, radius, shadow); never hard-code hex or magic numbers. Layout is ~60-70% like the reference app `apps/Seva-One/sevaone-mobile-app-legacy` (read-only; look at its screens for composition, do not copy code).
2. **Small screen first.** Every screen must work at **375x667** (iPhone SE 2020) with Kannada text, which is taller than English. No text clipped, no horizontal scroll, scrollable content, touch targets at least 44x44.
3. **No hard-coded user-visible strings.** Add keys to both `en.json` and `kn.json` (a unit test fails if they diverge). Plurals use `key_one` / `key_other`. Kannada is machine-drafted for now; mark uncertain ones in the handoff.
4. Use `AppText`, not bare `Text`, so the right font is picked per language.
5. **Nothing opens outside the app.** Never call `Linking.openURL` for a web page; use `openInAppBrowser`.
6. Every screen handles four states: loading, error (with a Retry button that calls `refetch`), empty, and data. Build `LoadingState`, `ErrorState`, `EmptyState` components once and reuse them.
7. Add accessibility labels/roles to tappable elements. Add a unit or component test for every component and screen (`@testing-library/react-native`, files under `__tests__/`).
8. Expo SDK 57 differs from what you remember: check `https://docs.expo.dev/versions/v57.0.0/` before using any Expo API. Use `npx expo install` (ask Track A) rather than `npm install` for native libraries.
9. Do not run `npm install` or start Metro/Expo (Track A owns `node_modules` and the dev server on port 8081; two dev servers collide). `npm run lint`, `npm run typecheck`, `npm test` must all pass before you finish.
10. **Do not open `docs/secrets/` or any `.env` file.** You never need credentials; DigiLocker, SMS and JWT keys are Track A's job.
11. Owner tests on the Android emulator and an iPhone SE (2020). You may not be able to run either: describe in the handoff exactly how to see each change (route path + steps) so the owner can check.

### Tests must NOT live under `src/app/`
Expo Router registers **every** `.ts`/`.tsx` file under `src/app/` as a route and bundles it into the app (its route matcher only excludes `+api`, `+html` and `+middleware`). A test file there becomes a route (`/services/__tests__/...`) and ships test code in the production bundle. Put screen tests under `src/__tests__/` (for example `src/__tests__/screens/serviceCatalogue.test.tsx`) and import the screen with `@/app/(tabs)/services`. **Existing files to move:** everything in `src/app/(tabs)/services/__tests__/`.

## Tasks, in order (do the first unfinished one; tick it in the handoff)

### B-1  UI primitives (`src/components/`)
`Button` (primary / secondary / ghost, disabled + loading), `Card`, `SearchBar` (with clear button, controlled), `ListRow` (title, optional subtitle/badge, chevron, pressable),
`Chip`, `LoadingState`, `ErrorState` (message + Retry), `EmptyState` (message + optional action), `Screen` (safe-area wrapper with background colour and optional scroll).
*Done when:* each has a test, and a temporary route `src/app/dev-gallery.tsx` (only rendered when `__DEV__`) shows them all in both languages.

### B-2  Catalogue: department list (route `src/app/(tabs)/services/index.tsx`, replaces the Services placeholder)
Search bar at the top; below it the department list (`useDepartments(search)`), Kannada name first when the language is `kn` and `nameKn` exists, else English (**most departments have no Kannada name: 31 of 44 are null, and that is expected**). Each row shows the
service count (plural-aware). Debounce the search by 300 ms. Tap navigates to `/services/[departmentId]`.
*Done when:* all four states work; searching "rev" narrows to Revenue; tests cover render, search and tap.
*Route note:* converting the `services` tab into a folder means `src/app/(tabs)/services/_layout.tsx` (a `Stack` with our header styling) plus `index.tsx`. Keep the tab title and icon unchanged.

### B-3  Catalogue: sub-departments -> services
- `/services/[departmentId]`: `useSubDepartments`. **If the result is empty (0 or 1 real sub-department), replace the route with the services list** (`router.replace`) so the back button does not bounce. Otherwise list sub-departments with counts. The list may end with an entry named `_other` (exported as `OTHER_SUB` from `src/api/types.ts`): show it as a localised "Other services" (`catalogue.otherServices`) and pass it through as `sub=_other`.
- `/services/[departmentId]/list?sub=<name>`: `useServices(departmentId, sub, search)` with an in-screen search bar. Row = service name (Kannada first when available), small "Apply online" badge only when `hasApplyUrl`.
- Try the skip-level case with `karnataka-public-service-commission` in the fixtures and a multi-sub one like `revenue-department`.
*Done when:* both paths tested, header titles show the department/sub-department name, and back navigation is sensible.

### B-4  Catalogue: service detail + Apply
Route `/services/detail/[serviceId]` using `useService`. Sections shown only when non-null: definition, eligibility, supporting documents, procedure, fee, delivery time.
**Apply button** (sticky at the bottom): if `applyUrl` is set call `openInAppBrowser({ url: applyUrl, title: name })`; if null show a disabled button labelled "Not available online".
If `applyUrlType === "department-homepage"`, show a note above the button: "This opens the department's website; you may need to find the service there."
*Done when:* tested for all three cases (deep link, homepage, no URL); long Kannada text scrolls without clipping at 375x667.
*Note:* `openInAppBrowser` is now the real in-app browser (M2 done). Apply for a Seva Sindhu link currently ends on a "security certificate is not valid" screen because the snapshot data's link points at a host with a bad certificate; that is expected, the owner says ignore it (links refresh with the original DB), and it is not an app bug. You may see the browser by hand at route `/dev-browser`.

### B-5  Login screens (B2 is DONE: the auth API is live, start now)
Screens `src/app/(auth)/mobile.tsx`, `otp.tsx`, `digilocker.tsx`, plus a language picker on the first one. **You build UI only; all logic is in `@/lib/auth` (Track A, tested):**
- `startOtp(mobile)` -> `{ challengeId, expiresInSeconds, resendAfterSeconds }` (use `resendAfterSeconds`, currently 30, for the resend countdown). Errors are `ApiError`s: 400 invalid number, 429 too many/too soon, 502 SMS failed, 503 unavailable; show `error.message`.
- `verifyOtp(challengeId, otp)` -> `{ kind: "signedIn" }` (existing DigiLocker-linked citizen: go to the app) or `{ kind: "digilockerRequired", pendingId }` (go to the DigiLocker screen). A wrong code rejects with an `ApiError` (status 403); `attemptsLeft(error)` gives the tries left (5 total), or `null` for an expired/locked challenge (send them back to request a new OTP).
- `signInWithDigiLocker(pendingId)` opens DigiLocker **inside the app** and resolves when signed in. It rejects with `DigiLockerCancelled` (user closed the browser: offer "Try again") or an `ApiError` (show its message).
- `useAuth()` (zustand) gives `{ status: "loading" | "signedOut" | "signedIn", citizen }`; `signOut()` ends the session. `restoreSession()` already runs at app start.
- Try the real flow by hand at route `/dev-login` (dev builds): in dev mode the OTP is always `123456` and no SMS is sent.
UI: 10-digit mobile input with inline validation (Indian mobiles start 6-9); 6-box OTP entry (paste-friendly, auto-advance, resend countdown, "N tries left" message); a DigiLocker explainer screen with one button and a "Try again" state; never display or log the OTP after entry; nothing may open outside the app.
*Done when:* every error/empty/loading state exists, tests cover validation and the error messages with `@/lib/auth` mocked (see `src/lib/__tests__/auth.test.ts` for how), and screens work at 375x667 in Kannada and English. Tests go under `src/__tests__/`, not `src/app/`.

### B-6  Home dashboard (`src/app/(tabs)/index.tsx`)
Compose per the reference app: greeting + bell, search bar (opens the Services search), promo carousel, category strip, quick-services grid, recently used. Use fixture data in `src/components/home/fixtures.ts` for anything without an API (promo banners, quick services); mark it clearly as placeholder.

### B-8  AI assistant chat screen (M5 is DONE: build the UI)
Route `src/app/assistant.tsx` (a stack screen, opened from a card/button on Home and later a floating button). **All logic is in `@/lib/assistant`, `@/lib/voice` and `@/lib/chatMarkdown` (Track A, tested); you build the UI only.**
- `useAssistant()` gives `{ messages, sending, sessionId }`; `sendQuestion(text, "Text" | "Voice")` sends (Kannada/English follows the app language, and follow-up questions keep context automatically); `retryLast()` retries a failed question; `resetConversation()` starts a new chat.
- Message: `{ id, role: "user" | "assistant", text, responseType, service, suggestedQuestions, error }`. Render assistant `text` with `parseChatMessage(text)` -> blocks (`paragraph` with bold `inline` segments, `bullets`, `numbered`): the raw text contains `**bold**` and `- ` markers that must never show as symbols.
- `message.service` (set when the assistant resolved one service): show a "View service" button. On press call `resolveCatalogueService(message.service)` -> a catalogue id, then `router.push({ pathname: "/services/detail/[serviceId]", params: { serviceId } })`. It returns `null` when there is no confident match: show a short note ("This service is not in the catalogue yet") instead of a link.
- `message.suggestedQuestions`: tappable chips that call `sendQuestion`. `message.error`: an error bubble with a Retry button (`retryLast`). While `sending`: a typing indicator, and the send button disabled.
- Input bar: multiline text input (max `MAX_QUESTION_LENGTH` = 500), send button, and a microphone button using `useVoiceInput()` (see B-9 for the voice states). Empty state with three example questions (i18n strings). A small permanent line: "AI answers can be wrong: check the service page before applying."
- Try the logic by hand at route `/dev-assistant` (dev builds).
*Done when:* all states exist (empty, sending, answer, clarify, error, no-catalogue-match), the markdown renders without symbols, tests mock `@/lib/assistant` and `@/lib/voice` (see `src/lib/__tests__/assistant.test.ts`), and it works at 375x667 in Kannada and English. Put tests under `src/__tests__/`.

### B-9  Voice input (microphone) on the catalogue search and the chat
`useVoiceInput()` from `@/lib/voice` returns `{ state: "idle" | "recording" | "transcribing", seconds, start(), stop(), cancel() }`. `start()` asks for microphone permission the first time; `stop()` returns the transcript (a string) or throws `VoiceError` with `kind`: `"permission"` (show how to enable the microphone in Settings), `"empty"` ("I could not hear that, please try again"), `"network"` ("Check your connection"), `"failed"`. Recording stops itself after `MAX_RECORDING_SECONDS` (30).
Add a microphone button to `SearchBar` (a new optional `onVoicePress`/voice props, keep existing props working) and wire it on the department, sub-department and service search screens: tap to start (show the elapsed seconds and a clear "listening" state), tap again to stop, put the transcript into the search text. Same button in the chat input bar.
*Done when:* each `VoiceError` kind has a visible message, the recording state is obvious (and screen-reader announced), and tests mock `@/lib/voice`.

### B-10  Small fixes noticed during testing
- Service detail shows the department name twice when the sub-department has the same name (for example "Revenue Department" under "Revenue Department"): hide the sub-department line when it equals the department name (compare case-insensitively).
- Move any tests still under `src/app/` to `src/__tests__/` (see the rule above).

### B-7  Helpline, feedback, settings (the backend is DONE: build UI only)
- **Helpline:** a static directory (police 100, ambulance 108, women helpline 181, child helpline 1098, Sakala, and so on; put the list in `src/components/helpline/data.ts`, marked as static). Tapping a number must NOT leave the app to a browser; phone numbers use the phone dialler via `Linking.openURL("tel:...")` (the only allowed use of `Linking.openURL`, for `tel:` numbers).
- **Feedback form:** `submitFeedback({ category, subject?, comments, name?, mobile?, email? })` from `@/lib/feedback` -> `{ ok, reference }`. `category` is free text (the department or topic, max 100), `comments` 5 to 2000 characters. Signed-in citizens: send only category/subject/comments (the server attaches their verified profile and ignores contact fields). Guests: `mobile` (10 digits starting 6-9) or `email` is required. Show the returned `reference` on success. Errors are `ApiError`s: 422 (`error.message` explains the field), 429 (too many messages), network. Validate the same rules inline before sending.
- **Settings:** language switch (`useLanguage().setLanguage`; when signed in also call `updateProfile({ preferredLanguage })` from `@/lib/auth`, and adopt `citizen.preferredLanguage` after login), the low-literacy/icon-first toggle (local only for now), and for signed-in citizens a read-only profile card (name, mobile, Aadhaar last 4, district; email is editable through `updateProfile({ email })`, and `email: null` clears it) plus a "Sign out" button (`signOut()`).
- **Delete my account (required by Apple, App Store rule 5.1.1(v)):** a clearly labelled button in settings for signed-in citizens. It must open a confirmation that says what is deleted (your account, verified profile and sign-in on this device; feedback you sent stays only as anonymous text) and cannot be undone; on confirm call `deleteAccount()` from `@/lib/auth` (server deletes, then this device is signed out). Show a loading state, and on failure a retryable error (the session stays). After success navigate to the signed-out state.
- **Not in scope (owner decisions, 2026-09-21):** grievance and notifications remain parked or skipped. Applications status was subsequently unparked by the owner on 2026-09-22; follow B-16 below.
*Done when:* every state exists (loading, success with reference, each error), tests mock `@/lib/feedback` and `@/lib/auth`, and it works at 375x667 in Kannada and English. Tests go under `src/__tests__/`.

### B-16  Recent services, featured schemes and Sakala application tracking (owner request 2026-09-22)
- Record a service after its final detail data loads. Persist a de-duplicated list in SecureStore and render at most five cards horizontally below Home's Quick access section; use the service's Kannada name when available and fall back to English.
- Add light token gradients to Featured services, including Gruha Lakshmi and Gruha Jyothi, and keep each card linked to its department catalogue route.
- Applications is no longer a placeholder. Add a 15-digit GSC input and guest lookup using the Python route `POST /api/v1/mobile/applications/track`. Signed-in citizens save a resolved number through `POST /api/v1/mobile/applications` and see their latest five from `GET /api/v1/mobile/applications` under My Applications. The Python backend calls Sakala; the app never calls .NET or Sakala directly.
- Keep all new copy in `en.json` and `kn.json`, validate exact GSC length before requesting, and show not-found/network/error states. Do not invent or seed a GSC test number until the owner supplies one.
*Done when:* detail visits survive a reload, Home shows only the newest five, Featured services contains the two named schemes, and the Applications tracker renders a mapped Sakala result and saves it for a signed-in citizen.

### B-8  AI assistant chat screen (M5 is DONE: build the UI)
Route `src/app/assistant.tsx` (a stack screen, opened from a card/button on Home and later a floating button). **All logic is in `@/lib/assistant`, `@/lib/voice` and `@/lib/chatMarkdown` (Track A, tested); you build the UI only.**
- `useAssistant()` gives `{ messages, sending, sessionId }`; `sendQuestion(text, "Text" | "Voice")` sends (Kannada/English follows the app language, and follow-up questions keep context automatically); `retryLast()` retries a failed question; `resetConversation()` starts a new chat.
- Message: `{ id, role: "user" | "assistant", text, responseType, service, suggestedQuestions, error }`. Render assistant `text` with `parseChatMessage(text)` -> blocks (`paragraph` with bold `inline` segments, `bullets`, `numbered`): the raw text contains `**bold**` and `- ` markers that must never show as symbols.
- `message.service` (set when the assistant resolved one service): show a "View service" button. On press call `resolveCatalogueService(message.service)` -> a catalogue id, then `router.push({ pathname: "/services/detail/[serviceId]", params: { serviceId } })`. It returns `null` when there is no confident match: show a short note ("This service is not in the catalogue yet") instead of a link.
- `message.suggestedQuestions`: tappable chips that call `sendQuestion`. `message.error`: an error bubble with a Retry button (`retryLast`). While `sending`: a typing indicator, and the send button disabled.
- Input bar: multiline text input (max `MAX_QUESTION_LENGTH` = 500), send button, and a microphone button using `useVoiceInput()` (see B-9 for the voice states). Empty state with three example questions (i18n strings). A small permanent line: "AI answers can be wrong: check the service page before applying."
- Try the logic by hand at route `/dev-assistant` (dev builds).
*Done when:* all states exist (empty, sending, answer, clarify, error, no-catalogue-match), the markdown renders without symbols, tests mock `@/lib/assistant` and `@/lib/voice` (see `src/lib/__tests__/assistant.test.ts`), and it works at 375x667 in Kannada and English. Put tests under `src/__tests__/`.

### B-9  Voice input (microphone) on the catalogue search and the chat
`useVoiceInput()` from `@/lib/voice` returns `{ state: "idle" | "recording" | "transcribing", seconds, start(), stop(), cancel() }`. `start()` asks for microphone permission the first time; `stop()` returns the transcript (a string) or throws `VoiceError` with `kind`: `"permission"` (show how to enable the microphone in Settings), `"empty"` ("I could not hear that, please try again"), `"network"` ("Check your connection"), `"failed"`. Recording stops itself after `MAX_RECORDING_SECONDS` (30).
Add a microphone button to `SearchBar` (a new optional `onVoicePress`/voice props, keep existing props working) and wire it on the department, sub-department and service search screens: tap to start (show the elapsed seconds and a clear "listening" state), tap again to stop, put the transcript into the search text. Same button in the chat input bar.
*Done when:* each `VoiceError` kind has a visible message, the recording state is obvious (and screen-reader announced), and tests mock `@/lib/voice`.

### B-10  Small fixes noticed during testing
- Service detail shows the department name twice when the sub-department has the same name (for example "Revenue Department" under "Revenue Department"): hide the sub-department line when it equals the department name (compare case-insensitively).
- Move any tests still under `src/app/` to `src/__tests__/` (see the rule above).

### B-7  Helpline, feedback, settings
Helpline directory (static list), feedback form (`POST /api/feedback` exists on the backend; Track A wires the call, you build the form and validation), settings (language switch, low-literacy/icon-first toggle).

## Definition of done for the whole track
All tasks ticked in the handoff; lint, typecheck, tests green; each screen checked at 375x667 in Kannada and English; no hard-coded strings or colours; the handoff describes how to open every screen.

> **UPDATE 2026-09-21 (owner, after testing on a real phone): the app opens on the LOGIN screen first, with a "Skip for now" button that goes to Home as a guest.** This supersedes item 1 below ("the app must open on Home") and the "no redirect" wording. Implemented by Track A: `src/lib/launch.ts` (`useLaunch.guestChosen`, in memory only, so a guest sees the login screen again next launch), `src/app/(tabs)/_layout.tsx` (a signed-out user who has not skipped is redirected to `/(auth)/mobile`; signing in clears the choice, so signing out or deleting the account shows the login screen again), and the login screen's Skip button (label `login.continueWithoutSigningIn` = "Skip for now") calls `chooseGuest()` before returning. Everything else in B-11 still holds (guests get Home, Services, Helpline, Feedback; the assistant and Profile need sign-in). Do not remove the login-first redirect.

### B-11  Guest access (owner decision 2026-09-21: "guest for browsing, sign in for the assistant")
The app must open on Home for everyone. Guests can use Home, Services (the whole catalogue and Apply), Helpline, Feedback and language settings. Signing in is required only for the **AI assistant** (text and voice) and for the Profile tab. The backend enforces this: `/api/v1/mobile/assistant/*` answers 401 without a session and caps use per citizen (40 questions and 20 voice clips an hour, then 429).
1. **Remove the automatic redirect to login** from `src/app/(tabs)/_layout.tsx` (and any other place that sends a signed-out user to `/(auth)/mobile` on launch). The app starts on the tabs; `unstable_settings.initialRouteName` already guarantees that.
2. **Profile tab, signed out:** do not redirect. Show a friendly card ("Sign in to see your profile") with a "Sign in" button that opens the login screens. The language switch, Helpline and Feedback stay reachable for guests (from Profile or Home).
3. **Sign-in entry points for guests:** a "Sign in" button on Home (top area) and on the assistant gate (next item).
4. **Assistant gate:** when `useAuth().status !== "signedIn"`, `/assistant` shows an explanation ("Sign in to ask the assistant") with a "Sign in" button and never calls `sendQuestion`; hide or disable its microphone. Home's assistant shortcut stays visible to guests and leads to this gate.
5. **Return after sign-in:** pass a `returnTo` route through the login screens (`mobile` -> `otp` -> `digilocker`) and go back to it when signed in (default: Home). Add a "Continue without signing in" link on the first login screen that returns to where the user was.
6. **Assistant error kinds:** assistant error messages now carry `errorKind`: `"signin"` (session missing or expired: show the sign-in button, no Retry), `"limit"` (hourly cap: show `message.text`, no Retry), `"network" | "timeout" | "server"` (Retry as before). Add translated strings for each.
7. **Voice error kinds:** `VoiceError.kind` also has `"signin"` and `"limit"` now. In `src/components/useVoiceCapture.ts` these currently map to the generic "failed" text (a stopgap by Track A): give them proper Kannada/English messages. Hide the catalogue search microphone for guests (voice needs a session).
8. Keep **all screens usable at 375x667 in Kannada**; tests under `src/__tests__/`; nothing may open outside the app.
*Done when:* a cold start with no saved session lands on Home (not the login screen); a guest can browse services, open a service and its Apply page, call a helpline number and send feedback; the assistant shows the sign-in gate; signing in from the gate returns to the assistant; lint, typecheck and tests pass.

### B-12  Polish found while reviewing B-11 on the emulator (small)
1. **The assistant sign-in gate has no visible way out.** It opens as a bare card at the top of a blank screen with no header or back button (only the Android hardware back or the iOS edge swipe leave it). Give it a header with a back arrow and a title, like the login screens have.
2. **Guests are told "Welcome back".** Home greets a signed-out visitor with "ಮತ್ತೆ ಸ್ವಾಗತ / Welcome back" above "ನಾಗರಿಕರು / Citizen". A first-time guest has not been here before: use a neutral greeting ("Welcome") when signed out and keep "Welcome back" for signed-in citizens.
3. Re-check both at 375x667 in Kannada and English; tests under `src/__tests__/`.

### B-13  Modern login screen (owner request 2026-09-21: "the UI is not modern at all")
**Goal:** the first screen a citizen sees should feel like a current consumer app, not a form on a coloured bar. Redesign `src/app/(auth)/mobile.tsx` first; then give `otp.tsx` and `digilocker.tsx` the same shell so the flow is consistent. Behaviour must not change: keep `startOtp`, `returnTo`, the `useLaunch` "Skip for now" logic (`chooseGuest()` then `router.replace(returnTo)`), validation rules, error and loading states, and every existing `testID`/accessible label the tests use (update tests when text changes).

**Assets and libraries already provided (do not install anything):**
- `assets/images/sevasindhu-logo.png`: the Seva Sindhu logo (tree with service icons, "ಸೇವಾ ಸಿಂಧು", waves), transparent background, 960x789 (upscaled from a 320px original: use it at 140-180dp wide, not larger; the owner will supply a sharper original later).
- `expo-linear-gradient` and `react-native-svg` are installed (both work in Expo Go).
- `src/components/AppSplash.tsx` (Track A) is the startup splash; match its look: `#F6F5FF` to `#FFFFFF` gradient, logo, three small dots in saffron, gold and green.

**Colour theme ("SevaOne Modern"):** brand indigo `colors.primary #5553D2` (actions, selected states, links), deep indigo `#4137A6` (pressed), navy `#1A1C37` (headings), muted `#5F6275` (secondary text), surfaces white with a lavender wash `#F6F5FF`. Accents taken from the logo and the web app: saffron `#F67D51`, gold `#EFB146`, green `#00AD82`, teal tint `#CAF5F2`. Use accents sparingly (illustration shapes, tiny indicators), never for large fills or text. Add any new tokens (gradients, larger radii such as 24, a softer shadow) to `src/theme` and use only tokens in components.

**Layout (design for 375x667, then let it breathe on taller phones):**
1. **No coloured header bar.** Hide the native header on the auth screens (`headerShown: false`). Full-screen gradient background (`#F6F5FF` to `#FFFFFF`), dark status bar. On OTP/DigiLocker only, a plain icon-only back button (no bar, no title bar colour).
2. **Hero (about the top third):** the logo on a soft circular glow, with a light illustration behind and around it built with `react-native-svg`: overlapping translucent blobs (indigo 8-12%, teal, saffron 12-15%) and 5-6 small white rounded "service chips" carrying Ionicons (document, home, flash, water, card, car: the same services as the logo's leaves) at varied positions with soft shadows. A slow, tiny floating motion (`Animated`, 3-5 s loops, a few dp) that is switched off when reduce-motion is on. On short screens (height under 700dp) or while the keyboard is open, shrink the hero to the logo only.
3. **Headline** "ಸೇವಾಒನ್‌ಗೆ ಸ್ವಾಗತ" / "Welcome to SevaOne": large (28-30) in the heading font, navy; one muted subtitle line below. Kannada may wrap to two lines: use `lineHeightFor`.
4. **Form card:** white, radius 24, soft shadow, generous padding. One large field with a leading phone icon and a fixed "+91" prefix, inline label, clear focused/error/valid states (border colour change plus the message below), then a full-width pill primary button (56dp) with an indigo gradient and an arrow icon, and a loading state. The primary button is the only filled control on the screen.
5. **"Skip for now"** as a plain text button under the card (no fill, no border), muted indigo, at least 44dp tall.
6. **Trust line:** a small lock/shield icon plus a short caption ("Secured with OTP and DigiLocker"), muted. New strings in both languages.
7. **Language selector at the very bottom** (respect the bottom safe area): plain text only, `ಕನ್ನಡ   |   English`. The selected language is in the primary colour and bold with a small dot or underline under it; the other is muted. No chips, pills, boxes or backgrounds. Touch target at least 44dp.
8. **Motion:** a short staggered entrance (hero fades and scales in; the card slides up about 16dp and fades; about 60 ms apart, 400 ms total), skipped when reduce-motion is on. Keyboard: `KeyboardAvoidingView` plus scrolling so the field and button are never hidden; nothing jumps.
9. **OTP screen:** six rounded boxes with clear empty/active/filled/error states (keep paste, auto-advance, resend countdown and attempts-left), same shell and card style. **DigiLocker screen:** same shell, an illustration of a shield/document with the explanation and one primary button.
10. **Quality bar:** text contrast at least 4.5:1, works at 200% font scale without clipping, all controls labelled for screen readers, tap targets at least 44dp, no hard-coded colours or strings (both languages), no external links.

**Reusable pieces:** build `src/components/auth/AuthShell.tsx` (background, safe areas, keyboard handling, optional back button), `AuthHero.tsx` (logo plus illustration), `LanguageFooter.tsx` and a `PrimaryGradientButton`, so other screens can adopt the look later. Tests under `src/__tests__/`.
*Done when:* the three auth screens share the new look at 375x667 and on a tall phone in Kannada and English, keyboard open and closed, and lint, typecheck and tests pass. Describe in the handoff exactly how the owner can see each state (cold start shows the login screen).

### B-14  Fixes from the owner-side review of B-13 (real bugs first; the visual design itself is approved)
Reviewed on the emulator at 750x1334 (the iPhone SE's 375x667 points) and a tall phone, Kannada and English. The new look is good: keep it. These are defects, in priority order:
1. **OTP entry loses and mis-handles digits (real bug, affects real phones).** (a) Sending `123456` as fast input (the emulator, a fast typist, **paste, and Android's SMS-code autofill** all do this) filled the boxes with `3 4 5 6 _ _`: the first digits were lost. (b) **Backspace does not go back:** with `1 2 3 4 5 _` and focus on the last box, pressing backspace repeatedly clears nothing and focus never moves to the previous box, so a wrong digit cannot be corrected without tapping the exact box. Typing one digit at a time on a fresh screen works, which is why it was missed. **Fix by design, not by patching:** drive the six visual boxes from ONE hidden `TextInput` (`maxLength={6}`, `keyboardType="number-pad"`, `textContentType="oneTimeCode"`, `autoComplete="sms-otp"`, autofocus, digits only, strip non-digits from pasted text); the boxes only display `value[i]` and highlight the caret box (`min(value.length, 5)`); tapping any box focuses the input. That makes typing, fast entry, paste, backspace and autofill all correct for free. Keep resend countdown, attempts-left and error states. **Tests:** typing 6 digits at once, pasting `"12 34-56"`, backspace from a full and a partial code, and rejecting letters.
2. **On a small screen the Verify button hides behind the keyboard** (only its top edge shows at 375x667 with the keyboard open). Keep the primary button visible above the keyboard: collapse the hero further (hide the logo) while the keyboard is open on this screen, or pin the button above the keyboard. Also submit automatically when the sixth digit is entered (with a short "verifying" state), keeping the button as a fallback.
3. **DigiLocker screen: the logo is clipped at the top** at 375x667 (only "ಸೇವಾ ಸಿಂಧು" and the waves show, the tree is cut off). The logo, illustration and card must all fit without clipping; on short screens drop the logo from this screen (the illustration is enough) or shrink it.
4. **Login screen: grey square behind the logo** in the compact hero at 375x667 while the field is not focused (it disappears once the field has focus). Almost certainly an Android `elevation`/shadow drawn from a view with a transparent background: remove the elevation from the logo wrapper, or give it a matching solid background, and check both the resting and focused states.
5. **Language names must always be in their own script:** the footer shows "Kannada | English" when the app is in English. It must always read `ಕನ್ನಡ  |  English` (a Kannada speaker who cannot read English has to be able to find it). Use fixed strings, not translated ones.
6. **OTP screen: "Resend code" is left-aligned** under a centred "Didn't get the code?" line: centre it.
7. Optional: faint peach blobs peek out at the bottom corners of the logo circle on the login hero; either make them intentional or remove them.
*Done when:* items 1 to 6 are fixed with tests (item 1 is the priority), and the owner can retest on a real phone: type the OTP fast, paste it, use backspace, and see the verify button on a small screen.

### B-15  Design language from the owner's Figma ("Task management & to-do list app", owner request 2026-09-22: "I want you design like this")
Reference: Figma Community file "Task management & to-do list app" (https://www.figma.com/design/3rg9VC3mligUZ066uD8XPl, node 1-77; public, read only). It is a *task* app, so copy its **visual language and component shapes**, not its content. B-13's login look is approved and already close; B-15 carries the same language through every tab screen. Do B-14 first; do B-15 in the order at the bottom, one screen per step, and check each at 375x667 in Kannada and English.

**What the reference looks like (measured from the frames at 200% zoom):**
- **Background:** very pale lavender (`#F6F5FF`) with large, soft, blurred colour washes (lavender, faint yellow-green, faint pink) drifting behind the content; nothing is flat grey. No coloured header bars anywhere: screens start with a plain row (round back arrow at the left, screen title centred in bold navy, a bell with a small violet dot at the right).
- **Cards:** white, radius about 20, no border, extremely soft shadow, 16-20 padding, 12-14 gap between cards. Two text levels in a card: a small muted grey label on top (12-13), a larger medium/semibold title below (16-17).
- **Icon tile:** a 32-36 rounded-square tile (radius 10) with a pastel fill (pink, violet, peach) and a matching darker glyph, top-right inside a card.
- **Status tags:** tiny pills (radius 8-10, 11-12 text) with a pale fill and a matching darker text: Done = lavender/violet, In progress = peach/coral, To do = pale blue/blue. A small clock icon in violet plus the time sits at the card's lower left.
- **Selectable strip:** a horizontal row of small white cards (radius 16; month small, day number bold, weekday small); the selected one is a solid violet pill with white text.
- **Filter chips:** a horizontal scroll row; selected = solid violet, white text, radius 12; others = pale violet fill (`#E9E6FB`-like) with violet text. Chips are wide and generously padded.
- **Primary card:** one solid violet card (radius 24) with a circular progress ring (translucent white track, brighter arc, percentage in the middle) and a small "..." chip at the top right.
- **Form rows:** white rounded rows, a small muted label above a bold value, a leading violet duotone icon, a trailing chevron; a wide violet pill button ("Add Project") at the bottom with strong shadow.
- **Bottom navigation:** NOT the stock tab bar. A floating rounded bar (lavender `#E8E3FF`-like, radius about 28, inset from the screen edges, soft shadow) with four duotone violet icons (active one darker with a soft glow) and a **round violet "+" button in the middle that rises above the bar** with a glow shadow.
- **Intro screen:** big soft illustration on a pastel wash, a two-line centred title, one muted paragraph, one full-width violet pill button "Let's Start" with a small arrow-in-circle at its right.
- **Type:** the reference font is **Lexend** (rounded geometric). Sizes: screen title 18-20 bold, card title 16-17, label 12-13, tags 11.

**Tokens (add to `src/theme/tokens.ts`; keep the SevaOne brand colours, do not switch to the reference's exact violet):**
- Keep `primary #5553D2` as the violet. Add `primaryTint #E9E6FB` (pale chip/nav fill), `primaryWash #F6F5FF` (already `authWash`), `navTint #E8E3FF`, `navy`/`mutedForeground` unchanged.
- Pastel tag pairs (fill / text): done `#ECE8FF` / `#5553D2`; progress `#FFE9E1` / `#D9532F`; todo `#E2F1FF` / `#2478C8`; success `#DDF7EF` / `#00805F`; warning `#FFF3D6` / `#9A6B00`. Check contrast 4.5:1 for text on the fill.
- Icon-tile pastels: pink `#FFE3F1`, violet `#E9E6FB`, peach `#FFE7D6`, teal `#CAF5F2`, mint `#DDF7EF`.
- `radius`: add `xl: 20`; `shadow.card` = navy, opacity 0.06, radius 16, offset (0,6), elevation 2; `shadow.fab` = primary, opacity 0.35, radius 16, offset (0,8), elevation 8. Gradients: `screenWash` (`#F6F5FF` to `#FFFFFF`).
- **Font:** `@expo-google-fonts/lexend` is now installed (Track A did it; do not run `npm install`). Use Lexend (regular/medium/semibold/bold) for **English** body and headings, replacing DM Sans and Space Grotesk in `src/theme/typography.ts` and `fontAssets`; Kannada keeps Noto Sans Kannada (Lexend has no Kannada). Remove the unused DM Sans / Space Grotesk imports only if nothing else uses them; do not uninstall packages.

**New/updated shared components (`src/components/`, reuse everywhere, tests under `src/__tests__/`):**
1. `ScreenHeader` (back arrow round button, centred title, optional right slot; also a bell variant with dot). Replaces every coloured native header: set `headerShown: false` on the tab and stack screens and render `ScreenHeader` instead; keep Android hardware back working.
2. `WashBackground` (the gradient plus 2-3 blurred colour blobs from `react-native-svg`, static, cheap): used by `Screen` so every screen gets it.
3. `Card` gets the new radius/shadow and an optional `IconTile` (pastel tile + Ionicon). `Tag` (pastel status pill). `Chip` gets the solid/pale states above. `ListRow` becomes the two-level card row (small label, bold title, meta row, tile top-right).
4. `FloatingTabBar`: custom `tabBar` for Expo Router `Tabs`. Layout: **Home, Services, [round Assistant button in the middle], Applications, Profile.** The middle button opens `/assistant` (signed-out users see the existing sign-in gate); it is not a tab. Keep labels for screen readers (`accessibilityLabel`) and show a tiny label under each icon at 10-11 since the users are not power users. Respect the bottom safe area; screens need bottom padding so content never hides behind the bar.
5. `HeroCard` (the solid violet card): used on Home for "Ask the assistant" (title, one line, round white arrow button); optional decorative ring/arcs from `react-native-svg`.

**Screens, in this order (each one: restyle only, behaviour, routes, testIDs and data hooks unchanged):**
1. **Tabs shell** (`(tabs)/_layout.tsx`): custom `FloatingTabBar`, no coloured headers. Keep the login-first redirect and the guest logic exactly as they are.
2. **Home:** `ScreenHeader`-like top row (greeting "Welcome" / name and a bell that opens Helpline for now), search bar card, the violet `HeroCard` (assistant), a horizontal row of quick-action cards in the selectable-strip style (Services, Helpline, Feedback, Language), then "Popular departments" as two-column icon-tile cards.
3. **Services (departments list):** header, search bar, filter chips (All plus the most useful groups only if the data supports it; otherwise omit), department cards in the two-level row style with a service-count tag. Sub-department and services lists use the same row; service detail gets the form-row styling with a wide violet pill "Apply" button.
4. **Profile:** header, a white identity card (initials tile, name, masked mobile, DigiLocker "Verified" tag), settings as form rows (label above value, chevron), sign-out and delete-account as quiet text rows (delete in `destructive`), language as the plain-text `ಕನ್ನಡ | English` selector from B-13.
5. **Assistant:** header with back; chat bubbles: user = solid primary, radius 20 with a small tail; assistant = white card; a floating white input bar with round violet send button and mic; suggestion chips in the pale-violet chip style.
6. **Helpline, Feedback, Applications:** same header/card language (Feedback fields become the white form rows; submit = wide pill button; Applications uses the B-16 GSC tracker and My Applications cards).
7. **Welcome/intro (optional, only if the owner asks):** not part of B-15.

**Rules:** no hard-coded colours or strings (kn + en for anything new); every control 44dp or larger; text contrast 4.5:1 (pastel tag text included); works at 200% font scale without clipping; all existing tests keep passing (update them where text/structure changes); `npm run lint`, `npm run typecheck`, `npm test` green. Do not add libraries (blur is done with SVG gradients, not `expo-blur`); ask Track A if something is missing. Do not run git.
*Done when:* the four tabs, the assistant, the services drill-down and the profile all share this look at 375x667 and on a tall phone in Kannada and English, and the handoff says how to open each one.

## Wave 2 UI tasks (planned 2026-09-27; plan in MOBILE_PLAN.md section 8c)

Track A builds the backend, `src/api`, `src/lib` and types first (contracts in `src/api/types.ts`); Track B builds the screens against them. Every new screen: loading, empty, error and success states; works at 375x667 in Kannada and English; no hard-coded colours or strings (new i18n prefixes `favourites`, `notifications`, `grievance`, `centres`, `documents`, en and kn together); controls 44dp or larger; deep links into the Services stack reuse `lib/servicesStackNav.ts` / Home's `enterServicesThenPush`. Tests mock `expo-router` and `@/lib/auth` inline, as the existing screen tests do.

### B-17  Favourites (M8)
- A heart toggle on the service detail screen (header right icon or beside Apply); a "Favourite services" strip on Home under Recently viewed and a `favourites.tsx` screen listing all of them (tap opens the service through the Services stack); a Profile row that opens it.
- Guests can favourite too (stored on the device); after sign-in the merge is automatic. Show a short "saved on this device, sign in to keep them" hint for guests.
*Done when:* toggling persists across a reload, the list matches Home's strip, and signing in keeps the guest's favourites.

### B-18  Recent services controls (M8)
- Keep Home's strip (newest five). Add a "Clear history" action in settings with a confirmation. Nothing else changes visually.
*Done when:* clearing empties the strip and, for a signed-in citizen, the server copy.

### B-19  Applications v2 (M8)
- Remove the five-item cap; each saved application shows service name, status, "last updated" and a source badge (Sakala now); pull-to-refresh; delete with confirmation; show stale data with a banner if refresh fails (no silent drops).
*Done when:* more than five saved numbers list correctly, delete works, and a failed refresh keeps the stored status visible.

### B-20  Notifications inbox (M10)
- Rewrite `src/app/notifications.tsx` as a list (title, short body, time, unread dot) with mark read, mark all read and delete; the Home bell shows an unread count badge (extend `ScreenHeader` from `showDot`); a settings toggle for push and per-kind muting; ask the OS notification permission in context (when the user turns push on), not at launch. A notification tap uses only the whitelisted `route` values (applications, grievance detail, document detail, notifications).
*Done when:* unread count, read state and the empty/error states work, and `notificationsScreen.test.tsx` is updated for the fetching screen.

### B-21  DigiLocker documents (M11)
- Extend `src/app/digilocker-document.tsx`: format chips (only those the document offers), a structured viewer for JSON/XML fields, a clear "Sample document" banner when `isSample`, and honest states for loading, "not available right now" (`document_unavailable`) and rate limit. No share or export; the PDF viewer stays behind a flag that is off.
*Done when:* the demo citizen opens a sample document end to end, a real account shows the unavailable state without crashing.

### B-22  Grievances (M12)
- Screens `src/app/grievance/new.tsx`, `index.tsx`, `[id].tsx`. Form: category, optional service or saved application, subject, description, incident date, district/taluk prefilled, contact name and mobile shown read-only from the account, a required consent checkbox, up to three attachments (jpg/png/pdf, 5 MB). Submit once (the api layer sends the idempotency key). Detail shows the reference, a status timeline, attachments, and the plain note "Registered with SevaOne. Forwarding to the authority is pending." until forwarded. Withdraw while allowed. Entry from Profile and Helpline; show the account-deletion note.
*Done when:* every validation and 422/429 state exists, a double tap creates one grievance, and the list and detail render the stored data.

### B-23  Nearby service centres (M9)
- `src/app/centres.tsx`: list with distance, filters by type and district/taluk, detail with address, hours, services and a Call button (`tel:` only). "Use my location" asks for foreground location only on tap; if denied, fall back to the profile district/taluk. Show a "Sample data" tag on rows with `isSample`. No map link in v1.
*Done when:* nearby search, filters, denied-permission fallback and the empty state work with the two sample centres.

### B-24  Polish (P6)
- Kannada review of all wave 2 strings, accessibility pass (screen reader labels, 200% font scale), and the en/kn parity test green.
*Done when:* the parity test and the full suite pass and each wave 2 screen has been walked through on the emulator and the phone.

## Wave 2 kickoff for Codex (Track B) and the split with Claude (Track A), 2026-09-27

**P0 is DONE (Track A):** every wave 2 contract, api module, hook and fixture exists, so you can build all screens now against fixture data without waiting for the backend. Run the app with `EXPO_PUBLIC_USE_FIXTURES=1` (Jest always does). Read `MOBILE_PLAN.md` section 8c first.

### Contract map (import these; do not edit `src/api/**` or `src/lib/**`: ask in the handoff)
| Task | Use these (all exist, tested, fixtures included) |
|---|---|
| B-17 Favourites | `useFavouriteServices()` from `@/lib/favouriteServices` (`ids`, `isFavourite(id)`, `toggle(id)`; works for guests, syncs itself when signed in); `useServicesByIds(ids)` from `@/api/libraryHooks` for names |
| B-18 Recents | `useRecentServices()` and `clearRecentServices()` from `@/components/home/recentServices` (clear also clears the account copy) |
| B-19 Applications v2 | `@/api/applicationHooks`: `useSavedApplications`, `useRefreshApplications` (pull to refresh), `useRefreshApplication`, `useRemoveApplication`, `useSaveApplication`; type `SavedApplication` (`id`, `source`, `externalRef`, `lastCheckedAt`, `stale`). Guests still use `applicationsApi.track` |
| B-20 Notifications | `@/api/notificationHooks`: `useNotifications`, `useUnreadNotificationCount` (badge; `data` is a number), `useMarkNotificationRead`, `useMarkAllNotificationsRead`, `useDeleteNotification`, `useNotificationPrefs`, `useSaveNotificationPrefs`, `useSendTestNotification`. `AppNotification` carries both languages, like `CatalogDepartment` (`title`/`titleKn`, `body`/`bodyKn`): pick with `useLanguage` when rendering, the same as Home does for a citizen's name — do not expect the api layer to localise these. `route` is one of `applications`, `grievance`, `document`, `notifications` (+ `routeId`): map only those to screens. The OS permission and push registration come in P3 (`@/lib/push`, Track A): build the toggle UI now, it just saves preferences |
| B-21 Documents | `useDocumentPreview(documentId)`, `useRefreshDocument`, `isDocumentUnavailable(error)`, `isDocumentRateLimited(error)` from `@/api/documentHooks` / `@/api/documents`; `DigiLockerDocument` gained optional `documentId`, `formats`, `isSample`. Fixture id `doc-unavailable` gives the 502 state |
| B-22 Grievances | `@/api/grievanceHooks`: `useGrievances`, `useGrievance(id)`, `useCreateGrievance` (variables `{ input, idempotencyKey }`; make the key once per form with `newIdempotencyKey()` from `@/api/grievances`), `useWithdrawGrievance`, `useAddGrievanceAttachment(id)`, `useRemoveGrievanceAttachment(id)`; limits in `GRIEVANCE_LIMITS`. Attachments must be a `File` (expo-file-system) or Blob. Picking a file needs `expo-document-picker`/`expo-image-picker`, which are not installed: build the picker behind a small function and ask in the handoff (Track A installs in P3's rebuild) |
| B-23 Centres | `@/api/centreHooks`: `useCentres(search, enabled)`, `useCentre(id)`, `useCentreFacets()`. Location needs `expo-location` (P3 rebuild): until then use manual district/taluk and leave a clearly marked `TODO` seam for "use my location". Two sample centres in fixtures |

### Split, so both tracks work at the same time
| | Track A (Claude), sequential | Track B (Codex), parallel from now |
|---|---|---|
| Now | P1 backend: migrations 005, 006, endpoints for favourites, recents, applications v2, with tests | B-17, B-18, B-19 |
| Next | P2 backend: centres (007) | B-23 |
| Then | P3: notifications backend (008), push adapter, one native rebuild (`expo-notifications`, `expo-location`, pickers) and a phone APK | B-20 |
| Then | P4: storage adapter, MinIO, documents (009) | B-21 |
| Then | P5: grievances backend (010), attachments | B-22 |
| Last | P6 support | B-24 |

**Ownership (no shared files, so no edit collisions):** Track A owns `apps/Seva-Sindhu-One/backend/**`, `src/api/**`, `src/lib/**`, `app.json`, `package.json`, the root layout (`src/app/_layout.tsx`), `docker-deploy/**` and the docs' contract sections. Track B owns `src/components/**` (except the sync functions marked "Track A logic" in `components/home/recentServices.ts`), `src/theme/**`, every screen under `src/app/**` except `_layout.tsx`, and `en.json`/`kn.json` (only Track B edits them for wave 2). Track B tests its own screens; Track A tests api, lib and backend. Neither runs `git commit`, `checkout`, `stash`, `reset` or `clean`. The owner commits.

**Sync points:** when a backend phase goes live Track A writes "M8 backend live on :5094" (etc.) in `SESSION_HANDOFF.md`; Track B then runs without `EXPO_PUBLIC_USE_FIXTURES=1` and checks the screens against the real API (no code change: the same hooks switch over). If a screen needs a contract change (a field, a hook), add a note under Track B in the handoff; Track A changes `src/api/types.ts` and the plan first.

**Order for Codex:** B-17 then B-19 then B-18, then B-23, then B-20, B-21, B-22, and B-24 last. Every screen: loading, empty, error, success; 375x667 in Kannada and English; no hard-coded strings or colours; update `notificationsScreen.test.tsx` for B-20.

## Status update for Codex, 2026-09-27: all five Track A backends are live -- no more waiting on Track A

All of P1 through P5 are done and running on `:5094` (`docker-deploy`, `sevasindhu-backend`), each with its own passing test suite (152 backend tests total, only 7 pre-existing unrelated catalogue failures). Nothing in the contract map above changed from what P0 gave you, with two corrections already folded into the table (both from before you likely reached those screens, but stated here so nothing is missed):
- **Centres (B-23) and notifications (B-20) never take a `language`/`lang` argument.** The backend always returns both `name`/`nameKn` (centres) and `title`/`titleKn`/`body`/`bodyKn` (notifications) in the same response, same as `CatalogDepartment`; you pick with `useLanguage()` client-side. If any draft of your screen code passes a language param into `useCentres`/`useNotifications` or the underlying api calls, drop it -- the hooks already don't accept one.
- **Grievances (B-22) match the contract exactly as documented** in the table above: `useCreateGrievance` needs a fresh `newIdempotencyKey()` per form (not per app-load, so a resubmitted form after a validation fix still creates a new grievance, but a double-tap on submit does not); `GRIEVANCE_LIMITS` caps attachments at 3 files / 5 MB each, jpg/png/pdf only; a grievance stays editable (attachments can be added/removed) only while its status is `received` -- once withdrawn or forwarded, attachment endpoints return 409, so disable the add/remove UI once `status !== "received"`.

You can now drop `EXPO_PUBLIC_USE_FIXTURES=1` for any screen you want to check against the real backend instead of fixtures -- same hooks, no code change, just re-run against `:5094`. Demo/stub-account testing (sample DigiLocker documents, the DigiLocker gateway) needs `DEMO_MOBILE_NUMBER`/`DEMO_OTP_CODE` wired into the local deployment, which is not yet done (owner's infra item, flagged in `SESSION_HANDOFF.md`) -- until then, use fixtures for B-21's sample-document states and switch the rest of B-21 (a real citizen's own documents) to live whenever convenient.

**Suggested order from here, given B-17/B-18/B-19 were likely finished already while P2-P5 were being built:** if not already done, B-23 (centres -- two sample rows are seeded and live) and B-20 (notifications -- the inbox, badge and preferences endpoints are all live; push delivery itself is still `PUSH_PROVIDER=none` pending an FCM project, but that only affects OS push, not the in-app inbox you're building) are unblocked now. B-21 (documents) and B-22 (grievances) are also fully unblocked on the backend side; B-21's file-picker dependency (`expo-document-picker`/`expo-image-picker`) still needs the native rebuild Track A does in P3 alongside `expo-notifications`/`expo-location` -- keep that picker behind the small function/seam already called out in the contract table so it drops in without a screen rewrite once the rebuild happens. Finish B-24 (polish, i18n parity, accessibility) last, once every other wave-2 screen above is in.
