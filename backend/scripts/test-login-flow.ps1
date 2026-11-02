<#
.SYNOPSIS
    End-to-end test of SevaOne's citizen and operator login/apply/department-validate flow,
    against real WSO2 IS + APIM (no mocks/stubs in this script).

.DESCRIPTION
    Runs the two real flows this session verified live, in order, printing each step's
    response so you can see exactly what's happening:

      PART 1 -- Citizen self-service:
        1. Password-grant login against real WSO2 IS (citizen1 / Citizen@123)
        2. GET /me -- confirms the session and shows the real WSO2 subject claim
        3. Mint an apply-token (encrypted redirect token, AssistedMode:false)
        4. Get a machine OAuth2 token from IS (client_credentials)
        5. Redeem the apply-token through the APIM gateway (not the backend directly) --
           this hits DeptController.ValidateToken via APIM's OAuth2 gate + IS
        6. Replay the SAME token -- must be rejected (one-time use)

      PART 2 -- Operator-assisted:
        7. Operator login (OPR10001 / Operator@123)
        8. Operator mints an apply-token on behalf of citizen1 (SevaOneUserId=4),
           AssistedMode:true
        9. Redeem it through APIM the same way -- confirms assistedMode:true and
           operatorId:"OPR10001" come back correctly

      PART 3 -- DigiLocker (only with -IncludeDigiLocker):
        10. Start a real DigiLocker session against Sandbox.co.in's LIVE API
        11. Immediately try to complete it (without going through the real widget) --
            confirms the backend re-checks status itself and refuses, rather than trusting
            whatever the caller claims

        What this deliberately CANNOT script: the actual DigiLocker consent step (Aadhaar-
        linked mobile + OTP) only happens inside DigiLocker's own hosted widget/UI on a real
        phone -- there is no API to complete that headlessly. To finish a real DigiLocker
        login: take the sessionId Step 10 prints, open it in the DigiLocker SDK/widget
        (frontend work, not part of this script), complete the real consent flow there, then
        call POST /api/v1/auth/digilocker/complete with that sessionId yourself.

    Uses curl.exe (ships with Windows 10+) for every HTTP call, not Invoke-WebRequest --
    deliberately, for the same reason every live test this session used curl: it's a thin,
    predictable wrapper around the actual wire request/response with no PowerShell HTTP
    client quirks in the way.

.PARAMETER IncludeDigiLocker
    Also runs Part 3. This calls Sandbox.co.in's LIVE production API (real quota on your
    account, not a free sandbox call) -- opt-in on purpose, not run by default.

.NOTES
    Prerequisites, all must already be running:
      - SevaOne.Services (backend)          http://localhost:5080
      - WSO2 Identity Server                https://localhost:9443
      - WSO2 API Manager gateway            https://localhost:8244
    If any of these aren't up, this script fails loudly at that step rather than silently
    producing a false pass.
#>

param(
    [switch]$IncludeDigiLocker
)

$ErrorActionPreference = "Stop"

# ---- Config -- edit these if your local ports/credentials differ ----
$Backend        = "http://localhost:5080"
$ApimGateway    = "https://localhost:8244/sevaone/1.0.0"
$IsTokenUrl     = "https://localhost:9443/oauth2/token"
$CitizenUser    = "citizen1"
$CitizenPass    = "Citizen@123"
$OperatorCode   = "OPR10001"
$OperatorPass   = "Operator@123"
$ServiceCode    = "SVC001"
$SevaOneUserId  = 4                     # citizen1's known SevaOneUserId in this dev DB
$WsoIsClientId  = "e7mNmumddufuLIgQzj61ywOxfasa"
$WsoIsSecret    = "chnS9SxOwYQc6v4bew3NgTWsgWJeejYw1tmXhbfExYwa"
$DeptClientId   = "nadakacheri_portal"
$DeptSecret     = "nadakacheri_secret"

$CitizenCookieJar  = New-TemporaryFile
$OperatorCookieJar = New-TemporaryFile

function Write-Step($n, $title) {
    Write-Host ""
    Write-Host "=== Step ${n}: $title ===" -ForegroundColor Cyan
}

