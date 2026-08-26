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

import unittest

from Core.SourceMetric import (
    NEEDS_UPDATE_FIELDS,
    NeedsUpdateResult,
    SourceMetric,
    SourceMetricException,
    build_local_metrics_query,
    needs_update,
)


def make_metric(**overrides) -> SourceMetric:
    ''' A fully populated metric; keyword overrides mutate single fields. '''
    values = dict(
        source_id="asset-001",
        source_type="Saltworks.Template",
        instance="TEMPLATE1",
        last_scan="2026-08-01T12:00:00.000Z",
        issue_count=10,
        critical=1,
        high=2,
        medium=3,
        low=4,
        is_not_scanned=False,
        attributes={"team": "alpha", "org": "example"},
    )
    values.update(overrides)
    return SourceMetric(**values)


class NeedsUpdateTests(unittest.TestCase):
    """Unit tests for the config-driven NeedsUpdate comparison (no ES required)."""

    # -- full-set equality ----------------------------------------------------

    def test_identical_metrics_are_equal(self):
        result = needs_update(make_metric(), make_metric(), NEEDS_UPDATE_FIELDS)
        self.assertTrue(result.is_equal)
        self.assertFalse(result.needs_update)
        self.assertEqual(result.messages, [])

    def test_default_field_set_is_the_full_set(self):
        result = needs_update(make_metric(), make_metric())
        self.assertTrue(result.is_equal)

    def test_missing_local_metric_needs_update(self):
        result = needs_update(make_metric(), None)
        self.assertFalse(result.is_equal)
        self.assertTrue(result.needs_update)
        self.assertIn("Local metric missing", result.messages[0])

    # -- per-field mismatch ---------------------------------------------------

    def test_each_scalar_field_mismatch_is_detected(self):
        cases = {
            "LastScan": {"last_scan": "2026-08-02T12:00:00.000Z"},
            "IssueCount": {"issue_count": 11},
            "Critical": {"critical": 2},
            "High": {"high": 0},
            "Medium": {"medium": 9},
            "Low": {"low": 0},
            "Instance": {"instance": "TEMPLATE2"},
            "SourceId": {"source_id": "asset-002"},
            "SourceType": {"source_type": "Saltworks.Other"},
            "IsNotScanned": {"is_not_scanned": True},
        }
        for field_name, override in cases.items():
            with self.subTest(field=field_name):
                result = needs_update(make_metric(**override), make_metric(),
                                      NEEDS_UPDATE_FIELDS)
                self.assertFalse(result.is_equal)
                self.assertEqual(len(result.messages), 1)
                self.assertIn(field_name, result.messages[0])

    def test_multiple_mismatches_collect_multiple_messages(self):
        source = make_metric(issue_count=99, critical=5)
        result = needs_update(source, make_metric(), NEEDS_UPDATE_FIELDS)
        self.assertFalse(result.is_equal)
        self.assertEqual(len(result.messages), 2)
        self.assertTrue(any("IssueCount" in m for m in result.messages))
        self.assertTrue(any("Critical" in m for m in result.messages))

    def test_mismatch_message_carries_both_values(self):
        result = needs_update(make_metric(issue_count=99), make_metric(),
                              ["IssueCount"])
        self.assertIn("L: 10", result.messages[0])
        self.assertIn("S: 99", result.messages[0])

    # -- NeedsUpdateFields subsetting -----------------------------------------

    def test_subset_ignores_fields_not_listed(self):
        source = make_metric(critical=5, attributes={"team": "changed"})
        result = needs_update(source, make_metric(), ["LastScan", "IssueCount"])
        self.assertTrue(result.is_equal)

    def test_subset_still_detects_listed_field(self):
        source = make_metric(last_scan="2026-08-02T12:00:00.000Z", critical=5)
        result = needs_update(source, make_metric(), ["LastScan"])
        self.assertFalse(result.is_equal)
        self.assertEqual(len(result.messages), 1)
        self.assertIn("LastScan", result.messages[0])

    def test_unknown_field_name_raises(self):
        with self.assertRaises(SourceMetricException):
            needs_update(make_metric(), make_metric(), ["LastScan", "IssueCuont"])

    def test_empty_field_list_raises(self):
        with self.assertRaises(SourceMetricException):
            needs_update(make_metric(), make_metric(), [])

    def test_none_field_list_raises(self):
        with self.assertRaises(SourceMetricException):
            needs_update(make_metric(), make_metric(), None)

    def test_missing_source_metric_raises(self):
        with self.assertRaises(SourceMetricException):
            needs_update(None, make_metric())

    # -- Attributes dict comparison -------------------------------------------

    def test_equal_attributes_dicts_are_equal(self):
        result = needs_update(make_metric(), make_metric(), ["Attributes"])
        self.assertTrue(result.is_equal)

    def test_both_attributes_none_is_equal(self):
        result = needs_update(make_metric(attributes=None),
                              make_metric(attributes=None), ["Attributes"])
        self.assertTrue(result.is_equal)

    def test_source_attributes_set_local_none_mismatches(self):
        result = needs_update(make_metric(), make_metric(attributes=None),
                              ["Attributes"])
        self.assertFalse(result.is_equal)
        self.assertIn("Attributes were not empty and now are", result.messages[0])

    def test_source_attributes_none_local_set_mismatches(self):
        result = needs_update(make_metric(attributes=None), make_metric(),
                              ["Attributes"])
        self.assertFalse(result.is_equal)
        self.assertIn("Attributes were empty and now are not", result.messages[0])

    def test_attribute_count_difference_mismatches(self):
        source = make_metric(attributes={"team": "alpha"})
        result = needs_update(source, make_metric(), ["Attributes"])
        self.assertFalse(result.is_equal)
        self.assertIn("Different number of Attributes", result.messages[0])
        self.assertIn("L: 2", result.messages[0])
        self.assertIn("S: 1", result.messages[0])

    def test_attribute_value_change_mismatches(self):
        source = make_metric(attributes={"team": "beta", "org": "example"})
        result = needs_update(source, make_metric(), ["Attributes"])
        self.assertFalse(result.is_equal)
        self.assertEqual(len(result.messages), 1)
        self.assertIn("team Attribute Value", result.messages[0])
        self.assertIn("L: alpha", result.messages[0])
        self.assertIn("S: beta", result.messages[0])

    def test_attribute_missing_key_mismatches(self):
        source = make_metric(attributes={"team": "alpha", "region": "us"})
        result = needs_update(source, make_metric(), ["Attributes"])
        self.assertFalse(result.is_equal)
        self.assertTrue(any("region Attribute Missing" in m for m in result.messages))

    def test_attribute_mismatch_does_not_hide_scalar_mismatch(self):
        source = make_metric(issue_count=99, attributes={"team": "beta", "org": "example"})
        result = needs_update(source, make_metric(), NEEDS_UPDATE_FIELDS)
        self.assertFalse(result.is_equal)
        self.assertTrue(any("IssueCount" in m for m in result.messages))
        self.assertTrue(any("team Attribute Value" in m for m in result.messages))

    # -- supporting shapes ----------------------------------------------------

    def test_result_defaults(self):
        result = NeedsUpdateResult()
        self.assertTrue(result.is_equal)
        self.assertEqual(result.messages, [])

    def test_local_metrics_query_uses_last_scan_field_and_excludes_removed(self):
        query = build_local_metrics_query("saltminer.attributes.template_last_updated")
        aggs = query["aggs"]["version_id"]["aggs"]
        self.assertEqual(aggs["last_scan"]["max"]["field"],
                         "saltminer.attributes.template_last_updated")
        self.assertIn({"term": {"vulnerability.is_removed": True}},
                      query["query"]["bool"]["must_not"])
        self.assertEqual(query["size"], 0)


if __name__ == "__main__":
    unittest.main()
