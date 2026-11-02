# Flutter app: clean + new structure (brief for Codex)

App: `apps/eserviceplus-master` (NIC ServicePlus citizen app being turned into SevaOne).
Android/iOS id: `com.org.ep.servicepluscitizenapp` — never change it.
Branch: `flutter-modernize` (already has the fixes below + the toolchain upgrade). Work only on this branch.
Toolchain: Flutter 3.47.6 (installed), Gradle 8.14.3, AGP 8.11.1, Kotlin 2.2.20. Keep AGP < 9.

## Owner's hard rules (do not break)

1. **Never remove any ServicePlus API.** Every method in `lib/services/api_service.dart` and `common_service.dart` must survive the refactor (moved/renamed is fine, deleted is not), even ones with no caller today: `getAllStateUrl`, `forgotPassword`, `getServiceJson`, `getAllApplRefNoByUser`, `applTrackStatus`, `getCertificate`, `getIncomleteApplList` (server typo — keep the wire name), `getAllRevalidatePaymentApplication`, `revalidatePayment`, `getServiceList`, `getTaskList`, `searchApplication`, `getDashboardData`, `pullApplication`, `extractBarCodeContent`, `validateUser`, plus the session headers (`DEVICE_TOKEN`, `USER_TOKEN`, `USER_SIGN`). Also keep `Utility.httpPostFormData` and both RSA helpers (`utils/AsymmetricCryptography.dart` and `utils/AsymmetricCryptography ols.dart` — the second loads `assets/publickey`, needed for a future QR feature).
2. **Wire format is fixed.** Same URLs, query params, HTTP methods, bodies (JSON `null`), multipart login with AES-ECB fields, `X-Requested-With: XMLHttpRequest`. Refactoring structure must not change a single request on the wire.
3. **WebView behaviour verified on 2026-10-05 must keep working** (owner allows full code cleanup of `webview_screen.dart`, not behaviour changes):
   - `window.Android` Proxy + `window.PdfExport` injected at document start.
   - `Android.androidFucCallExportToPdfAck(data)` → POST page token to `checkPdfCreate.do` inside the WebView (fallback `mExportToPdfAck`). `Android.androidFucCloseAck()` → pop screen.
   - `onDownloadStartRequest` handling, `isInspectable: kDebugMode`, the stable `_headersFuture`.
4. Tenant is hard-coded to Karnataka: `https://sevaone.karnataka.gov.in/services/` (`utils/serviceplus_instance.dart`, `common_service.dart`). Keep.
5. SevaOne backend client (`lib/services/sevaone_mobile_api.dart`, GPU backend `https://101.53.138.83:8002` with pinned CA `assets/gpu_backend_ca.pem`) stays separate from the ServicePlus client. The mobile app never calls the .NET backend.
6. Don't commit secrets (`serviceplus.jks`, `android/key.properties` are gitignored). Remove the keystore password from `README.md`/`AppInformation.txt` text.

## Work, in order (commit after each step; app must build after each)

### 1. Packages
`flutter pub upgrade --major-versions` where safe. Remove unused deps: `archive`, `bootstrap_icons`, `change_app_package_name`, `cupertino_icons`, `easy_url_launcher`, `flutter_linkify`, `google_fonts`, `http_parser`, `path`, `smart_text_flutter` (verify each with grep first; `basic_utils` is used by the RSA helper — keep). Move `flutter_launcher_icons` to dev_dependencies. Fix breaking API changes (`permission_handler`, `device_info_plus`, `local_auth`, `flutter_secure_storage`, `open_file`).

### 2. One ServicePlus client
Replace the session boilerplate copy-pasted into every `APIService` method (read `user_token`/`device_token`/`signNo`/`instance` from GetStorage, rebuild Instance) with one `ServicePlusSession` + `ServicePlusClient` (`lib/core/serviceplus/`). Build URLs with `Uri.replace(queryParameters:)` only where it produces the identical wire string; otherwise keep the existing string format. Typed returns, errors as exceptions/results (not `print` + `false`). Keep a thin compatibility layer only if it simplifies migration; delete it by the end.

### 3. Folder structure
```
lib/
  main.dart, app.dart (theme, routes)
  core/        serviceplus/ (client, session, models, crypto), sevaone/ (backend client), storage/, theme/, widgets/
  features/
    auth/          login, sign_up, forgot_password, splash
    home/
    services/      catalogue, webview (apply form)
    applications/  list, track status, drafts, re-verify payment, certificate
    profile/       profile, language, helpline, feedback
    assistant/, notifications/, digilocker/
    officials/     home_wfp, inbox
```
Files `lower_snake_case.dart`; classes renamed sensibly (`TrachStatusScreen` → `TrackStatusScreen`). Models: proper Dart naming with `@JsonKey`-style mapping or manual mapping to the server field names (server names unchanged).

### 4. Screens
- Delete empty, unreachable shells with no API: `change_password_screen`, `manage_pin_screen`, `edit_profile_screen`, `update_kyc_screen`, the old drawer in `Widgets.dart`, `welcome_screen` (only if truly unreachable), `services_screen copy.dart`.
- Old `tracking_screen`/`incomplete_applications_screen` are superseded by the Applications tab — delete after confirming nothing unique is lost.
- **Link Re-verify Payment for citizens**: add a "Pending payments" entry in Applications that opens it (uses `getAllRevalidatePaymentApplication` + `revalidatePayment`).
- Biometric screen: keep only if wired; otherwise delete (no API).
- Officials screens: keep, move to `features/officials`, restyle to SevaOne theme.

### 5. Clutter
Remove all commented-out code (~260 lines), all `print()` (67; they log session tokens) → `debugPrint` only where useful, behind `kDebugMode`. Fix every analyzer issue (146 now): deprecated APIs (`withOpacity`, `MaterialState*`, Radio `groupValue`, `Color.value/red`), naming, imports. Target: `flutter analyze` = **No issues found**. Remove the hard-coded sample/test data. Keep `assets/` that are referenced; delete unreferenced images.

### 6. Known bugs to fix while there
- Home "Quick access" labels wrap/clip ("Application / s", "Languag").
- Pull-to-refresh on Applications while statuses are loading can leave them unloaded (`_statusChecking` guard).
- `speechSynthesis is not defined` loop on ServicePlus form pages (page script; harmless — optionally inject a no-op `speechSynthesis` stub).

## Verify (Android emulator `Medium_Phone`, debug build)
`flutter analyze` clean; `flutter build apk --debug` and `--release` succeed. On device (owner's test account already signed in on the emulator — don't enter credentials yourself): Home loads; Services search → open a service → form loads in WebView; Applications shows real statuses (red Rejected, green Delivered); open a Delivered app; Drafts open; Pending payments opens; acknowledgement Export to PDF still saves/opens; logout/login. **Do not submit applications or make payments yourself** — ask the owner.

## Open item (owner testing 2026-10-06)
`getCertificate` is wired but delivered steps so far carry an empty token (`64877923~Output Certificate|1|true|`); with id as token the server returns 400 "Certificate details not found". Don't change this logic beyond the refactor; the owner will test with a freshly approved certificate.

## When done
Append a dated entry to `docs/mobile/SESSION_HANDOFF.md` (what changed, how verified, what's left). Don't merge to `main`; the owner reviews.