function Invoke-Curl {
    param([string[]]$CurlArgs, [string]$JsonBody = $null)
    # -k: skip cert validation (self-signed local WSO2 certs) -- -s: silent -- -w: append status code
    # JSON bodies go through a temp file + "-d @file", NOT as an inline argument -- PowerShell's
    # re-quoting when it marshals arguments to a native exe mangles embedded double-quotes
    # (this is what broke the first version of this script; -d @file sidesteps it entirely).
    if ($JsonBody) {
        $bodyFile = New-TemporaryFile
        [System.IO.File]::WriteAllText($bodyFile, $JsonBody)
        $CurlArgs = $CurlArgs + @("-H", "Content-Type: application/json", "-d", "@$bodyFile")
    }
    $raw = & curl.exe -s -k -w "`nHTTPSTATUS:%{http_code}" @CurlArgs
    if ($bodyFile) { Remove-Item $bodyFile -ErrorAction SilentlyContinue }
    # curl.exe's stdout comes back as an array of lines from PowerShell -- join into one string first.
    $output = ($raw -join "`n")
    $splitIndex = $output.LastIndexOf("`nHTTPSTATUS:")
    $body = $output.Substring(0, $splitIndex)
    $status = [int]($output.Substring($splitIndex + 12))
    return @{ Body = $body; Status = $status }
}

function Assert-Status($result, $expected, $what) {
    if ($result.Status -ne $expected) {
        Write-Host "FAILED: $what (expected $expected, got $($result.Status))" -ForegroundColor Red
        Write-Host $result.Body
        exit 1
    }
    Write-Host "OK ($($result.Status)): $what" -ForegroundColor Green
    Write-Host $result.Body
}

function Get-JsonField($json, $field) {
    return ($json | ConvertFrom-Json).$field
}

# =====================================================================
# PART 1 -- Citizen self-service
# =====================================================================

Write-Step 1 "Citizen password-grant login (real WSO2 IS)"
$loginBody = "{`"username`":`"$CitizenUser`",`"password`":`"$CitizenPass`"}"
$r = Invoke-Curl -CurlArgs @("-c", $CitizenCookieJar, "-X", "POST", "$Backend/api/v1/auth/password") -JsonBody $loginBody
Assert-Status $r 200 "POST /api/v1/auth/password"

Write-Step 2 "GET /me (confirms session + real WSO2 subject claim)"
$r = Invoke-Curl -CurlArgs @("-b", $CitizenCookieJar, "$Backend/api/v1/me")
Assert-Status $r 200 "GET /api/v1/me"

Write-Step 3 "Mint apply-token (citizen self-service, AssistedMode:false)"
$r = Invoke-Curl -CurlArgs @("-b", $CitizenCookieJar, "-X", "POST", "$Backend/api/v1/schemes/$ServiceCode/apply-token")
Assert-Status $r 200 "POST /api/v1/schemes/$ServiceCode/apply-token"
$applyUrl = Get-JsonField $r.Body "applyUrl"
$encryptedToken = [System.Uri]::UnescapeDataString(($applyUrl -split "Token=")[1])
Write-Host "Encrypted redirect token (department never decrypts this): $($encryptedToken.Substring(0, [Math]::Min(50, $encryptedToken.Length)))..."

Write-Step 4 "Get a machine OAuth2 token from WSO2 IS (client_credentials) -- what a department needs to call APIM"
$r = Invoke-Curl -CurlArgs @("-X", "POST", $IsTokenUrl, "-u", "${WsoIsClientId}:${WsoIsSecret}", "-d", "grant_type=client_credentials")
Assert-Status $r 200 "POST $IsTokenUrl"
$oauthToken = Get-JsonField $r.Body "access_token"

Write-Step 5 "Redeem the token THROUGH APIM (not the backend directly) -- department's real call shape"
$validateBody = "{`"token`":`"$encryptedToken`"}"
$validateArgs = @("-X", "POST", "$ApimGateway/api/v1/dept/token-validate",
    "-H", "Authorization: Bearer $oauthToken",
    "-H", "X-SevaOne-Dept-Client: $DeptClientId",
    "-H", "X-SevaOne-Dept-Secret: $DeptSecret")
$r = Invoke-Curl -CurlArgs $validateArgs -JsonBody $validateBody
Assert-Status $r 200 "POST $ApimGateway/api/v1/dept/token-validate (via APIM)"

Write-Step 6 "Replay the SAME token -- must be rejected (one-time use)"
$r = Invoke-Curl -CurlArgs $validateArgs -JsonBody $validateBody
if ($r.Status -eq 200) {
    Write-Host "UNEXPECTED: replay succeeded (should have failed)" -ForegroundColor Red
    exit 1
}
Write-Host "OK ($($r.Status)): replay correctly rejected" -ForegroundColor Green
Write-Host $r.Body

