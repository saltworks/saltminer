# Integration Test Runner - Starts DataApi, runs tests, stops API
# Usage: .\run-api-test.ps1 [TestFilter]
# Example: .\run-api-test.ps1 "ScanTests.Crud"

param(
    [string]$TestFilter = "ScanTests.Crud"
)

$ErrorActionPreference = "Stop"

Write-Host "=== SaltMiner DataApi Integration Test Runner ===" -ForegroundColor Cyan
Write-Host "Test Filter: $TestFilter`n" -ForegroundColor Yellow

# API Configuration
$apiPath = "C:\Source\saltminer\Saltworks.SaltMiner.DataApi\Saltworks.SaltMiner.DataApi"

# [1/4] Start API Process
Write-Host "[1/4] Starting DataApi..." -ForegroundColor Green
Push-Location $apiPath
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = "run --no-build"
$psi.WorkingDirectory = $apiPath
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $false

# Copy current environment variables and set API-specific ones
foreach ($key in [Environment]::GetEnvironmentVariables().Keys) {
    $psi.EnvironmentVariables[$key] = [Environment]::GetEnvironmentVariable($key)
}
$psi.EnvironmentVariables["SALTMINER_ENVIRONMENT"] = "Local"
$psi.EnvironmentVariables["SALTMINER_API_CONFIG_PATH"] = "C:\Source\saltminer-internal\config\api"

$apiProcess = [System.Diagnostics.Process]::Start($psi)
Pop-Location

Write-Host "      API started with PID: $($apiProcess.Id)" -ForegroundColor Gray

# [2/4] Wait for API to be ready
Write-Host "[2/4] Waiting for API to start..." -ForegroundColor Green
$maxWaitTime = 15
$waitInterval = 1
$elapsed = 0
$apiReady = $false

while ($elapsed -lt $maxWaitTime) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/swagger/index.html" `
            -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $apiReady = $true
            Write-Host "      API is ready after $elapsed seconds" -ForegroundColor Gray
            break
        }
    } catch {
        # API not ready yet, keep waiting
    }
    Start-Sleep -Seconds $waitInterval
    $elapsed += $waitInterval
}

if (-not $apiReady) {
    Write-Host "      ERROR: API did not start within $maxWaitTime seconds!" -ForegroundColor Red
    Write-Host "      This usually indicates:" -ForegroundColor Yellow
    Write-Host "        - Wrong environment variables (SALTMINER_ENVIRONMENT, SALTMINER_API_CONFIG_PATH)" -ForegroundColor Yellow
    Write-Host "        - Missing dependencies (Elasticsearch, database)" -ForegroundColor Yellow
    Write-Host "        - Port 5000 already in use" -ForegroundColor Yellow
    Write-Host "      Check API output above for details, then fix and retry." -ForegroundColor Yellow
    $apiProcess.Kill()
    Read-Host -Prompt "`nPress Enter to continue"
    exit 1
}

# [3/4] Run Tests
Write-Host "`n[3/4] Running integration tests..." -ForegroundColor Green
Push-Location "C:\Source\saltminer\Saltworks.SaltMiner.DataClient\Saltworks.SaltMiner.DataClient.IntegrationTests"
dotnet test --no-build --filter "FullyQualifiedName~$TestFilter" --logger "console;verbosity=normal"
$testExitCode = $LASTEXITCODE
Pop-Location

# [4/4] Stop API
Write-Host "`n[4/4] Stopping DataApi (PID: $($apiProcess.Id))..." -ForegroundColor Green
try {
    $apiProcess.Kill()
    $apiProcess.WaitForExit(5000)
    Write-Host "      API stopped successfully" -ForegroundColor Gray
} catch {
    Write-Host "      Warning: Could not stop API gracefully: $_" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=== Test Run Complete ===" -ForegroundColor Cyan
if ($testExitCode -eq 0) {
    Write-Host "Result: PASSED" -ForegroundColor Green
} else {
    Write-Host "Result: FAILED (Exit Code: $testExitCode)" -ForegroundColor Red
}

exit $testExitCode
