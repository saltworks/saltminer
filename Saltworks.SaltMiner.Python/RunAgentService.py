import logging
import os
import sys

from Core.Application import Application
from Core.Agent import Agent, AgentArgs
from Sources.SyncWorker import SyncWorkerFactory, SyncQueueStage

sourceNames = []
prog = os.path.splitext(os.path.basename(__file__))[0]
prm_service = True
prm_log_tag = None
if len(sys.argv) > 1:
    prm_service = bool(sys.argv[1])     # Run as service, defaults to True, will keep going until stopped
if len(sys.argv) > 2:
    prm_log_tag = sys.argv[2]     # Custom logging tag, defaults to none

app = Application(loggingCustomTag=prm_log_tag)

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
    agent.run(stop_when_empty=not prm_service)
    logging.info("Sync Agent stopped")

main()