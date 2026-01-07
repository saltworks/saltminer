# Saltworks.SaltMiner.ElasticClient - AI Coding Agent Instructions

## Project Overview
Elasticsearch client abstraction layer for SaltMiner security platform. **Currently migrating from deprecated NEST package to Elastic.Clients.Elasticsearch (v8.19.13)**. Both `NestClient` (obsolete) and `EsClient` implementations coexist during transition.

## Critical Architecture Points

### Dual Client Pattern (Migration in Progress)
- **EsClient/** - New implementation using `Elastic.Clients.Elasticsearch` (target)
- **NestClient/** - Legacy implementation using NEST 7.17.5 (marked `[Obsolete]`, pending removal)
- **IElasticClient** - Shared interface (398 lines) defines contract for both implementations
- When adding/modifying features: prioritize `EsClient`, reference `NestClient` for business logic patterns

### Response Wrapper Pattern
All Elasticsearch operations return `IElasticClientResponse` or `IElasticClientResponse<T>`:
- `IsSuccessful` - Operation status
- `Results`/`Result` - Single or multiple documents via `IElasticClientDto<T>`
- `PagingInfo` - Continuation state for multi-page queries
- `CountAffected` - Records modified count
- Implementation classes: `EsClientResponse`, `EsClientResponse<T>`, `EsClientAggregateResponse`

### Entity Base Class Constraint
All CRUD operations require entities extending `SaltMinerEntity` (from `Saltworks.SaltMiner.Core.Entities`):
```csharp
public IElasticClientResponse<T> AddUpdate<T>(T doc, string index) where T : SaltMinerEntity
```
Common entities: `Asset`, `Scan`, `Issue`, `Engagement` (each has `GenerateIndex()` method for index naming)

## Client Configuration & Initialization

### Configuration Object
See [ClientConfiguration.cs](../Saltworks.SaltMiner.ElasticClient/ClientConfiguration.cs) for all options:
- Connection: `ElasticSearchHost[]`, `Port`, `HttpScheme`, `Username`, `Password`, `VerifySsl`
- Paging: `DefaultPageSize` (10-5000, defaults to 1000), `MaxIndexDocsForPaging` (5000)
- Behavior: `ExceptionOnInvalidResponse`, `EnableDebugInfoInElasticsearchResponse`, `SingleNodeCluster`

### Factory Pattern
```csharp
var config = new ClientConfiguration { /* settings */ };
var factory = new EsClientFactory(config) { Logger = logger };
IElasticClient client = factory.CreateClient();
```
Used by DI extensions in [ConfigureClientExtensions.cs](../Saltworks.SaltMiner.ElasticClient/Extensions/ConfigureClientExtensions.cs): `AddEsClient()`, `UseEsClient()`

## Search & Query Patterns

### SearchRequest DSL
From `Saltworks.SaltMiner.Core.Data`, not Elasticsearch native:
- `Filter.FilterMatches` - Dictionary of field/value pairs (converted to Elasticsearch queries)
- Use `DataFilterExtensions.ToSearchRequest()` to convert to search filters
- **Snake case conversion**: Field names auto-converted (e.g., `saltminer.name` → `saltminer_name`)

### Paging Strategies
1. **Point-in-Time (PIT)**: For large datasets, uses `PagingInfo.PitId` and `PagingInfo.SearchAfter`
2. **UI Paging**: For user-facing queries, uses `PagingInfo.AfterKeys` (composite aggregations)
3. Both unified in `PagingInfo` class (replaces legacy `PitPagingInfo`/`UIPagingInfo`)

## Integration Testing Conventions

### Test Setup Pattern (MSTest)
See [CRUDTests.cs](../Saltworks.SaltMiner.ElasticClient.IntegrationTests/CRUDTests.cs):
```csharp
[ClassInitialize]
public static void Initialize(TestContext _) {
    var config = Helpers.SettingsConfig("settings.json"); // Load from file
    Client = Helpers.GetElasticClient(config);
}

