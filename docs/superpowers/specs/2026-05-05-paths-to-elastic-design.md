# Migrate File-Path Settings From Env Vars to Elastic

**Date:** 2026-05-05
**Component:** `frontend/smuiapi4` (Flask API) and `frontend/smui4` (Vue UI)

## Problem

Four file-path settings in `frontend/smuiapi4` are configured via environment variables:

- `CUSTOM_JOBS_PATH` — consumed by `app/services/custom_jobs_service.py`
- `SALTMINER_JOBS_PATH` — declared in `.env` but no Python consumer yet
- `SSL_CERTS_PATH` — consumed by `app/services/ssl_service.py`
- `REPORT_TEMPLATES_PATH` — consumed by `app/services/report_templates_service.py`

The project's stated convention is that all settings outside Elastic/Kibana connection details live in the `sys_config` Elastic index. These four are the only remaining exceptions. They should move to `sys_config` with code-level defaults applied when the doc is missing.

## Goals

1. Remove the four path env vars from `.env` and from service modules.
2. Read each path from `sys_config` on every use; fall back to a hard-coded default if the doc is absent or Elastic is unreachable.
3. Expose GET/PUT endpoints to manage the paths.
4. Add a card to the Settings → General tab so users can view and edit the four paths.

## Non-Goals

- No changes to `Config` (ES + Kibana env vars stay).
- No seeding job — defaults live in code.
- No changes to the Dockerfile's pre-created directories.
- No changes to the Vue components for Custom Jobs, SaltMiner Jobs, SSL, or Reports beyond what flows from the API change (none expected — they call routes, not paths).

## Storage

Section/subsection/property layout in the existing `sys_config` index:

| section | subsection | property              | default                                      |
|---------|------------|-----------------------|----------------------------------------------|
| general | paths      | customJobsPath        | `/opt/saltworks/saltminer/custom-jobs/`      |
| general | paths      | saltminerJobsPath     | `/opt/saltworks/saltminer/saltminer-jobs/`   |
| general | paths      | sslCertsPath          | `/opt/saltworks/saltminer/ssl/`              |
| general | paths      | reportTemplatesPath   | `/opt/saltworks/saltminer/report-templates/` |

`value_type` is `string` for all four. `label` and `description` are populated when the UI saves them.

## Backend Design

### New module: `app/services/paths_service.py`

Single source of truth for path resolution. Each call queries Elastic — no caching (read-through, option B from brainstorming).

```
DEFAULTS = {
    "customJobsPath":      "/opt/saltworks/saltminer/custom-jobs/",
    "saltminerJobsPath":   "/opt/saltworks/saltminer/saltminer-jobs/",
    "sslCertsPath":        "/opt/saltworks/saltminer/ssl/",
    "reportTemplatesPath": "/opt/saltworks/saltminer/report-templates/",
}

def custom_jobs_path()      -> str   # _get("customJobsPath")
def saltminer_jobs_path()   -> str   # _get("saltminerJobsPath")
def ssl_certs_path()        -> str   # _get("sslCertsPath")
def report_templates_path() -> str   # _get("reportTemplatesPath")

def _get(property_name: str) -> str:
    # 1. Query sys_config for (section=general, subsection=paths, property=property_name)
    # 2. Return the stored value if found and non-empty.
    # 3. On miss, on empty value, or on any Elastic exception: return DEFAULTS[property_name].
    # 4. Log the exception path at WARNING; never raise.
```

Implementation reuses `settings_service.get_settings_by_section("general", subsection="paths")` once per call and indexes into the result. A single search returning all four is fine — service callsites are not in tight loops.

### Service refactors

Replace module-level path constants with function calls inside methods so each call hits ES fresh.

- `app/services/custom_jobs_service.py`
  - Remove `SCRIPTS_PATH = os.environ.get(...)`.
  - In `list_scripts()`, call `paths_service.custom_jobs_path()` and use locally.

- `app/services/ssl_service.py`
  - Remove `SSL_CERTS_PATH = os.environ.get(...)`.
  - In every function that currently references `SSL_CERTS_PATH` (`get_certificate_info`, `save_certificate`), call `paths_service.ssl_certs_path()` and bind to a local variable.

- `app/services/report_templates_service.py`
  - Remove `TEMPLATES_PATH = os.environ.get(...)`.
  - In every function that references `TEMPLATES_PATH` (`list_templates`, `get_template_path`, `template_exists`, `save_template`, `delete_template`), call `paths_service.report_templates_path()` locally.

