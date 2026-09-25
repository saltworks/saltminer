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

# Hand-written engagement record for the Word merge tests. Every value is distinct and greppable.
# The shape is what PBI-046 fixes: plain dicts and lists, every group a list of dicts keyed by the
# group name, every field a string.

ISSUE_FIELDS = (
    "Description", "Product", "Vendor", "Severity", "ReportId", "AssetId", "FoundDate", "TestStatus",
    "TestingInstructions", "IsSuppressed", "IsActive", "IsRemoved", "RemovedDate", "Location",
    "LocationFull", "Classification", "Enumeration", "Reference", "Proof", "Details", "Implication",
    "Recommendation", "References", "IssueAttributes|tested_by", "IssueAttributes|influencers",
    "IssueAttributes|developer_note", "IssueAttributes|comments",
)


def _issue(number: int) -> dict:
    record = {field: f"ISSUE-{number}-{field}" for field in ISSUE_FIELDS}
    record["Name"] = f"ISSUE-{number}"
    return record


def sample_record() -> dict:
    return {
        "Name": "ENG-NAME",
        "State": "ENG-STATE",
        "Customer": "ENG-CUSTOMER",
        "Timestamp": "ENG-TIMESTAMP",
        "EngagementAttributes|tester": "ENG-TESTER",
        "Summary": "ENG-SUMMARY",
        "AssetTocs": [
            {
                "Id": "ASSET-A-ID",
                "Name": "ASSET-A",
                "Description": "ASSET-A-DESC",
                "SeverityGroups": [
                    {"Severity": "A-CRITICAL", "Issues": [{"Name": "A-CRITICAL-ISSUE", "Total": "1"}]},
                    {"Severity": "A-HIGH", "Issues": [{"Name": "A-HIGH-ISSUE", "Total": "1"}]},
                ],
            },
            {
                "Id": "ASSET-B-ID",
                "Name": "ASSET-B",
                "Description": "ASSET-B-DESC",
                "SeverityGroups": [
                    {"Severity": "B-LOW", "Issues": [{"Name": "B-LOW-ISSUE", "Total": "1"}]},
                ],
            },
        ],
        "IssueTocs": [
            {"Severity": "TOC-CRITICAL", "SeverityGroups": [{"Name": "TOC-CRITICAL-ISSUE", "Total": "1"}]},
            {"Severity": "TOC-HIGH", "SeverityGroups": [{"Name": "TOC-HIGH-ISSUE", "Total": "1"}]},
            {"Severity": "TOC-LOW", "SeverityGroups": [{"Name": "TOC-LOW-ISSUE", "Total": "1"}]},
        ],
        "IssueDetails": [_issue(1), _issue(2), _issue(3)],
    }
