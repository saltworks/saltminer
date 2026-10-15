# Elasticsearch Direct Access for Debugging (AI Agent Guide)

## Overview

When debugging integration tests, directly accessing Elasticsearch verifies data state when the API layer returns unexpected results. This document outlines the AiHelper pattern for diagnostic Elasticsearch access.

## AiHelper Class Pattern

Create an `AiHelper` class for all Elasticsearch direct-access methods in your test project (i.e. `Saltworks.SaltMiner.DataClient.IntegrationTests/AiHelper.cs`).  Template for this class should be located in ai/templates, named TemplateAiHelper.cs (recreate if missing).  Ask user for connection information for the connection (do not record connection information in TemplateAiHelper.cs).  Once connection info is established in any test project, it can be assumed it should be copied for other test projects in the same workspace.  Connection information should be placed into ai/scratch, which is .gitignored to prevent credentials from entering source control.

### Purpose
- Direct HTTP access to Elasticsearch for debugging
- Verification of data existence and structure
- Diagnostic output to help identify API layer issues
- **TEMPORARY**: This class and its callers should be removed once debugging is complete.

### Current Methods

#### `CheckElasticsearchData(string indexPattern)`
Connects directly to Elasticsearch and provides diagnostic information.

**Parameters:**
- `indexPattern` - Glob pattern for index names (e.g., "test_*", "specific_index")

**Output:**
- Cluster health status
- Matching indices with document counts
- Sample _source keys from first document
- Full sample document for inspection

**Usage:**
```csharp
AiHelper.CheckElasticsearchData("test_cleanup_serialization_*");
```

**Example Output:**
```
[Elasticsearch Direct Check]
  Cluster Status: yellow
  Found 1 matching index/indices:
    test_cleanup_serialization_c556438a - Docs: 5
    Sample fields from first document:
      Key: Id, Value: 12345
      Key: Name, Value: TestAsset
      Key: TypeName, Value: SaltMinerEntity
```

#### `VerifyIndexExists(params string[] indexNames)`
Checks whether specific index names exist in Elasticsearch.

**Purpose:**
- Verify indices were properly created before cleanup
- Confirm indices were deleted by cleanup mechanism
- Test index lifecycle

**Parameters:**
- `indexNames` - Variable number of index names to check (e.g., "cleanup_verify_1_abc123", "test_data")

**Output:**
- For each index: EXISTS (✓) or DELETED (✗)
- HTTP status if check fails

**Usage:**
```csharp
// Check if cleanup actually removed the indices
AiHelper.VerifyIndexExists("test_index_1", "test_index_2");
```

**Example Output:**
```
[Index Existence Check]
  ✓ Index EXISTS: test_index_1
  ✗ Index DELETED: test_index_2
  ? Index status UNKNOWN: test_index_3 (HTTP 500)
```

## Connection Configuration

### If Test Instance Not Responding

If Elasticsearch is not accessible at the default location:
1. **Ask the user for connection details:**
   - Elasticsearch host/IP
   - Port number
   - Username and password
   - HTTP or HTTPS protocol

2. **Update connection parameters in AiHelper:**
   - Modify the `_elasticsearchUrl`, `_username`, and `_password` constants
   - Test connectivity with a quick diagnostic call

3. **Document the issue** if the connection needs to be parameterized for different environments

## Adding New Helper Methods

When additional Elasticsearch diagnostic needs arise (e.g., verifying mappings, inspecting query responses), add new methods to the `AiHelper` class:

1. Add the method with clear documentation and parameters
2. Keep methods focused on diagnostic/verification purposes only
3. Ensure output is clear and easy to parse from test output
4. Update this file with the new method documentation
5. Update the template (TemplateAiHelper.cs) with the new method
6. **IMPORTANT:** Mark all calling code with `// TODO: TEMPORARY DEBUGGING - Remove after testing`

### Example: Future Method Template
```csharp
public static void GetIndexMapping(string indexName)
{
    // Implementation using HttpClient to retrieve ES index mapping
    // Output mapping structure for debugging
}
```

## Temporary Calling Code

### Marking Temporary Code
All calls to `AiHelper` methods should be marked with a TODO comment:
```csharp
// TODO: TEMPORARY DEBUGGING - Remove after testing
AiHelper.CheckElasticsearchData(testIndex);
```
Code analyzer warnings for TODO comments can be ignored in calls that are going to be removed.

### Cleanup Process
1. Before final testing completion, search workspace for `TODO: TEMPORARY DEBUGGING`
2. Remove all marked lines of code
3. Re-run tests to confirm everything still works
4. Remove (do not comment out the entire class) `AiHelper` class

## Integration with Test Patterns

### When to Use AiHelper
- API search returns zero results when you believe data exists
- Debugging deserialization issues
- Verifying test data was properly indexed
- Checking if indices are properly cleaned up

### When NOT to Use AiHelper
- For normal test setup/teardown (use standard test helpers)
- For production-like behavior verification (keep tests using the API)
- For permanent test infrastructure (use Helpers.cs patterns)

## Future Enhancements

As test infrastructure evolves, document additional methods here:
- Index existence verification
- Index deletion verification
- Query execution and response inspection
- Mapping validation
- Template verification

## Related Documentation

- [architecture.md](architecture.md) - Application architecture and data flow layers
- [api-integrated-testing.md](api-integrated-testing.md) - Running integration tests with live API
- [test-debugging.md](test-debugging.md) - Test debugging strategies
- [dotnet-debug.md](dotnet-debug.md) - API endpoint debugging approach
