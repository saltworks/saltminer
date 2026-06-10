# smuiapi4 — SaltMiner v4 API

Flask REST API serving the `smui4` Vue frontend. Talks directly to Elasticsearch and validates user sessions against Kibana. Exposes endpoints under `/smuiapi4/*`.

## Stack

- Python 3.12+
- Flask
- Elasticsearch Python client (8.x / 9.x compatible)
- Gunicorn (production)
- pytest (tests)

## Prerequisites

- Python 3.12 or newer
- Access to a SaltMiner Elasticsearch cluster
- Access to a Kibana instance for SID validation (optional during early development)

## First-time setup

```bash
cd src/smuiapi4
python3 -m venv venv
source venv/bin/activate            # macOS/Linux
# venv\Scripts\activate              # Windows
pip install -r requirements.txt
cp .env.example .env                 # then edit values
```

### Environment variables

Create `.env` in `src/smuiapi4/` (not committed):

| Variable | Required | Default | Description |
|---|---|---|---|
| `ES_HOST` | yes | `https://localhost:9200` | Elasticsearch URL |
| `ES_USER` | yes | `elastic` | ES username |
| `ES_PASSWORD` | yes | `changeme` | ES password |
| `ES_VERIFY_CERTS` | no | `false` | Verify TLS certs |
| `KIBANA_URL` | no | (empty = passthrough auth) | Kibana base URL for SID validation |
| `REPORT_TEMPLATES_PATH` | no | `/opt/saltworks/saltminer/report-templates/` | Where uploaded report templates are stored |
| `CUSTOM_JOBS_PATH` | no | `/opt/saltworks/saltminer/custom-jobs/` | Where custom job scripts live (read-only listing) |
| `SSL_CERTS_PATH` | no | `/opt/saltworks/saltminer/ssl/` | Cert/key shared with the Nginx container |

For local development outside Docker, point the path variables at folders you can write to (`./data/...`).

```bash
mkdir -p data/report-templates data/custom-jobs data/ssl
```

## Run the API locally

```bash
source venv/bin/activate
python run.py
```

The dev server runs on **`http://localhost:5001`** (port 5000 is used by AirPlay on macOS).

In development you typically run the `smui4` Vue app alongside this API — it proxies `/smuiapi4/*` calls here. See [`../smui4/README.md`](../smui4/README.md).

## Run the tests

```bash
source venv/bin/activate
pytest tests/ -v
```

All tests mock Elasticsearch — no live ES connection required to run them.

## Project layout

```
smuiapi4/
├── app/
│   ├── __init__.py             Flask app factory
│   ├── config.py               Reads env vars into Flask config
│   ├── auth.py                 require_auth decorator + Kibana SID validation
│   ├── routes/                 One blueprint per domain area
│   │   ├── settings.py         /smuiapi4/settings/...
│   │   ├── integrations.py     /smuiapi4/integrations/...
│   │   ├── scanning.py         /smuiapi4/scanning/...
│   │   ├── dashboards.py       /smuiapi4/dashboards/... (mock data)
│   │   ├── custom_jobs.py      /smuiapi4/custom-jobs/scripts (filesystem listing)
│   │   ├── report_templates.py /smuiapi4/report-templates/...
│   │   ├── ssl.py              /smuiapi4/ssl/certificate
│   │   └── auth.py             /smuiapi4/auth/me
│   ├── services/               ES queries and filesystem ops, no HTTP concerns
│   │   ├── es_client.py
│   │   ├── settings_service.py
│   │   ├── integration_service.py
│   │   ├── scanning_service.py
│   │   ├── dashboard_service.py
│   │   ├── custom_jobs_service.py
│   │   ├── report_templates_service.py
│   │   └── ssl_service.py
│   └── utils/
│       └── responses.py        success_response / error_response helpers
├── tests/                      pytest suite, mirrors app/ structure
│   ├── conftest.py             Shared app/client/mock_es fixtures
│   └── test_*.py
├── Dockerfile
├── requirements.txt
└── run.py                      Entry point (debug mode for dev)
```

## Patterns to follow

- **All ES interaction goes through the service layer.** Routes call services; services call `get_es_client()`. Never query ES from a route handler.
- **Bulk writes use `refresh=True`** so the indexed data is searchable immediately. Without this, `fetchSomething()` after `saveSomething()` returns stale data.
- **Mock at the service module boundary in tests.** Patch `app.services.<module>.get_es_client` (where it's used), not `app.services.es_client.get_es_client` (where it's defined). Python imports cache the binding.
- **Auth is currently passthrough if `KIBANA_URL` is unset.** Set it in production to enforce real auth.

## Response envelope

Every API response uses this shape:

```json
{ "data": ..., "error": null, "warning": "optional message" }
```

Errors:

```json
{ "data": null, "error": { "code": "ES_ERROR", "message": "..." } }
```

The frontend axios interceptor unwraps `response.data` automatically — composables receive `{ data, error, warning }` and read `response.data` for the payload.

## API surface (high level)

| Path | Purpose |
|---|---|
| `GET /smuiapi4/auth/me` | Current user from Kibana SID |
| `GET/PUT /smuiapi4/settings/general` | Top-level org settings |
| `GET/POST/PUT/DELETE /smuiapi4/settings/general/other` | User-defined "Additional Settings" |
| `GET/POST/PUT/DELETE /smuiapi4/integrations/*` | Adapter templates and configured instances (sys_config) |
| `GET/POST/PUT/DELETE /smuiapi4/scanning/*` | Scanner configs, jobs, schedules (sys_config) |
| `GET /smuiapi4/dashboards/{type}` | Dashboard data (currently mock) |
| `GET /smuiapi4/custom-jobs/scripts` | Lists files in `CUSTOM_JOBS_PATH` |
| `GET/POST/DELETE /smuiapi4/report-templates/*` | Upload/download/delete .docx templates |
| `GET/POST /smuiapi4/ssl/certificate` | View and replace nginx SSL cert |

## Run with Docker

The project root has a `docker-compose.yml` that builds this service plus an Nginx container that serves `smui4`. See [`../../docs/guides/development-setup.md`](../../docs/guides/development-setup.md) for details.

## Troubleshooting

**`Permission denied` writing to `/opt/saltworks/saltminer/...`**
Override the path env vars to a folder you can write to. Most common during local dev.

**Tests fail with `ImportError: werkzeug.urls.url_quote`**
You're running with the system Python instead of the venv. Run `source venv/bin/activate` first.

**Auth always returns "unknown"**
`KIBANA_URL` isn't set in `.env`, so auth is in passthrough mode. Set it (and run via the Vite proxy so the SID cookie reaches Flask) to enable real auth.
