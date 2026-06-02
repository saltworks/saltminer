# GHAS Adapter — Re-baseline & Deployment Procedure

This runbook covers deploying the rate-limit fix and performing a clean
re-baseline so SaltMiner holds only current, complete GHAS data.

---

## 0. Background — what this fix changes and why

A full run was under-reporting code-scanning by ~94%. Root cause (confirmed by
a single-repo isolation run): the adapter overran **GitHub's secondary rate
limit on the code-scanning endpoint** under high concurrency. The resulting
`403`s were **misclassified as "engine inaccessible"** and the scopes were
silently skipped while stale data was preserved — so the run summary blamed PAT
permissions when the real cause was throttling.

Proof: `om-clientfundingmanagement-svc/code_scanning` returned **0** alerts in
the full run but **1,724** when run alone (`--instance ghas3`, single repo).
Same code, same token, same query — the only variable was load.

What changed:

1. **Layered rate-limit handling** in `GHASClient` — request pacing + a
   dedicated low concurrency gate for code-scanning + header-driven backoff
   (`Retry-After` → `x-ratelimit-reset` → exponential fallback).
2. **`GHASRateLimitError`** — a secondary-limit failure is now distinct from a
   permission failure. It is **never** silently skipped; the scope keeps its
   state and is retried, and it is reported separately in the summary.
3. **Re-baseline mode** — when the state file is empty, the client automatically
   uses gentler code-scanning settings so a cold full pull completes in one pass.
4. **Archived-repo tombstone** — archived repos are purged from SaltMiner
   (empty Scan+Asset with `ReplaceIssues=True`) instead of leaving phantom rows.
