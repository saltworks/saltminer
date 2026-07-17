# Running SaltMiner on Podman

SaltMiner's `docker-compose.yml` runs under **Docker (rootful)** or **Podman (rootless)**.
The differences are a few `.env` values, a little host setup, and **one edit to the
compose file** (removing the `init:` entries from `depends_on` — see below). Everything
in this document was validated end-to-end on CentOS Stream 9 (SELinux enforcing,
podman 5.8, podman-compose 1.6).

## TL;DR

1. Create the application root (default `/opt/saltworks`) **owned by the account that
   will run Podman** (ideally a dedicated service account with lingering enabled — see
   [Boot persistence](#boot-persistence)). `docker-compose.yml` + `.env` can live there
   too, or anywhere readable by that account.
2. In `.env`, use the Podman values: **comment out** `CONTAINER_UID_GID` and
   `INIT_CHOWN` (unset, the containers run as container-root, which rootless podman
   maps to your unprivileged account — verified the app images default to root), and set:
   ```ini
   APP_ROOT=/opt/saltworks
   NGINX_HTTP_PORT=8080
   NGINX_HTTPS_PORT=8443
   ```
3. Make the **one compose edit**: delete the two `init:` / `condition:` lines from every
   service's `depends_on` (and the `depends_on:` key itself for `api`, where init is the
   only entry). Why: podman implements `depends_on` with `--requires`, which demands the
   dependency be *running* — `sm-init` is a oneshot that exits by design, so with those
   entries every dependent service fails with `container state improper` / hangs.
4. Forward standard ports with the host firewall (rootless podman cannot bind <1024,
   and capability workarounds do **not** work — see [Privileged ports](#6-privileged-ports-80--443)):
   ```sh
   sudo firewall-cmd --permanent --add-forward-port=port=80:proto=tcp:toport=8080
   sudo firewall-cmd --permanent --add-forward-port=port=443:proto=tcp:toport=8443
   sudo firewall-cmd --permanent --add-port=8080/tcp --add-port=8443/tcp
   sudo firewall-cmd --reload
   ```
5. Create the network and start:
   ```sh
   podman network create saltminer
   podman-compose up -d
   ```

`sm-init` creates `${APP_ROOT}/saltminer` and its entire subtree, already owned
correctly. You pre-create only `${APP_ROOT}` itself — nothing under it.

---

## Why the differences exist

### 1. Run-as user (`CONTAINER_UID_GID`)

`CONTAINER_UID_GID` sets the uid:gid the app containers (everything but nginx) run as.
Its **only** purpose is to run unprivileged per Docker best practice — the apps
themselves are perfectly happy running as root (they did so before the uid/gid
settings existed).

The same `user:` value resolves to a different **host** identity on each engine:

| Engine            | `user: 10000` maps to (host)              | `user: 0` maps to (host)          |
| ----------------- | ----------------------------------------- | --------------------------------- |
| Docker (rootful)  | host UID 10000 (real)                     | host **root** — do not use        |
| Podman (rootless) | subordinate range (`/etc/subuid`, ~110000)| the account that launched podman  |

So under rootless Podman the Docker trick (`10000`) maps into the subordinate UID
range — *not* your account — and can't write bind mounts owned by your account.

The fix is to run as **container-root**: comment `CONTAINER_UID_GID` out entirely.
Unset, the containers fall back to the images' default user — root (verified for all
app images) — which rootless Podman maps to your unprivileged host service account, so
it can write the bind mounts and is **still unprivileged on the host** — the same
security posture Docker gets from `user: 10000`, reached a different way. (Setting
`CONTAINER_UID_GID=0:0` explicitly is equivalent.)

> **WARNING:** running as container-root (commented-out `CONTAINER_UID_GID`, or `0:0`)
> is safe **only** under rootless Podman. Never do it under rootful Docker — that is
> real host root.

### 2. Init chown (`INIT_CHOWN`)

`sm-init` creates the directory tree the other containers bind-mount. Under Docker it
also `chown`s that tree to `CONTAINER_UID_GID` so the `user: 10000` services can write
it. Under rootless Podman a chown to `10000:10001` would translate through the user
namespace to a subordinate UID and break access — so **comment `INIT_CHOWN` out**
(unset and empty are equivalent; init skips the chown). `sm-init` (container-root =
your account) creates the tree already owned by your account, and every app container
can write it.

### 3. Directory creation & bind mounts (`APP_ROOT`)

`APP_ROOT` (default `/opt/saltworks`) is the **application root**: the one directory a
human creates. `sm-init` mounts it and provisions the whole application tree beneath it
(`${APP_ROOT}/saltminer/{app,config,logs,python,scripts}`), which the other services
bind-mount.

* **`APP_ROOT` must exist before the first run.** Under Podman a missing `APP_ROOT`
  fails hard at mount time (`no such file or directory`) — the desired behavior.
  Docker instead **auto-creates missing bind sources** (root-owned), so a missing or
  mistyped `APP_ROOT` cannot be detected from inside a container there; create it
  deliberately.
* Under rootless Podman it must be **owned/writable by the account running podman** —
  `sm-init` checks this and exits with a clear `ERROR: APP_ROOT is not writable`
  instead of a cryptic `mkdir`/`chown` failure.
* Relative values (e.g. `.`) also work and resolve against the compose file's
  directory.

Even with the `init:` entries removed from `depends_on`, provisioning still wins the
race: `podman-compose` creates and starts services sequentially in file order, and
`init` is the first service, so the tree exists (a few ms of `mkdir`) before the app
containers are created. Services also carry `restart: always` and healthchecked
startup, so they converge even if a first attempt races.

### 4. The one required compose edit: `depends_on` and the exited init

Compose long-form conditions behave differently per engine (validated):

| `depends_on` condition                    | Docker Compose v2 | podman-compose 1.6 |
| ----------------------------------------- | ----------------- | ------------------- |
| `service_healthy` (running dependency)    | honored           | honored — health-gating between `api`/`ui-api` etc. works |
| `service_completed_successfully` (oneshot)| honored           | **breaks the stack** |

podman-compose maps every `depends_on` entry onto podman's `--requires`, and podman
requires those containers to be **running** at start time. A completed-and-exited
oneshot (`sm-init`, exit 0) can never satisfy that, so dependents fail with
`... is not a valid container, cannot be used as a dependency` / `container state
improper`, and `up` hangs waiting. **Delete the `init:` entries from `depends_on`
when running under Podman.** Everything else in the compose file is engine-portable.

### 5. SELinux volume labels (`:z`)

On RHEL / Rocky / Alma / Fedora, SELinux is enforcing by default. A bind-mounted host
directory keeps its original SELinux label, which the container process
(`container_t`) is not allowed to write — you get `Permission denied` even when uid/gid
ownership is correct (look for `AVC denied` in `/var/log/audit/audit.log`).

Every bind mount in `docker-compose.yml` therefore carries a `:z` suffix, which relabels
the directory to a shared container label (`container_file_t`). `:z` (shared) is used
rather than `:Z` (private) because `config` and `logs` are mounted by multiple
containers. On non-SELinux hosts the suffix is simply ignored, so it is safe on Docker
too.

A related, less obvious SELinux effect: binaries under `/home` (label `user_home_t`)
**cannot be executed by systemd services** — the denial is dontaudit-suppressed, so it
surfaces only as `status=203/EXEC, Permission denied`. This is why podman-compose must
be installed to a system path (next section).

### 6. Privileged ports (80 / 443)

Rootless Podman publishes ports through a host-side helper (`rootlessport` on a named
bridge network like `saltminer`), and binding <1024 requires privilege **in the host's
initial user namespace**. Tested on podman 5.8 — the commonly suggested capability
workarounds **do not work** with rootless port publishing:

* `AmbientCapabilities=CAP_NET_BIND_SERVICE` on a systemd unit: the cap does not
  survive podman's re-exec into its rootless namespace; the bind still fails.
* `setcap cap_net_bind_service+ep` on the helper binary: cap lands in the wrong user
  namespace; the bind still fails. (Red Hat explicitly recommends against setcap on
  podman binaries.)
* "Podman 5 defaults to pasta": true, but pasta only publishes ports in the *default*
  network mode. On a named bridge network (required here for inter-container DNS),
  ports still go through `rootlessport`.

Working options:

1. **Recommended:** publish high ports via `.env` (`NGINX_HTTP_PORT=8080`,
   `NGINX_HTTPS_PORT=8443`) and forward 80/443 with the host firewall (firewalld
   commands in the TL;DR). Note: forward-ports apply to traffic arriving on the wire —
   LAN clients use standard `https://host/`, but **on-host** access must use `:8443`.
2. Lower `net.ipv4.ip_unprivileged_port_start` (works, but weakens the protected-port
   boundary host-wide; some security policies disallow it).

### 7. Compose provider (RHEL 9 / CentOS Stream 9)

Neither `docker-compose` nor `podman-compose` is packaged in the base repos (EPEL has
them, if it is acceptable in your environment). The validated no-EPEL route:

```sh
sudo dnf -y install container-tools podman-docker python3-pip
sudo python3 -m pip install pipx
sudo PIPX_HOME=/opt/pipx PIPX_BIN_DIR=/usr/local/bin python3 -m pipx install podman-compose
```

Install **system-wide** (`/usr/local/bin`), not per-user: a `~/.local/bin` install
cannot be executed by systemd units under SELinux (see §5), which breaks boot
persistence. `podman-docker` additionally provides a `docker` CLI shim
(`touch /etc/containers/nodocker` silences its banner).

### 8. Memory limits

`docker-compose.yml` uses the portable short-form keys `mem_limit` / `mem_reservation`
rather than the Swarm-only `deploy.resources` block (which `podman-compose` silently
ignores). Enforcing these under **rootless** Podman requires **cgroups v2 with systemd
delegation** — the default on Fedora 31+ and RHEL 9+.

### 9. Network

The `saltminer` network is declared `external`, so create it once before the first
`up`:

```sh
podman network create saltminer
```

(If another compose project on the host — e.g. a local ELK dev stack — declares the
same network with `external: false`, let *that* project own/create it and bring it up
first.)

## Boot persistence

Rootless containers do not come back after a reboot by themselves — `restart: always`
only applies while the user's podman is up. The validated pattern:

1. Dedicated service account, with lingering (user manager runs without login and
   starts at boot):
   ```sh
   sudo useradd --create-home saltminer
   sudo loginctl enable-linger saltminer
   ```
2. A systemd **user** unit that runs `podman-compose up -d` at boot:
   ```ini
   # ~saltminer/.config/systemd/user/saltminer-sm.service
   [Unit]
   Description=SaltMiner stack (rootless podman-compose)
   Wants=network-online.target
   After=network-online.target

   [Service]
   Type=oneshot
   RemainAfterExit=yes
   WorkingDirectory=/opt/saltworks
   ExecStart=/usr/local/bin/podman-compose -f /opt/saltworks/docker-compose.yml --env-file /opt/saltworks/.env up -d
   ExecStop=/usr/local/bin/podman-compose -f /opt/saltworks/docker-compose.yml --env-file /opt/saltworks/.env down
   TimeoutStartSec=1800

   [Install]
   WantedBy=default.target
   ```
   ```sh
   sudo -u saltminer env XDG_RUNTIME_DIR=/run/user/$(id -u saltminer) \
     systemctl --user enable --now saltminer-sm
   ```

Notes from testing:
* Interacting with a lingered account's user systemd needs
  `XDG_RUNTIME_DIR=/run/user/<uid>` set (as above), and commands run via `sudo -u`
  should be issued from a directory the account can read (not your own `$HOME`).
* Do **not** try to run the stack from a *system* systemd unit with `User=` — rootless
  podman depends on the per-user runtime dir/session and the setup is fragile.

## Misc lessons learned (podman-compose)

* Quote `restart: "no"` in YAML — unquoted `no` parses as boolean false and
  podman-compose passes `--restart=False`, which podman rejects.
* Container healthchecks that probe `localhost` fail if the service binds a specific
  interface (e.g. Elasticsearch with `network.host=_site_`) — probe the service's own
  hostname instead.
* `podman-compose up` output is quiet during long image pulls; it has not hung.

## Quick checklist

- [ ] `APP_ROOT` (e.g. `/opt/saltworks`) exists, owned by the podman service account
- [ ] `.env`: `CONTAINER_UID_GID` and `INIT_CHOWN` commented out, `APP_ROOT=/opt/saltworks`,
      `NGINX_HTTP_PORT=8080`, `NGINX_HTTPS_PORT=8443`
- [ ] Compose edit: `init:` entries removed from all `depends_on`
- [ ] `podman network create saltminer` (or an owning project brings it up first)
- [ ] Firewall forward-ports 80→8080 / 443→8443 (+ open 8080/8443)
- [ ] podman-compose installed system-wide (pipx → `/usr/local/bin`)
- [ ] Service account lingering + user unit enabled for boot persistence
- [ ] cgroups v2 (for memory limits) — Fedora 31+ / RHEL 9+
- [ ] `podman-compose up -d`
