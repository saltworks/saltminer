# Refactoring Plan: ElasticClient Migration & DataApi Refactor

Migrate DataApi from deprecated NestClient/PitPagingInfo to EsClient/PagingInfo, propagate changes through DataClient and dependent projects, with secondary code cleanup.

## Model & Agent Recommendations

| Phase | Recommended Model | Reasoning |
|-------|------------------|-----------|
| **Planning** | Claude Sonnet 4 or Opus 4 | Complex multi-project analysis |
| **Execution** | Claude Sonnet 4 | Best balance of capability + speed for refactoring |
| **Testing/Validation** | Agent with Sonnet 4 | Can run builds and tests iteratively |

**Multi-Agent Strategy:** Use sequential single-agent execution per goal. Each goal should complete before the next starts, allowing validation between phases.

---

## Phase 1: DataApi Refactor (Primary Goal 1)

### 1.1 DI Registration
Change DI registration in [Program.cs](Saltworks.SaltMiner.DataApi/Saltworks.SaltMiner.DataApi/Program.cs) from `services.AddNestClient(...)` to `services.AddEsClient(...)` preserving all configuration options.

### 1.2 ElasticDataRepo Method Updates
Update [ElasticDataRepo.cs](Saltworks.SaltMiner.DataApi/Saltworks.SaltMiner.DataApi/Data/ElasticDataRepo.cs) for renamed `IElasticClient` methods.

**Method Naming Convention Change:** Methods renamed to area-first pattern.
- Example: `AddUpdateBulk` → `BulkAddUpdate`
- Match by: method signature + keyword in method name
- If unable to find new method name: **ASK FOR ASSISTANCE**

**Search Strategy:**
1. Find all `IElasticClient` method calls in DataApi
2. For each call, search `IElasticClient.cs` interface for matching signature pattern
3. Update method names to new convention

### 1.3 Context Classes Audit
Audit all 31 Context classes in [Contexts/](Saltworks.SaltMiner.DataApi/Saltworks.SaltMiner.DataApi/Contexts/) for:
- Any direct `PitPagingInfo` usage → replace with `PagingInfo`
- Any renamed `IElasticClient` method calls

### 1.4 Controllers Audit
Audit Controllers in [Controllers/](Saltworks.SaltMiner.DataApi/Saltworks.SaltMiner.DataApi/Controllers/) for:
- `PitPagingInfo` references in API signatures
- Renamed method calls

### 1.5 Build Verification
Build solution `Saltworks.SaltMiner.DataApi.sln` - fix all compilation errors.

### 1.6 Documentation
Create [ControllerChanges.md](Saltworks.SaltMiner.DataApi/ControllerChanges.md) documenting:
- Required breaking changes (if any)
- Suggested breaking changes for new functionality

---

## Phase 2: DataClient Refactor (Primary Goal 2)

### 2.1 PagingInfo Migration
Audit [DataClient.cs](Saltworks.SaltMiner.DataClient/Saltworks.SaltMiner.DataClient/DataClient.cs) (2115 lines) for:
- `PitPagingInfo` usages → replace with `PagingInfo`
- Methods consuming obsolete response properties

