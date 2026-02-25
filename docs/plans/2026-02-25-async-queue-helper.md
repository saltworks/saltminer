# AsyncQueueHelper Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create `AsyncQueueHelper.py` as a source-agnostic async queue helper where `TargetType` and `TargetInstance` are mutable properties set by the worker before each operation, replacing the fixed `sourceName` init parameter.

**Architecture:** Copy `SyncQueueHelper.py` to `AsyncQueueHelper.py`, rename all classes, restructure `__init__` to accept only `appSettings` (reading operational settings from a dedicated `AsyncQueue.json` config), remove the SSC/FOD validation, initialize `TargetType`/`TargetInstance` to `None`, and expose them as public settable properties. No method signatures change — all methods already use `self.__TargetType` / `self.__TargetInstance` internally.

**Tech Stack:** Python 3, Elasticsearch (via internal `ElasticClient`), `ApplicationSettings` / `appSettings.GetSource()`

---

### Task 1: Create `Config/Sources/AsyncQueue.json`

**Files:**
- Create: `Saltworks.SaltMiner.Python/Config/Sources/AsyncQueue.json`

**Step 1: Create the config file**

Write the following to `Saltworks.SaltMiner.Python/Config/Sources/AsyncQueue.json`:

```json
{
  "_Comments": "Async queue operational settings shared across all sources.",
  "Source": "AsyncQueue",
  "AsyncQueueBatchSize": 500,
  "AsyncQueueRetentionDays": 1,
  "AsyncQueueLockRetentionDays": 1
}
```

**Step 2: Verify**

Open the file and confirm the JSON is valid and contains the three setting keys.

**Step 3: Commit**

```bash
git add Saltworks.SaltMiner.Python/Config/Sources/AsyncQueue.json
git commit -m "feat: add AsyncQueue.json config for async queue operational settings"
```

---

### Task 2: Create `AsyncQueueHelper.py`

**Files:**
- Create: `Saltworks.SaltMiner.Python/Utility/AsyncQueueHelper.py`
- Reference: `Saltworks.SaltMiner.Python/Utility/SyncQueueHelper.py` (source to copy from — do NOT modify)

**Step 1: Copy `SyncQueueHelper.py` content into the new file**

Create `Saltworks.SaltMiner.Python/Utility/AsyncQueueHelper.py` starting from the full content of `SyncQueueHelper.py`. All changes below are applied to the new file only.

**Step 2: Rename all class names throughout the file**

Apply these renames everywhere they appear (class definitions, instantiations, isinstance checks, type hints, static method calls, property names):

| Find | Replace |
|---|---|
| `SyncQueueHelper` | `AsyncQueueHelper` |
| `SyncQueueHelperException` | `AsyncQueueHelperException` |
| `SyncQueuePriorityDoc` | `AsyncQueuePriorityDoc` |
| `SyncQueueDoc` | `AsyncQueueDoc` |
| `SyncQueueDto` | `AsyncQueueDto` |

Note: `SyncQueueDto` has a property also named `SyncQueueDoc` (the property that holds the doc object). Rename that property to `AsyncQueueDoc` as well — including all references to `sqdto.SyncQueueDoc` in `SetInProgress` and `SetComplete`.

**Step 3: Rewrite `__init__`**

Replace the entire `__init__` method body. The new version:

