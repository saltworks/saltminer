''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-06-30
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import json 
from typing import Dict, Any, Optional
from datetime import datetime
from pydantic import BaseModel
class SnykDocs:
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
                        "Port": None,
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
                        "QueueScanId": None
                    }
                },
                "Timestamp": None
            }
    
    def map_scan_doc(self):
        return {
            "Saltminer": {
                "Internal": {
                    "AgentId": None,
                    "IssueCount": None,
                    "CurrentQueueScanId": None,
                    "ReplaceIssues": False
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


#The DTO classes are used to process the Queue_Scan, Queue_Issue, and Queue_Asset json docs through a data validation process prior to sending them to Saltminer
class BaseDocDTO(BaseModel):
    '''
    A base DTO to contain information shared across all data types in the adapter (Asset, Scan, Issue)
    '''
    Timestamp: Optional[datetime] = None

#These are a collection of shared DTOs for Assets, Scans, and Issues.
class Internal(BaseModel):
    QueueScanId: Optional[str] = None
    IssueCount: Optional[int] = -1

class InventoryAsset(BaseModel):
    Key: str = ""

class Scanner(BaseModel):
    ApiUrl: Optional[str] = None
    GuiUrl: Optional[str] = None
    Id: Optional[str] = None
    AssessmentType: Optional[str] = None
    Product: Optional[str] = None
    Vendor: Optional[str] = None

#These are a collection of DTOs specific to Assets
class Asset(BaseModel):
    Name: Optional[str] = None
    Description: Optional[str] = None
    VersionId: Optional[str] = None
    Version: Optional[str] = None
    ConfigName: Optional[str] = None
    SourceType: Optional[str] = None
    IsSaltMinerSource: bool = True
    SourceId: Optional[str] = None
    IsProduction: Optional[bool] = None
    AssetType: Optional[str] = None
    Instance: Optional[str] = None
    Attributes: Dict[str, Any] = {}
    LastScanDaysPolicy: str = "30"

class SaltminerAsset(BaseModel):
    Asset: Asset
    InventoryAsset: InventoryAsset
    Internal: Internal

class MapAssetDocDTO(BaseDocDTO):
    Saltminer: SaltminerAsset

#These  are a collection of DTOs specific to Scans 
class Scan(BaseModel):
    AssessmentType: str = None
    ProductType: str = None
    Product: Optional[str] = None
    Vendor: Optional[str] = None
    ReportId: str = None
    ScanDate: Optional[str] = None
    SourceType: Optional[str] = None
    IsSaltMinerSource: bool = True
    ConfigName: Optional[str] = None
    AssetType: str = None
    Instance: Optional[str] = None
    Rulepacks: list[str] = []


class SaltminerScan(BaseModel):
    Internal: Dict[str, Any]
    Scan: Scan


class MapScanDocDTO(BaseDocDTO):
    Saltminer: SaltminerScan

#These are a collection of DTOs specific to Issues 
class Source(BaseModel):
    Analyzer: Optional[str] = None
    Confidence: Optional[str] = None
    Impact: Optional[str] = None
    IssueStatus: Optional[str] = None
    Kingdom: Optional[str] = None
    Likelihood: Optional[str] = None


class Score(BaseModel):
    Base: Optional[float] = None
    Environmental: Optional[float] = None
    Temporal: Optional[float] = None
    Version: Optional[str] = None


class Vulnerability(BaseModel):
    IsActive: Optional[bool] = None
    Audit: Dict[str, Any] = {}
    FoundDate: Optional[str] = None
    Id: list[str] = None
    IsFiltered: Optional[bool] = None
    IsRemoved: bool
    IsSuppressed: Optional[bool] = None
    Location: Optional[str] = None
    LocationFull: Optional[str] = None
    RemovedDate: Optional[str] = None
    SourceSeverity: Optional[str] = None
    ReportId: Optional[str] = None
    Category: list[str] = ["Application"]
    Classification: str = ""
    Description: str = ""
    Enumeration: str = ""
    Name: Optional[str] = None
    Reference: Optional[str] = None
    Severity: Optional[str] = None
    Scanner: Scanner
    Score: Score


class SaltminerIssue(BaseModel):
    QueueScanId: Optional[str] = None
    QueueAssetId: Optional[str] = None
    Source: Source
    Attributes: Dict[str, Any] = {}


class MapIssueDocDTO(BaseDocDTO):
    Saltminer: SaltminerIssue
    Vulnerability: Vulnerability
    Labels: Dict[str, Any] = {}
    Message: Optional[str] = None
    Tags: Optional[list[str]] = None
