# SaltMiner Manager

> Context ID: `manager`

Batch back-end processor for SaltMiner scan data. A short-lived CLI — one operation per invocation,
then it exits — that reads and writes everything through the SaltMiner data API. Scheduling is
external (host cron, or the service manager).

## Operations

| Verb | Does |
|---|---|
| `queue` | Ingests pending queue scans into published assets and issues. Optionally scoped with `--source-type` or `--queue-scan-id`. |
| `snapshot` | Generates rollup snapshots. |
| `cleanup` | **Deprecated** — superseded by `Saltworks.SaltMiner.Python/RunUtilCleanQueue.py`. Ages out old queue scans and removes orphaned queue assets and issues. |

`--list-only` previews what `queue` would process without writing anything.

## Running

```
dotnet run --project Saltworks.SaltMiner.Manager -- queue -n 100
dotnet run --project Saltworks.SaltMiner.Manager -- queue --queue-scan-id <id>
dotnet run --project Saltworks.SaltMiner.Manager -- snapshot
```

Configuration comes from `appsettings.json` (defaults in `appsettings-default.json`); the config
directory can be set with `SALTMINER_CONFIG_PATH`. `Saltworks.SaltMiner.Manager.IntegrationTests`
requires a reachable data API and Elasticsearch — there is no unit-test project.

## Layout

- `Program.cs` — CLI verbs and host builder · `Manager.cs` — dispatch to the processor for the verb
- `QueueProcessor.cs` · `SnapshotProcessor.cs` · `CleanUpProcessor.cs` — the three operations
- `RuntimeConfig.cs` — per-operation argument carriers · `ManagerConfig.cs` — config schema
