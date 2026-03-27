import logging

from Core.Application import Application
from Core.Agent import Agent, AgentArgs
from Sources.SyncWorker import SyncWorkerFactory, SyncQueueStage


def main():
    app = Application()
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
    logging.info("Starting Sync Agent with %d workers", agent.worker_count)
    agent.run()
    logging.info("Sync Agent stopped")

main()