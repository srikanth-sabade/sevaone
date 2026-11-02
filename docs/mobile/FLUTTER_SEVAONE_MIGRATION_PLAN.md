# SevaOne experience in the NIC Flutter app

**Target:** extend `apps/eserviceplus-master` so the app uses the NIC-approved Android application ID `com.org.ep.servicepluscitizenapp`, keeps its working ServicePlus behaviour, and presents the SevaOne experience and features.

**Progress (2026-10-02):** Phase 1 is underway. Shared SevaOne colors, bundled DM Sans/Space Grotesk/Noto Sans Kannada fonts, Material component styling, the initial Home layout and quick actions, and the SevaOne login presentation are in place. Dart formatting completed. The Flutter analyzer could not start because its SDK cache is mounted read-only; these edits have not yet been built or emulator-checked. The citizen navigation shell and additional feature ports remain next.

**Source-of-truth boundary:** all Flutter WebView behaviour is fixed reference behaviour. Do not edit `lib/screens/webview_screen.dart`, its WebView settings/callbacks, request headers, form URL creation, submit/payment navigation, or the code that passes a service into it. UI work around those flows must preserve the same screen destination and arguments. Before any release, compare the WebView-related diff against the NIC baseline and require it to be empty.

## Feature map

| SevaOne experience | NIC Flutter starting point | Work in Flutter |
|---|---|---|
| Splash and brand | `main.dart`, `welcome_screen.dart`, launch assets | Apply SevaOne wordmark, Karnataka leadership artwork, and restrained splash layout. Preserve startup/session routing. |
| ServicePlus login and recovery | `login_screen.dart`, `forgot_password.dart`, `sign_up_screen.dart`, `common_service.dart` | Restyle to SevaOne; preserve NIC validation, token/signNo creation, registration/recovery destinations, and role handling. |
| Home and navigation | `home_screen.dart`, `Widgets.dart` drawer | Build SevaOne Home composition and bottom navigation; keep existing routes reachable. |
| Service catalogue and application form | `services_screen.dart`, `webview_screen.dart` | Restyle catalogue/search/service cards; retain the exact service selection → existing WebView request and submit/payment flow. WebView source is frozen. |
| Applications and tracking | `incomplete_applications_screen.dart`, `tracking_screen.dart`, `track_status_screen.dart`, payment screens | Merge NIC drafts/applied/tracking views into SevaOne Applications presentation without changing NIC request behavior. |
| Profile and account settings | `edit_profile_screen.dart`, `change_password_screen.dart`, `manage_pin_screen.dart`, `update_kyc_screen.dart`, `biometric_screen.dart` | Restyle and organize under SevaOne Profile; preserve each existing NIC action. |
| DigiLocker and document list | NIC project inventory/API plus current SevaOne design and Python endpoints | Add SevaOne document link/list/detail UI where the existing integration supports it; keep external authentication and browser behavior on the established path. |
| Favourites and recent services | SevaOne Python backend `/api/v1/mobile/favourites`, `/recents`; no matching NIC screen identified | Add Flutter screens and data access after the account/session bridge is agreed. |
| Nearby centres | SevaOne Python `/api/v1/mobile/centres`; no matching NIC screen identified | Add the centre list/details and sample/real data states. |
| Notifications | SevaOne Python `/api/v1/mobile/notifications`; no matching NIC screen identified | Add in-app inbox/preferences; native push configuration remains a separate deployment task. |
| Grievances | SevaOne Python `/api/v1/mobile/grievances`; no matching NIC screen identified | Add grievance form/list/detail and attachment states; external forwarding remains controlled by backend configuration. |
| Assistant, feedback, helpline, language | SevaOne mobile screens and Python assistant/feedback APIs; some NIC menu/profile settings exist | Port the SevaOne visual design and connect supported backend features, reusing the NIC locale/account controls where appropriate. |

## Delivery sequence

1. **Baseline and guardrails:** record current Flutter app ID, login result shape, persisted session keys, feature routes, and a known-good end-to-end form submission. Preserve all existing working-tree edits. Snapshot/checksum the WebView-related files and ensure later diffs do not touch them.
2. **SevaOne design foundation:** establish shared SevaOne colors, typography, spacing, buttons, fields, cards, icon treatments, English/Kannada behavior, and the app shell. Reuse approved artwork from `mobile/assets/images` where licensing and resolution permit.
3. **Existing NIC screens:** restyle splash/welcome, login/recovery, Home, service catalogue, Applications/tracking, and Profile. After catalogue changes, re-run the owner's proven complete application submission and payment flow.
4. **SevaOne-only features:** add DigiLocker documents, favourites/recents, centres, notifications, grievances, assistant, feedback, helpline, and language settings in coherent Home/navigation locations.
5. **Session bridge and API integration:** use a server-verified identity/session exchange before calling citizen-specific SevaOne APIs. Never treat client-supplied `USER_TOKEN`, `DEVICE_TOKEN`, or `USER_SIGN` as a verified SevaOne session by themselves. Keep NIC tokens in secure storage and never send them to unrelated providers. Decide this interface with the backend contract before wiring the new screens.
6. **Release validation:** verify the exact package ID and NIC-approved signing/release process; compare the WebView flow with the baseline; check English and Kannada at 375×667; test login, catalogue, form submit, payment, return-to-app/status, and each new feature's loading/error/empty/success states.

## Known constraints and open inputs

- NIC confirmed that ServicePlus requires `com.org.ep.servicepluscitizenapp`; the owner has manually verified a successful Flutter form submission under that identity.
- The local Flutter repository currently has existing edits in `lib/services/common_service.dart` and `lib/utils/Constants.dart`. Preserve and review these before changing adjacent code.
- The Flutter login persists NIC session data in `GetStorage`; SevaOne citizen APIs require a verified backend session. The session-exchange contract and ownership must be settled before personal-library, notifications, grievances, or other authenticated SevaOne APIs are connected.
- `apps/Seva-Sindhu-One/mobile` remains the visual and feature reference during the port. It is not the package to release after migration.
- The Android release must use the NIC-approved signing pipeline/key. A matching application ID alone is insufficient to update an already installed app.
- No WebView implementation changes are in scope, including security, network, header, SSL, loading, or callback changes. Any defect noticed there is recorded separately for NIC; do not patch it in this migration.
