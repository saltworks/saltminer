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
Enumeration of every syncable target ID in a source system, for callers that need
to (re)load a whole source into a queue.

The target types are spelled as literals rather than imported from Sources.SyncWorker:
SyncWorker imports the SSC/FOD extractors, which import this module, so importing it
back would close an import cycle.
'''

import logging

from Core.FodClient import FodClient
from Core.SscClient import SscClient

SSC = "SSC"
FOD = "FOD"
SUPPORTED_TARGET_TYPES = (SSC, FOD)


class IdLoaderException(Exception):
    pass


def is_supported(target_type: str) -> bool:
    '''True if all-ID enumeration is implemented for this target type.'''
    return target_type in SUPPORTED_TARGET_TYPES


def iter_all_target_ids(target_type: str, app_settings=None, src_name: str = None,
                        client=None, include_inactive: bool = False, logger=None):
    '''
    Generator yielding every target ID in the source, in the source's own order.

    :target_type: SSC or FOD (see SUPPORTED_TARGET_TYPES)
    :app_settings: ApplicationSettings, required only when client is None
    :src_name: source instance name, e.g. SSC1, required only when client is None
    :client: an existing SscClient/FodClient to use.  When supplied it is used as-is and
        left open for the caller; when omitted one is built here and cleaned up on exit.
    :include_inactive: SSC only - include inactive project versions.  FOD's release API
        exposes no equivalent filter, so the flag is ignored (and logged) for FOD.
    :logger: optional Logger, defaults to this module's logger

    IDs are yielded as the source returns them (int for SSC, int for FOD) - callers that
    need strings should stringify, as Utility.QueueLoader does.

    Cleanup of a locally created client happens in a finally block, so a caller that
    abandons the generator early should close() it (or let it fall out of scope) to
    release the SSC auth token.
    '''
    log = logger or logging.getLogger(__name__)

    if not is_supported(target_type):
        raise IdLoaderException(
            f"Loading all IDs is not supported for target type '{target_type}' "
            f"(supported: {', '.join(SUPPORTED_TARGET_TYPES)}).")

    owned = client is None
    if owned:
        if app_settings is None or not src_name:
            raise IdLoaderException("app_settings and src_name are required when no client is supplied.")
        client = SscClient(app_settings, src_name) if target_type == SSC else FodClient(app_settings, src_name)
        log.debug("IdLoader created a local %s for source '%s'.", type(client).__name__, src_name)

    try:
        if target_type == SSC:
            for pv in client.GetProjectVersionsGenerator(fields='id', inactive=include_inactive):
                yield pv['id']
        else:
            if include_inactive:
                log.debug("include_inactive is not supported for FOD releases - ignoring.")
            scroller = client.GetReleases(fields='releaseId', scroller=True)
            while True:
                results = scroller.Results
                if not results:
                    break
                for itm in results:
                    yield itm['releaseId']
                scroller.GetNext()
                # GetNext leaves Results untouched when the scroll is exhausted or the call
                # fails, so identity is the only reliable "no more pages" signal.
                if scroller.Results is results:
                    break
    finally:
        if owned:
            cleanup = getattr(client, "Cleanup", None)
            if callable(cleanup):
                try:
                    cleanup()
                except Exception:
                    log.warning("IdLoader failed to clean up its local %s.", type(client).__name__, exc_info=True)


def get_all_target_ids(target_type: str, app_settings=None, src_name: str = None,
                       client=None, include_inactive: bool = False, logger=None) -> list:
    '''
    List form of iter_all_target_ids(), for callers that want the whole set in memory.
    Arguments are identical.
    '''
    return list(iter_all_target_ids(target_type, app_settings=app_settings, src_name=src_name,
                                    client=client, include_inactive=include_inactive, logger=logger))
