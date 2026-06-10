# Snapshots Skill

Manage SaltMiner Elasticsearch snapshot indices and run snapshot history generation.

## Connection Info

Read `ai/scratch/snapshots-connection.md` from the repo root. Format — one value per line:
```
Line 1: Elasticsearch base URL  (e.g. https://host:9200)
Line 2: Username
Line 3: Password
```

If the file is missing or any field is blank, ask the user for the missing values and offer to create/update the file before continuing.

Always pass `-sk` to curl (self-signed cert on this server).

## Subcommands

The user invokes this skill as: `/snapshots <subcommand> [args]`

---

### delete
List all `snap*` indices, show the list to the user, and ask for confirmation before deleting.

```bash
# list
curl -sk -u {user}:{pass} "{url}/_cat/indices/snap*?h=index&s=index"

# delete one index (repeat per index)
curl -sk -u {user}:{pass} -X DELETE "{url}/{index}"
```

After deletion, verify by listing `snap*` indices again and confirm the list is empty.

---

### query `<index_pattern>` `<query_json>`
Run an arbitrary Elasticsearch query and display the pretty-printed result.

```bash
curl -sk -u {user}:{pass} -H "Content-Type: application/json" \
  "{url}/{index_pattern}/_search?pretty" -d '{query_json}'
```

---

### run `[source_type]` `[--rebuild]`
Run snapshot history generation via DebugStart.py.

Steps:
1. Save the current contents of `Saltworks.SaltMiner.Python/DebugStart.py`.
2. Rewrite the top section (everything above the first `###...###` separator comment) to:
   ```python
   import sys
   # append source_type arg if provided
   sys.argv.append("{source_type}")   # omit this line if no source_type
   sys.argv.append("--rebuild")       # omit this line if --rebuild not requested
   import RunGenerateSnapshotHistory  # noqa: E402, F401
   ```
3. Run from `Saltworks.SaltMiner.Python/`:
   ```bash
   cd Saltworks.SaltMiner.Python && \
   SALTMINER_2_CONFIG_PATH="C:/Source/saltminer-internal/config/python" \
   .venv/Scripts/python.exe -m DebugStart
   ```
4. Restore DebugStart.py to its saved contents after the run completes (success or failure).

---

### rebuild `[source_type]`
Delete all `snap*` indices then run snapshot history generation for the given source type with `--rebuild`.

Steps:
1. Run the `delete` subcommand (get confirmation first).
2. Run the `run` subcommand with `--rebuild` (and `source_type` if provided).

---

## Notes
- The separator in DebugStart.py is the `#####...#####` comment block — do not modify anything at or below it.
- Config path is always `C:/Source/saltminer-internal/config` (from launch.json).
- Always restore DebugStart.py even if the run fails.
