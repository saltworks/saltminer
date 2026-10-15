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

import json 
from typing import Annotated, Any, Dict, Optional
from datetime import datetime
from pydantic import BaseModel, BeforeValidator, field_validator
class SmDocsAndDTOs:
    """
    A class to handle all needed data structures for the SnykAdpater in the form of a function that returns 
    a json dictionary
    """

    def __init__(self):
        pass 

    def map_asset_doc(self):
        return {
                "Saltminer": {
                    "Asset": {  
                        "Name": None,
                        "Description": None,
                        "Ip":None,
                        "Scheme": None,
                        "Port": 0,
                        "VersionId": None,
                        "Version": None,
                        "ConfigName": None,
                        "SourceType": None,
                        "IsSaltMinerSource": True,
                        "SourceId": None,
                        "IsProduction": True, 
                        "AssetType": None,
                        "Instance": None,
                        "Attributes": {},
                        "LastScanDaysPolicy": "30"
                    },
                    "InventoryAsset": {
                        "Key": ""
                    },
                    "Internal": {
                        "QueueScanId": None,
                        "NeverScanned": False
                    }
                },
                "Timestamp": None
            }
    
    def map_scan_doc(self):
        return {
            "Saltminer": {
                "Internal": {
                    "IssueCount": -1,
                    "CurrentQueueScanId": None,
                    "QueueStatus": "Loading",
                    "ReplaceIssues": False,
                    "LastError": "",
                    "LockId": ""
                },
                "Scan": {
                    "AssessmentType": None,
                    "ProductType": None,
                    "Product": None,
                    "Vendor": None,
                    "ReportId": None,
                    "ScanDate": None,
                    "SourceType": None,
                    "IsSaltMinerSource": True,
                    "ConfigName":None,
                    "AssetType": None,
                    "Instance": None,
                    "Rulepacks": []
                }
            },
            "Timestamp": None
        }
    
    def map_issue_doc(self):
        return {
                "Saltminer": {
                    "QueueScanId": None,
                    "QueueAssetId": None,
                    "Source": {
                        "Analyzer": None,
                        "Confidence": None,
                        "Impact": None,
                        "IssueStatus": None,
                        "Kingdom": None,
                        "Likelihood": None,
                    },
                    "Attributes":{},
                },
                "Vulnerability": {
                    "IsActive": True,
                    "Audit": {
                        "Audited": False,
                        "Auditor": "",                     
                        "LastAudit": None                  
                    },
                    "FoundDate": None,
                    "Id": [],                            
                    "IsFiltered": False,
                    "IsRemoved": False,
                    "IsSuppressed":False,
                    "Location": None,
                    "LocationFull": None,
                    "Recommendation": None,
                    "RemovedDate": None,
                    "SourceSeverity": None,
                    "ReportId": None,
                    "Category": [ "Application" ],         
                    "Classification": "",                  
                    "Description": "",                     
                    "Enumeration": "",                     
                    "Name": None,
                    "Reference": None,
                    "Severity": None,
                    "Scanner": {
                        "ApiUrl": None,
                        "GuiUrl":None,
                        "Id": None,
                        "AssessmentType": None,
                        "Product": None,
                        "Vendor": None,
                    },
                    "Score": {
                        "Base": 0, 
                        "Environmental": 0,   
                        "Temporal": 0,             
                        "Version": None                                           
                    }
                },
                "Labels": {},                               
                "Message": None,                            
                "Tags": None,                             
                "Timestamp": None,
            }


# ===========================================================================
# DTOs
# ===========================================================================
#
# These validate a Scan / Asset / Issue document before it is submitted.
#
# They are written against the C# entities the DataApi deserializes into, not
# against what the templates above happen to contain:
#
#   Entities/Asset/AssetInfo.cs          -> Asset
#   Entities/Scan/ScanInfo.cs            -> Scan          (QueueScanInfo)
#   Entities/Internal.cs                 -> ScanInternal / AssetInternal
#   Entities/VulnerabilityInfo.cs        -> Vulnerability
#   Entities/ScannerInfo.cs              -> Scanner
#   Entities/ScoreInfo.cs                -> Score
#   Entities/SourceInfo.cs               -> Source
#   Entities/AuditInfo.cs                -> Audit
#   Entities/RulepackItem.cs             -> RulepackItem
#   Entities/Queues/QueueScan.cs         -> QueueScanStatus
#
# The point is that a document the API would reject fails HERE, with a stack
# trace pointing at the mapping line, instead of surfacing as a
# DataClientException three network hops away.
#
# Adapters build a dict, validate it by constructing a DTO from it, then submit
# the dict.  Pydantic's default extra="ignore" means a field these models do not
# declare is skipped at validation and sent anyway - so every field the API
# cares about has to be declared here, or it travels unchecked.