### 2.2 IElasticClient Expanded Functionality Overloads
Add method overloads for expanded `IElasticClient` functionality exposed through DataApi:
- Additional parameters that were added to `IElasticClient` methods
- Preserve existing method signatures (add overloads, don't modify)
- Keep DRY - new overloads call base implementation with defaults

**Note:** This is separate from PagingInfo changes (2.1).

### 2.3 Build Verification
Build solution `Saltworks.SaltMiner.DataClient.sln` - fix all compilation errors.

### 2.4 Documentation
Create [DataClientChanges.md](Saltworks.SaltMiner.DataClient/DataClientChanges.md) documenting:
- Required breaking changes
- Suggested breaking changes
- New overloads added

---

## Phase 3: Integration Tests (Primary Goal 3)

### 3.1 PagingTests Update
Update [PagingTests.cs](Saltworks.SaltMiner.DataClient/Saltworks.SaltMiner.DataClient.IntegrationTests/PagingTests.cs):
- Replace `PitPagingInfo` with `PagingInfo`

### 3.2 SearchFilterTests Update
Update [SearchFilterTests.cs](Saltworks.SaltMiner.DataClient/Saltworks.SaltMiner.DataClient.IntegrationTests/SearchFilterTests.cs):
- Replace `PitPagingInfo` with `PagingInfo`

### 3.3 ScanTests Update
Update [ScanTests.cs](Saltworks.SaltMiner.DataClient/Saltworks.SaltMiner.DataClient.IntegrationTests/ScanTests.cs):
- Replace `private PitPagingInfo PagingInfo` field with `private PagingInfo PagingInfo`

### 3.4 Full Test Audit
Review all 21 test files for additional `PitPagingInfo` usages.

### 3.5 Build and Run Tests
Build test project and run tests against debug API.

### 3.6 Documentation
Create [SuggestedAdditions.md](Saltworks.SaltMiner.DataClient/Saltworks.SaltMiner.DataClient.IntegrationTests/SuggestedAdditions.md) listing:
- DataClient methods without test coverage
- New functionality needing tests

### API Running Strategy

**Primary Approach (Option 1):** Agent manages both terminals
1. Start DataApi in Terminal 1: `dotnet run`
2. Wait for startup (check for listening message or delay)
3. Run tests in Terminal 2: `dotnet test --filter "TestName"`
4. After tests complete, stop API (Ctrl+C) in Terminal 1
5. Review output from both terminals

**Fallback (Option 3):** If Terminal 1 approach has issues, API has file logging available - agent can read logs after test run.

**Alternative:** User runs API in VS debugger separately, provides connection info in `appsettings.local.json`, agent runs tests only.

---

## Phase 4: Dependent Projects (Primary Goal 4)

### 4.1 Project Refactoring Order

Refactor projects in order of dependency:

| Order | Project | Location |
|-------|---------|----------|
| 1 | UiApiClient | [Saltworks.SaltMiner.UiApiClient/](Saltworks.SaltMiner.UiApiClient/) |
| 2 | SourceAdapters.Core | [Saltworks.SaltMiner.SourceAdapters.Core/](Saltworks.SaltMiner.SourceAdapters.Core/) |
| 3 | SyncAgent | [Saltworks.SaltMiner.SyncAgent/](Saltworks.SaltMiner.SyncAgent/) |
| 4 | Manager | [Saltworks.SaltMiner.Manager/](Saltworks.SaltMiner.Manager/) |
| 5 | ServiceManager | [Saltworks.SaltMiner.ServiceManager/](Saltworks.SaltMiner.ServiceManager/) |
| 6 | JobManager | [Saltworks.SaltMiner.JobManager/](Saltworks.SaltMiner.JobManager/) |

### 4.2 Per-Project Refactoring Steps

For each project:
1. Search for `PitPagingInfo` usages → update to `PagingInfo`
2. Search for DataClient method calls that changed signatures → update calls
3. Build solution
4. Verify success before moving to next project

---

## Phase 5: Secondary Goals (Inline During Phases 1-4)

### 5.1 Namespace Syntax Conversion
Convert block namespace syntax to file-scoped declarations:
```csharp
// From:
namespace Saltworks.SaltMiner.DataApi { ... }
// To:
namespace Saltworks.SaltMiner.DataApi;
```
Reset indentation for all code after conversion.

### 5.2 BOM Character Removal
Remove U+FEFF BOM characters from `.cs` files (confirmed present in many files).
Report any other non-printable characters found.

### 5.3 Code Cleanup
- Remove unused `using` statements
- Fix logging patterns:
  - `$"message {var}"` → `"{Var}", var` (camelCase → PascalCase in template)
  - Single-message: `Logger.LogInformation("{Msg}", message)`
- Address other easy code smells (nullable warnings, etc.)

---

## Further Considerations

1. **IElasticClient Method Mapping:** If method signature matching fails to find the new method name, pause and ask for assistance rather than guessing.

2. **Test Database:** Verify integration tests have access to appropriate Elasticsearch instance via `appsettings.local.json`.

3. **Breaking Change Scope:** Should API versioning be introduced for breaking controller changes, or is a single breaking release acceptable?

4. **Rollback Strategy:** Create git branch/tag before starting execution to enable easy rollback.

---

## Execution Checklist

- [ ] Phase 1.1: DI Registration updated
- [ ] Phase 1.2: ElasticDataRepo methods updated
- [ ] Phase 1.3: Context classes audited
- [ ] Phase 1.4: Controllers audited
- [ ] Phase 1.5: DataApi builds successfully
- [ ] Phase 1.6: ControllerChanges.md created
- [ ] Phase 2.1: DataClient PagingInfo migration
- [ ] Phase 2.2: DataClient overloads added
- [ ] Phase 2.3: DataClient builds successfully
- [ ] Phase 2.4: DataClientChanges.md created
- [ ] Phase 3.1-3.4: Test files updated
- [ ] Phase 3.5: Tests pass
- [ ] Phase 3.6: SuggestedAdditions.md created
- [ ] Phase 4: All dependent projects build
- [ ] Phase 5: Secondary cleanup complete
