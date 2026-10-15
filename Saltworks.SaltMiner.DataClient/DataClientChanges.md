# DataClient Changes

## Summary
This document details changes made to the DataClient library resulting from the DataApi migration from NestClient/PitPagingInfo to EsClient/PagingInfo, plus API coverage enhancements.

---

## Breaking Changes

### 1. ✅ FIXED: Critical Route Bug
**File:** `DataClient.cs` line 197

**Issue:** Route mismatch between DataClient and DataApi for scan count endpoint

**Change:**
```csharp
// OLD (WRONG):
ScansCountByInventoryAssetKey(string inventoryKey)
    → "scan/count/assetinv/{inventoryKey}"

// NEW (CORRECT):
ScansCountByInventoryAssetKey(string inventoryKey)
    → "scan/count/InventoryAsset/{inventoryKey}"
```

**Impact:** This was causing 404 errors. Existing calls will now work correctly.  
**Priority:** CRITICAL - Production bug fix

---

## New Methods Added

### 2. Webhook Support
**File:** `DataClient.cs` lines 1951-1970

**WebhookPost** - Post webhook payload
```csharp
public NoDataResponse WebhookPost(string source, object payload)
```
- Posts a webhook payload to the specified source
- Returns success/failure response

**WebhookGet** - Retrieve webhook queue items
```csharp
public DataResponse<QueueSyncItem> WebhookGet(string source)
```
- Gets webhook queue items for the specified source
- Returns queue sync items

**Impact:** New functionality - enables webhook integration  
**Priority:** HIGH - Required for external integrations

---

### 3. Role Management Enhancements
**File:** `DataClient.cs` lines 1854-1883

Added missing Role management methods:

**RoleGetAll** - Get all roles
```csharp
public DataResponse<AppRole> RoleGetAll()
```
- Returns all roles without search criteria
- Convenience method for admin scenarios

**RolesUpdateByQuery** - Bulk role updates
```csharp
public BulkResponse RolesUpdateByQuery(UpdateQueryRequest<AppRole> request)
```
- Updates multiple roles using query-based bulk update
- Matches pattern used for other entities (Issues, QueueItems, etc.)

**Impact:** New functionality - completes Role management API  
**Priority:** HIGH - Required for role administration features

---

## Parameter Enhancements

### 4. QueueLogRead - Optional Parameter
**File:** `DataClient.cs` line 519

**Enhancement:** Added optional `leaveUnread` parameter

**Change:**
```csharp
// OLD:
public DataResponse<QueueLog> QueueLogRead()
    → Marks messages as read

// NEW:
public DataResponse<QueueLog> QueueLogRead(bool leaveUnread = false)
    → Optional: keep messages unread if needed
```

**Impact:** Backward compatible (default behavior unchanged)  
**Priority:** LOW - Nice to have for special scenarios

---

## Documentation Updates

### 5. Comment Corrections
**File:** `DataClient.cs` line 1007

**Change:** Updated XML documentation to reflect current PagingInfo properties

```csharp
// OLD:
/// <param name="searchRequest">Request supports PagingInfo.Size, PagingInfo.AfterKeys, and FilterMatches</param>

// NEW:
/// <param name="searchRequest">Request supports PagingInfo.Size, PagingInfo.AggregateKeys, and FilterMatches</param>
```

**Reason:** `AfterKeys` was removed from DataResponse - now accessed via `PagingInfo.NextAfterKeys` or `PagingInfo.CurrentAfterKeys`

---

## Migration Guide

### For Existing Code

**No changes required** for existing DataClient consumers EXCEPT:

1. **Scan Count Calls** - Will now work correctly (was 404 before)
2. **Webhook Integration** - New capability, opt-in usage
3. **Role Bulk Updates** - New capability, opt-in usage  
4. **QueueLog Read** - Can now optionally leave messages unread

### Recommended Actions

1. **Test Scan Counts** - Verify `ScansCountByInventoryAssetKey` now works
2. **Review Webhook Needs** - Use new methods if webhook integration required
3. **Update Role Management** - Use `RolesUpdateByQuery` for bulk updates
4. **Update Tests** - Integration tests may need updates (see Phase 3)

---

## API Coverage Statistics

**Analysis Results:**
- **DataClient Methods:** 160 public methods (after additions)
- **DataApi Endpoints:** 178 endpoints
- **Coverage:** ~90% (up from ~85%)
- **Critical Bugs Fixed:** 1 (route mismatch)
- **New Methods Added:** 4
- **Parameter Enhancements:** 1

### Remaining Gaps (Low Priority)

- Some admin-only utility endpoints (e.g., Encrypt) intentionally not exposed
- A few specialized endpoints used only internally by the API

---

## Testing Recommendations

### Priority Testing

1. ✅ **ScansCountByInventoryAssetKey** - Verify route fix works
2. ✅ **WebhookPost/WebhookGet** - If using webhook integration
3. ✅ **RolesUpdateByQuery** - If using bulk role updates
4. ✅ **RoleGetAll** - Verify convenience method works
5. ✅ **QueueLogRead** with `leaveUnread=true` - If needed for monitoring

### Integration Test Updates Needed (Phase 3)

Several integration tests need updates for removed obsolete properties:
- `PitPagingInfo` → use `PagingInfo`
- `AfterKeys` → use `PagingInfo.NextAfterKeys`
- `UIPagingInfo` → removed (was never in DataClient)

See Phase 3 of RefactoringPlan.md for test migration details.

---

## Version Compatibility

**Current State:** All changes are backward compatible

**Bug Fix Impact:**
- `ScansCountByInventoryAssetKey` was broken (404 errors)
- Now works correctly
- Any workarounds can be removed

**New Methods:** All are additive - no breaking changes to existing API

---

## Related Documentation

- See [RefactoringPlan.md](../RefactoringPlan.md) for overall migration strategy
- See [DataApi/ControllerChanges.md](../Saltworks.SaltMiner.DataApi/ControllerChanges.md) for API-side changes
- See Phase 3 for Integration Test updates
