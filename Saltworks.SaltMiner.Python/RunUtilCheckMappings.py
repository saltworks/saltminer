"""
Reports indices whose live mapping disagrees with the template in Template/Mappings.

The failure this exists for: a field the template declares as 'keyword' but the live index mapped as
analysed 'text', which happens when an index is created by a write before its template is applied.
Term queries never match such a field (the indexed token is lowercased), so counts come back 0 while
the documents are plainly there - see AppVulsProcessor.__WaitForExpectedCount.

Read-only.  Run from Saltworks.SaltMiner.Python/ :  python3 RunUtilCheckMappings.py
"""
import glob
import json
import os
import sys
sys.path.insert(0, '.')
from Core.Application import Application

app = Application(skipCleanFiles=True)
es = app.GetElasticClient()

def flatten(props, prefix=''):
    """{'a': {'properties': {'b': {'type':'keyword'}}}} -> {'a.b': 'keyword'}"""
    out = {}
    for name, spec in (props or {}).items():
        if not isinstance(spec, dict):
            continue
        path = f"{prefix}{name}"
        if 'properties' in spec:
            out.update(flatten(spec['properties'], path + '.'))
        else:
            out[path] = spec.get('type', 'object')
    return out

problems = 0
checked = 0
for path in sorted(glob.glob('Template/Mappings/*.json')):
    index = os.path.splitext(os.path.basename(path))[0]
    try:
        if not es.IndexExists(index):
            continue
        expected = flatten(json.load(open(path)).get('mappings', {}).get('properties', {}))
        live_raw = es.GetIndexMapping(index)
        live_props = list(dict(live_raw).values())[0].get('mappings', {}).get('properties', {}) \
            if live_raw else {}
        live = flatten(live_props)
    except Exception as ex:
        print(f"  {index:24} SKIPPED ({type(ex).__name__}: {str(ex)[:70]})")
        continue
    checked += 1
    bad = {f: (t, live.get(f)) for f, t in expected.items()
           if f in live and live[f] != t and t == 'keyword'}
    missing = [f for f in expected if f not in live]
    if bad or missing:
        problems += 1
        print(f"\n  {index}")
        for f, (want, got) in sorted(bad.items()):
            flag = '  <<< term queries will NOT match' if got == 'text' else ''
            print(f"      {f:34} template={want:8} live={got}{flag}")
        for f in sorted(missing):
            print(f"      {f:34} template=declared  live=ABSENT (dynamic mapping will guess)")

print(f"\n{checked} live index(es) checked, {problems} with mapping drift.")
if problems:
    print("A field's type cannot be changed in place - the index must be reindexed, or dropped and")
    print("rebuilt by a full re-sync (MapESIndices recreates it from the template).")
