# API-Integrated Testing Pattern

## Overview

This document describes the testing pattern developed for running integration tests that require a live API server. This approach enables rapid debugging by automatically managing the API lifecycle (start/test/stop) without user interaction.

## Problem Statement

Integration tests that depend on a running API traditionally require:
- Manual API startup in separate terminal/process
- Coordination between API availability and test execution
- Manual API shutdown after tests complete
- Repetitive approval prompts when stopping processes
- Difficulty seeing both API and test output simultaneously

This creates friction during debugging and slows down the development cycle.

## Solution Architecture

### Core Pattern
```
PowerShell Script → Start API Process → Wait for Ready → Run Tests → Stop API Process
```

### Key Innovation
Start the API as a **managed separate process** (not in a terminal) while running tests in the normal terminal. This provides:
- Single command execution
- Combined output visibility
- Automatic cleanup
- No approval prompts after initial script approval

## Implementation

### Step 1: Create Test Runner Script

Create `run-api-integrated-test.ps1` in your test project directory:

```powershell
# Integration Test Runner - Starts API, runs tests, stops API
# Usage: .\run-test.ps1 [TestFilter]

param(
    [string]$TestFilter = "YourDefaultTest"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Integration Test Runner ===" -ForegroundColor Cyan
Write-Host "Test Filter: $TestFilter`n" -ForegroundColor Yellow

# API Configuration
$apiPath = "C:\Path\To\Your\Api\Project"

# [1/4] Start API Process
Write-Host "[1/4] Starting API..." -ForegroundColor Green
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
$psi.EnvironmentVariables["YOUR_ENV_VAR"] = "value"
$psi.EnvironmentVariables["YOUR_CONFIG_PATH"] = "C:\path\to\config"

$apiProcess = [System.Diagnostics.Process]::Start($psi)
Pop-Location

Write-Host "      API started with PID: $($apiProcess.Id)" -ForegroundColor Gray

# [2/4] Wait for API to be ready
Write-Host "[2/4] Waiting for API to start..." -ForegroundColor Green
$maxWaitTime = 10
$waitInterval = 1
$elapsed = 0
$apiReady = $false

while ($elapsed -lt $maxWaitTime) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/health" `
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
    Write-Host "        - Wrong environment variables or config path" -ForegroundColor Yellow
    Write-Host "        - Missing dependencies (database, Elasticsearch)" -ForegroundColor Yellow
    Write-Host "        - Port 5000 already in use" -ForegroundColor Yellow
    Write-Host "      Check API output above for details, then fix and retry." -ForegroundColor Yellow
    $apiProcess.Kill()
    Read-Host -Prompt "`nPress Enter to continue"
    exit 1
}

# [3/4] Run Tests
Write-Host "`n[3/4] Running integration tests..." -ForegroundColor Green
Push-Location "C:\Path\To\Your\Test\Project"
dotnet test --no-build --filter "FullyQualifiedName~$TestFilter" --logger "console;verbosity=normal"
$testExitCode = $LASTEXITCODE
Pop-Location

# [4/4] Stop API
Write-Host "`n[4/4] Stopping API (PID: $($apiProcess.Id))..." -ForegroundColor Green
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
```

### Step 2: Configure Test Settings

   
   2 
Create `settings.json` in your test project:

```json
{
  "ApiBaseAddress": "http://localhost:5000",
  "ApiKey": "your-test-api-key",
  "ApiKeyHeader": "Authorization",
  "TimeoutSec": 30,
  "VerifySsl": false
}
```

Ensure it's copied to output directory in `.csproj`:

```xml
<ItemGroup>
  <None Update="settings.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Step 3: Test Initialization Pattern

```csharp
public static class Helpers
{
    public static YourClient GetClient()
    {
        var config = JsonSerializer.Deserialize<Config>(
            File.ReadAllText("settings.json"));
        
        // Configure your client with settings
        return new YourClient(config);
    }
}

[TestClass]
public class YourTests
{
    private static YourClient Client;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        Client = Helpers.GetClient();
        // Client constructor should validate connection
    }
    
    [TestMethod]
    public void YourTest()
    {
        // Test using Client...
    }
}
```