[ClassCleanup(ClassCleanupBehavior.EndOfClass)]
public static void Cleanup() {
    // Delete all test indices registered during test run
    foreach (var index in _indicesToDelete) 
        Client.DeleteIndex(index);
}
```

### Test Configuration
- Copy [settings.json.sample](../Saltworks.SaltMiner.ElasticClient.IntegrationTests/settings.json.sample) to `settings.json` 
- **Never commit** `settings.json` with real credentials
- Tests require live Elasticsearch instance (localhost:9200 default)

### Index Lifecycle in Tests
**Always** create temporary indices and clean up:
```csharp
var testIndex = $"{Asset.GenerateIndex("Mocked", "test", "temp")}-{Guid.NewGuid()}";
RegisterDeleteIndex(testIndex); // Tracks for cleanup
// ... perform test operations
```
Avoid dependencies on pre-existing production indices.

## Common Pitfalls & Solutions

### NEST vs Elastic.Clients.Elasticsearch API Differences
- NEST uses `ElasticClient.LowLevel` for raw calls → Use `ElasticsearchClient.Transport.RequestAsync<T>()`
- Query DSL completely rewritten → Compare [NestClient.cs](../Saltworks.SaltMiner.ElasticClient/NestClient/NestClient.cs) line 1200+ for patterns
- Aggregations: `NEST.AggregationDictionary` → `Elastic.Clients.Elasticsearch.Aggregations.AggregateDictionary`

### Auto-Create Index Setting
Assume cluster has `action.auto_create_index=false`, test classes can fail if not set in test class initializer:
```csharp
Assert.True("true", GetClusterSetting<string>("action.auto_create_index"))
```

### Bulk Operation Error Diagnostics
Set `ClientConfiguration.EnableBulkAddErrorDiagnostics = true` for detailed failure info (impacts performance, appropriate for debugging):
- Enables per-document error tracking in `BulkErrorMessages` dictionary
- Use only during debugging, not production

## Integration Test Conventions (Enforced)

### Settings & Connection
- **Environment Variable**: Tests use `ELASTIC_SETTINGS_PATH` to locate a local `settings.json` file **outside the repository**
- **Setup Pattern**: Each test class calls `Helpers.ValidateSettingsAndConnect()` in `[ClassInitialize]`; this validates the settings file exists and Elasticsearch is reachable
- **Failure Behavior**: If settings file is missing or Elasticsearch is unreachable, tests fail fast at ClassInitialize (before any test methods run)

### Namespace & File Format
- **File-scoped namespaces**: Use `namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;` (not block-scoped `{ }`)
- **No BOM characters**: Remove U+FEFF (BOM) anywhere found - usually it is seen before the first `using` statement when present

### Index Lifecycle
- **Unique Indices**: Create temporary indices with unique names (e.g. test-specific suffixes) whenever possible
- **ThrowawayEntity Preference**: Use `ThrowawayEntity` for non-entity-specific tests instead of real entities (`Asset`, `Issue`, `Scan`, etc.)
- **Cleanup Registration**: Register all created indices in a static `_indicesToDelete` list via `RegisterDeleteIndex(index)`
- **ClassCleanup**: Delete all tracked indices in `[ClassCleanup(ClassCleanupBehavior.EndOfClass)]`

### Client Initialization Pattern
```csharp
[ClassInitialize]
public static void Initialize(TestContext _)
{
    Helpers.ValidateSettingsAndConnect();  // Validates settings & connectivity before proceeding
    var config = Helpers.SettingsConfig();
    Client = Helpers.GetElasticClient(config);  // Creates EsClient via factory
}

[ClassCleanup(ClassCleanupBehavior.EndOfClass)]
public static void Cleanup()
{
    foreach (var index in _indicesToDelete)
    {
        try { Client.DeleteIndex(index); }
        catch (Exception ex) { Console.WriteLine($"Error deleting index {index}: {ex.Message}"); }
    }
}
```

### EsClient-First Focus
- All tests use the modern `Elastic.Clients.Elasticsearch` implementation (accessed via `IElasticClient` interface created by `EsClientFactory`)
- No tests should instantiate `NestClient` directly

## File Structure
- **EsClient/** - Modern implementation
- **NestClient/** - Legacy (ignore for new features)
- **Interfaces/** - Contracts (`IElasticClient`, `IElasticClientResponse`, etc.)
- **Extensions/** - DI registration and data filter conversions
- **IntegrationTests/** - Live Elasticsearch integration tests

## Commands & Workflows

### Build
```powershell
dotnet build Saltworks.SaltMiner.ElasticClient.sln
```

### Run Tests
```powershell
# Requires settings.json with valid Elasticsearch connection
dotnet test Saltworks.SaltMiner.ElasticClient.IntegrationTests/
```

## License Header
All `.cs` files include auto-generated Business Source License header. Preserve when creating new files.
