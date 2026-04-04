# Running Python Tests

## The Required Environment Variable

All Python test runs need `SALTMINER_2_CONFIG_PATH` set to the external config directory. Without it, `Application()` loads `Config/` from the repo which has Docker hostnames (`http://api`) that don't resolve on the host. The correct config is at:

```
C:\Source\saltminer-internal\config\python
```

This is the same path set in `.vscode/launch.json` under `"Py Current File"`.

## Running Tests from the Terminal

Always run from `Saltworks.SaltMiner.Python/` as the working directory:

```bash
cd C:/Source/saltminer/Saltworks.SaltMiner.Python

# Run a specific test module
SALTMINER_2_CONFIG_PATH="C:/Source/saltminer-internal/config/python" python -m unittest UnitTests.DataClientTests -v

# Run a specific test class
SALTMINER_2_CONFIG_PATH="C:/Source/saltminer-internal/config/python" python -m unittest UnitTests.DataClientTests.DataClientTests -v

# Run a specific test method
SALTMINER_2_CONFIG_PATH="C:/Source/saltminer-internal/config/python" python -m unittest UnitTests.DataClientTests.DataClientTests.test_queue_scan_add_update -v
```

## Available Test Modules

| Module | Requires DataApi | Requires Elasticsearch | Notes |
|---|---|---|---|
| `UnitTests.LoggingTests` | No | No | Tests `logging.info()` and `get_thread_logger()` |
| `UnitTests.DataClientTests` | **Yes** | No* | Sync and async DataClient integration tests |
| `UnitTests.RestClientTests` | No | No | Uses postman-echo.com |
| `UnitTests.ElasticClientTests` | No | **Yes** | |
| `UnitTests.SettingsTests` | No | No | |

*`DataClientTests.DataClientTests.setUpClass` calls `app.GetElasticClient()` which logs warnings if Elasticsearch is down, but does not fail the tests.

## Integration Test Prerequisites

`DataClientTests` and any test that constructs `DataClient` directly requires:

- **DataApi running** — typically at `http://localhost/smapi` (see `DataClient.json` in the external config)
- **Valid `ApiKey` and `ManagerApiKey`** — configured in the external config; `ManagerApiKey` is needed for `scan_search`, `scan_delete`, `asset_delete`, `issues_delete_by_scan`

## Debugging the `Application()` Config Loading Error

If you see:

```
ApplicationConfigurationException: Settings incorrect or missing value for config 'Logging' and key 'FileFormat'
```

The env var is missing or wrong. Verify:

```bash
echo $SALTMINER_2_CONFIG_PATH
ls "C:/Source/saltminer-internal/config/python/Logging.json"
```

If `Logging.json` exists but `FileFormat` is absent, that is fine — `Settings.Get('Logging', 'FileFormat', None)` returns `None` and `LoggingProvider` defaults to `'simple'` format. If the error still appears, check that `ApplicationSettings` has the sentinel fix (see `Core/ApplicationSettings.py` — `_MISSING` sentinel in `__GetFromConfig`).

## Async Tests and Event Loops

`DataClientAsyncTests` uses `unittest.IsolatedAsyncioTestCase`. Each test method runs on a fresh event loop. The `httpx.AsyncClient` inside `DataClient` is bound to the loop that was current at construction time, so a new `DataClient` is created per test in `asyncSetUp` and torn down in `asyncTearDown`. Do not share a single `DataClient` instance across `IsolatedAsyncioTestCase` test methods.

## `asyncSetUp` Slowness Warning

You may see log lines like:

```
Executing <Task finished ... asyncSetUp() ...> took 0.3 seconds
```

This is normal — `httpx.AsyncClient` establishes its connection pool on first use, and `asyncio` logs tasks exceeding 0.1 s. It is not a test failure.
