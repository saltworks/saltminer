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
SourceMetric + NeedsUpdate.

Per-asset change detection for source adapters, ported from the C# base class
(SourceAdapters.Core/SourceAdapter.cs NeedsUpdate + Data/Entities/SourceMetric.cs)
with two deliberate departures, both ruled 2026-08-26:

1. Field selection is config-driven, not code-driven.  The C# skipChecks
   parameter is replaced by the NeedsUpdateFields list in the source's config:
   needs_update() compares exactly the fields that list names, nothing else.

2. There is no local metric store - no SQL database, no metrics index.  The
   "local" side of the comparison is derived from the source's final issues
   index (see derive_local_metrics), so the metric reflects what was actually
   delivered, not what an adapter once attempted.  A downstream drop reads as a
   mismatch and self-corrects on the next run; pointing at a new instance finds
   an empty index and correctly triggers a full sync.

This module lives in Core rather than QueueClient so that non-threaded adapters
can reach it without importing any queue machinery.
'''

import logging
from dataclasses import dataclass, field


# Canonical NeedsUpdateFields names, as written in a source's config.  Severity
# count fields use SaltMiner severity names rather than the C# Sev1-Sev4 labels.
FIELD_LAST_SCAN      = "LastScan"
FIELD_ISSUE_COUNT    = "IssueCount"
FIELD_CRITICAL       = "Critical"
FIELD_HIGH           = "High"   
FIELD_MEDIUM         = "Medium"
FIELD_LOW            = "Low"
FIELD_INSTANCE       = "Instance"
FIELD_SOURCE_ID      = "SourceId"
FIELD_SOURCE_TYPE    = "SourceType"
FIELD_IS_NOT_SCANNED = "IsNotScanned"
FIELD_ATTRIBUTES     = "Attributes"

# Full comparison set.  Template configs ship with all of these listed; a source
# narrows the list in its own config (the config-driven replacement for the C#
# skipChecks mechanism).
NEEDS_UPDATE_FIELDS = (
    FIELD_LAST_SCAN,
    FIELD_ISSUE_COUNT,
    FIELD_CRITICAL,
    FIELD_HIGH,
    FIELD_MEDIUM,
    FIELD_LOW,
    FIELD_INSTANCE,
    FIELD_SOURCE_ID,
    FIELD_SOURCE_TYPE,
    FIELD_IS_NOT_SCANNED,
    FIELD_ATTRIBUTES,
)

# Config field name -> SourceMetric attribute, for the scalar comparisons.
# Attributes is handled separately (dict semantics).
_SCALAR_FIELD_ATTRS = {
    FIELD_LAST_SCAN:      "last_scan",
    FIELD_ISSUE_COUNT:    "issue_count",
    FIELD_CRITICAL:       "critical",
    FIELD_HIGH:           "high",
    FIELD_MEDIUM:         "medium",
    FIELD_LOW:            "low",
    FIELD_INSTANCE:       "instance",
    FIELD_SOURCE_ID:      "source_id",
    FIELD_SOURCE_TYPE:    "source_type",
    FIELD_IS_NOT_SCANNED: "is_not_scanned",
}


class SourceMetricException(Exception):
    ''' Raised for invalid metric comparisons - unknown or empty NeedsUpdateFields. '''
    pass


@dataclass
class SourceMetric:
    '''
    One asset's sync state, on either side of the comparison.

    The source side is built by the adapter from the vendor payload
    (TemplateAdapter.build_source_metric); the local side is derived from the
    final issues index (derive_local_metrics).  Full C# field parity, with the
    per-severity counts named for SaltMiner severities instead of Sev1-Sev4.

    last_scan is compared with ==, so both sides must supply the same shape -
    the convention is the source's own last-updated string, ISO8601 UTC,
    written to the issue attributes so the local side can aggregate it back out.
    '''
    source_id: str = None
    source_type: str = None
    instance: str = None
    last_scan: str = None
    issue_count: int = 0
    critical: int = 0
    high: int = 0
    medium: int = 0
    low: int = 0
    is_not_scanned: bool = False
    attributes: dict = None


@dataclass
class NeedsUpdateResult:
    '''
    Outcome of one needs_update() comparison.

    is_equal True means the asset is unchanged and may be skipped - which, per
    the retirement rule, means submitting NOTHING for it (no scan, no asset, no
    issues).  messages holds one line per mismatched field, for logging.
    '''
    is_equal: bool = True
    messages: list = field(default_factory=list)

    @property
    def needs_update(self) -> bool:
        return not self.is_equal


def needs_update(source_metric: SourceMetric, local_metric: SourceMetric,
                 needs_update_fields=NEEDS_UPDATE_FIELDS) -> NeedsUpdateResult:
    '''
    Compares a source-side metric against the locally derived one, returning a
    NeedsUpdateResult.  Source-agnostic - one implementation, no per-adapter
    overrides, matching the C# base class where no adapter overrides it.

    :source_metric: metric built from the vendor's current data.  Required.
    :local_metric: metric derived from the final issues index, or None when the
        asset has never been delivered - None always needs an update.
    :needs_update_fields: the field names to compare, from the source config's
        NeedsUpdateFields key.  Unknown names raise rather than silently never
        comparing (a typo here must not disable change detection); an empty
        list raises for the same reason.
    '''
    if source_metric is None:
        raise SourceMetricException("source_metric is required.")
    fields = list(needs_update_fields or [])
    if not fields:
        raise SourceMetricException(
            "NeedsUpdateFields is empty - comparing nothing would skip every asset forever. "
            f"List the fields to compare; the full set is {list(NEEDS_UPDATE_FIELDS)}.")
    unknown = [f for f in fields if f not in NEEDS_UPDATE_FIELDS]
    if unknown:
        raise SourceMetricException(
            f"Unknown NeedsUpdateFields entr{'ies' if len(unknown) > 1 else 'y'} {unknown}; "
            f"valid names are {list(NEEDS_UPDATE_FIELDS)}.")

    result = NeedsUpdateResult()
    if local_metric is None:
        result.is_equal = False
        result.messages.append("Local metric missing, processing metric")
        return result

    for name in fields:
        if name == FIELD_ATTRIBUTES:
            _compare_attributes(source_metric.attributes, local_metric.attributes, result)
            continue
        attr = _SCALAR_FIELD_ATTRS[name]
        source_value = getattr(source_metric, attr)
        local_value = getattr(local_metric, attr)
        if source_value != local_value:
            result.is_equal = False
            result.messages.append(
                f"{name} different L: {local_value} S: {source_value}, processing metric")
    return result


def _compare_attributes(source_attrs: dict, local_attrs: dict, result: NeedsUpdateResult):
    ''' Dict comparison matching the C# Attributes checks, collecting every mismatch. '''
    if source_attrs is not None and local_attrs is None:
        result.is_equal = False
        result.messages.append("Attributes were not empty and now are, processing metric")
        return
    if source_attrs is None and local_attrs is not None:
        result.is_equal = False
        result.messages.append("Attributes were empty and now are not, processing metric")
        return
    if source_attrs is None:
        return
    if len(source_attrs) != len(local_attrs):
        result.is_equal = False
        result.messages.append(
            f"Different number of Attributes L: {len(local_attrs)} S: {len(source_attrs)}, "
            "processing metric")
        return
    for key, value in source_attrs.items():
        if key not in local_attrs:
            result.is_equal = False
            result.messages.append(f"{key} Attribute Missing, processing metric")
        elif value != local_attrs[key]:
            result.is_equal = False
            result.messages.append(
                f"{key} Attribute Value L: {local_attrs[key]} S: {value} changed, "
                "processing metric")


