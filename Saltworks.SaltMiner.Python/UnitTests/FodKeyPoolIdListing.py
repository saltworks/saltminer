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
PBI-130 AC-7 helper: lists what a FOD release produced in SM2, and compares two listings.
app_vuls_fod is listed by the top-level scanner_id field (the scanner issue id, what AC-7 calls
vulnerability.scanner.id; Sources/FOD/AppVulsProcessor.py sets it from Issue['id']), app_scan_history_fod
by _id. An empty scanner_id fails snapshot and compare. Every _id in both indices must start with FOD1-; snapshot exits 1 if one does not.

  python3 UnitTests/FodKeyPoolIdListing.py snapshot <releaseId> <out.json>
  python3 UnitTests/FodKeyPoolIdListing.py compare <a.json> <b.json>

Run from the Saltworks.SaltMiner.Python directory, against the agent's Elasticsearch.
`compare` exits 0 only when the listed value sets and the counts are equal in every index.
'''

import json
import os
import sys

# Run as a script from UnitTests/, the package root is the directory above
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

DEFAULT_INDICES = ("app_vuls_fod", "app_scan_history_fod")
ID_PREFIX = "FOD1-"
# Indices listed by a source field instead of _id
SCANNER_ID_INDICES = ("app_vuls_fod",)


def _ScannerId(hit:dict) -> str:
    ''' Returns the top-level scanner_id of an app_vuls_fod hit as a string, or an empty string when absent. '''
    value = hit.get("_source", {}).get("scanner_id", "")
    return "" if value is None else str(value)


def Snapshot(releaseId:str, outFile:str, indices=DEFAULT_INDICES) -> dict:
    ''' Writes {index: sorted list} for the documents whose application_version_id is the release: the
    scanner_id values for app_vuls_fod, the _ids for the others. Raises ValueError, after
    writing the file, when any _id lacks the FOD1- prefix. '''
    from Core.Application import Application
    app = Application(skipCleanFiles=True)
    es = app.GetElasticClient()
    listing = {}
    badIds = []
    for index in indices:
        byField = index in SCANNER_ID_INDICES
        query = {"query": {"term": {"application_version_id": {"value": str(releaseId)}}}, "sort": ["_doc"],
                 "_source": ["scanner_id"] if byField else False}
        values = []
        with es.SearchScroll(index, query, scrollSize=1000) as scroller:
            for hit in scroller.Generator():
                if not hit["_id"].startswith(ID_PREFIX):
                    badIds.append(f"{index}: {hit['_id']}")
                values.append(_ScannerId(hit) if byField else hit["_id"])
        listing[index] = sorted(values)
    with open(outFile, "w") as f:
        json.dump(listing, f, indent=2)
    for index, values in listing.items():
        if any(v == "" for v in values) and index in SCANNER_ID_INDICES:
            badIds.append(f"{index}: empty scanner_id")
        print(f"{index}: {len(values)} {'scanner_id values' if index in SCANNER_ID_INDICES else 'document ids'}")
    if badIds:
        raise ValueError(f"{len(badIds)} problems (_id without the {ID_PREFIX} prefix, or empty scanner_id), first: {badIds[:5]}")
    print(f"every _id starts with {ID_PREFIX}")
    return listing


def Compare(fileA:str, fileB:str) -> int:
    ''' Returns 0 when both listings hold equal value sets and equal counts in every index, 1 otherwise. '''
    with open(fileA) as f:
        a = json.load(f)
    with open(fileB) as f:
        b = json.load(f)
    same = True
    for index in sorted(set(a) | set(b)):
        ida, idb = a.get(index, []), b.get(index, [])
        if any(v == "" for v in ida + idb):
            same = False
            print(f"DIFFERENT {index}: empty listed value(s), {ida.count('')} in first, {idb.count('')} in second")
            continue
        if len(ida) != len(idb) or set(ida) != set(idb) or len(set(ida)) != len(ida) or len(set(idb)) != len(idb):
            same = False
            print(f"DIFFERENT {index}: {len(ida)} vs {len(idb)} values; only in first: {sorted(set(ida) - set(idb))[:10]}; only in second: {sorted(set(idb) - set(ida))[:10]}")
        else:
            print(f"EQUAL {index}: {len(ida)} values")
    return 0 if same else 1


if __name__ == "__main__":
    if len(sys.argv) == 4 and sys.argv[1] == "snapshot":
        try:
            Snapshot(sys.argv[2], sys.argv[3])
        except ValueError as e:
            print(f"FAIL {e}")
            sys.exit(1)
    elif len(sys.argv) == 4 and sys.argv[1] == "compare":
        sys.exit(Compare(sys.argv[2], sys.argv[3]))
    else:
        print(USAGE)
        sys.exit(2)
