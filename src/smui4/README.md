# smui4 — SaltMiner v4 Web UI

Vue 3 + Vuetify 4 single-page application for the SaltMiner GUI. Served as static files from Nginx at `/smui4/` in production. Communicates with the `smuiapi4` Flask backend at `/smuiapi4/`.

## Stack

- Vue 3 (Composition API, `<script setup>`)
- Vuetify 4
- Vue Router
- Axios
- Vite

## Prerequisites

- Node.js 20+ (LTS)
- A running `smuiapi4` Flask backend (see [`../smuiapi4/README.md`](../smuiapi4/README.md))
- Network access to a SaltMiner instance for Kibana auth and legacy API calls

## First-time setup

```bash
cd src/smui4
cp .env.example .env       # then edit to point at your remote SaltMiner
npm install
```

### Environment variables

`.env` (not committed) sets the remote SaltMiner instance the Vite dev server proxies to:

| Variable | Description | Example |
|---|---|---|
| `VITE_REMOTE_SALTMINER` | Remote SaltMiner used for Kibana auth and legacy `/smuiapi/` API | `https://qatracking.saltminer.io` |

## Run the dev server

```bash
npm run dev
```

The dev server runs on **`https://localhost:5173/`** (HTTPS, with a self-signed cert from `@vitejs/plugin-basic-ssl` — your browser will warn the first time, accept it).

### Development routing

Vite's proxy splits traffic three ways:

| Path | Destination | Purpose |
|---|---|---|
| `/smui4/*` | Vue dev server (local) | Hot reload of the app you're editing |
| `/smuiapi4/*` | `http://localhost:5001` (local Flask) | The new Python API |
| Everything else | Remote SaltMiner (`VITE_REMOTE_SALTMINER`) | Kibana auth, legacy `/smuiapi/`, legacy GUI |

### Authentication during development

Auth uses the Kibana SID cookie. Because Vite proxies non-local requests to the remote SaltMiner, you can log in without leaving localhost:

1. Start Flask (`smuiapi4`) and Vite (this app)
2. Open `https://localhost:5173/` — Vite proxies to remote Kibana, which sets the `sid` cookie for `localhost`
3. Open `https://localhost:5173/smui4/` — you're authenticated; the cookie is sent automatically with every API request

If your SID expires, repeat the steps above. No manual cookie copying needed.

## Production build

```bash
npm run build
```

Outputs static files to `dist/`. In production, an Nginx container copies these into `/usr/share/nginx/html/smui4/` and serves them at `/smui4/`.

## Project layout

```
smui4/
├── public/                 Static assets served as-is (favicons, default icons)
├── src/
│   ├── App.vue
│   ├── main.js             Vuetify plugin setup + theme config
│   ├── router/             All client-side routes
│   ├── layouts/
│   │   └── DefaultLayout.vue   Sidebar nav + header (used by every page)
│   ├── views/              Top-level pages, one folder per top nav section
│   │   ├── dashboards/
│   │   ├── inventory/
│   │   ├── integrations/
│   │   ├── scanning/
│   │   └── settings/
│   ├── components/         Reusable widgets (cards, editors, builders)
│   ├── composables/        Reactive data + API call wrappers (useXxx.js)
│   ├── services/
│   │   ├── api.js          Axios client for our local /smuiapi4/ Flask API
│   │   └── legacyApi.js    Axios client for the existing /smuiapi/ .NET API
│   └── assets/
├── index.html
├── vite.config.js
└── package.json
```

## Patterns to follow

- **Composables** wrap API calls. Each gets its own `useXxx.js` and exposes refs (data, loading, error) plus action functions.
- **Services** are the axios clients. `api.js` for Flask (`/smuiapi4/`), `legacyApi.js` for the legacy .NET API (`/smuiapi/`).
- **Use Vuetify components by default.** Don't custom-build something Vuetify already provides.
- **Component names use PascalCase** in templates (`<MyComponent />`) — kebab-case auto-resolution can fail with consecutive uppercase letters in names (e.g. `SSLCertificateManager` does not resolve as `ssl-certificate-manager`).

## Common tasks

| Task | Command |
|---|---|
| Start dev server | `npm run dev` |
| Build for production | `npm run build` |
| Preview a production build | `npm run preview` |

## Troubleshooting

**Browser shows "session not found" overlay**
You're not logged into Kibana. Open `https://localhost:5173/`, log in, then return to `/smui4/`.

**"Connection refused" on `/smuiapi4/` calls**
The Flask backend isn't running. Start it from `src/smuiapi4/` (`python run.py`).

**Kibana login complains about secure connection**
Vite must be running over HTTPS. Confirm `https://` in the URL — the dev server uses `@vitejs/plugin-basic-ssl` to provide a self-signed cert.