## Usage

### Running Tests

**Single test:**
```powershell
.\run-test.ps1 "MyTestClass.MyTestMethod"
```

**Test class:**
```powershell
.\run-test.ps1 "MyTestClass"
```

**All tests:**
```powershell
.\run-test.ps1 ""
```

### First-Time Setup
1. Run the script once in a PowerShell session
2. Approve the execution policy when prompted
3. Script will run without prompts for remainder of session

## Debugging Strategy

### Combined Output
The script provides interleaved output showing:
- API startup logs
- API request/response logs
- Test execution results
- API shutdown confirmation

### When Tests Fail

1. **Check API Logs** - Look for errors in API startup or request processing
2. **Check Test Output** - Review assertion failures and stack traces
3. **Verify Configuration** - Ensure settings.json has correct values
4. **Check API Readiness** - Verify the health check endpoint is appropriate

### Common Issues

**API doesn't start within 10 seconds:**
- **CRITICAL**: Script will stop and prompt user for investigation
- Check the API output logs displayed above the error message
- Verify environment variables (SALTMINER_ENVIRONMENT, config paths)
- Ensure required services are running (Elasticsearch, database)
- Confirm port 5000 is not already in use: `Get-NetTCPConnection -LocalPort 5000`
- Try running API manually: `cd <api-path>; dotnet run --no-build`
- **DO NOT** increase wait time beyond 10 seconds - fix the root cause instead

**API doesn't start:**
- Check environment variables are set correctly
- Verify API project path is correct
- Ensure port 5000 is available
- Check `dotnet run --no-build` works manually

**Tests can't connect:**
- Verify ApiBaseAddress matches API listening port
- Check API authentication requirements
- Ensure API readiness check succeeds before tests run

**Process doesn't stop:**
- Script uses `Kill()` which should always work
- Check Task Manager if process persists
- May need to manually kill child processes

## Benefits

✅ **Speed**: ~15 second cycle time (rebuild → run script → results)  
✅ **Visibility**: See both API and test output in one place  
✅ **Automation**: No manual coordination required  
✅ **Reliability**: Guaranteed cleanup even if tests crash  
✅ **Debuggability**: API logs show exactly what requests were processed  

## Variations

### Multiple APIs
Start multiple processes before tests:

```powershell
$api1Process = [System.Diagnostics.Process]::Start($psi1)
$api2Process = [System.Diagnostics.Process]::Start($psi2)
# Wait for both to be ready...
# Run tests
$api1Process.Kill()
$api2Process.Kill()
```

### Database Seeding
Add step between API start and test run:

```powershell
# After API is ready
Write-Host "[2.5/4] Seeding test data..." -ForegroundColor Green
Invoke-RestMethod -Uri "http://localhost:5000/test/seed" -Method POST
```

### Parallel Test Execution
Use different ports for each test runner:

```powershell
$port = Get-Random -Minimum 5000 -Maximum 6000
$psi.EnvironmentVariables["ASPNETCORE_URLS"] = "http://localhost:$port"
# Update settings.json ApiBaseAddress to use $port
```

## Real-World Example: SaltMiner

Location: `Saltworks.SaltMiner.DataClient\run-test.ps1`

This pattern successfully tests:
- DataClient HTTP library against live DataApi
- DataApi business logic against real Elasticsearch (10.9.2.16:9201)
- Authentication/authorization flows
- Entity CRUD operations
- Search and pagination functionality

Run with:
```powershell
cd C:\Source\saltminer\Saltworks.SaltMiner.DataClient
.\run-test.ps1 "EngagementTests.CRUDTest"
```

## Conclusion

This pattern transforms integration testing from a manual, error-prone process into a streamlined automated workflow. By managing the API lifecycle within a single script, developers can iterate rapidly without context switching or coordination overhead.

The approach is particularly valuable when:
- Tests require live backend dependencies (databases, message queues, etc.)
- Debugging requires seeing both client and server behavior
- Multiple developers need consistent test execution
- CI/CD pipelines need reliable integration test runs
