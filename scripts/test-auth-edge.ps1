param(
    [Parameter(Mandatory=$true)][string]$BaseUrl,
    [Parameter(Mandatory=$true)][string]$Database
)
$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^voucherhub_test_[0-9a-f]{12}$') {
    throw 'Auth edge test requires the isolated test database'
}
$email = 'auth-edge-' + [Guid]::NewGuid().ToString('N') + '@example.test'
$password = 'Auth-Edge-123!'

function Request([string]$method, [string]$path, $body) {
    $args = @{ Method=$method; Uri="$BaseUrl$path"; UseBasicParsing=$true; ContentType='application/json'; Body=($body | ConvertTo-Json) }
    try { return [int](Invoke-WebRequest @args).StatusCode }
    catch {
        if ($null -eq $_.Exception.Response) { throw }
        return [int]$_.Exception.Response.StatusCode
    }
}
function Assert([int]$actual, [int]$expected, [string]$case) {
    if ($actual -ne $expected) { throw "$case expected $expected, got $actual" }
}
function Sql([string]$query) {
    $result = docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -t -A -U voucherhub -d $Database -c $query
    if ($LASTEXITCODE -ne 0) { throw "SQL failed: $query" }
    return $result
}
function LatestToken([string]$address) {
    $messages = Invoke-RestMethod 'http://127.0.0.1:8025/api/v1/messages'
    $mail = $messages.messages | Where-Object { @($_.To | ForEach-Object Address) -contains $address } |
        Sort-Object Created -Descending | Select-Object -First 1
    if ($null -eq $mail) { throw 'Auth edge mail missing' }
    $message = Invoke-RestMethod "http://127.0.0.1:8025/api/v1/message/$($mail.ID)"
    $match = [regex]::Match($message.Text, 'token=([A-Za-z0-9_-]+)')
    if (-not $match.Success) { throw 'Auth edge token missing' }
    return $match.Groups[1].Value
}

Assert (Request Post '/api/auth/register' @{email=$email.ToUpperInvariant();password=$password;displayName=' Auth Edge '}) 202 'register mixed case'
$token = LatestToken $email
Assert (Request Post '/api/auth/login' @{email=$email;password=$password}) 401 'login before verify'
Assert (Request Post '/api/auth/verify-email' @{token='invalid-token'}) 400 'invalid token'
Sql "UPDATE pending_registrations SET token_expires_at = NOW() - INTERVAL '1 minute' WHERE email = '$email'" | Out-Null
Assert (Request Post '/api/auth/verify-email' @{token=$token}) 400 'expired token'
Sql "UPDATE pending_registrations SET last_sent_at = NOW() - INTERVAL '2 minutes' WHERE email = '$email'" | Out-Null
Assert (Request Post '/api/auth/resend-verification' @{email=$email}) 202 'resend'
$newToken = LatestToken $email
if ($newToken -eq $token) { throw 'Resend did not rotate token' }
Assert (Request Post '/api/auth/verify-email' @{token=$token}) 400 'old token after resend'
$jobs = @(1,2) | ForEach-Object {
    Start-Job -ScriptBlock {
        param($url, $key)
        try {
            return [int](Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$url/api/auth/verify-email" -ContentType 'application/json' -Body (@{token=$key} | ConvertTo-Json)).StatusCode
        } catch {
            if ($null -eq $_.Exception.Response) { throw }
            return [int]$_.Exception.Response.StatusCode
        }
    } -ArgumentList $BaseUrl,$newToken
}
$statuses = @($jobs | Wait-Job | Receive-Job)
$jobs | Remove-Job
if (@($statuses | Where-Object { $_ -eq 200 }).Count -ne 1 -or
    @($statuses | Where-Object { $_ -eq 400 }).Count -ne 1) {
    throw "Concurrent verify expected 200/400, got $statuses"
}
Assert (Request Post '/api/auth/login' @{email=$email.ToUpperInvariant();password=$password}) 200 'login normalized'
Assert (Request Post '/api/auth/login' @{email=$email;password='wrong-password'}) 401 'wrong password'
$users = [int](Sql "SELECT COUNT(*) FROM users WHERE email = '$email'")
$wallets = [int](Sql "SELECT COUNT(*) FROM point_wallets WHERE user_id = (SELECT id FROM users WHERE email = '$email')")
if ($users -ne 1 -or $wallets -ne 1) { throw 'Concurrent verify created duplicate user/wallet' }
Write-Output 'PASS: normalized email, pre-verify login, expiry, resend rotation, concurrent verify and one wallet'
