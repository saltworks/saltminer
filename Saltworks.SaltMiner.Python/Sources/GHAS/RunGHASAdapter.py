"""
RunGHASAdapter.py
=================
Entry point for the SaltMiner GHAS Source Adapter.

Discovers all enabled GHAS instance configs in the platform Sources directory
(any .json file where `Source == "GHAS"` and `Enabled != false`), then runs each
instance independently and sequentially. A failure in one instance does not
prevent other instances from running.

Each config file represents one GHAS instance. The `SourceName` field in the
config is the key passed to `GHASAdapter` and to all `Settings.GetSource()`
calls. Convention: name the config file after its SourceName, e.g.
`ghas1.json` with `SourceName: "ghas1"`.

Usage:
    python RunGHASAdapter.py
    python RunGHASAdapter.py --first-load
    python RunGHASAdapter.py --instance ghas1          # Run a specific instance only
    python RunGHASAdapter.py --config-dir /path/to/Sources

Arguments:
    --first-load    Accepted for interface compatibility — first-load is
                    auto-detected from the absence of a watermark in each
                    instance's state file. To force a re-baseline, delete
                    or edit the relevant state file directly.
    --instance      Run only the named instance (matching SourceName) instead
                    of discovering all enabled instances.
    --config-dir    Directory to scan for GHAS instance configs.
                    Default: /etc/saltworks/saltminer-2.5.0/Sources
    --log-level     Logging verbosity. Default: INFO
"""

import argparse
import json
import logging
import os
import sys
from typing import List

# SaltMiner core bootstrap — adjust import path to match your deployment layout
from Core.Application import Application
from Sources.GHAS.GHASAdapter import GHASAdapter


def configure_logging(level: str = "INFO"):
    logging.basicConfig(
        level=getattr(logging, level.upper(), logging.INFO),
        format="%(asctime)s  %(levelname)-8s  %(name)s  %(message)s",
        datefmt="%Y-%m-%dT%H:%M:%S",
    )


def discover_ghas_instances(config_dir: str) -> List[str]:
    """
    Scan the given directory for GHAS instance configs.

    A config file qualifies as a GHAS instance if all of:
      - It is a .json file
      - Its 'Source' field equals 'GHAS' (case-sensitive type marker)
      - It has a non-empty 'SourceName' field
      - 'Enabled' is not explicitly set to False

    Returns a sorted list of SourceName values. A malformed config file is
    logged and skipped — it does not abort discovery.
    """
    log = logging.getLogger("RunGHASAdapter")

    if not os.path.isdir(config_dir):
        log.error("Config directory '%s' does not exist.", config_dir)
        return []

    instances = []
    for filename in sorted(os.listdir(config_dir)):
        if not filename.endswith(".json"):
            continue

        path = os.path.join(config_dir, filename)
        try:
            with open(path, "r", encoding="utf-8") as f:
                data = json.load(f)
        except (json.JSONDecodeError, OSError) as exc:
            log.warning("Skipping unreadable config '%s': %s", path, exc)
            continue

        if data.get("Source") != "GHAS":
            continue

        source_name = data.get("SourceName")
        if not source_name:
            log.warning(
                "Config '%s' has Source=GHAS but no SourceName — skipping.", path
            )
            continue

        if data.get("Enabled") is False:
            log.info("Instance '%s' is disabled in '%s' — skipping.", source_name, path)
            continue

        instances.append(source_name)

    return instances


def run_instance(source_name: str, first_load: bool) -> bool:
    """
    Run a single GHAS adapter instance. Returns True on success, False on failure.

    All exceptions are caught and logged so that a failure in one instance
    does not prevent other instances from running.
    """
    log = logging.getLogger("RunGHASAdapter")
    log.info("=== Starting instance: %s ===", source_name)
    try:
        app = Application()
        adapter = GHASAdapter(app, source_name)
        adapter.run_sync(first_load=first_load)
        log.info("=== Completed instance: %s ===", source_name)
        return True
    except Exception as exc:
        log.error(
            "Instance '%s' failed with unhandled exception: %s",
            source_name, exc, exc_info=True
        )
        return False


def main():
    parser = argparse.ArgumentParser(description="SaltMiner GHAS Source Adapter")
    parser.add_argument(
        "--first-load",
        action="store_true",
        default=False,
        help="Accepted for compatibility — first-load is auto-detected from state file.",
    )
    parser.add_argument(
        "--instance",
        default=None,
        help="Run only the named instance (matching SourceName) instead of all discovered ones.",
    )
    parser.add_argument(
        "--config-dir",
        default="/etc/saltworks/saltminer-2.5.0/Sources",
        help="Directory containing GHAS instance configs (default: /etc/saltworks/saltminer-2.5.0/Sources)",
    )
    parser.add_argument(
        "--log-level",
        default="INFO",
        choices=["DEBUG", "INFO", "WARNING", "ERROR"],
        help="Logging verbosity (default: INFO)",
    )
    args = parser.parse_args()

    configure_logging(args.log_level)
    log = logging.getLogger("RunGHASAdapter")

    try:
        if args.instance:
            instances = [args.instance]
            log.info("Running single specified instance: %s", args.instance)
        else:
            instances = discover_ghas_instances(args.config_dir)
            if not instances:
                log.warning(
                    "No enabled GHAS instances discovered in '%s'. Nothing to do.",
                    args.config_dir,
                )
                sys.exit(0)
            log.info(
                "Discovered %d enabled GHAS instance(s): %s",
                len(instances), ", ".join(instances),
            )

        failed = []
        for source_name in instances:
            ok = run_instance(source_name, args.first_load)
            if not ok:
                failed.append(source_name)

        if failed:
            log.error(
                "Run completed with %d failure(s) out of %d instance(s): %s",
                len(failed), len(instances), ", ".join(failed),
            )
            sys.exit(1)

        log.info("All %d instance(s) completed successfully.", len(instances))
        sys.exit(0)

    except KeyboardInterrupt:
        log.info("Run interrupted by operator.")
        sys.exit(0)
    except Exception as exc:
        log.critical(
            "Run script failed with unhandled exception: %s", exc, exc_info=True
        )
        sys.exit(1)


if __name__ == "__main__":
    main()
