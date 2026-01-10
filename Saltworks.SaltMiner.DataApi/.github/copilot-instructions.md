# Saltworks SaltMiner DataApi - AI Agent Instructions

## Project Overview
**Saltworks.SaltMiner.DataApi** is an ASP.NET Core 8.0 Web API that provides core data management and querying capabilities for the SaltMiner vulnerability assessment platform. It integrates with Elasticsearch for full-text search and complex data indexing, and communicates with Kibana for analytics.

## Architecture Pattern: Context/Repository Pattern

The codebase uses a **Context** layer that sits between Controllers and Data Repositories:

```
Controller → Context → IDataRepo (ElasticDataRepo) → Elasticsearch
```

**Key insight**: Each Context is a domain-specific service layer (e.g., `AssetContext`, `IssueContext`, `EngagementContext`). All Context classes inherit from `ContextBase` and are registered as transient in DI.

### Critical Files
- [../Saltworks.SaltMiner.DataApi/Program.cs](../Saltworks.SaltMiner.DataApi/Program.cs) - DI configuration, dynamically registers all Context subclasses
- [../Saltworks.SaltMiner.DataApi/Contexts/ContextBase.cs](../Saltworks.SaltMiner.DataApi/Contexts/ContextBase.cs) - Base class with shared logic
- [../Saltworks.SaltMiner.DataApi/Controllers/ApiControllerBase.cs](../Saltworks.SaltMiner.DataApi/Controllers/ApiControllerBase.cs) - Base controller establishing Context reference
- [../Saltworks.SaltMiner.DataApi/Data/ElasticDataRepo.cs](../Saltworks.SaltMiner.DataApi/Data/ElasticDataRepo.cs) - Elasticsearch integration

## Authentication & Authorization
- **Mechanism**: HMAC authentication via `[Auth]` attribute with bearer tokens
- **Roles**: Admin, Manager, Agent, Pentester, PentesterViewer, Config, JobManager, ServiceManager
- **Pattern**: Apply `[Auth(Role.Manager)]` to methods requiring specific roles
- [../Saltworks.SaltMiner.DataApi/Authentication/AuthAttribute.cs](../Saltworks.SaltMiner.DataApi/Authentication/AuthAttribute.cs) provides the authorization filter
- [../Saltworks.SaltMiner.DataApi/Authentication/HmacAuthenticator.cs](../Saltworks.SaltMiner.DataApi/Authentication/HmacAuthenticator.cs) validates HMAC signatures

## Data Models & Configuration

### ApiConfig (Models/ApiConfig.cs)
Central configuration loaded from `appsettings.json`. Key properties:
- `ElasticHost`, `ElasticPort`, `ElasticUsername`, `ElasticPassword` - Elasticsearch connection
- `KibanaBaseUrl` - Kibana analytics URL
- `KestrelMaxRequestBodySizeMb` - Request size limits
- Timeout and SSL verification settings

### Response Models
- `DataItemResponse<T>` - Single item response
- `DataResponse<T>` - Paginated response
- `NoDataResponse` - Status-only response
All include Success flag and Message for error details

## Request/Response Pattern

Responses are standardized:
```csharp
// In Context classes
return new DataResponse<Asset>(assets, totalCount, pageSize);
return new DataItemResponse<Asset>(asset);
return new NoDataResponse(true, "Operation completed");
```

The `ValidateModelAttribute` automatically validates incoming request models.

## Search & Filtering

Elasticsearch queries use `SearchRequest` with:
- `PageNumber`, `PageSize`
- `SortBy`, `SortDirection` 
- `FilteredColumns` for field-specific filters
- `SearchTerm` for full-text search

Example in ContextBase: `DataRepo.Search<T>(indexName, request)` returns paginated results.

## Critical Dependencies & Integrations

### External Projects (in broader workspace)
- **Saltworks.SaltMiner.Core** - Shared entities and data repository interfaces
- **Saltworks.SaltMiner.ElasticClient** - Elasticsearch client wrapper
- **Saltworks.Utility.ApiHelper** - HTTP client utilities for external API calls

### Key Singletons
- `ApiCache` - In-memory cache for manager instances and lookup data
- `LockHelper` - Concurrency control for multi-instance scenarios
- `ApiConfig` - Configuration injected everywhere

## Elasticsearch Index Mapping
The API manages multiple Elasticsearch indices:
- **Assets** - Infrastructure assets discovered by scanners
- **Issues** - Vulnerability findings
- **Engagements** - Penetration testing engagements
- **Lookups** - Reference data (dropdown values, enumerations)
- **Attributes** - Custom field definitions

When adding new indexed entities, update `Program.cs` to configure the `IElasticClientFactory`.

## Testing Patterns

### Unit Tests (Saltworks.SaltMiner.DataApi.UnitTests)
- Test authentication via `HmacAuthTests.cs`
- Use dependency injection with mock repositories

### Integration Tests (Saltworks.SaltMiner.DataApi.Tests)
- [../Saltworks.SaltMiner.DataApi.Tests/Helpers.cs](../Saltworks.SaltMiner.DataApi.Tests/Helpers.cs) provides test setup
- Requires live Elasticsearch and Kibana connections

## CLI Commands
Program.cs supports command-line operations:
```bash
dotnet run main                          # Run the API server
dotnet run configwizard main             # Interactive configuration
dotnet run crypto generate value1 ... value5   # Encryption utilities
dotnet run version                       # Show version info
```

## Build & Runtime Configuration

### Configuration Files
- `appsettings.json` - Default settings
- `Saltworks.SaltMiner.DataApi.csproj` - Project metadata, version 3.0.1
- `.vs/` folder excluded from tracking (Visual Studio cache)

### BuildingProj Targets
- **Debug**: Generates XML documentation (`Saltworks.SaltMiner.DataApi.xml`)
- **Release**: Includes symbols package (`snupkg`)
- Warnings as errors enabled for NuGet packages

## Common Tasks

### Adding a New Context
1. Create new class in `Contexts/` inheriting from `ContextBase`
2. Implement business logic methods
3. No explicit registration needed - DI auto-discovers via `BaseType == typeof(ContextBase)`

### Adding an Endpoint
1. Create Controller inheriting from `ApiControllerBase`
2. Inject context via constructor parameter
3. Apply `[Auth]` attribute with required roles
4. Return appropriate response model

### Querying Elasticsearch
Use `DataRepo.Search<T>()` in Context for consistent pagination and error handling. Avoid direct ElasticClient calls outside Data layer.

## Code Style Notes
- **Auto-generated markers**: Files with "auto-generated" comment block should not be manually edited
- **XML Documentation**: Generate with Debug build, required for Swagger
- **Logging**: Use injected `ILogger<T>`, configured with Serilog
- **Enum formatting**: Use `.ToString("g")` for enum-to-string conversions
- Remove u+feff characters if present in .cs files.
- If namespace block is present (i.e. `namespace Saltworks.SaltMiner.DataApi { [contents] }`), replace with namespace declaration syntax (`namespace Saltworks.SaltMiner.DataApi;\n[contents]`) and reset code indentation.

## License Header
All `.cs` files include auto-generated Business Source License header. Preserve when creating new files.