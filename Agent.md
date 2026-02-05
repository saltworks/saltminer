# Instructions for AI Work on SaltMiner Project

This document provides structured guidance for AI agents working on SaltMiner development and testing tasks.

## Application Architecture Overview

### Data Call Flow Through Application Layers

When debugging data operations, understand how a request flows through the system:

```
application 
  ↓ calls
DataClient method 
  ↓ calls
Data API *Controller action 
  ↓ calls
Data API *Context method 
  ↓ calls
ElasticClient method 
  ↓ calls
Elasticsearch .net library 
  ↓ calls
Elasticsearch
```

**Key Debugging Principle:** If ElasticClient layer tests pass but DataClient integration tests fail, the problem lies somewhere between the DataClient and ElasticClient layers (typically in the DataAPI Controller or Context).

## Testing Methodology

### Integration Tests with Data API

When debugging integration tests that require the Data API (also tests using DataClient):

1. **Use the ApiIntegratedTesting method** - Reference [ApiIntegratedTesting.md](ApiIntegratedTesting.md)
   - This approach starts the API server before running tests
   - Captures both test output AND API logs
   - Essential for understanding failures in API layer behavior
   - Allows verification of data flow from test → API → Elasticsearch → response

2. **Example Execution:**
   - Use `ai-run-api-test.ps1 "TestClassName"` from the DataClient workspace
   - This starts Kestrel API on port 5000
   - Runs specified test class
   - Stops API server
   - Returns full output including API server logs

### Direct Elasticsearch Access During Debugging

When API tests return unexpected results, or as a quick utility to verify elasticsearch data:

1. **Reference [Elasticsearch.md](Elasticsearch.md)** for the AiHelper pattern
   - Create direct HTTP connections to Elasticsearch for diagnosis
   - Verify data exists in index when API returns zero results
   - Check index state and document counts
   - Inspect sample documents and their field structure

2. **Connection Details:**
   - Ask user for connection details for first test project, then reuse those thereafter
   - Ask user for updated connection info if test instance not responding

### Unit Test Debugging Strategy

When working with unit test suites:

1. **First Run:** Execute all test classes ONE TIME to get a complete picture
   - Identify which test class(es) have failures
   - Note patterns (e.g., all failures in specific area, or scattered)

2. **Iterative Fix:** Work on a SINGLE test class at a time
   - Focus all debugging effort on one failing class
   - Run that class repeatedly as you make fixes
   - Once it passes, move to next failing class
   - Do NOT attempt to fix multiple test classes simultaneously

3. **Example Workflow:**
   - Run: All test classes → Results: 14 pass, 1 fails
   - Focus on: The 1 failing class
   - Fix and re-run: That specific class until it passes
   - Move to: Next failing class (if any)

### Debugging Strategy

**For Integration Test Failures:**

1. **If API server is NOT running:** Use `ai-run-api-test.ps1` to start API and run tests together
   - This automatically captures API server logs alongside test output
   - Easier to correlate test assertions with API behavior

2. **If you suspect data layer issues:**
   - Check if ElasticClient unit tests pass (lowest layer test)
   - Use `AiHelper.CheckElasticsearchData()` to verify data exists in Elasticsearch
   - If data exists but API returns nothing, problem is in DataAPI layer (Controller/Context)
   - If data doesn't exist, problem is in ElasticClient or Index creation

3. **If response format/structure is unexpected:**
   - Verify the mapping in Elasticsearch matches test expectations
   - Use `AiHelper.CheckElasticsearchData()` to inspect actual field structure of documents
   - Check for nested objects or unexpected transformations

## Test Index Management Pattern

### RegisterDeleteIndex Pattern

For tests that create indices in Elasticsearch:

1. **Registration:** After creating test index, immediately register for cleanup
   ```csharp
   string testIndex = "test_myfeature_" + Guid.NewGuid().ToString("N").Substring(0, 8);
   // ... create and populate index ...
   Helpers.RegisterDeleteIndex(testIndex);  // Mark for cleanup
   ```

2. **Cleanup:** The AssemblyHooks class automatically cleans up all registered indices
   ```csharp
   // In AssemblyHooks.cs
   [AssemblyCleanup]
   public static void Cleanup()
   {
       Helpers.CleanupRegisteredIndices();  // Deletes all registered indices
   }
   ```

3. **Benefits:**
   - Centralized cleanup after all tests complete
   - Prevents index pollution between test runs
   - Ensures test isolation and repeatability

### Test Index Naming Convention

When creating test indices:

1. **Use TestItem for SaltMinerEntity:** Default to using TestItem when testing generic SaltMinerEntity behavior
   - Keep tests focused on entity operations, not specific entity types
   - Reduces test complexity and maintenance
   - Use `TestEntity.GenerateIndex("myfeature")` for automatic naming and registration

2. **Use Specific Entities When Necessary:**
   - When testing entity-type-specific logic
   - When testing entity-specific mappings or behaviors
   - Document why specific entity is needed

3. **Recommended Naming Pattern:**
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

## Debugging and Temporary Code

### Temporary Debugging Code

When adding diagnostic code during debugging:

1. **Mark with TODO comment:** `// TODO: TEMPORARY DEBUGGING - Remove after testing`
2. **Track in todo list:** Add item to todo list when debugging starts
3. **Clean up completely:** Remove all diagnostic code before final completion
4. **Verify functionality:** Re-run tests after removing temporary code

### Using AiHelper for Elasticsearch Diagnosis

Reference Elasticsearch.md for creating diagnostic helper methods:
- Add methods to `AiHelper` class only (not existing helper classes)
- Use for verification and diagnosis during debugging
- Mark all calling code with TODO comment
- Remove all temporary calls before completing work

## Continuous Integration Considerations

- Test indices should be automatically cleaned via AssemblyHooks
- No manual index cleanup should be required between test runs
- If manual cleanup is needed, document the issue
- Each test run should start in a clean state

## Documentation Standards

- Do NOT create new .md files to report on output or debugging results
- .md files should only be created per explicit work instructions
- Exception: Update existing documentation (Elasticsearch.md, Agent.md) if infrastructure changes
- Use inline code comments and TODO markers to document temporary changes
