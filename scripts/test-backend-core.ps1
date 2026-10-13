param(
    [string]$BaseUrl = 'http://127.0.0.1:5108',
    [string]$Database = 'voucherhub'
)
$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^[a-z][a-z0-9_]*$') { throw 'Invalid test database name' }
$suffix = [Guid]::NewGuid().ToString('N')
$password = 'Backend-Test-123!'
$emails = @("core-admin-$suffix@example.test", "core-user-$suffix@example.test", "core-racer-$suffix@example.test")
$ids = @()
$campaignIds = @()
$campaignId = $null
$offerId = $null

function Request([string]$method, [string]$path, $body, $headers = @{}) {
    $args = @{ Method=$method; Uri="$BaseUrl$path"; Headers=$headers; UseBasicParsing=$true }
    if ($null -ne $body) { $args.ContentType='application/json'; $args.Body=($body | ConvertTo-Json -Depth 8) }
    try {
        $reply = Invoke-WebRequest @args
        return @{ status=[int]$reply.StatusCode; body=($reply.Content | ConvertFrom-Json) }
    } catch {
        if ($null -eq $_.Exception.Response) { throw }
        $reply = $_.Exception.Response
        $reader = New-Object System.IO.StreamReader($reply.GetResponseStream())
        $content = $reader.ReadToEnd()
        return @{ status=[int]$reply.StatusCode; body= $(if ($content) { $content | ConvertFrom-Json } else { $null }) }
    }
}

function AssertStatus($reply, [int]$expected, [string]$case) {
    if ($reply.status -ne $expected) { throw "$case expected $expected, got $($reply.status): $($reply.body | ConvertTo-Json -Depth 8)" }
}

function Sql([string]$query) {
    $result = docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -t -A -U voucherhub -d $Database -c $query
    if ($LASTEXITCODE -ne 0) { throw "SQL failed: $query" }
    return $result
}

function RegisterAndLogin([string]$email) {
    AssertStatus (Request Post '/api/auth/register' @{email=$email;password=$password;displayName='Core Test'}) 202 'register'
    $messages = Invoke-RestMethod 'http://127.0.0.1:8025/api/v1/messages'
    $mail = $messages.messages | Where-Object { @($_.To | ForEach-Object Address) -contains $email } | Select-Object -First 1
    if ($null -eq $mail) { throw "Mail missing for $email" }
    $message = Invoke-RestMethod "http://127.0.0.1:8025/api/v1/message/$($mail.ID)"
    $match = [regex]::Match($message.Text, 'token=([A-Za-z0-9_-]+)')
    if (-not $match.Success) { throw 'Verification token missing' }
    AssertStatus (Request Post '/api/auth/verify-email' @{token=$match.Groups[1].Value}) 200 'verify'
    $login = Request Post '/api/auth/login' @{email=$email;password=$password}
    AssertStatus $login 200 'login'
    $auth = @{Authorization="Bearer $($login.body.accessToken)"}
    $profile = Request Get '/api/users/me' $null $auth
    AssertStatus $profile 200 'profile'
    $script:ids += [string]$profile.body.id
    return $auth
}

