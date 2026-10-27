import logging
import sys

from Core.Application import Application
from Core.ApplicationExceptions import ApplicationConfigurationException
from Core.ApplicationSettings import ApplicationSettings
from Core.Agent import Agent, AgentArgs
from Core.FodClient import FodClient, FodClientConfigurationException
from Sources.SyncWorker import SyncWorkerFactory, SyncQueueStage


def parse_bool(value:str, default:bool=True) -> bool:
    '''
    Intuitive truthiness for a command-line flag.  False for an empty string and for anything
    starting with f/n/0/off ("false", "False", "no", "0", "off"); True otherwise.

    This replaces a plain bool(sys.argv[1]), which is True for *every* non-empty string - so
    "RunAgentService.py false" started a never-ending service and one-shot mode was unreachable.
    '''
    if value is None:
        return default
    v = value.strip().lower()
    if v == "":
        return False
    return not (v[0] in ("f", "n", "0") or v.startswith("off"))


def _to_limit(value, setting:str, key:str):
    """A non-negative int from hand-edited config, or None (logged) when it is not one."""
    try:
        count = int(value)
    except (TypeError, ValueError):
        logging.error("Ignoring %s for '%s': '%s' is not a number.", setting, key, value)
        return None
    if count < 0:
        logging.error("Ignoring %s for '%s': %s is negative.", setting, key, count)
        return None
    return count


def _credential_count(settings:ApplicationSettings, source_name:str, source_type:str) -> int:
    """How many credentials a source holds.  Only FOD sources hold a key pool; anything else counts 1.
    An invalid FOD key list logs ERROR and counts 1, so one bad file does not stop the agent - its
    FodClient fails on its own when a worker builds it."""
    if source_type != "FOD":
        return 1
    try:
        return max(1, len(FodClient.CredentialIds(settings, source_name)))
    except (ApplicationConfigurationException, FodClientConfigurationException) as ex:
        logging.error("Cannot count the credentials of source '%s', counting 1: %s", source_name, ex)
        return 1


def ResolveSourceNameLimits(settings:ApplicationSettings) -> dict[str, int]:
    """
    Worker cap per source name, resolved once at agent start.  First rule that applies, per source:

    1. SyncAgent.SourceNameWorkerLimits names the source: that value.
    2. SyncAgent.PerCredentialWorkerLimits names the source's type: that value times the number of
       distinct ClientIds in the source's key pool.
    3. Neither: no entry here, so the type limit in SourceWorkerLimits (if any) applies as before.

    Only ids are counted; no secret is kept or logged.
    """
    per_cred = settings.Get("SyncAgent", "PerCredentialWorkerLimits", {}) or {}
    by_name = settings.Get("SyncAgent", "SourceNameWorkerLimits", {}) or {}
    worker_count = settings.Get("SyncAgent", "WorkerCount", 4)
    names = list(settings.GetSourceNames())

    # A ClientId in two FOD sources is one server budget counted twice by per-credential caps.
    owners = {}
    for name in names:
        if settings.GetSource(name, "Source", "") != "FOD":
            continue
        try:
            for cid in FodClient.CredentialIds(settings, name):
                owners.setdefault(cid, []).append(name)
        except (ApplicationConfigurationException, FodClientConfigurationException):
            pass  # reported where the cap is counted
    for cid, sources in owners.items():
        if len(sources) > 1:
            logging.warning("ClientId '%s' is used by FOD sources %s; they share one server rate budget, "
                            "so per-credential worker caps count it once per source.", cid, ", ".join(sources))

    caps = {}
    for name in names:
        source_type = settings.GetSource(name, "Source", "")
        if name in by_name:
            cap = _to_limit(by_name[name], "source name worker limit", name)
            if cap is not None:
                caps[name] = cap
                logging.info("Source '%s' worker cap %d (SourceNameWorkerLimits), WorkerCount %s.", name, cap, worker_count)
                continue
        if source_type in per_cred:
            each = _to_limit(per_cred[source_type], "per-credential worker limit", source_type)
            if each is not None:
                count = _credential_count(settings, name, source_type)
                caps[name] = each * count
                logging.info("Source '%s' worker cap %d (%d per credential x %d key%s), WorkerCount %s.",
                             name, caps[name], each, count, "" if count == 1 else "s", worker_count)
    for name in by_name:
        if name not in names:
            logging.warning("SourceNameWorkerLimits names '%s', which is not a configured source.", name)
            cap = _to_limit(by_name[name], "source name worker limit", name)
            if cap is not None:
                caps[name] = cap
    return caps


def BuildAgentArgs(settings:ApplicationSettings) -> AgentArgs:
    """The sync agent's arguments, read from the SyncAgent section of the settings."""
    return AgentArgs(
        queue_index_pattern_tag = "sync",
        low_threshold_count = settings.Get("SyncAgent", "LowThresholdCount", 10),
        worker_count = settings.Get("SyncAgent", "WorkerCount", 4),
        polling_interval_secs = settings.Get("SyncAgent", "PollingIntervalSecs", 30),
        new_queue_item_stage = SyncQueueStage.SYNC,
        queue_batch_size=settings.Get("SyncAgent", "QueueBatchSize", 20),
        worker_error_threshold=settings.Get("SyncAgent", "WorkerErrorThreshold", 3),
        defunct_worker_timeout_secs=settings.Get("SyncAgent", "DefunctWorkerTimeoutSecs", 120),
        agent_id=settings.Get("SyncAgent", "AgentId", 1),
        # Per-source concurrency caps, e.g. {"FOD": 2} - for sources whose api rate limiting will not
        # tolerate the whole pool.  A source not listed here is uncapped, so SSC needs no entry.
        source_limits=settings.Get("SyncAgent", "SourceWorkerLimits", {}),
        # Per-source-name caps, resolved from SyncAgent.SourceNameWorkerLimits (a cap for one named
        # source) and SyncAgent.PerCredentialWorkerLimits (a cap per key times the source's key count).
        # Both ship empty; with neither set, only SourceWorkerLimits applies, as before.
        source_name_limits=ResolveSourceNameLimits(settings)
    )


def main():
    # run as service (poll until stopped) vs one-shot (drain the queue and exit)
    prm_service = parse_bool(sys.argv[1]) if len(sys.argv) > 1 else True
    prm_log_tag = sys.argv[2] if len(sys.argv) > 2 else None         # custom logging tag

    app = Application(loggingCustomTag=prm_log_tag)
    agent_args = BuildAgentArgs(app.Settings)
    agent = Agent(app, agent_args, SyncWorkerFactory())
    limits = agent.args.source_limits
    logging.info("Starting Sync Agent with %d workers in %s mode%s", agent.args.worker_count,
                 "service" if prm_service else "one-shot (exit when queue is empty)",
                 f", source worker limits: {limits}" if limits else "")
    if agent.args.source_name_limits:
        logging.info("Source name worker limits: %s", agent.args.source_name_limits)
    agent.run(stop_when_empty=not prm_service)
    logging.info("Sync Agent stopped")


if __name__ == "__main__":
    main()