5. **FIX-003** — FoundDate `+00:00Z` double-suffix corrected.
6. **Run-tag** on every queued doc + a companion orphan-cleanup script for
   repos deleted/renamed at GitHub (which the adapter can't enumerate to purge).
7. **Rewritten, categorized run summary** — rate-limited vs inaccessible vs
   archived-purged vs ingested vs clean, so the failure mode is never again
   misread.

All new config fields are **optional**; existing `ghas1`/`ghas2` configs run
unchanged on conservative defaults.

---

## 1. Deploy the code

Replace these files in the container (path:
`/usr/share/saltworks/saltminer-2.5.0/Sources/GHAS/`):

| Deliverable file              | Deploy as                | Applies to            |
|-------------------------------|--------------------------|-----------------------|
| `GHASClient.py`               | `GHASClient.py`          | **both** 3.4 and 3.5  |
| `GHASAdapter_3_5.py`          | `GHASAdapter.py`         | 3.5 deployments only  |
| `GHASAdapter_3_4.py`          | `GHASAdapter.py`         | 3.4 deployments only  |

The client is identical for both forks. Deploy the adapter that matches the
SaltMiner version on the host (3.4 uses `SmDataClient`; 3.5 uses `DataClient`).

`ghas_orphan_cleanup.py` runs from the **host** (outside the container), where
the `.env` with Elasticsearch credentials lives.

> Sanity check after copying: `python3 -m py_compile GHASClient.py GHASAdapter.py`
> from the code directory should produce no output.

---

## 2. (Optional) Tune rate-limit settings

Defaults are conservative and grounded in GitHub's published limits
(~900 points/min = ~15 GET/s ceiling per endpoint; code-scanning is paced well
under that because its server-side cost trips the CPU-time secondary limit
first). The defaults if you add nothing:

| Setting (config key)                       | Default | Meaning                                   |
|--------------------------------------------|--------:|-------------------------------------------|
| `CodeScanningConcurrencyLimit`             | 3       | max concurrent code-scanning requests     |
| `CodeScanningMinIntervalMs`                | 200     | ~5 req/s pacing on code-scanning          |
| `OtherEngineMinIntervalMs`                 | 0       | no pacing on dependabot/secret-scanning   |
| `RebaselineCodeScanningConcurrencyLimit`   | 2       | code-scanning concurrency during baseline |
| `RebaselineCodeScanningMinIntervalMs`      | 330     | ~3 req/s during baseline                  |
| `RebaselineSecondaryMaxAttempts`           | 10      | deep retry budget during baseline         |

See `ghas_sample_config.json` for a full example. **Start with defaults.** Only
tune if the run summary reports `RATE-LIMITED` scopes (then lower the
concurrency and/or raise the interval) or if runs are unacceptably slow and you
see zero rate-limiting (then you can raise concurrency).

---

## 3. Pre-flight check (single repo) — optional but recommended

Confirm code-scanning ingests under the new code before touching production
data. Using the `ghas3` single-repo technique:

```bash
cd /etc/saltworks/saltminer-2.5.0/Sources/
# build a one-repo test config from the real onbe config, isolated state:
python3 - <<'EOF'
import json
real = json.load(open("ghas2.json"))
real["StateFile"]      = "/tmp/ghas-test-state.json"
real["ExcludePattern"] = r"^(?!Onbe/om-clientfundingmanagement-svc$).*"
real["ExcludeRepos"]   = []; real["ExcludeTopics"] = []
real["SourceName"]     = "ghas3"; real["Enabled"] = True
json.dump(real, open("ghas3.json","w"), indent=4)
EOF
rm -f /tmp/ghas-test-state.json

cd /usr/share/saltworks/saltminer-2.5.0
python3 -m Sources.GHAS.RunGHASAdapter --instance ghas3 --log-level DEBUG 2>&1 | tee /tmp/ghas3-test.log
grep -E "Queueing [0-9]+ alerts.*code_scanning|RATE-LIMITED|SYNC SUMMARY" /tmp/ghas3-test.log
```

Expect: `Queueing 1724 alerts ... code_scanning`. Clean up afterward:

```bash
rm -f /etc/saltworks/saltminer-2.5.0/Sources/ghas3.json /tmp/ghas-test-state.json /tmp/ghas3-test.log
```

---

## 4. Re-baseline (the clean rebuild)

A re-baseline forces every scope to first-sync and re-queue with
`ReplaceIssues=True`, refreshing/cleaning all reachable data, AND tombstones
every archived repo (purging the phantom rows). It engages automatically when
the state file is empty.

**Per instance** (do one org at a time so you can read each summary cleanly):

```bash
# 1. Locate the instance's state file (StateFile in its config; default
#    ./ghas-state-<instance>.json relative to the run cwd).
#    Delete it to force a full re-baseline:
rm -f <path-to>/ghas-state-ghas2.json

# 2. Run that instance. Re-baseline mode engages automatically (you'll see
#    "RE-BASELINE run" + "RE-BASELINE MODE engaged" in the log).
cd /usr/share/saltworks/saltminer-2.5.0
python3 -m Sources.GHAS.RunGHASAdapter --instance ghas2 --log-level INFO 2>&1 | tee /tmp/ghas2-rebaseline.log
```

### What to expect on a re-baseline run

- **Slower code-scanning.** At ~3 req/s with concurrency 2, a large org's
  code-scanning phase takes meaningfully longer than the old (broken, fast,
  empty) runs. This is intended — it's the cost of staying under the secondary
  limit. Plan for the run to take substantially longer than the ~25 min the old
  Onbe run took, and prefer off-peak hours for the first one.
- **Archived repos tombstoned.** Every archived repo is purged (all engines).
  This clears the known phantom rows (e.g. `OnbeEast/demoaccessibilitytesting`).
- **The summary is the verdict.** Read the `GHAS SYNC SUMMARY` block at the end:

```
  ingested w/ alerts : <n>   (<N> alerts queued)
  clean (heartbeat)  : <n>
  archived (purged)  : <n>
  RATE-LIMITED       : 0     ← want zero; if >0 see §5
  INACCESSIBLE       : <n>   ← genuine permission/enablement only now
  failed (error)     : 0
  Alerts by engine   : code_scanning: <N>   secret_scanning: <N>   dependabot: <N>
```

### Convergence

Re-baseline mode is designed to complete code-scanning **in one pass**. Because
GitHub's secondary limit is dynamic and can fire for undisclosed reasons, this
is best-effort, not guaranteed. If the summary shows `RATE-LIMITED > 0`, those
specific scopes were **not** collected but **kept their state** — simply run the
instance again (do NOT delete state this time) and they'll be retried. Repeat
until `RATE-LIMITED` is 0. Each rerun only touches scopes that still need it.

---

## 5. If you still see RATE-LIMITED scopes

The summary prints the exact rate-limited scopes and a tuning hint. Options, in
order of preference:

1. **Re-run the instance** (without deleting state) — stragglers retry and the
   run is now much smaller. Often sufficient.
2. **Lower** `RebaselineCodeScanningConcurrencyLimit` (e.g. 2 → 1) and/or
   **raise** `RebaselineCodeScanningMinIntervalMs` (e.g. 330 → 500), then re-run.
3. These are throttling knobs, **not** permission settings — do not start
   changing the PAT. A high `RATE-LIMITED` count is explicitly *not* a
   permissions problem (that's the whole point of separating it from
   `INACCESSIBLE`).

A high `INACCESSIBLE` count (separate category) *would* indicate a genuine
PAT/SSO/approval/scope problem — see architecture doc §9.3.1.

---

## 6. Validate against GitHub

After `RATE-LIMITED` reaches 0, confirm counts line up with GitHub's
authoritative open-alert totals (the org endpoint used by the customer's
`pull_github.sh`):

```bash
# per engine, per org — authoritative OPEN counts
gh api --paginate "/orgs/Onbe/code-scanning/alerts?state=open&per_page=100" --jq '.[].number' | wc -l
gh api --paginate "/orgs/Onbe/dependabot/alerts?state=open&per_page=100"    --jq '.[].number' | wc -l
gh api --paginate "/orgs/Onbe/secret-scanning/alerts?state=open&per_page=100" --jq '.[].number' | wc -l
```

Compare to the `Alerts by engine` line in the run summary and to the SaltMiner
UI counts. They should match closely (small differences can be archived repos,
which GitHub's org endpoint excludes but which you've now tombstoned, or alerts
that changed state between the pull and the run).

---

## 7. Purge orphaned repos (deleted / renamed / transferred)

The adapter cannot tombstone a repo it can no longer see in the org listing.
Those leftover docs are cleaned out of band with the run-tag the adapter now
stamps on every document. Run from the **host** (where the `.env` is):

```bash
# DRY RUN first — reports what would be deleted, deletes nothing:
python3 ghas_orphan_cleanup.py --instance ghas2

# review the per-repo list, then apply:
python3 ghas_orphan_cleanup.py --instance ghas2 --apply
```

It deletes only docs whose run-tag is older than the latest run (minus a safety
margin, default 120 min), scoped to GHAS indices. It refuses to run if it
can't positively identify a latest run, so it can't wipe pre-run-tag data by
accident. Run it **after** a successful re-baseline so the latest-run marker is
current.

> Note: orphan cleanup only has effect once at least one run has stamped the new
> run-tag. The first post-deploy run establishes the baseline tag; orphans from
> before the deploy are cleaned on the first `--apply` after that run.

---

## 8. Known caveat — ephemeral state file (FIX-002, deferred)

The state file is currently **not** bind-mounted, so it does not survive
container recreation. Consequences:

- If the container is recreated, the next run sees an empty state file and
  performs a **full re-baseline automatically** (slower run, re-fires archived
  tombstones). This is correct but expensive and unintended if it happens
  silently.
- Therefore: treat the re-baseline you perform here as a **snapshot**, not a
  durable steady state, until a bind mount is added.

**Recommended follow-up (separate change):** bind-mount the state directory and
set `StateFile` to a path on the mounted volume, so state persists across
container restarts and runs stay incremental. Until then, be aware that a
restart = an involuntary (but safe) re-baseline.

---

## 9. Rollback

If anything misbehaves, restore the previous `GHASClient.py` / `GHASAdapter.py`
and re-run. The state file format is unchanged (schema v2; the new code only
*reads* `watermark_count` and *adds* `clear_watermark`, both backward
compatible), so rolling the code back does not require touching state. Note
that any archived-repo tombstones already applied remain applied (the phantom
rows stay purged), which is the desired outcome regardless.
```