```python
def __init__(self, appSettings):
    '''
    Setup the class
    '''
    if type(appSettings).__name__ != "ApplicationSettings":
        raise TypeError("Type of appSettings must be 'ApplicationSettings'")

    app = appSettings.Application
    logging.debug("AsyncQueueHelper init")

    self.__TargetType = None
    self.__TargetInstance = None
    self.__Index = 'async_queue'
    self.__PriorityIndex = 'async_queue_priority'
    self.__IdField = 'target_id'
    self.__Es = app.GetElasticClient()
    self.__BatchSize = appSettings.GetSource("AsyncQueue", "AsyncQueueBatchSize", 500)
    self.__DaysOld = appSettings.GetSource("AsyncQueue", "AsyncQueueRetentionDays", 1)
    self.__LockDaysOld = appSettings.GetSource("AsyncQueue", "AsyncQueueLockRetentionDays", 1)
    self.__Es.MapIndex(self.__Index, False)  # will map if doesn't exist
    self.__Es.MapIndex(self.__PriorityIndex, False)  # will map if doesn't exist
    self.__LoadExclusions = []
    self.__PriorityReservations = {}
    self.__SessionId = uuid.uuid4()
    self.__DefaultPriority = 5
    logging.debug("AsyncQueueHelper init complete.")
```

Key changes vs `SyncQueueHelper.__init__`:
- Signature: `(self, appSettings)` — `sourceName` removed
- `self.__TargetType = None` (was: looked up from `appSettings.GetSource(sourceName, "Source")`)
- `self.__TargetInstance = None` (was: `= sourceName`)
- SSC/FOD `if not self.__TargetType in ['SSC', 'FOD']:` validation block removed entirely
- Index: `'async_queue'` (was: `'syncqueue'`)
- PriorityIndex: `'async_queue_priority'` (was: `'syncqueue_priority'`)
- Settings source: `"AsyncQueue"` with keys `AsyncQueueBatchSize`, `AsyncQueueRetentionDays`, `AsyncQueueLockRetentionDays` (was: `sourceName` with keys `SyncQueueBatchSize`, `SyncQueueRetentionDays`, `SyncQueueLockRetentionDays`)

**Step 4: Add `TargetType` and `TargetInstance` properties**

Add these two properties to the `AsyncQueueHelper` class, immediately after the existing `Index` property (around line 67 in the original):

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

**Step 5: Verify the complete file**

Do a final check across the file for any remaining `Sync` references that should have been renamed. Search for `Sync` in the file — the only legitimate remaining occurrences should be in log message strings like `"Cleared sync queue for..."` and `"Cleared sync priority reservations for..."` (these describe the queue operation concept, not the class, so leave them as-is).

Confirm:
- `class AsyncQueueHelper(object):` ✓
- `def __init__(self, appSettings):` — no `sourceName` ✓
- `self.__Index = 'async_queue'` ✓
- `self.__PriorityIndex = 'async_queue_priority'` ✓
- `appSettings.GetSource("AsyncQueue", "AsyncQueueBatchSize", 500)` ✓
- `TargetType` and `TargetInstance` properties with setters present ✓
- `class AsyncQueuePriorityDoc(object):` ✓
- `class AsyncQueueDoc(object):` ✓
- `class AsyncQueueDto(object):` ✓
- `class AsyncQueueHelperException(Exception):` ✓
- `AsyncQueueDto` has property `AsyncQueueDoc` (not `SyncQueueDoc`) ✓
- `SetInProgress` references `sqdto.AsyncQueueDoc` ✓
- `SetComplete` references `sqdto.AsyncQueueDoc` ✓

**Step 6: Commit**

```bash
git add Saltworks.SaltMiner.Python/Utility/AsyncQueueHelper.py
git commit -m "feat: add AsyncQueueHelper — source-agnostic async queue helper with mutable TargetType/TargetInstance properties"
```

---

## Notes for Future Test Coverage

`SyncHelperTests.py` provides the integration test pattern for the sync version. A future `AsyncHelperTests.py` would follow the same pattern with these differences:
- Import `AsyncQueueHelper` instead of `SyncQueueHelper`
- `__init__` takes only `appSettings` — no `source_name`
- Set `cls.aqh.TargetType = "SSC"` and `cls.aqh.TargetInstance = "SSC1"` in `setUpClass` before calling any queue methods
- References to `q_item.SyncQueueDoc` become `q_item.AsyncQueueDoc`
- Tests run against `async_queue` index instead of `syncqueue`
