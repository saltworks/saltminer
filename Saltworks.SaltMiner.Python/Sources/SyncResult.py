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

# Shared result contract for the single-target sync path, used by both SSC and FOD.

class SyncResult(object):
    '''
    What a single-project-version sync did, for the refresh stage that runs straight after it.

    :synced: whether this run actually re-loaded the project version.  False when nothing had changed
    (see needsReset in __ProcessOne) or the PV was skipped - in that case issue_count means nothing,
    because the data in elasticsearch is from an earlier run and this run wrote none of it.
    :issue_count: issues indexed for the project version, zero-issue placeholder records included.
    The refresh stage uses it as the expected count: an authoritative number to wait for and to check
    the pull against, instead of comparing two figures that are both still settling.
    '''
    def __init__(self, synced:bool=False, issue_count:int=0):
        self.synced = synced
        self.issue_count = issue_count

    @property
    def expected_issue_count(self):
        '''The count to expect in elasticsearch, or None when this run didn't write it.'''
        return self.issue_count if self.synced else None

    def __repr__(self):
        return f"SyncResult(synced={self.synced}, issue_count={self.issue_count})"
