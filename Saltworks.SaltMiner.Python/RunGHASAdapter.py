"""
RunGHASAdapter.py
=================
Entry point for the SaltMiner GHAS Source Adapter.

Usage:
    python RunGHASAdapter.py
    python RunGHASAdapter.py --first-load
    python RunGHASAdapter.py --log-level DEBUG

Arguments:
    --first-load    Ignored (accepted for interface compatibility). The adapter
                    detects first-load automatically from the absence of a
                    watermark in the state file. To force a full re-baseline,
                    delete the state file directly.
    --log-level     Logging verbosity: DEBUG, INFO, WARNING, ERROR (default: INFO)
"""

import argparse
import logging
import sys

from Core.Application import Application
from Sources.GHAS.GHASAdapter import GHASAdapter


def configure_logging(level: str = "INFO"):
    logging.basicConfig(
        level=getattr(logging, level.upper(), logging.INFO),
        format="%(asctime)s  %(levelname)-8s  %(name)s  %(message)s",
        datefmt="%Y-%m-%dT%H:%M:%S",
    )


def main():
    parser = argparse.ArgumentParser(description="SaltMiner GHAS Source Adapter")
    parser.add_argument(
        "--first-load",
        action="store_true",
        default=False,
        help="Accepted for compatibility - first-load is auto-detected from state file.",
    )
    parser.add_argument(
        "--log-level",
        default="INFO",
        choices=["DEBUG", "INFO", "WARNING", "ERROR"],
        help="Logging verbosity (default: INFO)",
    )
    args = parser.parse_args()

    configure_logging(args.log_level)
    logger = logging.getLogger("RunGHASAdapter")

    try:
        app = Application()
        adapter = GHASAdapter(app)
        adapter.run_sync(first_load=args.first_load)
        logger.info("GHAS adapter run completed successfully.")
        sys.exit(0)
    except KeyboardInterrupt:
        logger.info("Run interrupted by operator.")
        sys.exit(0)
    except Exception as exc:
        logger.critical("GHAS adapter run failed with unhandled exception: %s", exc, exc_info=True)
        sys.exit(1)


if __name__ == "__main__":
    main()
