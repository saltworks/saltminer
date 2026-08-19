# Utility

> Context ID: `python/sm-api-client`

Shared helpers used across the python entrypoints and source adapters. Not a package with a single
theme — treat it as the toolbox.

| File | Purpose |
|---|---|
| `SmApiClient.py` | Maps legacy Fortify v2 staging data into SaltMiner queue scans / assets / issues and finalizes them for the manager. The largest and most involved thing here — see the Context ID above. |
| `QueueLoader.py`, `SyncQueueHelper.py`, `UpdateQueueHelper.py` | Load and manage the sync/update queues that drive the agent and the Fortify refresh. |
| `ProgressLogger.py`, `AlertLogger.py`, `EventLog.py` | Progress reporting and operator-facing event/alert output. |
| `JiraClient.py`, `SCDastClient.py`, `WizClient.py`, `SCWImport.py` | Third-party integrations. |
| `RiskRoller.py`, `IssueAggregationDocBuilder.py`, `AggregationFlattener*.py` | Risk rollup and aggregation document shaping. |
| `IndexSwap.py`, `Remapper.py` | Index maintenance helpers. |
| `CancelTracker.py`, `DImport.py`, `GeneralUtility.py`, `SettingsHelper.py`, `PassFail.py`, `PipelineParamsUtility.py`, `DevToolsUtility.py`, `SaltminerExceptions.py` | Small shared primitives — customization cancel signalling, dynamic import of customer overrides, settings access, CI helpers, exceptions. |

Run anything that imports these from the `Saltworks.SaltMiner.Python/` directory — imports are
package-relative.
