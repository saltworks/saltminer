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

USAGE = '''
PBI-130 AC-7 helper: lists the SM2 document ids a FOD release produced, and compares two listings.

  python3 UnitTests/FodKeyPoolIdListing.py snapshot <releaseId> <out.json>
  python3 UnitTests/FodKeyPoolIdListing.py compare <a.json> <b.json>

Run from the Saltworks.SaltMiner.Python directory, against the agent's Elasticsearch.
`compare` exits 0 only when the id sets and the counts are equal in every index.
'''

import json
import os
import sys

# Run as a script from UnitTests/, the package root is the directory above
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

DEFAULT_INDICES = ("app_vuls_fod", "app_scan_history_fod")


def Snapshot(releaseId:str, outFile:str, indices=DEFAULT_INDICES) -> dict:
    ''' Writes {index: sorted list of _id} for the documents whose application_version_id is the release. '''
    from Core.Application import Application
    app = Application(skipCleanFiles=True)
    es = app.GetElasticClient()
    listing = {}
    for index in indices:
        query = {"query": {"term": {"application_version_id": {"value": str(releaseId)}}}, "sort": ["_doc"], "_source": False}
        with es.SearchScroll(index, query, scrollSize=1000) as scroller:
            listing[index] = sorted(hit["_id"] for hit in scroller.Generator())
    with open(outFile, "w") as f:
        json.dump(listing, f, indent=2)
    for index, ids in listing.items():
        print(f"{index}: {len(ids)} document ids")
    return listing


def Compare(fileA:str, fileB:str) -> int:
    ''' Returns 0 when both listings hold equal id sets and equal counts in every index, 1 otherwise. '''
    with open(fileA) as f:
        a = json.load(f)
    with open(fileB) as f:
        b = json.load(f)
    same = True
    for index in sorted(set(a) | set(b)):
        ida, idb = a.get(index, []), b.get(index, [])
        if len(ida) != len(idb) or set(ida) != set(idb) or len(set(ida)) != len(ida) or len(set(idb)) != len(idb):
            same = False
            print(f"DIFFERENT {index}: {len(ida)} vs {len(idb)} ids; only in first: {sorted(set(ida) - set(idb))[:10]}; only in second: {sorted(set(idb) - set(ida))[:10]}")
        else:
            print(f"EQUAL {index}: {len(ida)} ids")
    return 0 if same else 1


if __name__ == "__main__":
    if len(sys.argv) == 4 and sys.argv[1] == "snapshot":
        Snapshot(sys.argv[2], sys.argv[3])
    elif len(sys.argv) == 4 and sys.argv[1] == "compare":
        sys.exit(Compare(sys.argv[2], sys.argv[3]))
    else:
        print(USAGE)
        sys.exit(2)