class QueueScanStatus:
    '''
    Valid Saltminer.Internal.QueueStatus values, in workflow order.

    Mirrors the QueueScanStatus enum in Entities/Queues/QueueScan.cs.  The API
    validates with Enum.TryParse and rejects anything else, including "" - which
    is what an omitted QueueStatus deserializes to.
    '''
    LOADING    = "Loading"
    PENDING    = "Pending"
    PROCESSING = "Processing"
    CANCEL     = "Cancel"
    COMPLETE   = "Complete"
    ERROR      = "Error"
    NONE       = "None"

    ALL = (LOADING, PENDING, PROCESSING, CANCEL, COMPLETE, ERROR, NONE)


def attr_value(value):
    '''
    Coerce one attribute value to a string.

    Attributes and Labels are Dictionary<string, string> on the API.  A list, a
    bool or a number there fails deserialization outright:
    "The JSON value could not be converted to System.String".

    Lists become a delimited string, matching the convention SnykAdapter already
    uses for its `dependencies` attribute.  Bools are lowercased because that is
    how they read back in search and filters.
    '''
    if value is None:
        return None
    if isinstance(value, str):
        return value
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, (list, tuple, set)):
        return ", ".join(attr_value(v) for v in value if v is not None)
    if isinstance(value, dict):
        return json.dumps(value, default=str)
    return str(value)


def attrs(mapping):
    '''
    Build an Attributes/Labels dict: every value stringified, every None dropped.

    Omitting nulls rather than writing "None" keeps documents smaller - which
    matters at ~1000 issues per asset - and an absent attribute and a null one
    read back the same way.
    '''
    if not mapping:
        return {}
    out = {}
    for key, value in mapping.items():
        coerced = attr_value(value)
        if coerced is not None:
            out[str(key)] = coerced
    return out


def _coerce_string_map(value):
    '''
    Before-validator for Dictionary<string, string> fields.

    Coerces rather than rejects deliberately.  The API can only store a string
    here, so refusing a list would fail a document that has a perfectly good
    representation - and every adapter would have to solve the same problem
    separately, which is how the current inconsistency arose.
    '''
    if isinstance(value, dict):
        return attrs(value)
    return value


StringMap = Annotated[Dict[str, str], BeforeValidator(_coerce_string_map)]


class BaseDocDTO(BaseModel):
    '''
    A base DTO to contain information shared across all data types in the adapter (Asset, Scan, Issue)
    '''
    Timestamp: Optional[datetime] = None


# -- shared -----------------------------------------------------------------

class InventoryAsset(BaseModel):
    Key: str = ""


class RulepackItem(BaseModel):
    ''' Entities/RulepackItem.cs.  Scan.Rulepacks is List<RulepackItem>, not a list of strings. '''
    Id: Optional[str] = None
    Name: Optional[str] = None
    Version: Optional[str] = None
    Language: Optional[str] = None


class ScannerInfo(BaseModel):
    ''' Entities/ScannerInfo.cs (ScannerInfo + ScannerInfoBase). '''
    ApiUrl: Optional[str] = None
    GuiUrl: Optional[str] = None
    Id: Optional[str] = None
    AssessmentType: Optional[str] = None
    Product: Optional[str] = None
    ProductType: Optional[str] = None
    ProductVersion: Optional[str] = None
    Vendor: Optional[str] = None


# -- assets -----------------------------------------------------------------

class AssetInternal(BaseModel):
    ''' Entities/Internal.cs :: QueueAssetInternal. '''
    QueueScanId: Optional[str] = None
    NeverScanned: bool = False


class Asset(BaseModel):
    ''' Entities/Asset/AssetInfo.cs (+ AssetInfoPolicy.LastScanDaysPolicy). '''
    Name: Optional[str] = None
    Description: Optional[str] = None
    VersionId: Optional[str] = None
    Version: Optional[str] = None
    Host: Optional[str] = None
    Ip: Optional[str] = None
    Scheme: Optional[str] = None
    # int, not int? - the API cannot take null here.  0 means "no port"; a null
    # or the string "None" both fail with "could not be converted to System.Int32".
    Port: int = 0
    Phase: Optional[str] = None
    ScanCount: int = 0
    SourceId: Optional[str] = None
    Instance: Optional[str] = None
    SourceType: Optional[str] = None
    AssetType: Optional[str] = None
    IsSaltMinerSource: bool = True
    IsRetired: bool = False
    IsProduction: Optional[bool] = None
    Attributes: StringMap = {}
    Id: Optional[str] = None
    LastScanDaysPolicy: str = "30"
    # Not present on AssetInfo; retained because adapters set it and extra fields
    # are ignored by the API rather than rejected.
    ConfigName: Optional[str] = None


class SaltminerAsset(BaseModel):
    Asset: Asset
    InventoryAsset: InventoryAsset
    Internal: AssetInternal


class MapAssetDocDTO(BaseDocDTO):
    Saltminer: SaltminerAsset


# -- scans ------------------------------------------------------------------

