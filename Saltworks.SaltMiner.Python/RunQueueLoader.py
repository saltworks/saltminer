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
Manually re-queue Project Version IDs for the sync agent, e.g. to push
difficult-to-process or failed items back through at elevated priority.

Usage:
  python3 RunQueueLoader.py <target_type> <src_name> <pvids> [-p N] [--force] [--reason TEXT] [--dry-run]

  target_type   SSC or FOD
  src_name      Source instance name, e.g. SSC1
  pvids         Comma-separated IDs, @path/to/file (one ID per line or comma-separated),
                or @all to queue every ID in the source (SSC and FOD only).
                Note @all is reserved: to read a file literally named "all", use @./all.
  -p, --priority  Integer, default 5. Lower numbers are processed first.
  --force         Bypass change detection during sync. Default False.
  --reason        Free text recorded on each queue document.
  --dry-run       Print what would be inserted; write nothing.
  --include-inactive  With @all, include inactive project versions (SSC only).

Examples:
  python3 RunQueueLoader.py SSC SSC1 12345,56789,101112 -p 1
  python3 RunQueueLoader.py SSC SSC1 12345
  python3 RunQueueLoader.py SSC SSC1 @stubborn_ids.txt -p 2 --force
  python3 RunQueueLoader.py SSC SSC1 @all -p 9
  python3 RunQueueLoader.py FOD FOD1 998877 --dry-run

Run with -h/--help for the same summary at the command line.
'''

import argparse
import logging
import sys

from Core.Application import Application
from Core.QueueClient import QueueClient, QueueClientException
from Sources.IdLoader import IdLoaderException, is_supported, iter_all_target_ids
from Sources.SyncWorker import SyncQueueType
from Utility.QueueLoader import load_queue_items

ALL_SENTINEL = "@all"

EPILOG = '''\
Examples:
  python3 RunQueueLoader.py SSC SSC1 12345,56789,101112 -p 1
  python3 RunQueueLoader.py SSC SSC1 12345
  python3 RunQueueLoader.py SSC SSC1 @stubborn_ids.txt -p 2 --force
  python3 RunQueueLoader.py SSC SSC1 @all -p 9
  python3 RunQueueLoader.py FOD FOD1 998877 --dry-run

"@all" is reserved - to read a file literally named "all", pass @./all instead.
'''


def parse_args(argv):
    parser = argparse.ArgumentParser(
        description="Manually re-queue Project Version IDs for the sync agent.",
        epilog=EPILOG,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument("target_type", help="SSC or FOD")
    parser.add_argument("src_name", help="Source instance name, e.g. SSC1")
    parser.add_argument("pvids", nargs="+",
                        help="Comma-separated IDs, @path/to/file (one ID per line or comma-separated), "
                             "or @all for every ID in the source")
    parser.add_argument("-p", "--priority", type=int, default=5,
                        help="Lower numbers are processed first. Default 5.")
    parser.add_argument("--force", action="store_true", default=False,
                        help="Bypass change detection during sync. Default False.")
    parser.add_argument("--reason", default=None,
                        help="Free text recorded on each queue document.")
    parser.add_argument("--dry-run", action="store_true", default=False,
                        help="Print what would be inserted; write nothing.")
    parser.add_argument("--include-inactive", action="store_true", default=False,
                        help="With @all, include inactive project versions (SSC only). Default False.")
    return parser.parse_args(argv)


def is_all_request(pvids: list) -> bool:
    '''True if the caller asked for every ID in the source ("@all", case-insensitive).'''
    return len(pvids) == 1 and pvids[0].strip().lower() == ALL_SENTINEL


def read_ids_file(path: str) -> list:
    '''Read IDs from a file, one per line or comma-separated, skipping blanks and # comments.'''
    ids = []
    with open(path) as fh:
        for line in fh:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            ids.extend(part.strip() for part in line.split(","))
    return ids


def normalize_pvids(pvids: list) -> list:
    '''
    Turn the raw argparse pvids list into a clean list of ID strings.  The "@all"
    sentinel is handled by the caller and never reaches here.

    A shell that splits an unquoted, comma-separated list produces multiple argv
    entries; joining them back on "," before splitting reconstructs the original
    list regardless of how the shell tokenized it. Surrounding [ ] are stripped
    defensively in case someone pastes a Python-style list.
    '''
    if len(pvids) == 1 and pvids[0].startswith("@"):
        raw_ids = read_ids_file(pvids[0][1:])
    else:
        joined = ",".join(pvids).strip("[]")
        raw_ids = joined.split(",")
    return [i.strip() for i in raw_ids if i.strip()]


def main(argv=None):
    logging.basicConfig(level=logging.INFO)
    args = parse_args(argv if argv is not None else sys.argv[1:])

    if not SyncQueueType.is_valid(args.target_type):
        logging.error("Invalid target_type '%s', expected one of SSC/FOD.", args.target_type)
        return 1
    if not args.src_name:
        logging.error("src_name is required.")
        return 1

    load_all = is_all_request(args.pvids)
    if load_all and not is_supported(args.target_type):
        logging.error("@all is not supported for target type '%s'.", args.target_type)
        return 1

    pvids = None
    if not load_all:
        try:
            pvids = normalize_pvids(args.pvids)
        except OSError as ex:
            logging.error("Unable to read ID file: %s", ex)
            return 1

        if len(pvids) == 0:
            logging.error("No IDs supplied after parsing input.")
            return 1

    app = Application()
    qcli = QueueClient(app, "sync")

    if load_all:
        reason = args.reason or f"RunQueueLoader full reload of {args.target_type} source {args.src_name}"
        # A generator, so IDs stream from the source into the queue a batch at a time.
        pvids = iter_all_target_ids(args.target_type, app_settings=app.Settings, src_name=args.src_name,
                                    include_inactive=args.include_inactive)
        logging.info("Queueing all %s IDs for source '%s'%s.", args.target_type, args.src_name,
                     " (including inactive)" if args.include_inactive else "")
    else:
        reason = args.reason or f"RunQueueLoader manual reinject of {len(pvids)} ID(s)"

    try:
        load_queue_items(
            qcli, pvids, args.target_type, args.src_name,
            priority=args.priority, force=args.force,
            source=args.target_type,
            change_reason=reason, dry_run=args.dry_run,
        )
    except QueueClientException as ex:
        logging.error("Queue insert failed: %s", ex)
        return 1
    except IdLoaderException as ex:
        logging.error("Unable to load IDs from source: %s", ex)
        return 1
    except ValueError as ex:
        logging.error("%s", ex)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
