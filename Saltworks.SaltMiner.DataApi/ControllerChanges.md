# DataApi Controller Changes

## Summary
This document tracks breaking changes and suggested improvements to DataApi controllers resulting from the migration from NestClient to EsClient and from PitPagingInfo to PagingInfo.

## Required Breaking Changes

### 1. Removed Obsolete PitPagingInfo Property
**Status:** ✅ COMPLETED - Breaking Change

**What Changed:**
- Removed `PitPagingInfo` property from `DataResponse<T>` class
- Removed `PitPagingInfo` property from `DataDictionaryResponse<TKey, TValue>` class
- Removed `PitPagingInfo` property from `ElasticAggResponse` class

**Migration Path:**
Use `PagingInfo` instead. The new `PagingInfo` class provides all functionality:
```csharp
// OLD - No longer supported
response.PitPagingInfo.Enabled
response.PitPagingInfo.PagingToken
response.PitPagingInfo.Size
response.PitPagingInfo.Total
response.PitPagingInfo.AggregateKeys

// NEW - Use PagingInfo
response.PagingInfo.EnablePit
response.PagingInfo.PitPagingToken
response.PagingInfo.Size
response.PagingInfo.TotalHits
response.PagingInfo.AggregateKeys
```

**Impact:** API clients must update to use `PagingInfo` for pagination

### 2. Removed AfterKeys Property
**Status:** ✅ COMPLETED - Breaking Change

**What Changed:**
- Removed `AfterKeys` property from `DataResponse<T>` class
- Removed `AfterKeys` property from `ElasticAggResponse` class

**Migration Path:**
Use `PagingInfo.CurrentAfterKeys` or `PagingInfo.NextAfterKeys`:
```csharp
// OLD - No longer supported
request.AfterKeys = response.AfterKeys;

// NEW - Use PagingInfo properties
request.PagingInfo.CurrentAfterKeys = response.PagingInfo.NextAfterKeys;
```

**Impact:** API clients must update keyset pagination logic to use `PagingInfo.NextAfterKeys`

## Controller API Compatibility

**No controller signatures were modified.** All changes were internal:
- DI registration changed from `AddNestClient` to `AddEsClient`
- Internal method name updates (e.g., `AddUpdateBulk` → `BulkAddUpdate`)
- Context methods updated to use `PagingInfo`

**Controllers remain backward compatible at the HTTP API level** - request/response JSON structure has changed for pagination properties.

## Suggested Enhancements for Future Versions

### 1. Leverage Enhanced PagingInfo Features
**Status:** Enhancement opportunity

**New Capabilities:**
The `PagingInfo` class provides enhanced paging features:
- `EnablePit` - Point-in-time searching for consistent pagination
- `NextAfterKeys` / `CurrentAfterKeys` - Efficient keyset pagination
- `TotalHitsCanBeTruncated` - Control count accuracy requirements
- `AggregateKeys` - Composite aggregation pagination support

**Suggested Enhancement:**
Consider exposing these capabilities through controller endpoints where beneficial:
- Search endpoints with large result sets could benefit from PIT
- Aggregation endpoints could expose keyset pagination for better performance
- Allow clients to opt-in to approximate counts for faster queries

**Example Enhancement:**
```csharp
// Enhanced search request
public class EnhancedSearchRequest 
{
    public PagingInfo PagingInfo { get; set; }
    public bool EnablePointInTime { get; set; } = false;  // Opt-in for consistency
    public bool AllowApproximateCounts { get; set; } = true;  // Opt-in for speed
}
```

## Implementation Notes

### Changes Made in This Refactor

1. **Program.cs** - DI Registration
   - Changed: `services.AddNestClient(...)` → `services.AddEsClient(...)`
   - All configuration options preserved

2. **Context Classes** - Internal Updates
   - `SnapshotContext`: Updated `PitPagingInfo` → `PagingInfo` in aggregate methods
   - `EngagementContext`: Updated `PitPagingInfo` → `PagingInfo` in aggregate methods
   - All method name updates follow area-first naming pattern:
     - `AddUpdateBulk` → `BulkAddUpdate`
     - `CreateIndex` → `IndexCreate`
     - `DeleteIndex` → `IndexDelete`
     - `RefreshIndex` → `IndexRefresh`
     - `FlushIndex` → `IndexFlush`
     - `CheckForIndex` → `IndexExists`
     - `AddUpdateBulkQueue` → `BulkQueueAddUpdate`
     - `UpdatePartialBulkWithLocking` → `BulkUpdatePartialWithLocking`
     - `GetClusterTaskCountAsync` → `ClusterTaskCountGetAsync`

3. **ElasticDataRepoExtensions** - Response Mapping
   - Updated to map from new `IElasticClientResponse<T>` structure
   - `AfterKeys` now pulled from `PagingInfo.NextAfterKeys`
   - `PitPagingInfo` set to null (obsolete property)

### Testing Recommendations

1. Verify backward compatibility of API responses
2. Test pagination scenarios with existing clients
3. Validate aggregate query continuation works correctly
4. Confirm obsolete property warnings appear in client builds

## Version Compatibility

- **Current Version:** Maintains full backward compatibility
- **Deprecation Timeline:** Consider removing obsolete properties in next major version
- **Migration Path:** Clients should update to use `PagingInfo` and prepare for removal of `PitPagingInfo`

## Related Documentation

- See `RefactoringPlan.md` for overall migration strategy
- See `Saltworks.SaltMiner.Core/Data/PagingInfo.cs` for new paging class details
- See `Saltworks.SaltMiner.ElasticClient/Interfaces/IElasticClient.cs` for updated method signatures
