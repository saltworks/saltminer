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

from Reports.Colors import COLOR_NAMES, resolve_color

# JobManagerConfig.FieldValueColorCustomizations, as a deployment would set it: the seven names
# the shipped configuration uses (Spikes/PBI-025-report-merge/fixture.py FIELD_VALUE_COLORS).
SHIPPED_FIELD_VALUE_COLORS = {
    "critical": "Red",
    "high": "OrangeRed",
    "medium": "Goldenrod",
    "low": "Green",
    "information": "Gray",
    "fail": "Red",
    "pass": "Green",
}


class ColorTable(unittest.TestCase):
    def test_full_known_color_table(self):
        self.assertEqual(len(COLOR_NAMES), 141)

    def test_shipped_names_are_covered(self):
        for name in SHIPPED_FIELD_VALUE_COLORS.values():
            self.assertIsNotNone(resolve_color(name), name)
        self.assertEqual(resolve_color("Red"), "FF0000")
        self.assertEqual(resolve_color("Green"), "008000")

    def test_lookup_is_case_and_space_insensitive(self):
        self.assertEqual(resolve_color("RED"), "FF0000")
        self.assertEqual(resolve_color(" Green "), "008000")

    def test_unknown_name_returns_none(self):
        self.assertIsNone(resolve_color("NotAColour"))


if __name__ == "__main__":
    unittest.main()
