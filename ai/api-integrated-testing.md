# API-Integrated Testing Pattern (AI Agent Guide)

## Overview

This pattern automates the lifecycle of running integration tests that require a live API server. It starts the API, waits for readiness, runs tests, and stops the API in a single command, capturing both API logs and test output for debugging.

## Why Use This Pattern

**Key Benefits:**
- Single command execution
- Combined API and test output visibility
- Automatic lifecycle management
- Rapid iteration cycle (~15 seconds: rebuild → run → results)

**When to Use:**
- Integration tests require live API
- Need to see both API logs and test output
- Debugging API layer behavior (Controllers/Contexts)
- API returns unexpected results and you need server-side context

## Script Pattern

### Core Structure

Create `ai-run-api-test.ps1` in test project directory using the template (ai-run-test.ps1.template):

1. **Start API Process** - Launch as managed process with environment variables
2. **Wait for Ready** - Poll health endpoint (10 second timeout)
3. **Run Tests** - Execute with specified filter
4. **Stop API** - Kill process and clean up

### Implementation

**Template:** [ai-run-test.ps1.template](ai-run-test.ps1.template)

**Customize the template:**
1. Set `$apiPath` to your API project directory
2. Set `$testPath` to your test project directory
3. Configure environment variables (see section marked "CUSTOMIZE")
4. Set health check endpoint (`$healthCheckUrl`)
5. Update default `$TestFilter` if desired

## Usage Patterns

**Single test method:**
```powershell
.\ai-run-api-test.ps1 "MyTestClass.MyTestMethod"
```

**Entire test class:**
```powershell
.\ai-run-api-test.ps1 "MyTestClass"
```

**All tests:**
```powershell
.\ai-run-api-test.ps1 ""
```

## Debugging with Script Output

### What You See

The script provides interleaved output:
- API startup logs → Early initialization errors
- API request/response logs → What the API actually received and  returned
- Test execution results → Assertion failures and expected vs actual
- API shutdown confirmation → Cleanup verification

### Common Failure Patterns

**API doesn't start within 10 seconds:**
- **Critical:** Script stops and prompts investigation
- Check API output logs displayed before the error
- Verify environment variables (SALTMINER_ENVIRONMENT, config paths)
- Ensure Elasticsearch is accessible
- Confirm port 5000 not in use: `Get-NetTCPConnection -LocalPort 5000`
- **Never** increase timeout - fix root cause instead

**Tests fail with connection errors:**
- Verify health check succeeded before tests ran
- Check ApiBaseAddress in settings.json matches API port
- Ensure API authentication configured correctly

**API returns unexpected results:**
- Review API logs for the specific request
- Look for null values, validation failures, exceptions
- Compare request received vs what test sent
- Use [elasticsearch.md](elasticsearch.md) AiHelper to verify data exists

## Configuration Requirements

### Test Settings File

Create `settings.json` in test project:

```json
{
  "ApiBaseAddress": "http://localhost:5000",
  "ApiKey": "your-test-api-key",
  "ApiKeyHeader": "Authorization",
  "TimeoutSec": 30,
  "VerifySsl": false
}
```

Ensure copied to output in `.csproj`:
```xml
<ItemGroup>
  <None Update="settings.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Test Initialization Pattern

```csharp
public static class Helpers
{
    public static YourClient GetClient()
    {
        var config = JsonSerializer.Deserialize<Config>(
            File.ReadAllText("settings.json"));
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
    }
    
    [TestMethod]
    public void YourTest()
    {
        // Test using Client...
    }
}
```

## SaltMiner Implementation

**Create script from template:**
```powershell
# Copy template to DataClient directory
Copy-Item ai\ai-run-test.ps1.template `
  Saltworks.SaltMiner.DataClient\ai-run-api-test.ps1
```

**Configure for SaltMiner DataApi:**
- `$apiPath`: `C:\Source\saltminer\Saltworks.SaltMiner.DataApi\Saltworks.SaltMiner.DataApi`
- `$testPath`: `C:\Source\saltminer\Saltworks.SaltMiner.DataClient\Saltworks.SaltMiner.DataClient.IntegrationTests`
- `$healthCheckUrl`: `http://localhost:5000/swagger/index.html`
- Environment variables:
  ```powershell
  $psi.EnvironmentVariables["SALTMINER_ENVIRONMENT"] = "Local"
  $psi.EnvironmentVariables["SALTMINER_API_CONFIG_PATH"] = "C:\Source\saltminer-internal\config\api"
  ```

**Example execution:**
```powershell
cd C:\Source\saltminer\Saltworks.SaltMiner.DataClient
.\ai-run-api-test.ps1 "EngagementTests.CRUDTest"
```

## Related Documentation

- [architecture.md](architecture.md) - Application architecture and data flow layers
- [dotnet-debug.md](dotnet-debug.md) - When and how to use this pattern for API debugging
- [test-debugging.md](test-debugging.md) - Unit test debugging strategies
- [elasticsearch.md](elasticsearch.md) - Direct data verification when API returns unexpected results