def build_local_metrics_query(last_scan_field: str, max_assets: int = 10000) -> dict:
    '''
    Aggregation over a final issues index that yields one bucket per asset
    version_id, carrying everything derive_local_metrics needs: doc count,
    per-severity counts, max last-scan value, and the asset identity fields.

    Removed issues are excluded - the source reports current state, so the
    local counts must be current state too or every closed issue would read as
    a permanent mismatch.

    :last_scan_field: the issue attribute the adapter writes its source
        last-updated value to, ex: "saltminer.attributes.acme_last_updated".
    '''
    return {
        "query": {
            "bool": {
                "must_not": [
                    {"term": {"vulnerability.is_removed": True}}
                ]
            }
        },
        "aggs": {
            "version_id": {
                "terms": {
                    "field": "saltminer.asset.version_id",
                    "size": max_assets,
                    "order": {"_key": "desc"}
                },
                "aggs": {
                    "last_scan": {"max": {"field": last_scan_field}},
                    "severities": {"terms": {"field": "vulnerability.severity", "size": 10}},
                    "source_id": {"terms": {"field": "saltminer.asset.source_id", "size": 1}},
                    "instance": {"terms": {"field": "saltminer.asset.instance", "size": 1}},
                    "source_type": {"terms": {"field": "saltminer.asset.source_type", "size": 1}}
                }
            }
        },
        "size": 0
    }


def derive_local_metrics(es, index: str, last_scan_field: str) -> dict:
    '''
    Derives the local SourceMetric per asset by aggregating the source's final
    issues index.  Returns {version_id: SourceMetric}; an absent or empty index
    returns {} so every asset compares as new and gets a full sync.

    This is the sanctioned direct-Elasticsearch verification read (ruled
    2026-08-26) - DataClient remains insert-only.  The derived metric can only
    carry what final-index documents carry: attributes is left None here, so a
    source that includes "Attributes" in its NeedsUpdateFields must also leave
    the source side's attributes None (or stop comparing them).

    :es: ElasticClient
    :index: the derived final issues index name - never a literal in an
        adapter, always from the adapter's index derivation helper.
    :last_scan_field: see build_local_metrics_query.
    '''
    if not es.IndexExists(index):
        logging.info("[SourceMetric] Index '%s' does not exist - all assets will be treated as new.", index)
        return {}
    result = es.Search(index=index, queryBody=build_local_metrics_query(last_scan_field),
                       size=10000, navToData=False)
    metrics = {}
    for bucket in result['aggregations']['version_id']['buckets']:
        severities = {b['key'].lower(): b['doc_count']
                      for b in bucket['severities']['buckets']}
        metrics[bucket['key']] = SourceMetric(
            source_id=_first_key(bucket['source_id']),
            source_type=_first_key(bucket['source_type']),
            instance=_first_key(bucket['instance']),
            last_scan=bucket['last_scan'].get('value_as_string'),
            issue_count=bucket['doc_count'],
            critical=severities.get('critical', 0),
            high=severities.get('high', 0),
            medium=severities.get('medium', 0),
            low=severities.get('low', 0),
            is_not_scanned=False,
            attributes=None
        )
    logging.info("[SourceMetric] Derived local metrics for %s asset(s) from '%s'.",
                 len(metrics), index)
    return metrics


def _first_key(terms_agg: dict):
    ''' First bucket key of a single-value terms agg, or None on an empty one. '''
    buckets = terms_agg.get('buckets') or []
    return buckets[0]['key'] if buckets else None