Note: trailing-slash behavior in defaults preserves the current convention (`os.path.join` is tolerant). User-edited values may or may not have a trailing slash; `os.path.join` handles both.

### Routes

Add to `app/routes/settings.py`:

- `GET /smuiapi4/settings/general/paths` → returns properties for `(general, paths)` via `get_settings_by_section`.
- `PUT /smuiapi4/settings/general/paths` → accepts a list of `{property, value, value_type, label, description}` and calls `update_settings("general", "paths", updates)`.

These mirror the existing `/general/other` style. No DELETE — the four path properties are fixed; deleting a doc just makes the service fall back to its default, which is fine, but no UI exposes deletion.

### Cleanup

- Remove from `frontend/smuiapi4/.env`:
  - `CUSTOM_JOBS_PATH`
  - `SALTMINER_JOBS_PATH`
  - `SSL_CERTS_PATH`
  - `REPORT_TEMPLATES_PATH`
- Leave the Dockerfile's `RUN mkdir -p ...` lines intact — they create the default-target directories in containers.

## Frontend Design

### Composable additions

Extend `frontend/smui4/src/composables/useSettings.js` with:

- `pathsSettings` ref (array, same shape as `otherSettings`).
- `fetchPathsSettings()` → GET `/settings/general/paths`, populates `pathsSettings`.
- `savePathsSettings(updates)` → PUT `/settings/general/paths`, then re-fetch.

Returned alongside the existing exports.

### UI: new card on Settings → General

Append a card to `frontend/smui4/src/views/settings/GeneralView.vue` after the existing "Additional Settings" card, inside the `general` `v-window-item`.

Card structure:

- Header: icon `mdi-folder-cog` + title "File Storage Paths"
- Helper text: "Server-side file paths used by SaltMiner. Defaults are used if left blank."
- Four `v-text-field` rows, one per property, each with:
  - `label` from server doc, falling back to a hard-coded display label:
    - `customJobsPath` → "Custom Jobs Path"
    - `saltminerJobsPath` → "SaltMiner Jobs Path"
    - `sslCertsPath` → "SSL Certificates Path"
    - `reportTemplatesPath` → "Report Templates Path"
  - `placeholder` showing the default value (so empty = "use default")
  - `hint` describing the purpose (e.g. "Where uploaded report templates are stored")
- "Save Changes" button → builds an update payload of all four properties (always all four, including empty values — see Edge Cases) and calls `savePathsSettings`.

The card appears only inside the `general` tab. `onMounted` flow already fetches general/other settings; add `fetchPathsSettings()` to that block and bind values into a local `pathsForm` reactive object after the fetch resolves.

### Edge cases

- **Empty input** = "use the default". The PUT payload sends the property with `value: ""`. The backend stores it; the read path treats empty-string as missing and returns the default. Document this in `_get` and in the field hints.
- **Whitespace** trimmed on save.
- **Trailing slash** not enforced — `os.path.join` handles both. Don't auto-rewrite user input.

## Tests

### New: `tests/test_paths_service.py`

- Returns default when ES returns no docs.
- Returns stored value when doc present.
- Returns default when stored `value` is empty string.
- Returns default when ES raises (mock `get_settings_by_section` to raise).
- Each public function (`custom_jobs_path`, etc.) returns its own default and reads its own property.

### Updated: `tests/test_custom_jobs.py`, `tests/test_ssl.py`, `tests/test_report_templates.py`

- Replace any reliance on env vars or `SCRIPTS_PATH`/`SSL_CERTS_PATH`/`TEMPLATES_PATH` constants with monkeypatching of the `paths_service.<name>_path` function (or patching `get_settings_by_section` to return the desired value).

### New: `tests/test_settings_routes.py` cases

- `GET /smuiapi4/settings/general/paths` returns docs from `(general, paths)`.
- `PUT /smuiapi4/settings/general/paths` calls `bulk` with section=`general`, subsection=`paths`.
- `PUT` with an empty value persists the empty value (the read path handles fallback).

## Migration / Rollout

- No data migration — first reads after deploy fall back to defaults. If the existing `.env` had non-default values, an operator must enter them once via the new UI.
- Tag this in the change description so deployers know to re-enter any custom paths.

## Open Questions

None.
