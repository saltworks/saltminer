# Running Python Scripts Directly (non-test)

## Environment

Python 3.13 lives in a `.venv` inside `Saltworks.SaltMiner.Python/`. Always use that interpreter — the system Python may not have the required packages.

```
C:\Source\saltminer\Saltworks.SaltMiner.Python\.venv\Scripts\python.exe
```

## Required env var

`SALTMINER_CONFIG_PATH` must point to the external config directory. Without it, `Application()` loads `Config/` from the repo which has Docker hostnames that don't resolve on the host.

```
C:\Source\saltminer-internal\config
```

Note: the config loader appends `\python` internally, so do **not** include `\python` here (unlike the unit-test doc which shows the full path with `\python`).

## Running a script from the terminal

Always run from `Saltworks.SaltMiner.Python/` as the working directory, using the `.venv` interpreter directly:

```bash
cd C:/Source/saltminer/Saltworks.SaltMiner.Python

SALTMINER_CONFIG_PATH="C:/Source/saltminer-internal/config" \
  .venv/Scripts/python.exe -m DebugStart
```

On Windows PowerShell:

```powershell
$env:SALTMINER_CONFIG_PATH = "C:\Source\saltminer-internal\config"
Set-Location C:\Source\saltminer\Saltworks.SaltMiner.Python
.\.venv\Scripts\python.exe -m DebugStart
```

## VS Code debugger

Use the **`Py: DebugStart`** launch profile (`.vscode/launch.json`). It sets:
- `"module": "DebugStart"` — runs `python -m DebugStart`
- `"cwd": "${workspaceFolder}/Saltworks.SaltMiner.Python"` — correct working dir
- `"SALTMINER_CONFIG_PATH": "C:\\Source\\saltminer-internal\\config"` — correct env var

## DebugStart.py pattern

`DebugStart.py` is the single entry point for ad-hoc debugging. Edit it to run whatever script or snippet you need, then launch with `Py: DebugStart`. The standard pattern for running a Run*.py script:

```python
import sys
sys.argv.append("FOD")          # positional arg 1
sys.argv.append("2024-01-01")   # positional arg 2
import RunGenerateSnapshotHistory  # noqa: E402, F401
```

`sys.argv` must be populated **before** the import because `Run*.py` scripts read `sys.argv` at module level. The `# noqa` suppresses the "import not at top" and "imported but unused" linter warnings, which are expected for this pattern.

## Key differences from python-debug.md (unit tests)

| | Unit tests | Direct scripts |
|---|---|---|
| Invocation | `python -m unittest ...` | `python -m DebugStart` |
| Entry point | test class/method | `DebugStart.py` |
| Config path suffix | includes `\python` | does **not** include `\python` |
| DataApi required | for DataClientTests | only if script uses DataClient |

## Verified working command (Bash tool)

```bash
cd "C:/Source/saltminer/Saltworks.SaltMiner.Python" && \
  SALTMINER_CONFIG_PATH="C:/Source/saltminer-internal/config" \
  .venv/Scripts/python.exe -m DebugStart 2>&1 | grep -v "^2026"
```

The `grep -v "^2026"` strips the dated log lines so only script output and errors show.