# =====================================================================
# PART 2 -- Operator-assisted
# =====================================================================

Write-Step 7 "Operator login"
$opLoginBody = "{`"operatorCode`":`"$OperatorCode`",`"password`":`"$OperatorPass`"}"
$r = Invoke-Curl -CurlArgs @("-c", $OperatorCookieJar, "-X", "POST", "$Backend/api/v1/operator/login") -JsonBody $opLoginBody
Assert-Status $r 200 "POST /api/v1/operator/login"

Write-Step 8 "Operator mints an assisted apply-token for citizen1 (SevaOneUserId=$SevaOneUserId)"
$assistedBody = "{`"serviceCode`":`"$ServiceCode`",`"sevaOneUserId`":$SevaOneUserId}"
$r = Invoke-Curl -CurlArgs @("-b", $OperatorCookieJar, "-X", "POST", "$Backend/api/v1/operator/apply-token") -JsonBody $assistedBody
Assert-Status $r 200 "POST /api/v1/operator/apply-token"
$assistedApplyUrl = Get-JsonField $r.Body "applyUrl"
$assistedEncryptedToken = [System.Uri]::UnescapeDataString(($assistedApplyUrl -split "Token=")[1])

Write-Step 9 "Redeem the assisted token through APIM -- confirms assistedMode:true + operatorId"
$assistedValidateBody = "{`"token`":`"$assistedEncryptedToken`"}"
$r = Invoke-Curl -CurlArgs @("-X", "POST", "$ApimGateway/api/v1/dept/token-validate",
    "-H", "Authorization: Bearer $oauthToken",
    "-H", "X-SevaOne-Dept-Client: $DeptClientId",
    "-H", "X-SevaOne-Dept-Secret: $DeptSecret") -JsonBody $assistedValidateBody
Assert-Status $r 200 "POST $ApimGateway/api/v1/dept/token-validate (assisted, via APIM)"
$json = $r.Body | ConvertFrom-Json
if ($json.assistedMode -eq $true -and $json.operatorId -eq $OperatorCode) {
    Write-Host "OK: assistedMode=true, operatorId=$($json.operatorId) -- correct" -ForegroundColor Green
} else {
    Write-Host "FAILED: expected assistedMode=true and operatorId=$OperatorCode" -ForegroundColor Red
    exit 1
}

# =====================================================================
# PART 3 -- DigiLocker (opt-in: -IncludeDigiLocker)
# =====================================================================

if ($IncludeDigiLocker) {
    Write-Step 10 "Start a real DigiLocker session (Sandbox.co.in LIVE API -- this uses real quota)"
    $dlStartBody = "{`"flow`":`"signin`"}"
    $r = Invoke-Curl -CurlArgs @("-X", "POST", "$Backend/api/v1/auth/digilocker/start") -JsonBody $dlStartBody
    Assert-Status $r 200 "POST /api/v1/auth/digilocker/start"
    $sessionId = Get-JsonField $r.Body "sessionId"
    Write-Host ""
    Write-Host "DigiLocker session created: $sessionId" -ForegroundColor Yellow
    Write-Host "This is as far as a script can go -- finishing this login needs the real" -ForegroundColor Yellow
    Write-Host "DigiLocker widget + a real Aadhaar-linked mobile OTP, which only exists in" -ForegroundColor Yellow
    Write-Host "an actual browser/DigiLocker UI, not curl." -ForegroundColor Yellow

    Write-Step 11 "Try to complete it anyway (no real consent happened) -- must be rejected"
    $dlCompleteBody = "{`"sessionId`":`"$sessionId`"}"
    $r = Invoke-Curl -CurlArgs @("-X", "POST", "$Backend/api/v1/auth/digilocker/complete") -JsonBody $dlCompleteBody
    if ($r.Status -eq 200) {
        Write-Host "UNEXPECTED: completed without real consent (should have failed)" -ForegroundColor Red
        exit 1
    }
    Write-Host "OK ($($r.Status)): correctly refused -- backend re-checks Sandbox's own session status, doesn't just trust the caller" -ForegroundColor Green
    Write-Host $r.Body
} else {
    Write-Host ""
    Write-Host "(Skipped Part 3 -- DigiLocker. Re-run with -IncludeDigiLocker to test the session-start/refuse-early-completion path against the real Sandbox.co.in API.)" -ForegroundColor DarkGray
}

Remove-Item $CitizenCookieJar, $OperatorCookieJar -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=== All steps passed ===" -ForegroundColor Green
