# Running Python Tests

## VS Code Launch Profiles

`.vscode/launch.json` already has profiles that set the environment correctly — prefer these over hand-rolling a command line:

| Profile | Runs |
|---|---|
| `Py:Current` | The currently open file (`${file}`) |
| `Py: DebugStart` | `Saltworks.SaltMiner.Python/DebugStart.py` |

Both set `cwd` to `Saltworks.SaltMiner.Python`, `justMyCode: false`, and the env below. To run something else from the terminal, copy the same env/cwd out of the profile.

## The Required Environment Variable

All Python runs need `SALTMINER_CONFIG_PATH` set to the **parent** of the config directory. `Application.__InitConfig` appends the `python` app folder itself (`Core/Application.py`, `APP_FOLDER = "python"`), so point it at `config`, not `config/python`. Without it, `Application()` falls back to the repo's `Config/` folder, which has Docker hostnames (`http://api`) that don't resolve on the host and a log folder of `/opt/saltworks/saltminer/logs` that isn't writable by a normal user.

```
SALTMINER_ENVIRONMENT=Local
SALTMINER_CONFIG_PATH=/mnt/g-drive/Source/Saltworks/saltminer-internal/config
```

Note the resolved config path is echoed at startup (`Configuration location: ...`) and written to `sm-run-config-location.json` — check that line first when a run picks up the wrong config.

## Running Tests from the Terminal

Always run from `Saltworks.SaltMiner.Python/` as the working directory:

```bash
cd /mnt/g-drive/Source/Saltworks/saltminer/Saltworks.SaltMiner.Python
export SALTMINER_ENVIRONMENT=Local
export SALTMINER_CONFIG_PATH=/mnt/g-drive/Source/Saltworks/saltminer-internal/config

# Run the async agent debug harness
python DebugStart.py

# Run a specific test module
python -m unittest UnitTests.DataClientTests -v

# Run a specific test class
python -m unittest UnitTests.DataClientTests.DataClientTests -v

# Run a specific test method
python -m unittest UnitTests.DataClientTests.DataClientTests.test_queue_scan_add_update -v
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
echo $SALTMINER_CONFIG_PATH
ls "$SALTMINER_CONFIG_PATH/python/Logging.json"
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