class ScanInternal(BaseModel):
    ''' Entities/Internal.cs :: QueueScanInternal. '''
    IssueCount: int = -1
    CurrentQueueScanId: Optional[str] = None
    # Required by the API and NOT defaulted by it.  Omitting it yields
    # " is not a valid Queue Scan Status" from QueueScanContext.ValidateStatus.
    QueueStatus: str = QueueScanStatus.LOADING
    ReplaceIssues: bool = False
    LastError: str = ""
    LockId: str = ""

    @field_validator("QueueStatus")
    @classmethod
    def _valid_status(cls, v):
        if v not in QueueScanStatus.ALL:
            raise ValueError(
                f"'{v}' is not a valid Queue Scan Status. Expected one of {list(QueueScanStatus.ALL)}.")
        return v


class Scan(BaseModel):
    ''' Entities/Scan/ScanInfo.cs :: QueueScanInfo. '''
    AssessmentType: Optional[str] = None
    ProductType: Optional[str] = None
    Product: Optional[str] = None
    ProductVersion: Optional[str] = None
    Vendor: Optional[str] = None
    ReportId: Optional[str] = None
    # DateTime, not DateTime? - a null fails at the API.  Required here so an
    # adapter that forgets it fails at the mapping line instead.
    ScanDate: str
    SourceType: Optional[str] = None
    IsSaltMinerSource: bool = True
    AssetType: Optional[str] = None
    Instance: Optional[str] = None
    Rulepacks: list[RulepackItem] = []
    LinesOfCode: int = 0
    # Not present on QueueScanInfo; see Asset.ConfigName.
    ConfigName: Optional[str] = None


class SaltminerScan(BaseModel):
    Internal: ScanInternal
    Scan: Scan


class MapScanDocDTO(BaseDocDTO):
    Saltminer: SaltminerScan


# -- issues -----------------------------------------------------------------

class SourceInfo(BaseModel):
    '''
    Entities/SourceInfo.cs.

    Confidence, Impact and Likelihood are float? on the API - they were declared
    as strings here, which would have been rejected the moment an adapter set one.
    '''
    Analyzer: Optional[str] = None
    Confidence: Optional[float] = None
    Impact: Optional[float] = None
    IssueStatus: Optional[str] = None
    Kingdom: Optional[str] = None
    Likelihood: Optional[float] = None


class ScoreInfo(BaseModel):
    ''' Entities/ScoreInfo.cs.  Base/Environmental/Temporal are float, not float?. '''
    Base: float = 0
    Environmental: float = 0
    Temporal: float = 0
    Version: Optional[str] = None


class AuditInfo(BaseModel):
    ''' Entities/AuditInfo.cs. '''
    Audited: bool = False
    Auditor: str = ""
    LastAudit: Optional[str] = None


class Vulnerability(BaseModel):
    '''
    Entities/VulnerabilityInfo.cs.

    IsActive and IsRemoved are deliberately absent: both are computed, read-only
    properties on the C# side (IsRemoved => RemovedDate != null), so a value sent
    for either is discarded. Adapters that still set them are harmless.
    '''
    SourceSeverity: Optional[str] = None
    FoundDate: Optional[str] = None
    Id: list[str] = []
    IsFiltered: bool = False
    IsSuppressed: bool = False
    Location: Optional[str] = None
    LocationFull: Optional[str] = None
    RemovedDate: Optional[str] = None
    ReportId: Optional[str] = None
    TestStatus: Optional[str] = None
    Audit: AuditInfo = AuditInfo()
    Category: list[str] = ["Application"]
    Classification: str = ""
    Description: str = ""
    Enumeration: str = ""
    Proof: Optional[str] = None
    Details: Optional[str] = None
    TestingInstructions: Optional[str] = None
    Implication: Optional[str] = None
    Recommendation: Optional[str] = None
    References: Optional[str] = None
    Name: Optional[str] = None
    Reference: Optional[str] = None
    Severity: Optional[str] = None
    Scanner: ScannerInfo = ScannerInfo()
    Score: ScoreInfo = ScoreInfo()


class SaltminerIssue(BaseModel):
    ''' Entities/Queues/QueueIssue.cs :: SaltMinerQueueIssueInfo. '''
    QueueScanId: Optional[str] = None
    QueueAssetId: Optional[str] = None
    IsHistorical: bool = False
    Source: SourceInfo = SourceInfo()
    IssueType: Optional[str] = None
    Attributes: StringMap = {}


class MapIssueDocDTO(BaseDocDTO):
    Saltminer: SaltminerIssue
    Vulnerability: Vulnerability
    # Dictionary<string, string> on the API, same as Attributes.
    Labels: StringMap = {}
    Message: Optional[str] = None
    # string[] on the API - a reference type, so null is accepted as well as [].
    # Kept Optional because the doc template ships None.
    Tags: Optional[list[str]] = None


# Backwards-compatible aliases.  Nothing outside this module imports these today
# (only Map*DocDTO and SnykDocs are imported elsewhere), but the previous names
# were public and renaming silently would be a trap.
Scanner = ScannerInfo
Score = ScoreInfo
Source = SourceInfo
Audit = AuditInfo
Internal = AssetInternal