try {
    AssertStatus (Request Get '/api/users/me' $null) 401 'missing JWT'
    $admin = RegisterAndLogin $emails[0]
    $user = RegisterAndLogin $emails[1]
    $racer = RegisterAndLogin $emails[2]
    AssertStatus (Request Post "/api/admin/users/$($ids[1])/points/grants" @{amount=400;reason='Test grant'} $user) 403 'user cannot grant'
    Get-Content -Raw "$PSScriptRoot/bootstrap-admin.sql" | docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U voucherhub -d $Database -v "admin_email=$($emails[0])" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Admin bootstrap failed' }
    $login = Request Post '/api/auth/login' @{email=$emails[0];password=$password}
    AssertStatus $login 200 'admin login'
    $admin = @{Authorization="Bearer $($login.body.accessToken)"}

    $grant = Request Post "/api/admin/users/$($ids[1])/points/grants" @{amount=400;reason='Test grant'} $admin
    AssertStatus $grant 201 'grant'
    if ($grant.body.balanceAfter -ne 400) { throw 'Wrong grant balance' }
    $badGrant = Request Post "/api/admin/users/$($ids[1])/points/grants" @{amount=0;reason='Invalid'} $admin
    AssertStatus $badGrant 400 'invalid grant'
    AssertStatus (Request Post "/api/admin/users/$($ids[2])/points/grants" @{amount=100;reason='Race grant'} $admin) 201 'racer grant'

    $start = [DateTimeOffset]::UtcNow.AddMinutes(-5).ToString('o')
    $end = [DateTimeOffset]::UtcNow.AddHours(1).ToString('o')
    $expiry = [DateTimeOffset]::UtcNow.AddDays(1).ToString('o')
    $created = Request Post '/api/admin/campaigns' @{name='Core Test';startsAt=$start;endsAt=$end;maxRedemptionsPerUser=2} $admin
    AssertStatus $created 201 'campaign create'
    $campaignId = [string]$created.body.id
    $campaignIds += $campaignId
    $createdOffer = Request Post "/api/admin/campaigns/$campaignId/offers" @{name='Core Offer';pointsCost=100;totalQuantity=2;expiresAt=$expiry} $admin
    AssertStatus $createdOffer 201 'offer create'
    $offerId = [string]$createdOffer.body.id
    AssertStatus (Request Get "/api/campaigns/$campaignId" $null) 404 'draft hidden'
    AssertStatus (Request Post "/api/admin/campaigns/$campaignId/publish" $null $admin) 200 'publish'
    AssertStatus (Request Get "/api/campaigns/$campaignId" $null) 200 'public detail'

    $key1 = [Guid]::NewGuid().ToString()
    $redeemHeaders = @{Authorization=$user.Authorization;'Idempotency-Key'=$key1}
    $first = Request Post "/api/voucher-offers/$offerId/redeem" $null $redeemHeaders
    AssertStatus $first 201 'first redeem'
    AssertStatus (Request Get "/api/users/me/vouchers/$($first.body.id)" $null $racer) 404 'voucher ownership'
    AssertStatus (Request Post "/api/voucher-offers/$offerId/redeem" $null $user) 400 'missing idempotency key'
    $replay = Request Post "/api/voucher-offers/$offerId/redeem" $null $redeemHeaders
    AssertStatus $replay 200 'replay'
    if ($replay.body.id -ne $first.body.id) { throw 'Replay returned a different voucher' }
    $key2 = [Guid]::NewGuid().ToString()
    $second = Request Post "/api/voucher-offers/$offerId/redeem" $null @{Authorization=$user.Authorization;'Idempotency-Key'=$key2}
    AssertStatus $second 201 'second redeem same offer'
    $key3 = [Guid]::NewGuid().ToString()
    $third = Request Post "/api/voucher-offers/$offerId/redeem" $null @{Authorization=$user.Authorization;'Idempotency-Key'=$key3}
    AssertStatus $third 409 'quota or stock'

    $profile = Request Get '/api/users/me' $null $user
    AssertStatus $profile 200 'profile after redeem'
    if ($profile.body.balance -ne 200) { throw 'Wrong final balance' }
    $history = Request Get '/api/users/me/point-transactions' $null $user
    AssertStatus $history 200 'point history'
    if ($history.body.totalCount -ne 3) { throw 'Expected one grant and two debits' }
    $vouchers = Request Get '/api/users/me/vouchers' $null $user
    AssertStatus $vouchers 200 'my vouchers'
    if ($vouchers.body.totalCount -ne 2) { throw 'Expected two vouchers' }
    $offers = Request Get "/api/admin/campaigns/$campaignId/offers" $null $admin
    AssertStatus $offers 200 'offer inventory'
    if ($offers.body[0].issuedQuantity -ne 2) { throw 'Wrong issued quantity' }

    $raceCampaign = Request Post '/api/admin/campaigns' @{name='Stock Race';startsAt=$start;endsAt=$end;maxRedemptionsPerUser=1} $admin
    AssertStatus $raceCampaign 201 'race campaign'
    $raceCampaignId = [string]$raceCampaign.body.id
    $campaignIds += $raceCampaignId
    $raceOffer = Request Post "/api/admin/campaigns/$raceCampaignId/offers" @{name='One left';pointsCost=100;totalQuantity=1;expiresAt=$expiry} $admin
    AssertStatus $raceOffer 201 'race offer'
    $raceOfferId = [string]$raceOffer.body.id
    AssertStatus (Request Post "/api/admin/campaigns/$raceCampaignId/publish" $null $admin) 200 'race publish'
    $jobs = @($user, $racer) | ForEach-Object {
        $authValue = $_.Authorization
        $raceKey = [Guid]::NewGuid().ToString()
        Start-Job -ScriptBlock {
            param($url, $offer, $auth, $key)
            try {
                $reply = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$url/api/voucher-offers/$offer/redeem" -Headers @{Authorization=$auth;'Idempotency-Key'=$key}
                return [int]$reply.StatusCode
            } catch {
                if ($null -eq $_.Exception.Response) { throw }
                return [int]$_.Exception.Response.StatusCode
            }
        } -ArgumentList $BaseUrl,$raceOfferId,$authValue,$raceKey
    }
    $raceStatuses = @($jobs | Wait-Job | Receive-Job)
    $jobs | Remove-Job
    if (@($raceStatuses | Where-Object { $_ -eq 201 }).Count -ne 1 -or
        @($raceStatuses | Where-Object { $_ -eq 409 }).Count -ne 1) {
        throw "Expected one 201 and one 409 in stock race; got $raceStatuses"
    }
    $raceInventory = Request Get "/api/admin/campaigns/$raceCampaignId/offers" $null $admin
    if ($raceInventory.body[0].issuedQuantity -ne 1) { throw 'Stock race over-issued' }

    $retryCampaign = Request Post '/api/admin/campaigns' @{name='Retry Race';startsAt=$start;endsAt=$end;maxRedemptionsPerUser=1} $admin
    AssertStatus $retryCampaign 201 'retry campaign'
    $retryCampaignId = [string]$retryCampaign.body.id
    $campaignIds += $retryCampaignId
    $retryOffer = Request Post "/api/admin/campaigns/$retryCampaignId/offers" @{name='Retry Offer';pointsCost=50;totalQuantity=2;expiresAt=$expiry} $admin
    AssertStatus $retryOffer 201 'retry offer'
    $retryOfferId = [string]$retryOffer.body.id
    AssertStatus (Request Post "/api/admin/campaigns/$retryCampaignId/publish" $null $admin) 200 'retry publish'
    $retryKey = [Guid]::NewGuid().ToString()
    $sameKeyJobs = @(1,2) | ForEach-Object {
        Start-Job -ScriptBlock {
            param($url, $offer, $auth, $key)
            try {
                $reply = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$url/api/voucher-offers/$offer/redeem" -Headers @{Authorization=$auth;'Idempotency-Key'=$key}
                return [int]$reply.StatusCode
            } catch {
                if ($null -eq $_.Exception.Response) { throw }
                return [int]$_.Exception.Response.StatusCode
            }
        } -ArgumentList $BaseUrl,$retryOfferId,$user.Authorization,$retryKey
    }
    $retryStatuses = @($sameKeyJobs | Wait-Job | Receive-Job)
    $sameKeyJobs | Remove-Job
    if (@($retryStatuses | Where-Object { $_ -eq 201 }).Count -ne 1 -or
        @($retryStatuses | Where-Object { $_ -eq 200 }).Count -ne 1) {
        throw "Expected one 201 and one replay 200; got $retryStatuses"
    }
    $retryInventory = Request Get "/api/admin/campaigns/$retryCampaignId/offers" $null $admin
    if ($retryInventory.body[0].issuedQuantity -ne 1) { throw 'Same-key retry over-issued' }
    $newKey = [Guid]::NewGuid().ToString()
    $quota = Request Post "/api/voucher-offers/$retryOfferId/redeem" $null @{Authorization=$user.Authorization;'Idempotency-Key'=$newKey}
    AssertStatus $quota 409 'campaign quota'
    if ($quota.body.code -ne 'CAMPAIGN_QUOTA_EXCEEDED') { throw 'Wrong quota error' }

    $quotaCampaign = Request Post '/api/admin/campaigns' @{name='Quota Race';startsAt=$start;endsAt=$end;maxRedemptionsPerUser=1} $admin
    AssertStatus $quotaCampaign 201 'quota race campaign'
    $quotaCampaignId = [string]$quotaCampaign.body.id
    $campaignIds += $quotaCampaignId
    $quotaOffer = Request Post "/api/admin/campaigns/$quotaCampaignId/offers" @{name='Quota Offer';pointsCost=25;totalQuantity=2;expiresAt=$expiry} $admin
    AssertStatus $quotaOffer 201 'quota race offer'
    $quotaOfferId = [string]$quotaOffer.body.id
    AssertStatus (Request Post "/api/admin/campaigns/$quotaCampaignId/publish" $null $admin) 200 'quota race publish'
    $quotaJobs = @(1,2) | ForEach-Object {
        $distinctKey = [Guid]::NewGuid().ToString()
        Start-Job -ScriptBlock {
            param($url, $offer, $auth, $key)
            try {
                $reply = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$url/api/voucher-offers/$offer/redeem" -Headers @{Authorization=$auth;'Idempotency-Key'=$key}
                return [int]$reply.StatusCode
            } catch {
                if ($null -eq $_.Exception.Response) { throw }
                return [int]$_.Exception.Response.StatusCode
            }
        } -ArgumentList $BaseUrl,$quotaOfferId,$user.Authorization,$distinctKey
    }
    $quotaStatuses = @($quotaJobs | Wait-Job | Receive-Job)
    $quotaJobs | Remove-Job
    if (@($quotaStatuses | Where-Object { $_ -eq 201 }).Count -ne 1 -or
        @($quotaStatuses | Where-Object { $_ -eq 409 }).Count -ne 1) {
        throw "Expected one 201 and one 409 in quota race; got $quotaStatuses"
    }
    $quotaInventory = Request Get "/api/admin/campaigns/$quotaCampaignId/offers" $null $admin
    if ($quotaInventory.body[0].issuedQuantity -ne 1) { throw 'Quota race issued twice' }

    foreach ($testedUserId in @($ids[1], $ids[2])) {
        $balance = [long](Sql "SELECT balance FROM point_wallets WHERE user_id = '$testedUserId'")
        $ledger = [long](Sql "SELECT COALESCE(SUM(amount),0) FROM point_transactions WHERE wallet_id = (SELECT id FROM point_wallets WHERE user_id = '$testedUserId')")
        if ($balance -ne $ledger) { throw "Wallet/ledger mismatch for $testedUserId" }
        $broken = [int](Sql "SELECT COUNT(*) FROM user_vouchers v LEFT JOIN point_transactions p ON p.user_voucher_id = v.id WHERE v.user_id = '$testedUserId' AND (p.id IS NULL OR p.amount <> -v.points_spent OR p.wallet_id <> (SELECT id FROM point_wallets WHERE user_id = '$testedUserId'))")
        if ($broken -ne 0) { throw "Voucher/debit mismatch for $testedUserId" }
        $issued = [int](Sql "SELECT COUNT(*) FROM user_vouchers WHERE user_id = '$testedUserId'")
        $auditCount = [int](Sql "SELECT COUNT(*) FROM audit_logs WHERE actor_user_id = '$testedUserId' AND action = 'VOUCHER_REDEEMED'")
        if ($issued -ne $auditCount) { throw "Voucher/audit mismatch for $testedUserId" }
    }
    foreach ($testedCampaignId in $campaignIds) {
        $stockMismatch = [int](Sql "SELECT COUNT(*) FROM voucher_offers o WHERE o.campaign_id = '$testedCampaignId' AND o.issued_quantity <> (SELECT COUNT(*) FROM user_vouchers v WHERE v.voucher_offer_id = o.id)")
        if ($stockMismatch -ne 0) { throw "Inventory/voucher mismatch for $testedCampaignId" }
    }

    $blockCampaign = Request Post '/api/admin/campaigns' @{name='Block Race';startsAt=$start;endsAt=$end;maxRedemptionsPerUser=1} $admin
    AssertStatus $blockCampaign 201 'block race campaign'
    $blockCampaignId = [string]$blockCampaign.body.id
    $campaignIds += $blockCampaignId
    $blockOffer = Request Post "/api/admin/campaigns/$blockCampaignId/offers" @{name='Block Offer';pointsCost=25;totalQuantity=1;expiresAt=$expiry} $admin
    AssertStatus $blockOffer 201 'block race offer'
    $blockOfferId = [string]$blockOffer.body.id
    AssertStatus (Request Post "/api/admin/campaigns/$blockCampaignId/publish" $null $admin) 200 'block race publish'
    $blockKey = [Guid]::NewGuid().ToString()
    $blockJob = Start-Job -ScriptBlock {
        param($url, $userId, $auth)
        try {
            $reply = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$url/api/admin/users/$userId/block" -Headers @{Authorization=$auth} -ContentType 'application/json' -Body '{"reason":"Race block"}'
            return [int]$reply.StatusCode
        } catch {
            if ($null -eq $_.Exception.Response) { throw }
            return [int]$_.Exception.Response.StatusCode
        }
    } -ArgumentList $BaseUrl,$ids[1],$admin.Authorization
    $redeemJob = Start-Job -ScriptBlock {
        param($url, $offer, $auth, $key)
        try {
            $reply = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$url/api/voucher-offers/$offer/redeem" -Headers @{Authorization=$auth;'Idempotency-Key'=$key}
            return [int]$reply.StatusCode
        } catch {
            if ($null -eq $_.Exception.Response) { throw }
            return [int]$_.Exception.Response.StatusCode
        }
    } -ArgumentList $BaseUrl,$blockOfferId,$user.Authorization,$blockKey
    $blockStatus = @($blockJob | Wait-Job | Receive-Job)[0]
    $redeemStatus = @($redeemJob | Wait-Job | Receive-Job)[0]
    @($blockJob,$redeemJob) | Remove-Job
    if ($blockStatus -ne 200 -or $redeemStatus -notin @(201,403)) {
        throw "Unexpected block/redeem race statuses: $blockStatus / $redeemStatus"
    }
    $blockInventory = Request Get "/api/admin/campaigns/$blockCampaignId/offers" $null $admin
    $expectedIssued = $(if ($redeemStatus -eq 201) { 1 } else { 0 })
    if ($blockInventory.body[0].issuedQuantity -ne $expectedIssued) { throw 'Block/redeem inventory mismatch' }
    $blocked = Request Post "/api/admin/users/$($ids[1])/block" @{reason='Repeated block'} $admin
    AssertStatus $blocked 200 'block idempotent'
    AssertStatus (Request Post "/api/voucher-offers/$offerId/redeem" $null $redeemHeaders) 403 'blocked replay'

    AssertStatus (Request Post "/api/admin/users/$($ids[2])/points/grants" @{amount=50;reason='Cancel race grant'} $admin) 201 'cancel racer grant'
    $cancelCampaign = Request Post '/api/admin/campaigns' @{name='Cancel Race';startsAt=$start;endsAt=$end;maxRedemptionsPerUser=1} $admin
    AssertStatus $cancelCampaign 201 'cancel race campaign'
    $cancelCampaignId = [string]$cancelCampaign.body.id
    $campaignIds += $cancelCampaignId
    $cancelOffer = Request Post "/api/admin/campaigns/$cancelCampaignId/offers" @{name='Cancel Offer';pointsCost=25;totalQuantity=1;expiresAt=$expiry} $admin
    AssertStatus $cancelOffer 201 'cancel race offer'
    $cancelOfferId = [string]$cancelOffer.body.id
    AssertStatus (Request Post "/api/admin/campaigns/$cancelCampaignId/publish" $null $admin) 200 'cancel race publish'
    $cancelJob = Start-Job -ScriptBlock {
        param($url, $campaign, $auth)
        try {
            $reply = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$url/api/admin/campaigns/$campaign/cancel" -Headers @{Authorization=$auth}
            return [int]$reply.StatusCode
        } catch {
            if ($null -eq $_.Exception.Response) { throw }
            return [int]$_.Exception.Response.StatusCode
        }
    } -ArgumentList $BaseUrl,$cancelCampaignId,$admin.Authorization
    $cancelRedeemJob = Start-Job -ScriptBlock {
        param($url, $offer, $auth, $key)
        try {
            $reply = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$url/api/voucher-offers/$offer/redeem" -Headers @{Authorization=$auth;'Idempotency-Key'=$key}
            return [int]$reply.StatusCode
        } catch {
            if ($null -eq $_.Exception.Response) { throw }
            return [int]$_.Exception.Response.StatusCode
        }
    } -ArgumentList $BaseUrl,$cancelOfferId,$racer.Authorization,([Guid]::NewGuid().ToString())
    $cancelStatus = @($cancelJob | Wait-Job | Receive-Job)[0]
    $cancelRedeemStatus = @($cancelRedeemJob | Wait-Job | Receive-Job)[0]
    @($cancelJob,$cancelRedeemJob) | Remove-Job
    if ($cancelStatus -ne 200 -or $cancelRedeemStatus -notin @(201,409)) {
        throw "Unexpected cancel/redeem race statuses: $cancelStatus / $cancelRedeemStatus"
    }
    $cancelInventory = Request Get "/api/admin/campaigns/$cancelCampaignId/offers" $null $admin
    $expectedCancelIssued = $(if ($cancelRedeemStatus -eq 201) { 1 } else { 0 })
    if ($cancelInventory.body[0].issuedQuantity -ne $expectedCancelIssued) { throw 'Cancel/redeem inventory mismatch' }
    AssertStatus (Request Get "/api/campaigns/$cancelCampaignId" $null) 404 'cancelled hidden'
    AssertStatus (Request Get '/api/admin/audit-logs' $null $admin) 200 'audit list'
    AssertStatus (Request Get '/api/admin/user-vouchers' $null $admin) 200 'issued list'
    AssertStatus (Request Post "/api/admin/campaigns/$campaignId/cancel" $null $admin) 200 'cancel'
    AssertStatus (Request Get "/api/campaigns/$campaignId" $null) 404 'cancel hidden after cache invalidation'
    if ($Database -like 'voucherhub_test_*') {
        $rollbackCampaign = Request Post '/api/admin/campaigns' @{name='Rollback Test';startsAt=$start;endsAt=$end;maxRedemptionsPerUser=1} $admin
        AssertStatus $rollbackCampaign 201 'rollback campaign'
        $rollbackCampaignId = [string]$rollbackCampaign.body.id
        $campaignIds += $rollbackCampaignId
        $rollbackOffer = Request Post "/api/admin/campaigns/$rollbackCampaignId/offers" @{name='Rollback Offer';pointsCost=25;totalQuantity=1;expiresAt=$expiry} $admin
        AssertStatus $rollbackOffer 201 'rollback offer'
        $rollbackOfferId = [string]$rollbackOffer.body.id
        AssertStatus (Request Post "/api/admin/campaigns/$rollbackCampaignId/publish" $null $admin) 200 'rollback publish'
        $beforeBalance = [long](Sql "SELECT balance FROM point_wallets WHERE user_id = '$($ids[2])'")
        $beforeLedger = [int](Sql "SELECT COUNT(*) FROM point_transactions WHERE wallet_id = (SELECT id FROM point_wallets WHERE user_id = '$($ids[2])')")
        $rollbackKey = [Guid]::NewGuid().ToString()
        $rollbackHeaders = @{Authorization=$racer.Authorization;'Idempotency-Key'=$rollbackKey}
        Sql "ALTER TABLE audit_logs ADD CONSTRAINT ck_test_redeem_audit_failure CHECK (action <> 'VOUCHER_REDEEMED') NOT VALID" | Out-Null
        try {
            AssertStatus (Request Post "/api/voucher-offers/$rollbackOfferId/redeem" $null $rollbackHeaders) 500 'injected audit failure'
            $afterBalance = [long](Sql "SELECT balance FROM point_wallets WHERE user_id = '$($ids[2])'")
            $afterLedger = [int](Sql "SELECT COUNT(*) FROM point_transactions WHERE wallet_id = (SELECT id FROM point_wallets WHERE user_id = '$($ids[2])')")
            $afterStock = [int](Sql "SELECT issued_quantity FROM voucher_offers WHERE id = '$rollbackOfferId'")
            $afterVouchers = [int](Sql "SELECT COUNT(*) FROM user_vouchers WHERE voucher_offer_id = '$rollbackOfferId'")
            if ($afterBalance -ne $beforeBalance -or $afterLedger -ne $beforeLedger -or
                $afterStock -ne 0 -or $afterVouchers -ne 0) {
                throw 'Transaction failed to roll back after audit failure'
            }
        } finally {
            Sql 'ALTER TABLE audit_logs DROP CONSTRAINT IF EXISTS ck_test_redeem_audit_failure' | Out-Null
        }
        AssertStatus (Request Post "/api/voucher-offers/$rollbackOfferId/redeem" $null $rollbackHeaders) 201 'retry after rollback'
    }
    Write-Output 'PASS: grant, catalog, redeem/replay, ledger invariants, stock/quota/key races, block/cancel races, audit, isolated rollback'
} finally {
    # Only accounts made in this run and their dependants are targeted.
    if ($ids.Count -gt 0) {
        $userIds = ($ids | ForEach-Object { "'$_'" }) -join ','
        Sql "DELETE FROM point_transactions WHERE wallet_id IN (SELECT id FROM point_wallets WHERE user_id IN ($userIds))" | Out-Null
        Sql "DELETE FROM user_vouchers WHERE user_id IN ($userIds)" | Out-Null
        Sql "DELETE FROM audit_logs WHERE actor_user_id IN ($userIds)" | Out-Null
        foreach ($ownedCampaignId in $campaignIds) {
            Sql "DELETE FROM voucher_offers WHERE campaign_id = '$ownedCampaignId'" | Out-Null
            Sql "DELETE FROM campaigns WHERE id = '$ownedCampaignId'" | Out-Null
        }
        Sql "DELETE FROM point_wallets WHERE user_id IN ($userIds)" | Out-Null
        Sql "DELETE FROM users WHERE id IN ($userIds)" | Out-Null
    }
    $emailValues = ($emails | ForEach-Object { "'$_'" }) -join ','
    Sql "DELETE FROM pending_registrations WHERE email IN ($emailValues)" | Out-Null
}
