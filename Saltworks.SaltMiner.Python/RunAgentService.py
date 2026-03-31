import logging
import os
import sys

from Core.Application import Application
from Core.Agent import Agent, AgentArgs
from Sources.SyncWorker import SyncWorkerFactory, SyncQueueStage

sourceNames = []
prog = os.path.splitext(os.path.basename(__file__))[0]
prmSourceName = None
prmAction = 'all'
prmLogInstance = None
enableDropCheck = True
if len(sys.argv) > 1:
    prmSourceName = sys.argv[1]         # Source name
if len(sys.argv) > 2:
    prmAction = sys.argv[2]             # Action ('sync', 'loadqueue', 'checkdrop', 'all'), defaults to 'all'
if len(sys.argv) > 3:
    prmLogInstance = sys.argv[3]        # Custom logging instance

msg = "Usage:\n\npython3 RunSync.py src [action] [lognum]\n\n:src: Source name, i.e. SSC1\n:action: sync, loadqueue, checkdrop, or all, defaults to all\n:lognum: logging instance number, defaults to none"
if len(sys.argv) == 0:
    logging.warning(msg)
    exit(1)
if prmAction not in ['sync', 'loadqueue', 'all', 'checkdrop']:
    raise ValueError(f"Invalid action '{prmAction}', expected 'sync', 'loadqueue', 'checkdrop', or 'all'.")
app = Application(loggingInstance=prmLogInstance)
logging.info(f"[{prog}] Starting, processing {'all sources' if prmSourceName is None else 'source ' + prmSourceName}, using '{prmAction}' action.")


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