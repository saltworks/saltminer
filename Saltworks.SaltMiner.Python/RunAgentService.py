import logging
import sys

from Core.Application import Application
from Core.Agent import Agent, AgentArgs
from Sources.SyncWorker import SyncWorkerFactory, SyncQueueStage


def main():
    prm_service = bool(sys.argv[1]) if len(sys.argv) > 1 else True   # run as service until stopped
    prm_log_tag = sys.argv[2] if len(sys.argv) > 2 else None         # custom logging tag

    app = Application(loggingCustomTag=prm_log_tag)
    agent_args = AgentArgs(
        queue_index_pattern_tag = "sync",
        low_threshold_count = app.Settings.Get("SyncAgent", "LowThresholdCount", 10),
        worker_count = app.Settings.Get("SyncAgent", "WorkerCount", 4),
        polling_interval_secs = app.Settings.Get("SyncAgent", "PollingIntervalSecs", 30),
        new_queue_item_stage = SyncQueueStage.SYNC,
        queue_batch_size=app.Settings.Get("SyncAgent", "QueueBatchSize", 20),
        worker_error_threshold=app.Settings.Get("SyncAgent", "WorkerErrorThreshold", 3)
    )
    agent = Agent(app, agent_args, SyncWorkerFactory())
    logging.info("Starting Sync Agent with %d workers", agent.args.worker_count)
    agent.run(stop_when_empty=not prm_service)
    logging.info("Sync Agent stopped")


if __name__ == "__main__":
    main()
