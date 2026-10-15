# Debugging .NET API Endpoints (AI Agent Guide)

## Overview

When debugging .NET API endpoints in SaltMiner, use the automated script approach rather than manual VS Code debugging. This provides better visibility into both API and test behavior simultaneously.

## API Debug Approach for AI Agents

### Use Automated API Testing Script

**Primary Method:** Use `ai-run-api-test.ps1` pattern (see [api-integrated-testing.md](api-integrated-testing.md))

**Why this approach:**
- Starts API, runs tests, stops API in single command
- Captures both API logs and test output
- No manual coordination required
- Reveals API-layer issues that unit tests miss

**Typical execution:**
```powershell
cd Saltworks.SaltMiner.DataClient
.\ai-run-api-test.ps1 "TestClassName"
```

### Available API Configurations

#### DataApi
- **Port:** `http://localhost:5000`
- **Config Path:** `C:\Source\saltMiner-internal\config\api`
- **Environment Variables:**
  - `ASPNETCORE_ENVIRONMENT=Development`
  - `SALTMINER_ENVIRONMENT=Local`
  - `SALTMINER_API_CONFIG_PATH=C:\Source\saltMiner-internal\config\api`
  - `ASPNETCORE_URLS=http://localhost:5000`

#### UiApi
- **Port:** `http://localhost:5001`
- **Config Path:** `C:\Source\saltMiner-internal\config\ui-api`
- **Environment Variables:**
  - `ASPNETCORE_ENVIRONMENT=Development`
  - `SALTMINER_ENVIRONMENT=Local`
  - `SALTMINER_API_CONFIG_PATH=C:\Source\saltMiner-internal\config\ui-api`
  - `ASPNETCORE_URLS=http://localhost:5001`

## Multi-Layer Debugging Strategy

When debugging issues spanning multiple layers (see [architecture.md](architecture.md)):

```
DataClient → DataApi Controller → DataApi Context → ElasticClient → Elasticsearch
```

**Diagnostic Sequence:**
1. **Run ElasticClient unit tests** - If these pass, problem is higher in stack
2. **Run DataClient integration tests with API** - Use `ai-run-api-test.ps1` to see API logs
3. **Check API logs for errors** - Look for exceptions, validation failures, null values
4. **Use AiHelper** - Verify data exists in Elasticsearch if API returns empty results (see [elasticsearch.md](elasticsearch.md))
5. **Identify the layer** - Problem is in Controller, Context, or ElasticClient integration

## Common Diagnostic Scenarios

### API Returns Zero Results But Data Should Exist

1. Use `AiHelper.CheckElasticsearchData("index_pattern")` to verify data exists
2. If data exists, problem is in DataApi Controller or Context
3. Check API logs from `ai-run-api-test.ps1` for:
   - Query construction issues
   - Mapping/serialization mismatches
   - Filter logic errors

### API Returns 500 Internal Server Error

1. Check API logs for exception details
2. Verify API startup completed successfully
3. Check configuration file paths are correct
4. Verify Elasticsearch connection is available

### API Doesn't Start Within Timeout

1. Verify external config files exist at expected paths
2. Check Elasticsearch is accessible
3. Confirm port is not already in use
4. Review API startup logs for initialization errors

## Configuration Requirements

Both APIs require external configuration files outside the repository:
- **DataApi:** `C:\Source\saltMiner-internal\config\api`
- **UiApi:** `C:\Source\saltMiner-internal\config\ui-api`

If tests fail with configuration errors, verify these paths exist and contain valid configuration.

## Related Documentation

- [architecture.md](architecture.md) - Application architecture and data flow
- [api-integrated-testing.md](api-integrated-testing.md) - Automated API testing pattern details
- [test-debugging.md](test-debugging.md) - Unit and integration test debugging strategies  
- [elasticsearch.md](elasticsearch.md) - Direct Elasticsearch access for verification
