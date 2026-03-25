# Unit and Integration Test Debugging (AI Agent Guide - dotnet code)

## Test Execution Strategy

### Initial Test Run

Execute all test classes **ONE TIME** to get complete picture:
- Identify which test class(es) have failures
- Note patterns (failures concentrated in one area vs scattered)
- Understand scope before making changes

### Iterative Fix Methodology

Work on **SINGLE test class at a time**:
1. Focus all debugging effort on one failing class
2. Run that class repeatedly as you make fixes
3. Once it passes, move to next failing class
4. **Never** attempt to fix multiple test classes simultaneously

**Example Workflow:**
```
Run: All tests → Results: 14 pass, 1 fails
Focus: The 1 failing class
Fix and re-run: That class until it passes
Move to: Next failing class (if any)
```

## Integration Test Debugging

### When API Server Required

Use `ai-run-api-test.ps1` pattern (see [api-integrated-testing.md](api-integrated-testing.md)):
- Automatically starts API, runs tests, stops API
- Captures both test output AND API logs
- Easier to correlate test assertions with API behavior
- Essential for DataClient integration tests

### Data Layer Issues

**If you suspect data problems:**
1. Check if ElasticClient unit tests pass (lowest layer)
2. Use `AiHelper.CheckElasticsearchData()` to verify data exists (see [elasticsearch.md](elasticsearch.md))
3. If data exists but API returns nothing → Problem is in DataAPI layer (Controller/Context)
4. If data doesn't exist → Problem is in ElasticClient or index creation

### Response Format Issues

**If response structure is unexpected:**
1. Verify mapping in Elasticsearch matches test expectations
2. Use `AiHelper.CheckElasticsearchData()` to inspect actual field structure
3. Check for nested objects or unexpected transformations
4. Review serialization settings in API layer

## Test Index Management

### RegisterDeleteIndex Pattern

For tests creating Elasticsearch indices:

1. **Registration:** Immediately after creating index, register for cleanup
   ```csharp
   string testIndex = "test_myfeature_" + Guid.NewGuid().ToString("N").Substring(0, 8);
   // ... create and populate index ...
   Helpers.RegisterDeleteIndex(testIndex);  // Mark for cleanup
   ```

2. **Cleanup:** AssemblyHooks automatically cleans up all registered indices
   ```csharp
   [AssemblyCleanup]
   public static void Cleanup()
   {
       Helpers.CleanupRegisteredIndices();
   }
   ```

### Test Index Naming Convention

**Preferred:** Use TestItem for SaltMinerEntity testing
- Keeps tests focused on entity operations, not specific types
- Reduces complexity and maintenance
- Use `TestEntity.GenerateIndex("myfeature")` for automatic naming and registration

**When to use specific entities:**
- Testing entity-type-specific logic
- Testing entity-specific mappings or behaviors
- Document why specific entity is needed

**Naming patterns:**
```csharp
// Generic test using TestItem (PREFERRED)
string testIndex = TestItem.GenerateIndex("myfeature");
Helpers.RegisterDeleteIndex(testIndex);

// Specific entity test (when needed)
string testIndex = Asset.GenerateIndex("mytopic");
Helpers.RegisterDeleteIndex(testIndex);

// Manual naming (only if GenerateIndex not suitable)
string testIndex = "test_cleanup_" + Guid.NewGuid().ToString("N").Substring(0, 8);
Helpers.RegisterDeleteIndex(testIndex);
```

## Temporary Debugging Code

### Marking and Cleanup

When adding diagnostic code:
1. **Mark with TODO:** `// TODO: TEMPORARY DEBUGGING - Remove after testing`
2. **Track in todo list:** Add item when debugging starts
3. **Clean up completely:** Remove all diagnostic code before completion
4. **Verify:** Re-run tests after removing temporary code

### AiHelper Usage

For Elasticsearch diagnosis (see [elasticsearch.md](elasticsearch.md)):
- Add methods to `AiHelper` class only (not existing helpers)
- Use for verification and diagnosis during debugging
- Mark ALL calling code with TODO comment
- Remove all temporary calls before completing work

## Related Documentation

- [architecture.md](architecture.md) - Application architecture and debugging principles
- [api-integrated-testing.md](api-integrated-testing.md) - Running integration tests with live API
- [elasticsearch.md](elasticsearch.md) - Direct Elasticsearch access for data verification
- [dotnet-debug.md](dotnet-debug.md) - API endpoint debugging approach
