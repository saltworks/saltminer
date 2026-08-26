''' --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
'''

'''
TemplateRunner - entry glue.

Wires config, client, loader, and adapter, and exposes run_sync(first_load=...)
so RunPythonAdapter.py can construct and run this source exactly like the
shipped ones (positional CLI: source, first_load, logging_instance, instance).
When copying the template, add your elif line there, passing the optional
instance argument through:

    elif prm_source.lower() == "yourname":
        adapter = YourNameRunner(app, source_name=prm_instance)

The instance name (the SourceName value in the source config, ex "SNYK2") is
what makes a second deployment of the same source possible: one config file
per instance, same adapter code, and the CLI names which one to run.  When the
CLI does not name one, the runner defaults to {SOURCE}1.

Which run path executes depends on the adapter's processing model (declared in
TemplateAdapter's docstring, not in config):

- run_sync():       threaded single-asset path.  SourceLoader gates and fills
                    the SMQ queue, then Core.Agent + SourceWorker drain it.
- run_sync_batch(): non-threaded path (incl. all batch adapters).  SourceLoader
                    gates and drives the adapter directly; no queue, no
                    workers.  A copied non-threaded adapter renames this to
                    run_sync and deletes the threaded body.

Run this file directly for the no-op template check: it maps the mock client's
canned payloads through the real DTO validation without sending anything.

    python Sources/Template/TemplateRunner.py
'''

import logging
import os
import sys

# Repo root, three levels up from Sources/Template/, so the mock run works standalone.
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

from Core.Agent import Agent, AgentArgs
from Core.Application import Application

from Sources.Template.TemplateAdapter import SourceLoader, SourceWorkerFactory, TemplateAdapter
from Sources.Template.TemplateClient import MockTemplateClient, TemplateClient

logger = logging.getLogger(__name__)

# Source identity, matching the Source value in the config.  The default
# instance is {SOURCE}1 - the first config file's SourceName by convention.
SOURCE = "TEMPLATE"
DEFAULT_SOURCE_NAME = f"{SOURCE}1"


class TemplateRunner:
    '''
    :app: Application instance
    :source_name: instance to run - the SourceName value of one source config
        file (ex "TEMPLATE2" for a second instance).  None defaults to
        DEFAULT_SOURCE_NAME ({SOURCE}1).
    :client: optional pre-built client (a MockTemplateClient for the no-op run)
    :dry_run: map and validate only, send nothing
    '''

    def __init__(self, app: Application, source_name: str = None,
                 client=None, dry_run: bool = False):
        source_name = source_name or DEFAULT_SOURCE_NAME
        self._app = app
        self._source_name = source_name
        self._dry_run = dry_run
        self._client = client or TemplateClient(app.Settings, source_name)
        self._adapter = TemplateAdapter(app, source_name, dry_run=dry_run)
        self._loader = SourceLoader(app, self._client, self._adapter, source_name)

    # -- threaded single-asset path (the template's declared classification) --

    def run_sync(self, first_load: bool = False):
        '''
        Gate + load the SMQ queue, then drain it with a worker pool.  Each
        worker processes one asset per queue item and owns its own DataClient.
        '''
        settings = self._app.Settings
        loaded = self._loader.load_queue(first_load=first_load)
        if loaded == 0:
            logger.info("[TemplateRunner] Nothing to process - queue not started.")
            return
        agent_args = AgentArgs(
            queue_index_pattern_tag=self._source_name.lower(),
            worker_count=int(settings.GetSource(self._source_name, "WorkerCount", 5)),
            worker_error_threshold=int(
                settings.GetSource(self._source_name, "WorkerErrorThreshold", 3)),
            polling_interval_secs=int(
                settings.GetSource(self._source_name, "PollingIntervalSecs", 30)),
            agent_id=settings.GetSource(self._source_name, "AgentId", 1),
        )
        agent = Agent(self._app, agent_args,
                      SourceWorkerFactory(self._source_name, type(self._client)))
        logger.info("[TemplateRunner] Draining %s queued asset(s) with %s worker(s).",
                    loaded, agent_args.worker_count)
        agent.run(stop_when_empty=True)

    # -- non-threaded path (incl. all batch adapters) -------------------------

    def run_sync_batch(self, first_load: bool = False) -> dict:
        '''
        Gate + process in this thread, one asset at a time, no queue machinery.
        For a non-threaded adapter, rename this to run_sync and delete the
        threaded method above.
        '''
        try:
            return self._loader.run(first_load=first_load)
        finally:
            self._adapter.close()


def main():
    ''' No-op template check: mock client + dry run, nothing sent anywhere. '''
    app = Application()
    runner = TemplateRunner(app, client=MockTemplateClient(source_name=DEFAULT_SOURCE_NAME),
                            dry_run=True)
    summary = runner.run_sync_batch(first_load=True)
    logging.info("[TemplateRunner] Mock dry run complete: %s", summary)
    print(f"Mock dry run complete: {summary}")


if __name__ == "__main__":
    main()
