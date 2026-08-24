import logging
import sys

from Core.Application import Application
from Core.Agent import Agent, AgentArgs
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


def main():
    # run as service (poll until stopped) vs one-shot (drain the queue and exit)
    prm_service = parse_bool(sys.argv[1]) if len(sys.argv) > 1 else True
    prm_log_tag = sys.argv[2] if len(sys.argv) > 2 else None         # custom logging tag

    app = Application(loggingCustomTag=prm_log_tag)
    agent_args = AgentArgs(
        queue_index_pattern_tag = "sync",
        low_threshold_count = app.Settings.Get("SyncAgent", "LowThresholdCount", 10),
        worker_count = app.Settings.Get("SyncAgent", "WorkerCount", 4),
        polling_interval_secs = app.Settings.Get("SyncAgent", "PollingIntervalSecs", 30),
        new_queue_item_stage = SyncQueueStage.SYNC,
        queue_batch_size=app.Settings.Get("SyncAgent", "QueueBatchSize", 20),
        worker_error_threshold=app.Settings.Get("SyncAgent", "WorkerErrorThreshold", 3),
        defunct_worker_timeout_secs=app.Settings.Get("SyncAgent", "DefunctWorkerTimeoutSecs", 120),
        agent_id=app.Settings.Get("SyncAgent", "AgentId", 1),
        # Per-source concurrency caps, e.g. {"FOD": 2} - for sources whose api rate limiting will not
        # tolerate the whole pool.  A source not listed here is uncapped, so SSC needs no entry.
        source_limits=app.Settings.Get("SyncAgent", "SourceWorkerLimits", {})
    )
    agent = Agent(app, agent_args, SyncWorkerFactory())
    limits = agent.args.source_limits
    logging.info("Starting Sync Agent with %d workers in %s mode%s", agent.args.worker_count,
                 "service" if prm_service else "one-shot (exit when queue is empty)",
                 f", source worker limits: {limits}" if limits else "")
    agent.run(stop_when_empty=not prm_service)
    logging.info("Sync Agent stopped")


if __name__ == "__main__":
    main()
