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

import time
import json
import logging
import datetime
from xml.dom import NotFoundErr

from dateutil.parser import parse as dtparse
from requests.exceptions import ConnectionError

from Core.RestClient import RestClient

class SmApiClient(object):
    '''
    SaltMiner API Client
    Use to send data to the SaltMiner queue. Documents should be created in this order:
    QueueScan -> QueueAsset -> QueueIssues
    '''

    def __init__(self, appSettings, sourceName, configName="SMv3"):
        '''
        Initializes the class.

        appSettings: ApplicationSettings instance containing configuration settings
        configName: Configuration key for SMv3 configuration settings
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise SmApiClientConfigurationException("Type of appSettings must be 'ApplicationSettings'")
        if not configName or not configName in appSettings.GetConfigNames():
            raise SmApiClientConfigurationException(f"Invalid or missing configuration for name '{configName}'")

        self.__IssueBatch = { "Documents": [] }
        self.BatchSize = appSettings.Get(configName, 'BatchSize', 100)
        self.__KeyMap = {}
        self.__SourceName = sourceName
        self.__Es = appSettings.Application.GetElasticClient()
        self.__AssessmentTypeMap = appSettings.Get(configName, 'AssessmentTypeMap', {})
        self.__EnableStupidNull = appSettings.FlagSet("Enable-Stupid-Null")
        self.__DefaultFilterset = appSettings.GetSource(sourceName, "FiltersetId", "")
        self.__GuiUrlTemplate = appSettings.GetSource(sourceName, "GuiUrlTemplate", "")
        self.__GuiUrlTemplate = self.__GuiUrlTemplate.replace("{baseUrl}", appSettings.GetSource(sourceName, "BaseUrl"))
        self.__GuiUrlTemplate = self.__GuiUrlTemplate.replace("{filterset}", self.__DefaultFilterset)
        self.__GuiUrlTemplate = self.__GuiUrlTemplate.replace("{groupingType}", appSettings.GetSource(sourceName, "GroupingTypeId", ""))
        self.__InventoryAssetKeyAttribute = appSettings.GetSource(sourceName, "InventoryAssetKeyAttribute", "")
        self.__AssetType = "app"
        self.SscSourceType = 'Saltworks.SSC'
        self.FodSourceType = 'Saltworks.FOD'
        self.__ManagerApiKeySettingName = "ManagerApiKey"
        self.__ExpectedAssessmentTypes = appSettings.GetSource(sourceName, "V3ExpectedAssessmentTypes", [])
        if not isinstance(self.__ExpectedAssessmentTypes, list):
            raise SmApiClientConfigurationException("Invalid source config - list type required for key V3ExpectedAssessmentType")

        baseurl = appSettings.Get(configName, 'ApiUrl')
        apikey = appSettings.Get(configName, 'ApiKey')
        mgrkey = appSettings.Get(configName, self.__ManagerApiKeySettingName, "")
        verify = appSettings.Get(configName, 'SslVerify', True)
        vcert = appSettings.Get(configName, 'SslVerifyCert')
        timeout = appSettings.Get(configName, 'TimeoutSec', 240)

        if verify and vcert:
            verify = vcert
            if verify.find(","):
                verify = (verify.split(',')[0], verify.split(',')[1])
        else:
            verify = ""

        if not baseurl or not apikey:
            raise SmApiClientConfigurationException(f"Check '{configName}' configuration, ApiUrl and ApiKey are required for this feature.")
        if not mgrkey:
            logging.info("No manager API key found in configuration (%s setting).  Some operations will be unavailable.", self.__ManagerApiKeySettingName)

        headers = {
            'Accept': 'application/json',
            'Content-Type': 'application/json;charset=UTF-8',
            'Authorization': apikey
        }
        self.__ManagerHeaders = {
            'Accept': 'application/json',
            'Content-Type': 'application/json;charset=UTF-8',
            'Authorization': mgrkey
        }
        RestClient.disableRequestWarnings()
        self.__Client = RestClient(baseurl, sslVerify=verify, defaultHeaders=headers, timeout=timeout, retryConnectionErrors=True)

        self.AgentId = None
        self.Role = None
        try:
            r = self.__Client.Get("register/role")
            if r.status_code == 200:
                self.Role = json.loads(r.text)['message']
            else:
                raise(SmApiClientException(f"Error initializing connection to SaltMiner API: [{r.status_code}] {r.reason}"))
            if self.Role.lower() == "agent":
                r = self.__Client.Get("register/agent")
                if r.status_code == 200:
                    self.AgentId = json.loads(r.text)['message']
            r = self.__Client.Get("admin/version")
        except ConnectionError as e:
            logging.exception(f"Failed to connect to SM API: [{type(e).__name__}] {e}")
            raise
        except SmApiClientException:
            logging.exception("SM API initialization error")
            raise
        except Exception as e:
            logging.exception("Error when first connecting to SM API")
            raise SmApiClientConfigurationException(f"Error when attempting to connect to SM API: [{type(e).__name__}] {e}") from e

        logging.debug(f"[SMAPI] SmApiClient initialization complete.  RestClient params - url: '{baseurl}', verify: {verify}, role: {self.Role}, agentID: {self.AgentId}")

    # API Calls
    #region

    def AddQueueScan(self, qScan, immediate=False):
        '''
        Adds queue scan.  QueueStatus is initially set to "Loading" to indicate in progress.
        '''
        if immediate:
            qScan['Saltminer']['Internal']['QueueStatus'] = "Pending"
        else:
            qScan['Saltminer']['Internal']['QueueStatus'] = "Loading"
        qScan['Saltminer']['Internal']['AgentId'] = self.AgentId
        #qScan['Index'] = 'queue_scans'
        qScan['Id'] = None
        r = self.__Client.Post("queuescan", { "Id": None, "Entity": qScan })
        self.__VerifyResponse("Error submitting new queue scan to API", r)
        return json.loads(r.text)['data']

    def AddQueueAsset(self, qAsset):
        '''
        Adds queue asset.  Make sure QueueScanId is set to the id of a valid QueueScan document.
        '''
        qAsset['Id'] = None
        #qAsset['Index'] = 'queue_assets'
        r = self.__Client.Post("queueasset", { "Id": None, "Entity": qAsset })
        self.__VerifyResponse("[SMAPI] Error submitting new queue asset to API", r)
        return json.loads(r.text)['data']

    def AddQueueIssue(self, qIssue):
        '''
        Adds queue issue (uses batching for better performance).  Make sure Saltminer.QueueScanId and Saltminer.QueueAssetId are set to the ids of valid QueueScan and QueueAsset documents.
        '''
        qIssue['Id'] = None
        #qIssue['Index'] = 'queue_issues'
        self.__BatchIssue(qIssue)

    def DeleteAsset(self, assetId, sourceType):
        '''
        Removes an asset by parameters provided (all required).  To completely remove an asset, remove issues and scans first (DeleteScan, DeleteScanIssues).
        NOTE: requires Manager API Key (ManagerApiKey setting) to function.
        '''
        r = self.__Client.Delete(f"asset/{assetId}/{self.__AssetType}/{sourceType}/{self.__SourceName}", headers=self.__ManagerHeaders)
        self.__VerifyResponse(f"[SMAPI] Error deleting asset with ID '{assetId}', source type '{sourceType}'", r)
        logging.debug("[SMAPI] Deleted asset with ID '%s', source type '%s'", assetId, sourceType)

    def DeleteScan(self, scanId, sourceType):
        '''
        Removes a scan by parameters provided (all required).  To completely remove an scan, remove issues first (DeleteScanIssues).
        NOTE: requires Manager API Key (ManagerApiKey setting) to function.
        '''
        r = self.__Client.Delete(f"scan/{scanId}/{self.__AssetType}/{sourceType}/{self.__SourceName}", headers=self.__ManagerHeaders)
        self.__VerifyResponse(f"[SMAPI] Error deleting scan with ID '{scanId}', source type '{sourceType}'", r)
        logging.debug("[SMAPI] Deleted scan with ID '%s', source type '%s'", scanId, sourceType)

    def DeleteScanIssues(self, scanId, sourceType):
        '''
        Removes all issues associated with scan by parameters provided (all required).
        NOTE: requires Manager API Key (ManagerApiKey setting) to function.
        '''
        r = self.__Client.Delete(f"issue/scan/{scanId}/{self.__AssetType}/{sourceType}/{self.__SourceName}", headers=self.__ManagerHeaders)
        self.__VerifyResponse(f"[SMAPI] Error deleting issue(s) for scan with ID '{scanId}', source type '{sourceType}'", r)
        logging.debug("[SMAPI] Deleted issue(s) for scan with ID '%s', source type '%s'", scanId, sourceType)

    def GetAssetScans(self, sourceId, sourceType, refreshFirst=False):
        '''
        Retrieves a list of scans by parameters provided.
        NOTE: requires Manager API Key (ManagerApiKey setting) to function.
        '''
        q = {
          "assetType": self.__AssetType,
          "sourceType": sourceType,
          "filter": {
            "anyMatch": False,
            "filterMatches": {
              "saltminer.asset.source_id": str(sourceId)
            }
          },
          "uiPagingInfo": {
            "size": 1000,
            "sortFilters": {
              "saltminer.scan.scan_date": False
            }
          }
        }
        if refreshFirst:
            ind = f"scans_app_saltworks.{sourceType.lower()}_{self.__SourceName.lower()}"
            self.RefreshIndex(ind)
        r = self.__Client.Post("scan/search", q, headers=self.__ManagerHeaders)
        self.__VerifyResponse(f"[SMAPI] Error searching for scans for asset with source ID '{sourceId}' and source type '{sourceType}'", r)
        r = json.loads(r.text)
        if 'data' in r.keys() and len(r['data']):
            return r['data']
        else:
            return None

    def RefreshIndex(self, indexName, suppressError=True):
        '''
        Calls API Refresh Index
        '''
        try:
            logging.info("Refreshing index '%s' (including a 2 sec delay)", indexName)
            self.__Client.Post("index/refresh/" + indexName)
            time.sleep(2)
        except Exception as e:
            if not suppressError:
                raise SmApiClientException(f"Failed to refresh index '{indexName}'") from e

    def DeleteAssetAll(self, assetId, sourceId, sourceType, exceptionOnFail=False):
        ''' 
        Deletes asset and all scans/issues associated with it.  Returns True for success, False otherwise.  Should NOT return an exception unless specified.
        NOTE: requires Manager API Key (ManagerApiKey setting) to function.
        '''
        ok = True
        cancel = False
        try:
            if not self.__ManagerHeaders['Authorization']:
                raise SmApiClientConfigurationException("Manager API key (%s setting) is required for this operation.", self.__ManagerApiKeySettingName)
            scans = self.GetAssetScans(sourceId, sourceType)
            while scans:
                logging.info("[SMAPI] Found %s scan(s) to delete in this pass.", len(scans))
                c = 0
                for scan in scans:
                    scanId = scan['id']

                    try:
                        self.DeleteScanIssues(scanId, sourceType)
                    except KeyboardInterrupt:
                        if cancel:
                            raise KeyboardInterrupt()
                        cancel = True
                        logging.info("Cancel requested, request again to stop immediately")
                    except Exception as e:
                        logging.info("[SMAPI] Unable to delete issues for scan ID '%s' and source type '%s'", scanId, sourceType)
                        if exceptionOnFail:
                            raise SmApiClientException("[SMAPI] Error deleting issues, see log for details") from e
                        ok = False

                    try:
                        self.DeleteScan(scanId, sourceType)
                    except KeyboardInterrupt:
                        if cancel:
                            raise KeyboardInterrupt()
                        cancel = True
                        logging.info("Cancel requested, request again to stop immediately")
                    except Exception as e:
                        logging.info("[SMAPI] Unable to delete scan with ID '%s' and source type '%s'", scanId, sourceType)
                        if exceptionOnFail:
                            raise SmApiClientException("[SMAPI] Error deleting scan, see log for details") from e
                        ok = False
                    c += 1
                    if c % 50 == 0 or c == len(scans):
                        logging.info("[SMAPI] Deleted %s of %s scans", c, len(scans))
                # end for
                scans = self.GetAssetScans(sourceId, sourceType, True)
            # end while

            try:
                self.DeleteAsset(assetId, sourceType)
            except KeyboardInterrupt:
                cancel = True
                logging.info("Cancel requested")
            except SmApiClientNotFoundException as e:
                logging.info("[SMAPI] Asset '%s' not found, cannot delete.", assetId)
            except Exception as e:
                #logging.warning("[SMAPI] Unable to delete asset with ID '%s' and source type '%s'", sourceId, sourceType)
                if exceptionOnFail:
                    raise SmApiClientException("[SMAPI] Error deleting asset, see log for details") from e
                ok = False
            if cancel:
                raise KeyboardInterrupt()
        except KeyboardInterrupt:
            logging.info("Cancelling process...")
            raise KeyboardInterrupt()
        except SmApiClientException:
            # no need to log or check whether to raise
            raise
        except Exception as e:
            logging.exception("[SMAPI] Error processing removal of asset, scan, or issues for asset ID '%s' and source type '%s'", sourceId, sourceType)
            if exceptionOnFail:
                raise SmApiClientException("[SMAPI] Error deleting asset/scan/issues, see log for details") from e
            ok = False
        finally:
            return ok

    def FinalizeQueue(self, qScanId):
        '''
        Marks the queue scan as Pending, completing the queue load process.
        '''
        r = self.__Client.Get(f"queuescan/status/{qScanId}/Pending")
        self.__VerifyResponse(f"[SMAPI] Error finalizing queue scan with ID {qScanId}", r)
        msg = f"[SMAPI] Completed queuescan with id {qScanId}, now in Pending status"
        logging.info(msg)

    def SearchLastScan(self, avid, atype, sourceType='Saltworks.SSC'):
        '''
        Returns last scan (by scan date) matching the passed app version id and assessment type
        '''
        q = {
          "assetType": self.__AssetType,
          "sourceType": sourceType,
          "filter": {
            "anyMatch": False,
            "filterMatches": {
              "saltminer.asset.source_id": avid,
              "saltminer.scan.assessment_type": atype
            }
          },
          "uiPagingInfo": {
            "size": 1000,
            "sortFilters": {
              "saltminer.scan.scan_date": False
            }
          }
        }
        r = self.__Client.Post("scan/search", q)
        self.__VerifyResponse(f"[SMAPI] Error searching for last scan for app version {avid} and assessment type '{atype}'", r)
        r = json.loads(r.text)
        if 'data' in r.keys() and len(r['data']):
            return r['data'][0]
        else:
            return None

    def GetWebhookEvents(self, sourceId):
        '''
        Retrieves a list of webhook events for the source ID given.
        '''
        r = self.__Client.Get(f"utility/webhook/{sourceId}")
        self.__VerifyResponse(f"[SMAPI] Error returning webhook events for source ID '{sourceId}'", r)
        r = json.loads(r.text)
        if 'data' in r.keys() and len(r['data']):
            return r['data']
        else:
            return None

    #endregion

    # API Helpers
    #region

    def __GetGuiUrl(self, avid, isSsc, issueId):
        if self.__GuiUrlTemplate:
            if isSsc:
                return self.__GuiUrlTemplate.replace("{avid}", str(avid)).replace("{instanceId}", issueId)
            else:
                return self.__GuiUrlTemplate.replace("{avid}", str(avid)).replace("{issueId}", issueId)
        else:
            return ""
    
    def __ReadResponseErrors(self, response):
        try:
            r = json.loads(response.text)
            errs = []
            if 'errors' in r.keys():
                for k in r['errors'].keys():
                    for e in r['errors'][k]:
                        errs.append(f"{k}: {e}")
            return errs
        except Exception as e:
            logging.debug("[SMAPI] Failed to read errors in response with status code %s: [%s] %s", response.status_code, type(e).__name__, e)

    def __VerifyResponse(self, baseMsg, response, suppressErrorLogging=False):
        if not 200 <= response.status_code < 300:
            msg = f"{baseMsg}: "
            try:
                msg = f"{msg}{json.loads(response.text)['message']}"
            except:
                msg = f"{msg}[{response.status_code}] {response.reason}"
            if response.status_code == 400:
                if response.text.startswith('{') and not response.text.startswith('{"type":"https://tools.ietf.org'):
                    rj = json.loads(response.text)
                    if 'message' in rj.keys() and len(rj['message']) > 0:
                        etype = 'Unknown' if not 'type' in rj.keys() else rj['type']
                        msg = f"{baseMsg}: Bad request error sending data to SMAPI.  Server response: [{etype}] {rj['message']}"
                    elif 'errorMessages' in rj.keys() and len(rj['errorMessages']) > 0:
                        etype = 'Unknown' if not 'errorType' in rj.keys() else rj['errorType']
                        msg = f"{baseMsg}: Bad request error sending data to SMAPI.  Server response: [{etype}] {rj['errorMessages']}"
                    else:
                        msg = f"{baseMsg}: Bad request error sending data to SMAPI.  Server response: {response.text}"
                    if not suppressErrorLogging:
                        logging.error(msg)
                    raise(SmApiClientException(msg))
                else:
                    msg = f"{baseMsg}: Validation error sending data to SMAPI.  Validation error(s):"
                    if not suppressErrorLogging:
                        logging.error(msg)
                        for e in self.__ReadResponseErrors(response):
                            logging.error("[SMAPI]---> %s", e)
                    raise(SmApiClientException(f'{baseMsg}: Validation error sending data to SMAPI. Details logged previously.'))
            if not suppressErrorLogging:
                logging.error(msg)
                logging.debug("[SMAPI] " + response.text)
            if response.status_code == 404:
                raise SmApiClientNotFoundException(msg)
            raise SmApiClientException(msg)

    def __BatchIssue(self, qIssue):
        if qIssue:
            self.__IssueBatch['Documents'].append(qIssue)
        doclen = len(self.__IssueBatch['Documents'])
        if doclen >= self.BatchSize or (not qIssue and doclen > 0):
            msg = f"[SMAPI] Sending batch of {doclen} queue issue(s) to API"
            logging.debug(msg)
            try:
                r = self.__Client.Post("queueissue/bulk", self.__IssueBatch)
            except Exception as e:
                logging.error("[SMAPI] Failed sending issue batch to API (%s), retrying in 5 sec...", type(e).__name__)
                r = self.__Client.Post("queueissue/bulk", self.__IssueBatch)
            try:
                self.__VerifyResponse("[SMAPI] Error sending batch of queue issues to API", r)
            except Exception:
                raise
            finally:
                self.__IssueBatch['Documents'] = []

    def __GetLatestSscScan(self, avid, scanType):
        q = {
          "query": {
            "bool": {
              "must": [
                { "term": { "projectVersionId": { "value": avid } } },
                { "term": { "type": { "value": scanType } } }
              ]
            }
          },
          "sort": [ { "artifactUploadDate": { "order": "desc" } } ]
        }
        r = self.__Es.Search("sscprojscans", q)
        if r and isinstance(r, list) and len(r) >= 1:
            return r[0]['_source']
        else:
            return None

    def __MapAssessmentType(self, atype):
        # This method may not be needed as configured mapping already happens in PopulateAppVuls.
        # Will only take action if the passed assessment type isn't a mapped value
        if atype in self.__AssessmentTypeMap.values():
            return atype  # already mapped
        if not atype in self.__AssessmentTypeMap.keys():
            logging.warning("Unmapped assessment type '%s' found.", atype)
            return atype
        else:
            return self.__AssessmentTypeMap[atype]

    def __Empty(self, strVal):
        strVal = str(strVal).strip()
        return not strVal or strVal == "" or len(strVal) == 0

    def __GetAttribute(self, source, key, default=None):
        if key in source.keys():
            if source[key]:
                if isinstance(source[key], list):
                    return ','.join([str(val) for val in source[key]])
                if len(source[key]) > 0:
                    return str(source[key])
        return default

    #endregion

    # Mapping Methods
    #region

    def FinalizeEverything(self):
        '''
        NOTE: This clears self.__KeyMap, so don't call it until after using that data
        '''
        cid = None
        errCount = 0
        self.__BatchIssue(None) # send any remaining queue issues
        try:
            self.__Es.FlushIndex("queue_issues")
            self.__Es.FlushIndex("queue_scans")
        except Exception as e:
            logging.warning("Error updating v3 indices - this is ok in multi-instance scenarios. %s", e)
        logging.info("Wait a moment for elasticsearch to catch up...")
        time.sleep(2)
        for id in self.__KeyMap.keys():
            try:
                cid = self.__KeyMap[id]['sid']
                self.FinalizeQueue(cid)
            except SmApiClientException as ex:
                errCount += 1
                # already logged error
            except Exception as ex:
                errCount += 1
                logging.error("[SMAPI] Error finalizing queue scan with ID %s: [%s] %s", cid, type(ex).__name__, ex)
            if errCount > 9:
                logging.critical("[SMAPI] %s errors encountered while finalizing queue scans, aborting process.", errCount)
                break
        if errCount > 0:
            logging.error("[SMAPI] %s errors encountered while finalizing queue scans - some data will be missing.", errCount)
        self.__KeyMap = {}

    def MapScanlessAsset(self, avid, scannerVendor, name, version, description, attributes, isProd=True, assessmentTypes=[]):
        if len(self.__ExpectedAssessmentTypes) == 0:
            logging.debug("No expected assessment types configured.  Skipping noscan processing.")
            return
        for eat in self.__ExpectedAssessmentTypes:
            if eat in assessmentTypes:
                continue

            try:
                logging.info("No scan found for expected assessment type '%s', adding noscan queue data...", eat)
                dtNow = datetime.datetime.now(datetime.timezone.utc).isoformat()
                severity = "noscan"
                source = self.__GetSource({"scanner_vendor": scannerVendor})
                ptype = SmApiClient.__GetProduct(source, eat)
                atype = self.__MapAssessmentType(eat)  # changes atype for rest of method...
                reportId = 'noscan|' + atype
                

                qscan = {
                    "Timestamp": dtNow,
                    "Saltminer": {
                        "Internal": {
                            "AgentId": self.AgentId,
                            "IssueCount": -1,
                            "CurrentQueueScanId": None,
                            "ReplaceIssues": True
                        },
                        "Scan": {
                            "AssessmentType": atype,
                            "ProductType": ptype,
                            "Product": "Fortify",
                            "Vendor": "Fortify",
                            "ReportId": reportId,
                            "ScanDate": dtNow,
                            "SourceType": source,
                            "IsSaltMinerSource": True,
                            "ConfigName": self.__SourceName,
                            "AssetType": self.__AssetType,
                            "Instance": self.__SourceName
                        }
                    }
                }
                qscan = self.AddQueueScan(qscan)
                qasset = {
                    "Saltminer": {
                        "Asset": {  
                            "Name": name,
                            "Description": description,
                            "VersionId": str(avid),
                            "Version": version,
                            "ConfigName": self.__SourceName,
                            "SourceType": source,
                            "IsSaltMinerSource": True,
                            "SourceId": str(avid),
                            "IsProduction": isProd, 
                            "AssetType": self.__AssetType,
                            "Instance": self.__SourceName,
                            "Attributes": {},
                            "LastScanDaysPolicy": "30"
                        },
                        "InventoryAsset": {
                            "Key": ""
                        },
                        "Internal": {
                            "QueueScanId": qscan['id']
                        }
                    },
                    "Timestamp": SmApiClient.CleanDateString(dtNow)
                }
                for attrib in attributes.keys():
                    attribVal = self.__GetAttribute(attributes, attrib)
                    if attribVal:
                        qasset['Saltminer']['Asset']['Attributes'][attrib] = attribVal
                # if configured and present, set inventory asset key from attributes
                if self.__InventoryAssetKeyAttribute and qasset['Saltminer']['Asset']['Attributes'] and self.__InventoryAssetKeyAttribute in qasset['Saltminer']['Asset']['Attributes'].keys():
                    qasset['Saltminer']['InventoryAsset']['Key'] = qasset['Saltminer']['Asset']['Attributes'][self.__InventoryAssetKeyAttribute]
                qasset = self.AddQueueAsset(qasset)

                qissue = {
                    "Saltminer": {
                        "QueueScanId": qscan['id'],
                        "QueueAssetId": qasset['id']
                    },
                    "Vulnerability": {
                        "IsActive": True,
                        "FoundDate": SmApiClient.CleanDateString(dtNow),
                        "Id": None,                             # ECS field.  Current code doesn't have a standards identifier, like "cve-1234"
                        "IsFiltered": False,
                        "IsRemoved": False,
                        "IsSuppressed": False,
                        "Location": "none",
                        "LocationFull": "none",
                        "SourceSeverity": severity,
                        "ReportId": reportId,
                        "Category": [ "Application" ],          # ECS always should be this value, defaults to this as well, can leave it out
                        "Classification": "",                   # ECS not required, current code hard-codes ""
                        "Description": "",                      # ECS not required
                        "Enumeration": "",                      # ECS not required, current code hard-codes ""
                        "Name": "No scan found for assessment type " + atype,
                        "Reference": "",
                        "Severity": self.__MapSeverity(severity),
                        "Scanner": {
                            "ApiUrl": "",
                            "GuiUrl": "",
                            "Id": reportId,                     # duplicate scan reportId, as noscan will always be one issue
                            "AssessmentType": atype,
                            "Product": ptype,
                            "Vendor": "Fortify"
                        }
                    },
                    "Timestamp": SmApiClient.CleanDateString(dtNow)
                }
                self.AddQueueIssue(qissue)
                key = f"{avid}|{atype}"
                if not key in self.__KeyMap.keys():
                    self.__KeyMap[key] = { 'sid': qscan['id'], 'aid': qasset['id'] }
                else:
                    logging.error("Unexpected app version ID %s and assessement type %s already found in v3 integration keymap.")
            except Exception as ex:
                logging.error("Error!", exc_info=ex)
                pass
        # end for

    def MapEverything(self, issue, issueAssetKeys, issueKeys, sscHistoryEnable = False):
        try:
            self.MapAndAddScanAndAsset(issue, issueAssetKeys, sscHistoryEnable)
            self.MapAndAddIssue(issue, issueKeys)
        except:
            logging.error("Failed to map queue resource", exc_info=True)

    def MapScan(self, source, atype, ptype, scanId, timestamp, issue=None, sscV2Scan=None, sscV3ScanId=None):
        scanDate = self.__GetScanDate(issue, source, sscV2Scan)
        qscan = {
            "Timestamp": timestamp,
            "Saltminer": {
                "Internal": {
                    "AgentId": self.AgentId,
                    "IssueCount": 0 if sscV3ScanId else -1,
                    "CurrentQueueScanId": sscV3ScanId if sscV3ScanId else ("NULL" if self.__EnableStupidNull else None),
                    "ReplaceIssues": True
                },
                "Scan": {
                    "AssessmentType": atype,
                    "ProductType": ptype,
                    "Product": "Fortify" if not issue else issue['engine_type'],
                    "Vendor": "Fortify",
                    "ReportId": scanId,
                    "ScanDate": scanDate,
                    "SourceType": source,
                    "IsSaltMinerSource": True,
                    "ConfigName": self.__SourceName,
                    "AssetType": self.__AssetType,
                    "Instance": self.__SourceName,
                    "Rulepacks": []
                }
            }
        }
        if sscV2Scan and 'rulepacks' in sscV2Scan:
            qscan['Saltminer']['Scan']['Rulepacks'] = []
            for rp in sscV2Scan['rulepacks']:
                qscan['Saltminer']['Scan']['Rulepacks'].append({ 
                    'Id': rp['guid'] if 'guid' in rp else '',
                    'Name': rp['name'] if 'name' in rp else '',
                    'Version': rp['version'] if 'version' in rp else '',
                    'Language': rp['language'] if 'language' in rp else ''
                })
        return self.AddQueueScan(qscan, sscV3ScanId)
        
    def MapAndAddScanAndAsset(self, issue, issueAssetKeys, sscAllHistoryEnable = False):
        avid = str(issue['application_version_id'])
        atype = self.__MapAssessmentType(issue['assessment_type'])
        key = f"{avid}|{atype}"
        if key in self.__KeyMap.keys():
            return # we've already seen this avid/assessment type
        source = self.__GetSource(issue)
        product = SmApiClient.__GetProduct(source, atype)
        sid = str(issue['report_id'])
        if 'SSC' in source.upper():
            v2SscScan = self.__GetLatestSscScan(avid, issue['engine_type'])
            if not v2SscScan:
                logging.error("No SSC scan was found for app version %s and type '%s', scan with id '%s' will be skipped.", avid, issue['engine_type'], sid)
                return
            # if SSC we overwrite the report id with a combo of date and id to avoid duplicates
            # this allows re-uploads to still be considered scan history
            sid = SmApiClient.__FormatScanId(v2SscScan['artifactUploadDate'], v2SscScan['id'])
        else:
            v2SscScan = None
        section = "queue scan"
        qscan = None
        try:
            qscan = self.MapScan(source, atype, product, sid, SmApiClient.CleanDateString(issue['timestamp']), issue, v2SscScan)
            qsid = qscan['id']
            isProd = True
            if 'SSC' in source.upper():
                self.__MapAndAddSscScanHistory(avid, atype, issue['engine_type'], product, qscan, sscAllHistoryEnable)
            else:
                 if 'sdlc_status' in issue.keys() and issue['sdlc_status'] != 'Production':
                     isProd = False
            section = "queue asset"
            qasset = {
                "Saltminer": {
                    "Asset": {  
                        "Name": issue['application_name'],
                        "Description": issue['application_description'],
                        "VersionId": avid,
                        "Version": issue['application_version_name'],
                        "ConfigName": self.__SourceName,
                        "SourceType": source,
                        "IsSaltMinerSource": True,
                        "SourceId": avid,
                        "IsProduction": isProd, 
                        "AssetType": self.__AssetType,
                        "Instance": self.__SourceName,
                        "Attributes": {},
                        "LastScanDaysPolicy": "30"
                    },
                    "InventoryAsset": {
                        "Key": ""
                    },
                    "Internal": {
                        "QueueScanId": qsid
                    }
                },
                "Timestamp": SmApiClient.CleanDateString(issue['timestamp'])
            }
            self.AddDiffAttributes(issueAssetKeys, issue, qasset['Saltminer']['Asset']['Attributes'])
            # if configured and present, set inventory asset key from attributes
            if self.__InventoryAssetKeyAttribute and qasset['Saltminer']['Asset']['Attributes'] and self.__InventoryAssetKeyAttribute in qasset['Saltminer']['Asset']['Attributes'].keys():
                qasset['Saltminer']['InventoryAsset']['Key'] = qasset['Saltminer']['Asset']['Attributes'][self.__InventoryAssetKeyAttribute]
            qasset = self.AddQueueAsset(qasset)
        
            # add keys to keymap so can map queue issues to correct queue scan and asset ids
            if not key in self.__KeyMap.keys():
                self.__KeyMap[key] = { 'sid': qsid, 'aid': qasset['id'] }

        except KeyError as e:
            msg = f"[SMAPI] Error mapping queue {'asset' if qscan else 'scan'} - [KeyError] for field {e} (section {section}, local id {avid})."
            logging.error(msg)
            raise SmApiClientException(msg) from e
        except SmApiClientException as e:
            msg = f"[SMAPI] Error mapping queue {'asset' if qscan else 'scan'} - [SmApiClientException] {e})."
            logging.error(msg)
            logging.warning(f"[SMAPI] Failed to add {section} for app / version with local id {avid} and scan with local id {sid}.")
            raise SmApiClientException(msg) from e
        except Exception as e:
            msg = f"[SMAPI] Error adding {section} for app / version with local id {avid} and scan with local id {sid}: [{type(e).__name__}] {e}"
            logging.error(msg)
            raise SmApiClientException(msg) from e

    def MapAndAddIssue(self, issue, issueKeys):
        qscanId = ''
        qassetId = ''
        source = self.__GetSource(issue)
        isSsc = "ssc" in source.lower()
        sscInstanceId = issue['issue_instance_id'] if isSsc and 'issue_instance_id' in issue.keys() else "instance_id_missing"
        avid = str(issue['application_version_id'])
        atype = self.__MapAssessmentType(issue['assessment_type'])
        key = f"{avid}|{atype}"
        gui_url = ""
        if not issue['name']:
            issue['name'] = "NAME UNKNOWN"
        if not issue['scanner_id']:
            if not issue['severity'] == "Zero":
                raise ValueError("Missing scanner_id (required) for issue")
            issue['scanner_id'] = source + '|' + avid + '|' + 'zero'
        else:
            gui_url = self.__GetGuiUrl(avid, isSsc, str(issue['scanner_id']) if not isSsc else sscInstanceId)
        try:
            if key not in self.__KeyMap.keys():
                raise SmApiClientException(f"Failure to map issue, KeyMap does not contain App Release id '{avid}' and assessment type '{atype}'.")
            qscanId = self.__KeyMap[key]['sid']
            qassetId = self.__KeyMap[key]['aid']

            if not issue['severity']:
                logging.warning("Issue '{}' is missing a severity", issue['scanner_id'])
            tags = None if not issue['tags'] else [ issue['tags'] ]

            issue['location'] = str(issue['location'])
            issue['location_full'] = str(issue['location_full'])
            if self.__Empty(issue['location']):
                issue['location'] = issue['location_full']
            if self.__Empty(issue['location']):
                issue['location'] = "[empty]"
            if self.__Empty(issue['location_full']):
                issue['location_full'] = issue['location']
            if self.__Empty(issue['location']):
                raise ValueError("Location missing/invalid.")

            customAttr = {}
            if 'customAttributes' in issue.keys():
                customAttr = issue['customAttributes']
                for k in customAttr.keys():
                    if k == 'customAttributes':
                        continue # in case bug pushes this through
                    customAttr[k] = str(customAttr[k])
            if not customAttr:
                customAttr = {}
            if 'customTagValues' in issue.keys():
                for kv in issue['customTagValues']:
                    customAttr[kv['keyValue']['name']] = str(kv['keyValue']['value'])

            qissue = {
                "Saltminer": {
                    "QueueScanId": qscanId,
                    "QueueAssetId": qassetId,
                    "Source": {
                        "Analyzer": issue['analyzer'],
                        "Confidence": float(issue['confidence']),
                        "Impact": float(issue['impact']),
                        "IssueStatus": issue['issue_status'],
                        "Kingdom": issue['kingdom'],
                        "Likelihood": float(issue['likelihood'])
                    },
                    "Attributes": customAttr
                },
                "Vulnerability": {
                    "IsActive": issue['active'],
                    "Audit": {
                        "Audited": issue['audited'],
                        "Auditor": "",                      # Not available
                        "LastAudit": None                   # Not available
                    },
                    "FoundDate": SmApiClient.CleanDateString(issue['found_date']),
                    "Id": None,                             # ECS field.  Current code doesn't have a standards identifier, like "cve-1234"
                    "IsFiltered": issue['hidden'],
                    "IsRemoved": issue['removed'],
                    "IsSuppressed": issue['suppressed'],
                    "Location": issue['location'],
                    "LocationFull": issue['location_full'],
                    "RemovedDate": SmApiClient.CleanDateString(issue['removed_date']),
                    "SourceSeverity": issue['severity'],
                    "ReportId": str(issue['report_id']),
                    "Category": [ "Application" ],          # ECS always should be this value, defaults to this as well, can leave it out
                    "Classification": "",                   # ECS not required, current code hard-codes ""
                    "Description": "",                      # ECS not required
                    "Enumeration": "",                      # ECS not required, current code hard-codes ""
                    "Name": issue['name'],
                    "Reference": issue['reference'],
                    "Severity": self.__MapSeverity(issue['severity']),
                    "Scanner": {
                        "ApiUrl": issue['sor_url'],
                        "GuiUrl": gui_url,
                        "Id": str(issue['scanner_id']),
                        "AssessmentType": atype,
                        "Product": SmApiClient.__GetProduct(source, atype),
                        "Vendor": "Fortify"
                    },
                    "Score": {
                        "Base": float(issue['score_base']),  # Hard-coded to "0" in current code
                        "Environmental": float(issue['score_environmental']),    # Hard-coded to "0" in current code
                        "Temporal": float(issue['score_temporal']),              # Hard-coded to "0" in current code
                        "Version": None                                             # Hard-coded to "2.0" in current code (?!)
                    }
                },
                "Labels": {},                               # ECS field. Not available
                "Message": None,                            # ECS field. Not available
                "Tags": tags,                               # ECS field.
                "Timestamp": SmApiClient.CleanDateString(issue['timestamp'])
            }
            if 'issue_instance_id' in issue.keys():
                qissue['Saltminer']['Attributes']['issue_instance_id'] = issue['issue_instance_id']
            if 'primary_rule_guid' in issue.keys():
                qissue['Saltminer']['Attributes']['primary_rule_guid'] = issue['primary_rule_guid']
            self.AddDiffAttributes(issueKeys, issue, qissue['Saltminer']['Attributes'])
            self.AddQueueIssue(qissue)
        except KeyError as e:
            logging.error("[SMAPI] Error mapping queue issue - KeyError for field %s (local scan id '%s').", e, avid)
            logging.warning(f"[SMAPI] Scan with local id '%s' may be out of sync with queue scan id '%s'.", avid, qscanId)
            raise SmApiClientException("Mapping error") from e
        except SmApiClientException as e:
            logging.error("[SMAPI] Error mapping queue issue - [SmApiClientException] %s", e)
            logging.warning("[SMAPI] Scan with local id '%s' may be out of sync with queue scan id '%s'.", avid, qscanId)
        except Exception as ex:
            scanId = '[unknown]' if not 'scan_id' in issue.keys() else issue['scan_id']
            logging.error("[SMAPI] Error adding issue with local id '%s' to local scan id '%s' (queue scan id %s): [%s] %s", issue['scanner_id'], scanId, qscanId, type(ex).__name__, ex)
            logging.warning("[SMAPI] Scan with local id '%s' may be out of sync with queue scan id '%s'.", avid, qscanId)

    def __MapAndAddSscScanHistory(self, avid, atype, etype, product, sscV3QueueScan, sscAllHistoryEnable=False):
        '''
        atype: assessment type (v3)
        etype: engine type (v2)
        '''
        q = {
          "query": {
            "bool": {
              "must": [
                { "term": { "projectVersionId": { "value": avid } } },
                { "term": { "type": { "value": etype } } }
              ]
            }
          },
          "sort": [ { "artifactUploadDate": { "order": "desc" } } ]
        }
        scanScroller = self.__Es.SearchScroll("sscprojscans", q, 200)
        timestamp = SmApiClient.CleanDateString(datetime.datetime.utcnow().isoformat())
        source = self.SscSourceType
        v3LastScan = self.SearchLastScan(avid, atype, source)
        v3LastScanDate = datetime.datetime.fromisoformat('1900-01-01') if not v3LastScan else v3LastScan['saltminer']['scan']['scanDate'] # use returned json format
        count = 0
        while scanScroller.Results:
            for scanCont in scanScroller.Results:
                scan = scanCont['_source']
                hScanDate = SmApiClient.CleanDateString(scan['artifactUploadDate'])
                try:
                    v3LastScanDate = dtparse(v3LastScanDate)
                except:
                    pass
                if not v3LastScanDate or not 'datetime.datetime' in str(type(v3LastScanDate)):
                    logging.error("Invalid v3 last scan date '%s' for app version id %s and assessment type '%s'. Skipping scan history record", v3LastScanDate, avid, atype)
                    continue
                # if the last scan we have in SMv3 is newer then skip this one (unless all history enabled)
                if not sscAllHistoryEnable and v3LastScanDate.date() >= dtparse(hScanDate).date():
                    continue
                v3ScanIdCurrent = sscV3QueueScan['saltminer']['scan']['reportId']
                v3ScanIdNew = SmApiClient.__FormatScanId(scan['artifactUploadDate'], scan['id'])
                # if this is the latest scan or of the wrong assessment type, don't load it
                if v3ScanIdNew == v3ScanIdCurrent or etype != scan['type']:
                    continue
                self.MapScan(source, atype, product, v3ScanIdNew, timestamp, None, scan, sscV3QueueScan['id'])
                count += 1
            try:
                scanScroller.GetNext()
            except NotFoundErr:
                logging.warning(f"[SMAPI] History query failed due to scroll expiration.  History may be truncated for app version {avid}")
        logging.info("[SMAPI] Processed v3 scan history for app version %s and assessment type %s - %s history scan(s).", avid, atype, count)
        try:
            scanScroller.Clear()
        except:
            pass # don't care if error on clear

    #endregion

    # Helpers
    #region

    
    @staticmethod
    def __FormatScanId(artUploadDate, id):
        return f"{artUploadDate}~{id}"

    def __GetSource(self, issue):
        if not 'scanner_vendor' in issue.keys():
            return 'Unknown'
        if issue['scanner_vendor'] == "Fortify":
            return self.SscSourceType
        else:
            return self.FodSourceType

    @staticmethod
    def __GetProduct(source, atype="any"):
        if 'SSC' in source:
            if atype == "DAST":
                return "Fortify WebInspect"
            if atype == "SAST":
                return "Fortify SCA"
            return "Fortify SSC"
        return "FOD"

    @staticmethod
    def __MapSeverity(sev):
        sev = sev.lower()
        map = { "critical": "Critical", "high": "High", "medium": "Medium", "low": "Low", "zero": "Zero", "noscan": "NoScan" }
        return map[sev] if sev in map.keys() else "Info"

    @staticmethod
    def __GetScanDate(issue, source, v2SscScan):
        if not issue and not v2SscScan:
            raise SmApiClientException("Invalid call to __GetScanDate, must include non-null issue or v2SscScan")
        if not issue:
            lastScanDate = None
            foundDate = None
            scanDate = None
        else:
            lastScanDate = issue['last_scan_date']
            foundDate = issue['found_date']
            scanDate = SmApiClient.CleanDateString(lastScanDate) if lastScanDate else SmApiClient.CleanDateString(foundDate)
        uploadDate = None if not v2SscScan else v2SscScan['artifactUploadDate']
        # we use the artifactUploadDate instead of the last_scan_date imported from v2 for SSC to support multiple uploads of the same artifact (cheating, but what can you do?)
        if 'SSC' in source.upper():
            scanDate = SmApiClient.CleanDateString(uploadDate) if uploadDate else scanDate
        if not scanDate:
            raise SmApiClientException(f"ScanDate cannot be null (last_scan_date: {lastScanDate}, found_date: {foundDate}, artifactUploadDate (SSC): {uploadDate}")
        return scanDate

    @staticmethod
    def AddDiffAttributes(orgKeys, source, target):
        '''
        Update target dict to add fields in source that weren't there when the source's keys (fields) were captured.  For attribute handling
        
        Parameters:
        
        orgKeys - source.keys() before additions
        source - source dict
        target - target dict that will receive the additions

        '''
        for k in source.keys():
            if not k in orgKeys:
                target[k] = str(source[k])

    @staticmethod
    def CleanDateString(ds):
        if not ds: return ds
        # Handle python bug that leaves out a digit sometimes on zero seconds
        if len(ds) == 18:
            ds += '0'
        if len(ds) < 19:
            try:
                ds = dtparse(ds).isoformat()
            except:
                raise(ValueError(f"Date string '{ds}' is incorrect"))
        i = ds.find(".")
        if i > -1:
            return ds[0:i]
        else:
            return ds

    #endregion

class SmApiClientException(Exception):
    pass

class SmApiClientNotFoundException(SmApiClientException):
    pass

class SmApiClientConfigurationException(SmApiClientException):
    pass