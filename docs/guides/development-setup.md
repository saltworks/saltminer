# Development Setup — smui4 + smuiapi4

This guide walks through running the **new** SaltMiner web stack locally for development:

- `src/smui4` — Vue 3 frontend
- `src/smuiapi4` — Flask backend

For component-specific details see the per-folder READMEs:
- [`src/smui4/README.md`](../../src/smui4/README.md)
- [`src/smuiapi4/README.md`](../../src/smuiapi4/README.md)

## Architecture in one picture

```
┌────────────┐   /smui4/*     ┌────────────────┐
│  Browser   │ ─────────────▶ │  Vue dev (Vite)│  https://localhost:5173
│            │                │                │
│            │   /smuiapi4/*  └────────┬───────┘
│            │ ─────────────▶          │
│            │                         ▼
│            │                ┌────────────────┐
│            │                │  Flask API     │  http://localhost:5001
│            │                │  (smuiapi4)    │
│            │                └────────┬───────┘
│            │                         │
│            │   anything else (Kibana,│
│            │   legacy /smuiapi/, etc)│
│            │ ─────────────▶          ▼
│            │                ┌────────────────┐
│            │                │  Remote        │
│            │                │  SaltMiner     │  https://qatracking.saltminer.io
└────────────┘                └────────┬───────┘
                                       │
                                       ▼
                                  Elasticsearch
```

The Vue dev server proxies anything that isn't `/smui4/*` or `/smuiapi4/*` to a remote SaltMiner instance. This way you authenticate via Kibana on the remote, and your local Flask receives the SID cookie set for `localhost`.

## Prerequisites

- Node.js 20+
- Python 3.12+
- Network access to a SaltMiner instance (for Kibana auth and legacy `/smuiapi/` calls)
- Optional: Docker + Docker Compose if you want to run the production-like stack

## One-time setup

### 1. Backend (smuiapi4)

```bash
cd src/smuiapi4
python3 -m venv venv
source venv/bin/activate          # Windows: venv\Scripts\activate
pip install -r requirements.txt
cp .env.example .env
```

Edit `src/smuiapi4/.env`:

```env
ES_HOST=https://your-es-host:9200
ES_USER=elastic
ES_PASSWORD=your-password
ES_VERIFY_CERTS=false
KIBANA_URL=https://your-kibana-host

# Local-dev paths (override the production /opt/... defaults)
REPORT_TEMPLATES_PATH=./data/report-templates/
CUSTOM_JOBS_PATH=./data/custom-jobs/
SSL_CERTS_PATH=./data/ssl/
```

Create the local data directories:

```bash
mkdir -p data/report-templates data/custom-jobs data/ssl
```

### 2. Frontend (smui4)

```bash
cd src/smui4
npm install
cp .env.example .env
```

Edit `src/smui4/.env`:

```env
VITE_REMOTE_SALTMINER=https://qatracking.saltminer.io
```

## Running both services

Open two terminals.

**Terminal 1 — Flask:**

```bash
cd src/smuiapi4
source venv/bin/activate
python run.py
```

Flask listens on `http://localhost:5001`.

**Terminal 2 — Vue:**

```bash
cd src/smui4
npm run dev
```

Vite serves at `https://localhost:5173`. The browser will warn about a self-signed cert — accept it.

## Logging in

Auth uses the Kibana SID cookie. The Vite proxy makes login work without leaving localhost:

1. Visit `https://localhost:5173/` → Vite proxies to remote Kibana → log in
2. Visit `https://localhost:5173/smui4/` → you're authenticated; the cookie travels with every API request

If your SID expires (you'll see "Session Not Found"), repeat step 1.

If you'd rather work without real auth during early development, leave `KIBANA_URL` empty in `src/smuiapi4/.env` — the API will skip validation entirely.

## Running the tests

```bash
cd src/smuiapi4
source venv/bin/activate
pytest tests/ -v
```

All tests mock Elasticsearch; no live ES connection is needed.

## Running with Docker (production-like)

A `docker-compose.yml` at the repository root brings up an Nginx + Flask stack that mirrors production:

```bash
cd <repo root>
cp .env.example .env       # set ES_HOST, KIBANA_URL, etc.
docker compose up --build -d
docker compose logs -f
```

Then open `https://localhost/smui4/`. Nginx terminates HTTPS and proxies `/smuiapi4/*` to the Flask container.

## Common workflows

| What you want to do | Where to do it |
|---|---|
| Add a new page | `src/smui4/src/views/` + a route in `src/smui4/src/router/index.js` |
| Add a new API endpoint | `src/smuiapi4/app/routes/` + a service in `src/smuiapi4/app/services/` |
| Wrap an existing API call for the frontend | A composable in `src/smui4/src/composables/` |
| Add a left-sidebar menu item | `src/smui4/src/layouts/DefaultLayout.vue` |
| Run unit tests | `pytest tests/ -v` (in `src/smuiapi4`) |
| Production build of the UI | `npm run build` (in `src/smui4`) |

## Troubleshooting

**`/smuiapi4/*` calls return 502 / connection refused**
Flask isn't running. Start it from `src/smuiapi4` with `python run.py`.

**Browser warns about untrusted cert**
The Vite dev server uses a self-signed cert. Accept it once per browser session.

**Login redirect loops or "secure connection required"**
Make sure you're accessing the dev server over `https://`, not `http://`. Kibana refuses to set its SID cookie on insecure origins.

**Settings or other pages show "unknown" as the user**
Either `KIBANA_URL` isn't set in `src/smuiapi4/.env`, or you haven't logged in yet via `https://localhost:5173/`.

**File upload returns Permission denied**
Override the path env vars (`REPORT_TEMPLATES_PATH`, `CUSTOM_JOBS_PATH`, `SSL_CERTS_PATH`) to point at writable folders. The default `/opt/saltworks/saltminer/...` is the production path.
