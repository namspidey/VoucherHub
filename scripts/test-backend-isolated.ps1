param([int]$Port = 5118)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $root 'backend/src/VoucherHub.Api'
$apiExe = Join-Path $apiProject 'bin/Release/net10.0/VoucherHub.Api.exe'
$dbName = 'voucherhub_test_' + [Guid]::NewGuid().ToString('N').Substring(0, 12)
$created = $false
$apiProcess = $null
$url = "http://127.0.0.1:$Port"

if (-not (Test-Path -LiteralPath $apiExe)) { throw 'Build Release before isolated test' }
$compose = Get-Content -LiteralPath (Join-Path $root 'compose.yaml') -Raw
$localPassword = [regex]::Match($compose, 'POSTGRES_PASSWORD:\s*([^\r\n]+)').Groups[1].Value.Trim()
if (-not $localPassword) { throw 'Local Compose database password not found' }

try {
    docker compose exec -T postgres createdb -U voucherhub $dbName
    if ($LASTEXITCODE -ne 0) { throw 'Could not create isolated test database' }
    $created = $true

    $env:ConnectionStrings__DefaultConnection = "Host=127.0.0.1;Port=5432;Database=$dbName;Username=voucherhub;Password=$localPassword"
    $env:Jwt__SigningKey = 'LOCAL-ISOLATED-TEST-KEY-ONLY-0000000000000000'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:RateLimits__RegistrationPerMinute = '100'
    $env:RateLimits__LoginPerMinute = '100'
    $env:RateLimits__RedeemPerMinute = '100'
    $env:Logging__LogLevel__Microsoft = 'Warning'

    dotnet ef database update --project backend/src/VoucherHub.Infrastructure --startup-project backend/src/VoucherHub.Api --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Migration failed on isolated test database' }

    $apiProcess = Start-Process -FilePath $apiExe -WorkingDirectory $apiProject -ArgumentList @('--urls', $url) -WindowStyle Hidden -PassThru
    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        try {
            $null = Invoke-WebRequest -UseBasicParsing "$url/health/ready" -TimeoutSec 2
            $ready = $true
            break
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'Isolated API did not become ready' }

    & "$PSScriptRoot/test-backend-core.ps1" -BaseUrl $url -Database $dbName
    if ($LASTEXITCODE -ne 0) { throw 'Core smoke test failed' }
    & "$PSScriptRoot/test-auth-edge.ps1" -BaseUrl $url -Database $dbName
    if ($LASTEXITCODE -ne 0) { throw 'Auth edge test failed' }
    Write-Output "PASS: isolated PostgreSQL database $dbName"
} finally {
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }
    if ($created -and $dbName -match '^voucherhub_test_[0-9a-f]{12}$') {
        docker compose exec -T postgres dropdb --if-exists --force -U voucherhub $dbName
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not drop isolated test database $dbName" }
    }
}
