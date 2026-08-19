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

import time
import logging
import datetime
import uuid
from xml.dom import NotFoundErr

from dateutil.parser import parse as dtparse

from Core.DataClient import DataClient, DataClientException, DataClientNotFoundException


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
        sourceName: Name of the source configuration section
        configName: Configuration key for SMv3 configuration settings
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise SmApiClientConfigurationException("Type of appSettings must be 'ApplicationSettings'")
        if not configName or configName not in appSettings.GetConfigNames():
            raise SmApiClientConfigurationException(f"Invalid or missing configuration for name '{configName}'")

        self._issue_batch = {"Documents": []}
        self.batch_size = appSettings.Get(configName, 'BatchSize', 100)
        self._key_map = {}
        self._history_done = set()
        self._queue_scan_ids = []
        self._source_name = sourceName
        self._es = appSettings.Application.GetElasticClient()
        self._assessment_type_map = appSettings.Get(configName, 'AssessmentTypeMap', {})
        self._enable_stupid_null = appSettings.FlagSet("Enable-Stupid-Null")
        self._default_filterset = appSettings.GetSource(sourceName, "FiltersetId", "")
        self._gui_url_template = appSettings.GetSource(sourceName, "GuiUrlTemplate", "")
        self._gui_url_template = self._gui_url_template.replace("{baseUrl}", appSettings.GetSource(sourceName, "BaseUrl"))
        self._gui_url_template = self._gui_url_template.replace("{filterset}", self._default_filterset)
        self._gui_url_template = self._gui_url_template.replace("{groupingType}", appSettings.GetSource(sourceName, "GroupingTypeId", ""))
        self._inventory_asset_key_attribute = appSettings.GetSource(sourceName, "InventoryAssetKeyAttribute", "")
        self._asset_type = "app"
        self.ssc_source_type = 'Saltworks.SSC'
        self.fod_source_type = 'Saltworks.FOD'
        self._expected_assessment_types = appSettings.GetSource(sourceName, "V3ExpectedAssessmentTypes", [])
        if not isinstance(self._expected_assessment_types, list):
            raise SmApiClientConfigurationException("Invalid source config - list type required for key V3ExpectedAssessmentType")

        try:
            self._data_client = DataClient(appSettings.Application, configName, validate_on_init=False)
            self.role = self._data_client.register_get_role()
            self._data_client.get_version()
        except DataClientException as e:
            logging.exception("SM API initialization error")
            raise SmApiClientConfigurationException(f"Error when attempting to connect to SM API: [{type(e).__name__}] {e}") from e
        except Exception as e:
            logging.exception("Error when first connecting to SM API")
            raise SmApiClientConfigurationException(f"Error when attempting to connect to SM API: [{type(e).__name__}] {e}") from e

        logging.debug("[SMAPI] SmApiClient initialization complete. Role: %s", self.role)

    # ------------------------------------------------------------------
    # API Calls
    # ------------------------------------------------------------------

    def add_queue_scan(self, q_scan, immediate=False):
        '''
        Adds queue scan.  QueueStatus is initially set to "Loading" to indicate in progress.
        '''
        if immediate:
            q_scan['Saltminer']['Internal']['QueueStatus'] = "Pending"
        else:
            q_scan['Saltminer']['Internal']['QueueStatus'] = "Loading"
        q_scan['Id'] = None
        rsp = self._data_client.queue_scan_add_update(q_scan)
        if not rsp:
            raise SmApiClientException("Queue scan add/update returned no data from API.")
        self._queue_scan_ids.append(rsp['id'])
        return rsp

    def add_queue_asset(self, q_asset):
        '''
        Adds queue asset.  Make sure QueueScanId is set to the id of a valid QueueScan document.
        '''
        q_asset['Id'] = None
        rsp = self._data_client.queue_asset_add_update(q_asset)
        if not rsp:
            raise SmApiClientException("Queue asset add/update returned no data from API.")
        return rsp

    def add_queue_issue(self, q_issue):
        '''
        Adds queue issue (uses batching for better performance).  Make sure Saltminer.QueueScanId and
        Saltminer.QueueAssetId are set to the ids of valid QueueScan and QueueAsset documents.
        '''
        q_issue['Id'] = None
        self._batch_issue(q_issue)

    def delete_asset(self, asset_id, source_type):
        '''
        Removes an asset by parameters provided (all required).  To completely remove an asset,
        remove issues and scans first (delete_scan, delete_scan_issues).
        NOTE: requires Manager API Key (ManagerApiKey setting) to function.
        '''
        self._data_client.asset_delete(asset_id, self._asset_type, source_type, self._source_name)
        logging.debug("[SMAPI] Deleted asset with ID '%s', source type '%s'", asset_id, source_type)

    def delete_scan(self, scan_id, source_type):
        '''
        Removes a scan by parameters provided (all required).  To completely remove a scan,
        remove issues first (delete_scan_issues).
        NOTE: requires Manager API Key (ManagerApiKey setting) to function.
        '''
        self._data_client.scan_delete(scan_id, self._asset_type, source_type, self._source_name)
        logging.debug("[SMAPI] Deleted scan with ID '%s', source type '%s'", scan_id, source_type)

    def delete_scan_issues(self, scan_id, source_type):
        '''
        Removes all issues associated with scan by parameters provided (all required).
        NOTE: requires Manager API Key (ManagerApiKey setting) to function.
        '''
        self._data_client.issues_delete_by_scan(scan_id, self._asset_type, source_type, self._source_name)
        logging.debug("[SMAPI] Deleted issue(s) for scan with ID '%s', source type '%s'", scan_id, source_type)

    def get_asset_scans(self, source_id, source_type, refresh_first=False):
        '''
        Retrieves a list of scans by parameters provided.
        NOTE: requires Manager API Key (ManagerApiKey setting) to function.
        '''
        q = {
            "assetType": self._asset_type,
            "sourceType": source_type,
            "filter": {
                "anyMatch": False,
                "filterMatches": {
                    "saltminer.asset.source_id": str(source_id)
                }
            },
            "uiPagingInfo": {
                "size": 1000,
                "sortFilters": {
                    "saltminer.scan.scan_date": False
                }
            }
        }
        if refresh_first:
            ind = f"scans_app_saltworks.{source_type.lower()}_{self._source_name.lower()}"
            self.refresh_index(ind)
        return self._data_client.scan_search(q)

    def refresh_index(self, index_name, suppress_error=True):
        '''
        Calls API Refresh Index
        '''
        try:
            logging.info("Refreshing index '%s' (including a 2 sec delay)", index_name)
            self._data_client.refresh_index(index_name)
            time.sleep(2)
        except Exception as e:
            if not suppress_error:
                raise SmApiClientException(f"Failed to refresh index '{index_name}'") from e

    def delete_asset_all(self, asset_id, source_id, source_type, exception_on_fail=False):
        '''
        Deletes asset and all scans/issues associated with it.  Returns True for success, False otherwise.
        NOTE: requires Manager API Key (ManagerApiKey setting) to function.
        '''
        ok = True
        cancel = False
        try:
            scans = self.get_asset_scans(source_id, source_type)
            while scans:
                logging.info("[SMAPI] Found %s scan(s) to delete in this pass.", len(scans))
                c = 0
                for scan in scans:
                    scan_id = scan['id']
                    try:
                        self.delete_scan_issues(scan_id, source_type)
                    except KeyboardInterrupt:
                        if cancel:
                            raise KeyboardInterrupt()
                        cancel = True
                        logging.info("Cancel requested, request again to stop immediately")
                    except Exception as e:
                        logging.info("[SMAPI] Unable to delete issues for scan ID '%s' and source type '%s'", scan_id, source_type)
                        if exception_on_fail:
                            raise SmApiClientException("[SMAPI] Error deleting issues, see log for details") from e
                        ok = False

                    try:
                        self.delete_scan(scan_id, source_type)
                    except KeyboardInterrupt:
                        if cancel:
                            raise KeyboardInterrupt()
                        cancel = True
                        logging.info("Cancel requested, request again to stop immediately")
                    except Exception as e:
                        logging.info("[SMAPI] Unable to delete scan with ID '%s' and source type '%s'", scan_id, source_type)
                        if exception_on_fail:
                            raise SmApiClientException("[SMAPI] Error deleting scan, see log for details") from e
                        ok = False
                    c += 1
                    if c % 50 == 0 or c == len(scans):
                        logging.info("[SMAPI] Deleted %s of %s scans", c, len(scans))
                scans = self.get_asset_scans(source_id, source_type, True)

            try:
                self.delete_asset(asset_id, source_type)
            except KeyboardInterrupt:
                cancel = True
                logging.info("Cancel requested")
            except DataClientNotFoundException:
                logging.info("[SMAPI] Asset '%s' not found, cannot delete.", asset_id)
            except Exception as e:
                if exception_on_fail:
                    raise SmApiClientException("[SMAPI] Error deleting asset, see log for details") from e
                ok = False
            if cancel:
                raise KeyboardInterrupt()
        except KeyboardInterrupt:
            logging.info("Cancelling process...")
            raise KeyboardInterrupt()
        except SmApiClientException:
            raise
        except Exception as e:
            logging.exception("[SMAPI] Error processing removal of asset, scan, or issues for asset ID '%s' and source type '%s'", source_id, source_type)
            if exception_on_fail:
                raise SmApiClientException("[SMAPI] Error deleting asset/scan/issues, see log for details") from e
            ok = False
        return ok

    def finalize_queue(self, q_scan_id):
        '''
        Marks the queue scan as Pending, completing the queue load process.
        '''
        self._data_client.queue_scan_update_status(q_scan_id, 'Pending')
        logging.info("[SMAPI] Completed queuescan with id %s, now in Pending status", q_scan_id)

    def search_last_scan(self, avid, atype, source_type='Saltworks.SSC'):
        '''
        Returns last scan (by scan date) matching the passed app version id and assessment type.
        '''
        q = {
            "assetType": self._asset_type,
            "sourceType": source_type,
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
        data = self._data_client.scan_search(q)
        return data[0] if data else None

    def get_webhook_events(self, source_id):
        '''
        Retrieves a list of webhook events for the source ID given.
        '''
        return self._data_client.webhook_get(source_id)

    # ------------------------------------------------------------------
    # API Helpers
    # ------------------------------------------------------------------

    def _get_gui_url(self, avid, is_ssc, issue_id):
        if self._gui_url_template:
            if is_ssc:
                return self._gui_url_template.replace("{avid}", str(avid)).replace("{instanceId}", issue_id)
            else:
                return self._gui_url_template.replace("{avid}", str(avid)).replace("{issueId}", issue_id)
        return ""

    def _batch_issue(self, q_issue):
        if q_issue:
            self._issue_batch['Documents'].append(q_issue)
        doclen = len(self._issue_batch['Documents'])
        if doclen >= self.batch_size or (not q_issue and doclen > 0):
            logging.debug("[SMAPI] Sending batch of %s queue issue(s) to API", doclen)
            try:
                self._data_client.queue_issues_add_update_bulk(self._issue_batch)
            except DataClientException as e:
                raise SmApiClientException("Error sending batch of queue issues to API") from e
            finally:
                self._issue_batch['Documents'] = []

    def _get_latest_ssc_scan(self, avid, scan_type):
        q = {
            "query": {
                "bool": {
                    "must": [
                        {"term": {"projectVersionId": {"value": avid}}},
                        {"term": {"type": {"value": scan_type}}}
                    ]
                }
            },
            "sort": [{"artifactUploadDate": {"order": "desc"}}]
        }
        r = self._es.Search("sscprojscans", q)
        if r and isinstance(r, list) and len(r) >= 1:
            return r[0]['_source']
        return None

    def _map_assessment_type(self, atype):
        if atype in self._assessment_type_map.values():
            return atype  # already mapped
        if atype not in self._assessment_type_map.keys():
            logging.warning("Unmapped assessment type '%s' found.", atype)
            return atype
        return self._assessment_type_map[atype]

    def _empty(self, str_val):
        str_val = str(str_val).strip()
        return not str_val or str_val == "" or len(str_val) == 0

    def _get_attribute(self, source, key, default=None):
        if key in source.keys() and source[key]:
            if isinstance(source[key], list):
                return ','.join([str(val) for val in source[key]])
            if len(source[key]) > 0:
                return str(source[key])
        return default

    def _nvl(self, obj: dict, prop: str, replace=None):
        return replace if prop not in obj.keys() else obj[prop]

    # ------------------------------------------------------------------
    # Mapping Methods
    # ------------------------------------------------------------------

    def finalize_everything(self):
        '''
        Finalizes queues in progress and returns the list of queue scan IDs created (resetting the list)

        NOTE: This clears self._key_map, so don't call it until after using that data.
        '''
        cid = None
        err_count = 0
        finalized_ids = []
        self._batch_issue(None)  # send any remaining queue issues
        try:
            self._es.FlushIndex("queue_issues")
            self._es.FlushIndex("queue_scans")
        except Exception as e:
            logging.warning("Error updating v3 indices - this is ok in multi-instance scenarios. %s", e)
        logging.info("Wait a moment for elasticsearch to catch up...")
        time.sleep(2)
        for id in self._key_map.keys():
            try:
                cid = self._key_map[id]['sid']
                self.finalize_queue(cid)
                finalized_ids.append(cid)
            except SmApiClientException:
                err_count += 1
                # already logged error
            except Exception as ex:
                err_count += 1
                logging.error("[SMAPI] Error finalizing queue scan with ID %s: [%s] %s", cid, type(ex).__name__, ex)
            if err_count > 9:
                logging.critical("[SMAPI] %s errors encountered while finalizing queue scans, aborting process.", err_count)
                break
        if err_count > 0:
            logging.error("[SMAPI] %s errors encountered while finalizing queue scans - some data will be missing.", err_count)
        # Return only what we actually finalized - one per app version + assessment type.  Scan-history
        # queue scans are bulk-created straight into Pending status (see _map_and_add_ssc_scan_history),
        # never enter _key_map, and there can be hundreds of them for one app version.  They are already
        # in the state the manager picks up, so the caller must not treat them as work to hand off
        # individually - that turns one app version into hundreds of manager runs.
        others = len(set(self._queue_scan_ids) - set(finalized_ids))
        if others:
            logging.info("[SMAPI] %s scan-history queue scan(s) created this run are already Pending and left for the manager's normal queue run.", others)
        self._key_map = {}
        self._history_done = set()
        self._queue_scan_ids = []
        return finalized_ids

    def cancel_queue_scan(self, queue_scan_id):
        '''
        Cancels one queue scan by ID.  Raises on failure so the caller can decide - see
        abort_everything for why Cancel rather than Error.
        '''
        self._data_client.queue_scan_update_status(queue_scan_id, 'Cancel')
        logging.info("[SMAPI] Cancelled queue scan with ID %s", queue_scan_id)


    def abort_everything(self, reason):
        '''
        Abandons the queues in progress: discards any batched queue issues that haven't been sent and
        cancels every queue scan created this run, so the manager never picks up a partial load.
        Returns the list of queue scan IDs cancelled (resetting the list, like finalize_everything).

        Only the _key_map scans are cancelled - one status update per assessment type.  Scan-history
        scans are bulk-created straight into Pending and there can be hundreds of them; cancelling those
        one at a time is too expensive to be worth it, and they are self-correcting either way (the cron
        manager processes them normally, and cleanup ages out anything left stuck).

        Cancel, not Error: the api only lets an Agent-role caller move a queue scan
        Loading/Pending -> Loading/Pending/Cancel.  Cancel is excluded from the manager's pending
        search and aged out by its cleanup run, which is the outcome we want either way.

        NOTE: This clears self._key_map, so don't call it until after using that data.
        '''
        logging.error("[SMAPI] Abandoning queue load: %s", reason)
        dropped = len(self._issue_batch['Documents'])
        if dropped:
            logging.warning("[SMAPI] Discarding %s unsent queue issue(s) from the current batch.", dropped)
            self._issue_batch['Documents'] = []
        err_count = 0
        cid = None
        cancelled_ids = []
        for id in self._key_map.keys():
            try:
                cid = self._key_map[id]['sid']
                self._data_client.queue_scan_update_status(cid, 'Cancel')
                cancelled_ids.append(cid)
                logging.info("[SMAPI] Cancelled queue scan with ID %s", cid)
            except Exception as ex:
                err_count += 1
                logging.error("[SMAPI] Error cancelling queue scan with ID %s: [%s] %s", cid, type(ex).__name__, ex)
        if err_count > 0:
            logging.error("[SMAPI] %s error(s) while cancelling queue scans - some may still be picked up by the manager.", err_count)
        self._key_map = {}
        self._history_done = set()
        self._queue_scan_ids = []
        return cancelled_ids

    def map_scanless_asset(self, avid, scanner_vendor, name, version, description, attributes, is_prod=True, assessment_types=[]):
        if len(self._expected_assessment_types) == 0:
            logging.debug("No expected assessment types configured.  Skipping noscan processing.")
            return
        for eat in self._expected_assessment_types:
            if eat in assessment_types:
                continue
            try:
                logging.info("No scan found for expected assessment type '%s', adding noscan queue data...", eat)
                dt_now = datetime.datetime.now(datetime.timezone.utc).isoformat()
                severity = "noscan"
                source = self._get_source({"scanner_vendor": scanner_vendor})
                ptype = SmApiClient._get_product(source, eat)
                atype = self._map_assessment_type(eat)
                report_id = 'noscan|' + atype

                q_scan = {
                    "Timestamp": dt_now,
                    "Saltminer": {
                        "Internal": {
                            "IssueCount": -1,
                            "CurrentQueueScanId": None,
                            "ReplaceIssues": True
                        },
                        "Scan": {
                            "AssessmentType": atype,
                            "ProductType": ptype,
                            "ProductVersion": "",
                            "Product": "Fortify",
                            "Vendor": "Fortify",
                            "ReportId": report_id,
                            "ScanDate": dt_now,
                            "SourceType": source,
                            "IsSaltMinerSource": True,
                            "ConfigName": self._source_name,
                            "AssetType": self._asset_type,
                            "Instance": self._source_name
                        }
                    }
                }
                q_scan = self.add_queue_scan(q_scan)
                q_asset = {
                    "Saltminer": {
                        "Asset": {
                            "Name": name,
                            "Description": description,
                            "VersionId": str(avid),
                            "Version": version,
                            "ConfigName": self._source_name,
                            "SourceType": source,
                            "IsSaltMinerSource": True,
                            "SourceId": str(avid),
                            "IsProduction": is_prod,
                            "AssetType": self._asset_type,
                            "Instance": self._source_name,
                            "Attributes": {},
                            "LastScanDaysPolicy": "30"
                        },
                        "InventoryAsset": {"Key": ""},
                        "Internal": {"QueueScanId": q_scan['id']}
                    },
                    "Timestamp": SmApiClient.clean_date_string(dt_now)
                }
                for attrib in attributes.keys():
                    attrib_val = self._get_attribute(attributes, attrib)
                    if attrib_val:
                        q_asset['Saltminer']['Asset']['Attributes'][attrib] = attrib_val
                if self._inventory_asset_key_attribute and q_asset['Saltminer']['Asset']['Attributes'] and self._inventory_asset_key_attribute in q_asset['Saltminer']['Asset']['Attributes'].keys():
                    q_asset['Saltminer']['InventoryAsset']['Key'] = q_asset['Saltminer']['Asset']['Attributes'][self._inventory_asset_key_attribute]
                q_asset = self.add_queue_asset(q_asset)

                q_issue = {
                    "Saltminer": {
                        "QueueScanId": q_scan['id'],
                        "QueueAssetId": q_asset['id']
                    },
                    "Vulnerability": {
                        "IsActive": True,
                        "FoundDate": SmApiClient.clean_date_string(dt_now),
                        "Id": None,
                        "IsFiltered": False,
                        "IsRemoved": False,
                        "IsSuppressed": False,
                        "Location": "none",
                        "LocationFull": "none",
                        "SourceSeverity": severity,
                        "ReportId": report_id,
                        "Category": ["Application"],
                        "Classification": "",
                        "Description": "",
                        "Enumeration": "",
                        "Name": "No scan found for assessment type " + atype,
                        "Reference": "",
                        "Severity": self._map_severity(severity),
                        "Scanner": {
                            "ApiUrl": "",
                            "GuiUrl": "",
                            "Id": report_id,
                            "AssessmentType": atype,
                            "Product": ptype,
                            "ProductType": ptype,
                            "ProductVersion": "",
                            "Vendor": "Fortify"
                        }
                    },
                    "Timestamp": SmApiClient.clean_date_string(dt_now)
                }
                self.add_queue_issue(q_issue)
                key = f"{avid}|{atype}"
                if key not in self._key_map.keys():
                    self._key_map[key] = {'sid': q_scan['id'], 'aid': q_asset['id'], 'prd': None, 'ptyp': None, 'pver': None}
                else:
                    logging.error("Unexpected app version ID %s and assessment type %s already found in v3 integration keymap.", avid, atype)
            except Exception as ex:
                logging.error("Error!", exc_info=ex)

    def map_everything(self, issue, issue_asset_keys, issue_keys, ssc_history_enable=False):
        try:
            self.map_and_add_scan_and_asset(issue, issue_asset_keys, ssc_history_enable)
            self.map_and_add_issue(issue, issue_keys)
        except Exception:
            logging.error("Failed to map queue resource", exc_info=True)

    def map_scan(self, source, atype, ptype, scan_id, timestamp, issue=None, ssc_v2_scan=None, ssc_v3_scan_id=None):
        q_scan = self._build_scan_doc(source, atype, ptype, scan_id, timestamp, issue, ssc_v2_scan, ssc_v3_scan_id)
        return self.add_queue_scan(q_scan, ssc_v3_scan_id)

    def _build_scan_doc(self, source, atype, ptype, scan_id, timestamp, issue=None, ssc_v2_scan=None, ssc_v3_scan_id=None):
        '''Builds a queue scan document without submitting it to the API.'''
        scan_date = self._get_scan_date(issue, source, ssc_v2_scan)
        q_scan = {
            "Timestamp": timestamp,
            "Saltminer": {
                "Internal": {
                    "IssueCount": 0 if ssc_v3_scan_id else -1,
                    "CurrentQueueScanId": ssc_v3_scan_id or ("NULL" if self._enable_stupid_null else None),
                    "ReplaceIssues": True
                },
                "Scan": {
                    "AssessmentType": atype,
                    "ProductType": ptype,
                    "Product": "Fortify" if not issue else self._nvl(issue, 'engine_type'),
                    "ProductVersion": None if not issue else self._nvl(issue, 'engine_version'),
                    "Vendor": "Fortify",
                    "ReportId": scan_id,
                    "ScanDate": scan_date,
                    "SourceType": source,
                    "IsSaltMinerSource": True,
                    "ConfigName": self._source_name,
                    "AssetType": self._asset_type,
                    "Instance": self._source_name,
                    "Rulepacks": []
                }
            }
        }
        if ssc_v2_scan and 'rulepacks' in ssc_v2_scan:
            q_scan['Saltminer']['Scan']['Rulepacks'] = []
            for rp in ssc_v2_scan['rulepacks']:
                q_scan['Saltminer']['Scan']['Rulepacks'].append({
                    'Id': rp['guid'] if 'guid' in rp else '',
                    'Name': rp['name'] if 'name' in rp else '',
                    'Version': rp['version'] if 'version' in rp else '',
                    'Language': rp['language'] if 'language' in rp else ''
                })
        return q_scan

    def map_and_add_scan_and_asset(self, issue, issue_asset_keys, ssc_all_history_enable=False):
        avid = str(issue['application_version_id'])
        atype = self._map_assessment_type(issue['assessment_type'])
        key = f"{avid}|{atype}"
        if key in self._key_map.keys():
            return  # already seen this avid/assessment type
        source = self._get_source(issue)
        product = SmApiClient._get_product(source, atype)
        sid = str(issue['report_id'])
        if 'SSC' in source.upper():
            v2_ssc_scan = self._get_latest_ssc_scan(avid, issue['engine_type'])
            if not v2_ssc_scan:
                logging.error("No SSC scan was found for app version %s and type '%s', scan with id '%s' will be skipped.", avid, issue['engine_type'], sid)
                return
            sid = SmApiClient._format_scan_id(v2_ssc_scan['artifactUploadDate'], v2_ssc_scan['id'])
        else:
            v2_ssc_scan = None
        section = "queue scan"
        q_scan = None
        try:
            q_scan = self.map_scan(source, atype, product, sid, SmApiClient.clean_date_string(issue['timestamp']), issue, v2_ssc_scan)
            qsid = q_scan['id']
            is_prod = True
            if 'SSC' in source.upper() and key not in self._history_done:
                self._map_and_add_ssc_scan_history(avid, atype, issue['engine_type'], product, q_scan, ssc_all_history_enable)
                self._history_done.add(key)  # don't re-send history if a later mapping step fails and this key is retried
            else:
                if 'sdlc_status' in issue.keys() and issue['sdlc_status'] != 'Production':
                    is_prod = False
            section = "queue asset"
            q_asset = {
                "Saltminer": {
                    "Asset": {
                        "Name": issue['application_name'],
                        "Description": issue['application_description'],
                        "VersionId": avid,
                        "Version": issue['application_version_name'],
                        "ConfigName": self._source_name,
                        "SourceType": source,
                        "IsSaltMinerSource": True,
                        "SourceId": avid,
                        "IsProduction": is_prod,
                        "AssetType": self._asset_type,
                        "Instance": self._source_name,
                        "Attributes": {},
                        "LastScanDaysPolicy": "30"
                    },
                    "InventoryAsset": {"Key": ""},
                    "Internal": {"QueueScanId": qsid}
                },
                "Timestamp": SmApiClient.clean_date_string(issue['timestamp'])
            }
            self.add_diff_attributes(issue_asset_keys, issue, q_asset['Saltminer']['Asset']['Attributes'])
            if self._inventory_asset_key_attribute and q_asset['Saltminer']['Asset']['Attributes'] and self._inventory_asset_key_attribute in q_asset['Saltminer']['Asset']['Attributes'].keys():
                q_asset['Saltminer']['InventoryAsset']['Key'] = q_asset['Saltminer']['Asset']['Attributes'][self._inventory_asset_key_attribute]
            q_asset = self.add_queue_asset(q_asset)

            if key not in self._key_map.keys():
                self._key_map[key] = {'sid': qsid, 'aid': q_asset['id'], 'prd': q_scan['saltminer']['scan']['product'], 'ptyp': q_scan['saltminer']['scan']['productType'], 'pver': q_scan['saltminer']['scan']['productVersion']}

        except KeyError as e:
            msg = f"[SMAPI] Error mapping queue {'asset' if q_scan else 'scan'} - [KeyError] for field {e} (section {section}, local id {avid})."
            logging.error(msg)
            raise SmApiClientException(msg) from e
        except SmApiClientException as e:
            msg = f"[SMAPI] Error mapping queue {'asset' if q_scan else 'scan'} - [SmApiClientException] {e})."
            logging.error(msg)
            logging.warning("[SMAPI] Failed to add %s for app / version with local id %s and scan with local id %s.", section, avid, sid)
            raise SmApiClientException(msg) from e
        except Exception as e:
            msg = f"[SMAPI] Error adding {section} for app / version with local id {avid} and scan with local id {sid}: [{type(e).__name__}] {e}"
            logging.error(msg)
            raise SmApiClientException(msg) from e

    def map_and_add_issue(self, issue, issue_keys):
        q_scan_id = ''
        q_asset_id = ''
        prd = ''
        ptyp = ''
        pver = ''
        source = self._get_source(issue)
        is_ssc = "ssc" in source.lower()
        ssc_instance_id = issue['issue_instance_id'] if is_ssc and 'issue_instance_id' in issue.keys() else "instance_id_missing"
        avid = str(issue['application_version_id'])
        atype = self._map_assessment_type(issue['assessment_type'])
        key = f"{avid}|{atype}"
        gui_url = ""
        if not issue['name']:
            issue['name'] = "NAME UNKNOWN"
        if not issue['scanner_id']:
            if issue['severity'] != "Zero":
                raise ValueError("Missing scanner_id (required) for issue")
            issue['scanner_id'] = source + '|' + avid + '|' + 'zero'
        else:
            gui_url = self._get_gui_url(avid, is_ssc, str(issue['scanner_id']) if not is_ssc else ssc_instance_id)
        try:
            if key not in self._key_map:
                raise SmApiClientException(f"Failure to map issue, KeyMap does not contain App Release id '{avid}' and assessment type '{atype}'.")
            q_scan_id = self._key_map[key]['sid']
            q_asset_id = self._key_map[key]['aid']
            prd = self._key_map[key]['prd']
            ptyp = self._key_map[key]['ptyp']
            pver = self._key_map[key]['pver']

            if not issue['severity']:
                logging.warning("Issue '%s' is missing a severity", issue['scanner_id'])
            tags = None if not issue['tags'] else [issue['tags']]

            issue['location'] = str(issue['location'])
            issue['location_full'] = str(issue['location_full'])
            if self._empty(issue['location']):
                issue['location'] = issue['location_full']
            if self._empty(issue['location']):
                issue['location'] = "[empty]"
            if self._empty(issue['location_full']):
                issue['location_full'] = issue['location']
            if self._empty(issue['location']):
                raise ValueError("Location missing/invalid.")

            custom_attr = {}
            if 'customAttributes' in issue.keys():
                custom_attr = issue['customAttributes']
                for k in custom_attr.keys():
                    if k == 'customAttributes':
                        continue  # in case bug pushes this through
                    custom_attr[k] = str(custom_attr[k])
            if not custom_attr:
                custom_attr = {}
            if 'customTagValues' in issue.keys():
                for kv in issue['customTagValues']:
                    custom_attr[kv['keyValue']['name']] = str(kv['keyValue']['value'])

            q_issue = {
                "Saltminer": {
                    "QueueScanId": q_scan_id,
                    "QueueAssetId": q_asset_id,
                    "Source": {
                        "Analyzer": issue['analyzer'],
                        "Confidence": float(issue['confidence']),
                        "Impact": float(issue['impact']),
                        "IssueStatus": issue['issue_status'],
                        "Kingdom": issue['kingdom'],
                        "Likelihood": float(issue['likelihood'])
                    },
                    "Attributes": custom_attr
                },
                "Vulnerability": {
                    "IsActive": issue['active'],
                    "Audit": {
                        "Audited": issue['audited'],
                        "Auditor": "",
                        "LastAudit": None
                    },
                    "FoundDate": SmApiClient.clean_date_string(issue['found_date']),
                    "Id": None,
                    "IsFiltered": issue['hidden'],
                    "IsRemoved": issue['removed'],
                    "IsSuppressed": issue['suppressed'],
                    "Location": issue['location'],
                    "LocationFull": issue['location_full'],
                    "RemovedDate": SmApiClient.clean_date_string(issue['removed_date']),
                    "SourceSeverity": issue['severity'],
                    "ReportId": str(issue['report_id']),
                    "Category": ["Application"],
                    "Classification": "",
                    "Description": "",
                    "Enumeration": "",
                    "Name": issue['name'],
                    "Reference": issue['reference'],
                    "Severity": self._map_severity(issue['severity']),
                    "Scanner": {
                        "ApiUrl": issue['sor_url'],
                        "GuiUrl": gui_url,
                        "Id": str(issue['scanner_id']),
                        "AssessmentType": atype,
                        "Product": prd,
                        "ProductType": ptyp,
                        "ProductVersion": pver,
                        "Vendor": "Fortify"
                    },
                    "Score": {
                        "Base": float(issue['score_base']),
                        "Environmental": float(issue['score_environmental']),
                        "Temporal": float(issue['score_temporal']),
                        "Version": None
                    }
                },
                "Labels": {},
                "Message": None,
                "Tags": tags,
                "Timestamp": SmApiClient.clean_date_string(issue['timestamp'])
            }
            if 'issue_instance_id' in issue.keys():
                q_issue['Saltminer']['Attributes']['issue_instance_id'] = issue['issue_instance_id']
            if 'primary_rule_guid' in issue.keys():
                q_issue['Saltminer']['Attributes']['primary_rule_guid'] = issue['primary_rule_guid']
            self.add_diff_attributes(issue_keys, issue, q_issue['Saltminer']['Attributes'])
            self.add_queue_issue(q_issue)
        except KeyError as e:
            logging.error("[SMAPI] Error mapping queue issue - KeyError for field %s (local scan id '%s').", e, avid)
            logging.warning("[SMAPI] Scan with local id '%s' may be out of sync with queue scan id '%s'.", avid, q_scan_id)
            raise SmApiClientException("Mapping error") from e
        except SmApiClientException as e:
            logging.error("[SMAPI] Error mapping queue issue - [SmApiClientException] %s", e)
            logging.warning("[SMAPI] Scan with local id '%s' may be out of sync with queue scan id '%s'.", avid, q_scan_id)
        except Exception as ex:
            scan_id = '[unknown]' if 'scan_id' not in issue.keys() else issue['scan_id']
            logging.error("[SMAPI] Error adding issue with local id '%s' to local scan id '%s' (queue scan id %s): [%s] %s", issue['scanner_id'], scan_id, q_scan_id, type(ex).__name__, ex)
            logging.warning("[SMAPI] Scan with local id '%s' may be out of sync with queue scan id '%s'.", avid, q_scan_id)

    def _map_and_add_ssc_scan_history(self, avid, atype, etype, product, ssc_v3_queue_scan, ssc_all_history_enable=False):
        q = {
            "query": {
                "bool": {
                    "must": [
                        {"term": {"projectVersionId": {"value": avid}}},
                        {"term": {"type": {"value": etype}}}
                    ]
                }
            },
            "sort": [{"artifactUploadDate": {"order": "desc"}}]
        }
        scan_scroller = self._es.SearchScroll("sscprojscans", q, 200)
        timestamp = SmApiClient.clean_date_string(datetime.datetime.now(datetime.timezone.utc).isoformat())
        source = self.ssc_source_type
        v3_last_scan = self.search_last_scan(avid, atype, source)
        v3_last_scan_date = datetime.datetime.fromisoformat('1900-01-01') if not v3_last_scan else v3_last_scan['saltminer']['scan']['scanDate']
        count = 0
        history_batch = []
        while scan_scroller.Results:
            for scan_cont in scan_scroller.Results:
                scan = scan_cont['_source']
                h_scan_date = SmApiClient.clean_date_string(scan['artifactUploadDate'])
                try:
                    v3_last_scan_date = dtparse(v3_last_scan_date)
                except Exception:
                    pass
                if not v3_last_scan_date or 'datetime.datetime' not in str(type(v3_last_scan_date)):
                    logging.error("Invalid v3 last scan date '%s' for app version id %s and assessment type '%s'. Skipping scan history record", v3_last_scan_date, avid, atype)
                    continue
                if not ssc_all_history_enable and v3_last_scan_date.date() >= dtparse(h_scan_date).date():
                    continue
                v3_scan_id_current = ssc_v3_queue_scan['saltminer']['scan']['reportId']
                v3_scan_id_new = SmApiClient._format_scan_id(scan['artifactUploadDate'], scan['id'])
                if v3_scan_id_new == v3_scan_id_current or etype != scan['type']:
                    continue
                # Build the doc and send in bulk batches - history scans don't need finalization
                # (QueueStatus goes straight to Pending), so 1x1 POSTs aren't needed.  Pre-assign the
                # doc ID so it can be tracked - the API uses a provided ID as-is (generates one only if empty).
                doc = self._build_scan_doc(source, atype, product, v3_scan_id_new, timestamp, None, scan, ssc_v3_queue_scan['id'])
                doc['Saltminer']['Internal']['QueueStatus'] = "Pending"
                doc['Id'] = str(uuid.uuid4())
                history_batch.append(doc)
                count += 1
                if len(history_batch) >= self.batch_size:
                    self._data_client.queue_scans_add_update_bulk(history_batch)
                    self._queue_scan_ids.extend(d['Id'] for d in history_batch)
                    history_batch = []
            try:
                scan_scroller.GetNext()
            except NotFoundErr:
                logging.warning("[SMAPI] History query failed due to scroll expiration.  History may be truncated for app version %s", avid)
        if history_batch:
            self._data_client.queue_scans_add_update_bulk(history_batch)
            self._queue_scan_ids.extend(d['Id'] for d in history_batch)
        logging.info("[SMAPI] Processed v3 scan history for app version %s and assessment type %s - %s history scan(s).", avid, atype, count)
        try:
            scan_scroller.Clear()
        except Exception:
            pass  # don't care if error on clear

    # ------------------------------------------------------------------
    # Helpers
    # ------------------------------------------------------------------

    @staticmethod
    def _format_scan_id(art_upload_date, id):
        return f"{art_upload_date}~{id}"

    def _get_source(self, issue):
        if 'scanner_vendor' not in issue.keys():
            return 'Unknown'
        if issue['scanner_vendor'] == "Fortify":
            return self.ssc_source_type
        return self.fod_source_type

    @staticmethod
    def _get_product(source, atype="any"):
        if 'SSC' in source:
            if atype == "DAST":
                return "Fortify WebInspect"
            if atype == "SAST":
                return "Fortify SCA"
            return "Fortify SSC"
        return "FOD"

    @staticmethod
    def _map_severity(sev):
        sev = sev.lower()
        sev_map = {"critical": "Critical", "high": "High", "medium": "Medium", "low": "Low", "zero": "Zero", "noscan": "NoScan"}
        return sev_map[sev] if sev in sev_map else "Info"

    @staticmethod
    def _get_scan_date(issue, source, v2_ssc_scan):
        if not issue and not v2_ssc_scan:
            raise SmApiClientException("Invalid call to _get_scan_date, must include non-null issue or v2_ssc_scan")
        if not issue:
            last_scan_date = None
            found_date = None
            scan_date = None
        else:
            last_scan_date = issue['last_scan_date']
            found_date = issue['found_date']
            scan_date = SmApiClient.clean_date_string(last_scan_date) if last_scan_date else SmApiClient.clean_date_string(found_date)
        upload_date = None if not v2_ssc_scan else v2_ssc_scan['artifactUploadDate']
        if 'SSC' in source.upper():
            scan_date = SmApiClient.clean_date_string(upload_date) if upload_date else scan_date
        if not scan_date:
            raise SmApiClientException(f"ScanDate cannot be null (last_scan_date: {last_scan_date}, found_date: {found_date}, artifactUploadDate (SSC): {upload_date}")
        return scan_date

    @staticmethod
    def add_diff_attributes(org_keys, source, target):
        '''
        Update target dict to add fields in source that weren't there when the source's keys (fields) were captured.

        Parameters:
        org_keys - source.keys() before additions
        source   - source dict
        target   - target dict that will receive the additions
        '''
        for k in source.keys():
            if k not in org_keys:
                target[k] = str(source[k])

    @staticmethod
    def clean_date_string(ds):
        if not ds:
            return ds
        # Handle python bug that leaves out a digit sometimes on zero seconds
        if len(ds) == 18:
            ds += '0'
        if len(ds) < 19:
            try:
                ds = dtparse(ds).isoformat()
            except Exception:
                raise ValueError(f"Date string '{ds}' is incorrect")
        i = ds.find(".")
        if i > -1:
            return ds[0:i]
        return ds


class SmApiClientException(Exception):
    pass


class SmApiClientNotFoundException(SmApiClientException):
    pass


class SmApiClientConfigurationException(SmApiClientException):
    pass
