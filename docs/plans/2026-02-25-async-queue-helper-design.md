# AsyncQueueHelper Design

**Date:** 2026-02-25
**Branch:** feat-SSCAsyncTest

## Overview

Create `AsyncQueueHelper.py` by refactoring `SyncQueueHelper.py` into a source-agnostic async queue helper. Every worker instance will share this class and set its active source via property setters before each document operation.

## Context

- `SyncQueueHelper` bakes `sourceName` into the instance at `__init__` time, restricting one instance to one source (SSC or FOD).
- Workers need to handle documents from any source, switching source context per document.
- An `async_queue` Elasticsearch index template already exists with the required field mappings.

## Design Decisions

### Source identity: property setters (not per-method parameters)

`TargetType` and `TargetInstance` become mutable public properties. Workers set them before each operation. No method signatures change — all methods continue to use `self.__TargetType` / `self.__TargetInstance` internally. Workers own the responsibility of knowing and setting the correct source before each call.

### Operational settings: dedicated `AsyncQueue.json` config

Batch size and retention settings are read once at `__init__` from a dedicated `Config/Sources/AsyncQueue.json` file using `appSettings.GetSource("AsyncQueue", ...)`. This separates queue behavior config from per-source identity config and avoids repeated `appSettings.GetSource` calls per method invocation.

### SSC/FOD validation removed

The `AsyncQueueHelper` is fully source-agnostic. No type validation in `__init__`.

## File & Class Changes

### New file
`Saltworks.SaltMiner.Python/Utility/AsyncQueueHelper.py`

### Class renames (all internal references updated)

| Old name | New name |
|---|---|
| `SyncQueueHelper` | `AsyncQueueHelper` |
| `SyncQueueHelperException` | `AsyncQueueHelperException` |
| `SyncQueueDoc` | `AsyncQueueDoc` |
| `SyncQueuePriorityDoc` | `AsyncQueuePriorityDoc` |
| `SyncQueueDto` | `AsyncQueueDto` |

## `__init__` Changes

**Old signature:** `def __init__(self, appSettings, sourceName)`
**New signature:** `def __init__(self, appSettings)`

- `sourceName` parameter removed
- SSC/FOD validation block removed
- `__TargetType` and `__TargetInstance` initialized to `None`
- Operational settings read from `AsyncQueue` source config:
  ```python
  self.__BatchSize = appSettings.GetSource("AsyncQueue", "AsyncQueueBatchSize", 500)
  self.__DaysOld = appSettings.GetSource("AsyncQueue", "AsyncQueueRetentionDays", 1)
  self.__LockDaysOld = appSettings.GetSource("AsyncQueue", "AsyncQueueLockRetentionDays", 1)
  ```
- Index names updated to match existing async_queue index template:
  ```python
  self.__Index = 'async_queue'
  self.__PriorityIndex = 'async_queue_priority'
  ```

## New Properties

```python
@property
def TargetType(self):
    return self.__TargetType

@TargetType.setter
def TargetType(self, value):
    self.__TargetType = value

@property
def TargetInstance(self):
    return self.__TargetInstance

@TargetInstance.setter
def TargetInstance(self, value):
    self.__TargetInstance = value
```

## New Config File

`Saltworks.SaltMiner.Python/Config/Sources/AsyncQueue.json`

```json
{
  "_Comments": "Async queue operational settings shared across all sources.",
  "Source": "AsyncQueue",
  "AsyncQueueBatchSize": 500,
  "AsyncQueueRetentionDays": 1,
  "AsyncQueueLockRetentionDays": 1
}
```

## Worker Usage Pattern

```python
helper = AsyncQueueHelper(appSettings)

# Per document:
helper.TargetType = "SSC"
helper.TargetInstance = "Ssc1"
batch, count = helper.GetSyncQueueBatch()

# Switching source for next document:
helper.TargetType = "FOD"
helper.TargetInstance = "Fod1"
batch, count = helper.GetSyncQueueBatch()
```

## What Does Not Change

- All public method signatures remain identical
- All method implementations remain identical (they already use `self.__TargetType` / `self.__TargetInstance`)
- `SyncQueueHelper.py` is not modified — `AsyncQueueHelper.py` is a new file
- DTO internal logic, locking, session ID, scroll/bulk patterns are unchanged
