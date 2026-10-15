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
Entry point for the Tanium source adapter.

    python RunTaniumAdapter.py
    python RunTaniumAdapter.py --log-level DEBUG
    python RunTaniumAdapter.py --resume-from 4210
    python RunTaniumAdapter.py --force

Run it from the Saltworks.SaltMiner.Python directory - Application locates
Config/ relative to the working directory.

Exit codes: 0 ok, 1 adapter or client failure, 130 interrupt.
'''

import argparse
import logging
import sys

from Core.Application import Application
from Sources.Tanium.TaniumAdapter import TaniumAdapter, TaniumAdapterExceptions
from Sources.Tanium.TaniumClient import TaniumException


def configure_logging(level: str = "INFO"):
    logging.basicConfig(
        level=getattr(logging, level.upper(), logging.INFO),
        format="%(asctime)s  %(levelname)-8s  %(name)s  %(message)s",
        datefmt="%Y-%m-%dT%H:%M:%S",
    )


def main():
    parser = argparse.ArgumentParser(description="SaltMiner Tanium Source Adapter")
    parser.add_argument("--source-name", default="Tanium",
                        help="Config source section to read. Default: Tanium.")
    parser.add_argument("--resume-from", default=None, metavar="ID",
                        help="Resume enumeration after this endpoint id, using an id GT filter "
                             "and no cursor. Take the value from a previous run's checkpoint.")
    parser.add_argument("--force", action="store_true", default=False,
                        help="Set force on every work item.")
    parser.add_argument("--log-level", default="INFO",
                        choices=["DEBUG", "INFO", "WARNING", "ERROR"],
                        help="Logging verbosity (default: INFO)")
    args = parser.parse_args()

    configure_logging(args.log_level)
    logger = logging.getLogger("RunTaniumAdapter")

    try:
        app = Application()
        adapter = TaniumAdapter(app, source_name=args.source_name)
        result = adapter.run_sync(resume_from=args.resume_from, force=args.force)
        # A run that errored every endpoint still "completes"; say so plainly rather
        # than exiting 0 on a run that loaded nothing.
        if result["errored"] and not result["completed"]:
            logger.error("Tanium adapter run failed: %s endpoint(s) errored, none completed.",
                         result["errored"])
            sys.exit(1)
        if result["errored"]:
            logger.warning("Tanium adapter run completed with %s errored endpoint(s).",
                           result["errored"])
        logger.info("Tanium adapter run completed: %s endpoint(s), %s issue(s).",
                    result["completed"], result["issues"])
        sys.exit(0)
    except KeyboardInterrupt:
        logger.warning("Interrupted.")
        sys.exit(130)
    except (TaniumAdapterExceptions, TaniumException) as e:
        logger.error("Tanium adapter failed: [%s] %s", type(e).__name__, e)
        sys.exit(1)
    except Exception as e:                               # noqa: BLE001 - top level guard
        logger.exception("Unexpected failure: [%s] %s", type(e).__name__, e)
        sys.exit(1)


if __name__ == "__main__":
    main()
