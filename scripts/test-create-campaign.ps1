param([string]$BaseUrl = 'http://127.0.0.1:5107')
$ErrorActionPreference = 'Stop'
$email = 'e2e-campaign-' + [Guid]::NewGuid().ToString('N') + '@example.test'
$password = 'Campaign-Test-123!'

function Sql([string]$query) {
    $result = docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -t -A -U voucherhub -d voucherhub -c $query
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL command failed.' }
    return $result
}

function Status([string]$method, [string]$path, $headers, $body) {
    try {
        $args = @{ Method=$method; Uri="$BaseUrl$path"; UseBasicParsing=$true; Headers=$headers }
        if ($null -ne $body) { $args.ContentType='application/json'; $args.Body=($body | ConvertTo-Json) }
        return [int](Invoke-WebRequest @args).StatusCode
    } catch {
        if ($null -eq $_.Exception.Response) { throw }
        return [int]$_.Exception.Response.StatusCode
    }
}

try {
    $body = @{name='E2E Campaign';description='Integration test';startsAt='2030-10-08T18:00:00+07:00';endsAt='2030-10-08T19:00:00+07:00';maxRedemptionsPerUser=3}
    if ((Status Post '/api/admin/campaigns' @{} $body) -ne 401) { throw 'Missing JWT must return 401.' }

    Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/register" -ContentType 'application/json' -Body (@{email=$email;password=$password;displayName='Campaign Test'} | ConvertTo-Json) | Out-Null
    $mails = Invoke-RestMethod 'http://127.0.0.1:8025/api/v1/messages'
    $mail = $mails.messages | Where-Object { @($_.To | ForEach-Object Address) -contains $email } | Select-Object -First 1
    if ($null -eq $mail) { throw 'Test email not found.' }
    $message = Invoke-RestMethod "http://127.0.0.1:8025/api/v1/message/$($mail.ID)"
    $match = [regex]::Match($message.Text, 'token=([A-Za-z0-9_-]+)')
    if (-not $match.Success) { throw 'Verification token not found.' }
    Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/verify-email" -ContentType 'application/json' -Body (@{token=$match.Groups[1].Value} | ConvertTo-Json) | Out-Null

    $loginBody = @{email=$email;password=$password} | ConvertTo-Json
    $userLogin = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" -ContentType 'application/json' -Body $loginBody
    if ((Status Post '/api/admin/campaigns' @{Authorization="Bearer $($userLogin.accessToken)"} $body) -ne 403) { throw 'USER must return 403.' }

    Get-Content -Raw "$PSScriptRoot/bootstrap-admin.sql" | docker compose exec -T postgres psql -U voucherhub -d voucherhub -v "admin_email=$email" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Bootstrap failed.' }
    $adminLogin = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" -ContentType 'application/json' -Body $loginBody
    $headers = @{Authorization="Bearer $($adminLogin.accessToken)"}

    $invalid = $body.Clone(); $invalid.endsAt=$invalid.startsAt
    if ((Status Post '/api/admin/campaigns' $headers $invalid) -ne 400) { throw 'Invalid time must return 400.' }
    $invalid = $body.Clone(); $invalid.maxRedemptionsPerUser=0
    if ((Status Post '/api/admin/campaigns' $headers $invalid) -ne 400) { throw 'Zero quota must return 400.' }
    $invalid = $body.Clone(); $invalid.name='   '
    if ((Status Post '/api/admin/campaigns' $headers $invalid) -ne 400) { throw 'Blank name must return 400.' }

    $response = Invoke-WebRequest -Method Post -Uri "$BaseUrl/api/admin/campaigns" -Headers $headers -ContentType 'application/json' -Body ($body | ConvertTo-Json) -UseBasicParsing
    if ([int]$response.StatusCode -ne 201) { throw 'Creation must return 201.' }
    $campaign = $response.Content | ConvertFrom-Json
    if ($campaign.status -ne 'DRAFT' -or $campaign.maxRedemptionsPerUser -ne 3 -or [DateTimeOffset]::Parse($campaign.startsAt).UtcDateTime.Hour -ne 11) { throw 'Draft/quota/UTC conversion mismatch.' }
    $get = Invoke-RestMethod -Uri "$BaseUrl/api/admin/campaigns/$($campaign.id)" -Headers $headers
    if ($get.id -ne $campaign.id) { throw 'Read-back mismatch.' }
    $count = Sql "SELECT count(*) FROM audit_logs WHERE entity_id = '$($campaign.id)' AND action = 'CAMPAIGN_CREATED';"
    if ([int]$count -ne 1) { throw 'Expected exactly one creation audit.' }

    if ((Status Post "/api/admin/campaigns/$($campaign.id)/publish" $headers $null) -ne 400) { throw 'Publishing without offers must fail.' }
    $expired = $body.Clone(); $expired.startsAt='2000-01-01T00:00:00Z'; $expired.endsAt='2000-01-02T00:00:00Z'
    if ((Status Put "/api/admin/campaigns/$($campaign.id)" $headers $expired) -ne 200) { throw 'Valid expired Draft should save.' }
    if ((Status Post "/api/admin/campaigns/$($campaign.id)/publish" $headers $null) -ne 400) { throw 'Ended campaign cannot publish.' }
    $body.name='Updated Draft'
    if ((Status Put "/api/admin/campaigns/$($campaign.id)" $headers $body) -ne 200) { throw 'Draft update failed.' }

    $offerBody = @{name='Offer A';description='Test offer';pointsCost=100;totalQuantity=10;expiresAt='2030-10-09T19:00:00+07:00'}
    $invalid = $offerBody.Clone(); $invalid.pointsCost=0
    if ((Status Post "/api/admin/campaigns/$($campaign.id)/offers" $headers $invalid) -ne 400) { throw 'Zero cost must fail.' }
    $invalid = $offerBody.Clone(); $invalid.expiresAt=$body.endsAt
    if ((Status Post "/api/admin/campaigns/$($campaign.id)/offers" $headers $invalid) -ne 400) { throw 'Expiry at campaign end must fail.' }
    $offerResponse = Invoke-WebRequest -Method Post -Uri "$BaseUrl/api/admin/campaigns/$($campaign.id)/offers" -Headers $headers -ContentType 'application/json' -Body ($offerBody | ConvertTo-Json) -UseBasicParsing
    if ([int]$offerResponse.StatusCode -ne 201) { throw 'Create offer must return 201.' }
    $offer = $offerResponse.Content | ConvertFrom-Json
    $offerBody.totalQuantity=20
    if ((Status Put "/api/admin/campaigns/$($campaign.id)/offers/$($offer.id)" $headers $offerBody) -ne 200) { throw 'Draft offer update failed.' }
    if ((Status Put "/api/admin/campaigns/$($campaign.id)/offers/$([Guid]::NewGuid())" $headers $offerBody) -ne 404) { throw 'Unknown offer must return 404.' }
    $invalid = $body.Clone(); $invalid.endsAt=$offerBody.expiresAt
    if ((Status Put "/api/admin/campaigns/$($campaign.id)" $headers $invalid) -ne 400) { throw 'Campaign update cannot invalidate offers.' }
    $offers = @(Invoke-RestMethod -Uri "$BaseUrl/api/admin/campaigns/$($campaign.id)/offers" -Headers $headers)
    if ($offers.Count -ne 1 -or $offers[0].totalQuantity -ne 20) { throw 'Offer read-back mismatch.' }
    $body.startsAt=[DateTimeOffset]::UtcNow.AddMinutes(-1).ToString('o')
    if ((Status Put "/api/admin/campaigns/$($campaign.id)" $headers $body) -ne 200) { throw 'Ongoing Draft update failed.' }

    # Start both requests before waiting: only one Draft -> Published transition may succeed.
    Add-Type -AssemblyName System.Net.Http
    $client = [System.Net.Http.HttpClient]::new()
    try {
        $client.DefaultRequestHeaders.Add('Authorization', $headers.Authorization)
        $first = $client.PostAsync("$BaseUrl/api/admin/campaigns/$($campaign.id)/publish", $null)
        $second = $client.PostAsync("$BaseUrl/api/admin/campaigns/$($campaign.id)/publish", $null)
        $firstResult = $first.GetAwaiter().GetResult()
        $secondResult = $second.GetAwaiter().GetResult()
        $codes = @([int]$firstResult.StatusCode, [int]$secondResult.StatusCode) | Sort-Object
        if ($codes[0] -ne 200 -or $codes[1] -ne 409) { throw "Concurrent publish expected 200 and 409, got $codes." }
        $firstResult.Dispose(); $secondResult.Dispose()
    } finally { $client.Dispose() }
    $count = Sql "SELECT count(*) FROM audit_logs WHERE entity_id = '$($campaign.id)' AND action = 'CAMPAIGN_PUBLISHED';"
    if ([int]$count -ne 1) { throw 'Concurrent publish must record exactly one audit.' }
    if ((Status Put "/api/admin/campaigns/$($campaign.id)" $headers $body) -ne 409) { throw 'Published campaign must be immutable.' }
    if ((Status Put "/api/admin/campaigns/$($campaign.id)/offers/$($offer.id)" $headers $offerBody) -ne 409) { throw 'Published offer must be immutable.' }
    if ((Status Post "/api/admin/campaigns/$($campaign.id)/offers" $headers $offerBody) -ne 409) { throw 'Cannot add offer after publish.' }
    if ((Status Post "/api/admin/campaigns/$($campaign.id)/cancel" $headers $null) -ne 200) { throw 'Cancel failed.' }
    if ((Status Post "/api/admin/campaigns/$($campaign.id)/cancel" $headers $null) -ne 200) { throw 'Repeat cancel failed.' }
    $count = Sql "SELECT count(*) FROM audit_logs WHERE entity_id = '$($campaign.id)' AND action = 'CAMPAIGN_CANCELLED';"
    if ([int]$count -ne 1) { throw 'Repeated cancellation must record one audit.' }
    if ((Status Post "/api/admin/campaigns/$($campaign.id)/publish" $headers $null) -ne 409) { throw 'Cancelled campaign cannot publish.' }

    Sql "UPDATE users SET status='BLOCKED' WHERE email='$email';" | Out-Null
    if ((Status Post '/api/admin/campaigns' $headers $body) -ne 403) { throw 'Blocked admin with old token must return 403.' }
    Sql "UPDATE users SET status='ACTIVE', role='USER' WHERE email='$email';" | Out-Null
    if ((Status Post '/api/admin/campaigns' $headers $body) -ne 403) { throw 'Demoted admin with old token must return 403.' }
    $count = Sql "SELECT count(*) FROM campaigns WHERE created_by IN (SELECT id FROM users WHERE email='$email');"
    if ([int]$count -ne 1) { throw 'Rejected requests must not create campaigns.' }
    Write-Output 'PASS: auth, validation, Draft edit, offer create/edit/read, expiry consistency, concurrent publish, published immutability, cancel/idempotency, audit, blocked/demoted token.'
} finally {
    Sql "BEGIN; DELETE FROM audit_logs WHERE actor_user_id IN (SELECT id FROM users WHERE email='$email'); DELETE FROM voucher_offers WHERE campaign_id IN (SELECT id FROM campaigns WHERE created_by IN (SELECT id FROM users WHERE email='$email')); DELETE FROM campaigns WHERE created_by IN (SELECT id FROM users WHERE email='$email'); DELETE FROM point_wallets WHERE user_id IN (SELECT id FROM users WHERE email='$email'); DELETE FROM users WHERE email='$email'; DELETE FROM pending_registrations WHERE email='$email'; COMMIT;" | Out-Null
    Write-Output "Removed test database records for $email."
}
