# Async DataClient

## Background

`DataClient` previously used the synchronous `requests` library via `RestClient`. For high-volume adapters that submit large numbers of issues, scans, and assets, blocking I/O wastes wall-clock time that could be used for concurrent work.

The client has been updated to use `httpx` (via a new `AsyncRestClient`) as its underlying HTTP library. Every endpoint now has a canonical `async def` implementation, and the original sync method names are preserved as thin wrappers — so no existing adapter code needs to change.

---

## What Changed

### `RestClient.py` — new `AsyncRestClient` class

`AsyncRestClient` mirrors the `RestClient` API exactly but uses `httpx.AsyncClient` internally. All HTTP verbs (`Get`, `Post`, `Put`, `Delete`, `Request`) are `async def`. Stat tracking (`RequestCount`, `RequestAvgDuration`, `RequestLastDuration`, `RequestStatsReport`) is unchanged and remains synchronous.

```python
from Core.RestClient import AsyncRestClient
```

You rarely need to use `AsyncRestClient` directly — `DataClient` manages it internally.

### `DataClient.py` — dual sync/async surface

Each endpoint method now comes in two forms:

| Sync (unchanged) | Async (new) |
|---|---|
| `queue_scan_add_update(q_scan)` | `await queue_scan_add_update_async(q_scan)` |
| `queue_asset_add_update(q_asset)` | `await queue_asset_add_update_async(q_asset)` |
| `queue_issue_add_update_batch(issue)` | `await queue_issue_add_update_batch_async(issue)` |
| `queue_issues_add_update_bulk(batch)` | `await queue_issues_add_update_bulk_async(batch)` |
| `queue_bulk_add_update(item)` | `await queue_bulk_add_update_async(item)` |
| `queue_bulk(batch)` | `await queue_bulk_async(batch)` |
| `queue_scan_update_status(id, status)` | `await queue_scan_update_status_async(id, status)` |
| `queue_scan_delete(id)` | `await queue_scan_delete_async(id)` |
| `queue_scan_delete_all(id)` | `await queue_scan_delete_all_async(id)` |
| `queue_asset_delete(id)` | `await queue_asset_delete_async(id)` |
| `register_get_role()` | `await register_get_role_async()` |
| `register_get_agent_id()` | `await register_get_agent_id_async()` |
| `get_version()` | `await get_version_async()` |
| `webhook_get(source)` | `await webhook_get_async(source)` |
| `scan_search(request)` | `await scan_search_async(request)` |
| `scan_delete(...)` | `await scan_delete_async(...)` |
| `asset_delete(...)` | `await asset_delete_async(...)` |
| `issues_delete_by_scan(...)` | `await issues_delete_by_scan_async(...)` |
| `refresh_index(name)` | `await refresh_index_async(name)` |
| `event_add(payload)` | `await event_add_async(payload)` |

**The logic lives in the `_async` method.** The sync version calls `_run_async()` to drive it on a persistent event loop stored on the `DataClient` instance. This means:

- All HTTP traffic — sync and async — flows through `httpx` and its connection pool.
- Existing adapters benefit from connection reuse without any code changes.
- Async callers get true non-blocking I/O.

### New: `close()`

`DataClient` now exposes a `close()` method that drains the `httpx` connection pool and shuts down the internal event loop. For long-running adapter services, call it when the adapter is done.

---

## Existing Adapters — No Changes Required

The sync API is fully preserved. `TenableAdapter` works today without modification:

```python
# TenableAdapter.py — existing code, unchanged

def sync_scan(self, scan_record):
    for issue_record in self.tenable_client.get_vuln_export_generator(scan_record['uuid']):
        if not self.current_scan_asset_dict.get(issue_record['asset']['uuid']):
            mapped_scan = self.map_scan(scan_record, issue_record)
            queue_scan = self.data_client.queue_scan_add_update(mapped_scan)       # sync — works as before
            mapped_asset = self.map_asset(issue_record, queue_scan['id'])
            queue_asset = self.data_client.queue_asset_add_update(mapped_asset)    # sync — works as before
            self.current_scan_asset_dict[issue_record['asset']['uuid']] = { ... }
        mapped_issue = self.map_issue(issue_record, ...)
        self.data_client.queue_issue_add_update_batch(mapped_issue)                # sync — works as before

def finalize_all_scans(self):
    self.data_client.queue_issue_add_update_batch(None)                            # flush remainder — unchanged
    for asset_id, scan_data in self.current_scan_asset_dict.items():
        self.data_client.queue_scan_update_status(scan_data['queue_scan_id'], QueueStatus.PENDING)
```

---

## Writing a New Async Adapter

For high-volume adapters, rewrite the inner loop as a coroutine and use the `_async` methods. Each `DataClient` instance owns its event loop, so one instance per adapter task is the right model.

```python
import asyncio
from Core.DataClient import DataClient, QueueStatus


class MyAdapter:
    def __init__(self, app):
        self.data_client = DataClient(app)

    def run(self):
        asyncio.run(self._run_async())
        self.data_client.close()

    async def _run_async(self):
        for scan_record in self.source_client.get_scans():
            await self._sync_scan_async(scan_record)

    async def _sync_scan_async(self, scan_record):
        asset_cache = {}

        for issue_record in self.source_client.get_issues(scan_record['id']):
            asset_id = issue_record['asset']['id']

            if asset_id not in asset_cache:
                mapped_scan = self.map_scan(scan_record, issue_record)
                queue_scan = await self.data_client.queue_scan_add_update_async(mapped_scan)

                mapped_asset = self.map_asset(issue_record, queue_scan['id'])
                queue_asset = await self.data_client.queue_asset_add_update_async(mapped_asset)

                asset_cache[asset_id] = {
                    'queue_scan_id': queue_scan['id'],
                    'queue_asset_id': queue_asset['id'],
                }

            mapped_issue = self.map_issue(issue_record, asset_cache[asset_id])
            await self.data_client.queue_issue_add_update_batch_async(mapped_issue)

        # Flush remaining issues and mark scans pending
        await self.data_client.queue_issue_add_update_batch_async(None)
        for scan_data in asset_cache.values():
            await self.data_client.queue_scan_update_status_async(
                scan_data['queue_scan_id'], QueueStatus.PENDING
            )
```

### Parallel scan processing

Because each `queue_*_async` call is a coroutine, you can process multiple scans concurrently with `asyncio.gather`. Each scan still processes its own issues sequentially (preserving batch ordering), but scans run in parallel:

```python
async def _run_async(self):
    scans = list(self.source_client.get_scans())
    await asyncio.gather(*[self._sync_scan_async(s) for s in scans])
```

> **Important:** each `DataClient` instance is safe for one coroutine at a time. If you run parallel scans using the same instance, the issue/queue batch buffers are shared. Either use one `DataClient` per scan coroutine, or ensure only one coroutine appends to a batch at a time.

---

## Calling Async Methods from Sync Code

The `_async` methods are ordinary coroutines — if you are already inside a sync context (e.g., an existing adapter with no event loop), you cannot `await` them directly. Use `asyncio.run()` to run a one-off coroutine:

```python
# Run a single async call from sync code:
result = asyncio.run(self.data_client.queue_scan_add_update_async(mapped_scan))
```

Or just use the sync wrapper — it does the same thing via the instance's persistent loop:

```python
result = self.data_client.queue_scan_add_update(mapped_scan)  # identical result
```

---

## Dependency

`httpx` must be installed. It is included in `requirements.txt`:

```
httpx
```

If using the WHL bundle for air-gapped deployment, add the appropriate `httpx` wheel for your target platform.
