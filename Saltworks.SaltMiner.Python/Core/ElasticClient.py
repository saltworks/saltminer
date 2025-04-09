''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-04-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

from codecs import ignore_errors
import time
import logging
import json
import os
import uuid
from importlib.metadata import version as vertest

import urllib3
from urllib3.exceptions import ReadTimeoutError
from elasticsearch import Elasticsearch, NotFoundError, exceptions, ConflictError
from elasticsearch import helpers
from elasticsearch.client import SecurityClient, IndicesClient, IngestClient

from .StringUtils import StringUtils

class ElasticClient(object):
    """ Elasticsearch client class """

    def __init__(self, appSettings, configName="Elastic"):
        '''
        Initializes the class.

        appSettings: Settings instance containing application settings
        configName: Config section for this class (usually "Elastic")
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        if not configName or not configName in appSettings.GetConfigNames():
            raise ElasticClientException(f"Invalid or missing configuration for name '{configName}'")
        version = vertest('elasticsearch')
        if not version[0] == '7':
            raise ElasticClientException(f"Expected elasticsearch python package to be version 7.x.x, but found {version} instead.  Please install elasticsearch 7.x.x.")
        self.__RequestTimeout = appSettings.Get(configName, 'RequestTimeout')
        self.__DeleteRequestTimeout = appSettings.Get(configName, 'DeleteRequestTimeout', 30)
        self.__BulkRequestTimeout = appSettings.Get(configName, 'BulkRequestTimeout', 120)
        self.__IndexRequestTimeout = appSettings.Get(configName, 'IndexRequestTimeout', 30)
        self.__RetryDelaySecs = appSettings.Get(configName, 'RetryDelaySecs', 120)
        self.__RetryConflictDelaySecs = appSettings.Get(configName, 'RetryConflictDelaySecs', 5)
        self.__RetryMaxAttempts = appSettings.Get(configName, 'RetryMaxAttempts', 2)
        self.__RetryCount = 0
        self.__App = appSettings.Application
        self.__DefaultScrollSize = appSettings.Get(configName, "DefaultScrollSize", 1000)
        self.__QueryTemplates = appSettings.Get(configName, "QueryTemplates", {})
        self.__MappingsPath = appSettings.Get(configName, "MappingsPath", "Mappings/")
        self.__MappingsTemplatePath = appSettings.Get(configName, "MappingsTemplatePath", "Template/Mappings/")
        self.__App = appSettings.Application
        self.__BulkBatch = []
        self.__BulkBatchSize = 10000

        # setup parameters for the Elasticsearch instance
        scheme = appSettings.Get(configName, 'Scheme')
        auth = (appSettings.Get(configName, 'Username'), appSettings.Get(configName, 'Password'))
        hosts = appSettings.Get(configName, 'Host')
        port = appSettings.Get(configName, 'Port')
        sslVerify = appSettings.Get(configName, 'SslVerify', True)
        try:
            # in case we get a string and not a bool try to manage it
            if sslVerify.lower() == "true":
                sslVerify = True
            if sslVerify.lower() == "false":
                sslVerify = False
        except Exception:
            pass
            
        sslWarn = False if sslVerify == False else True
        
        if scheme == 'http':
            
            logging.warning("No password or https set, this is for POC use only!")
            
            if not appSettings.Get(configName, 'UseAuth', True):
                print('Not using username or password')
                self.es = Elasticsearch( hosts = hosts,
                    scheme = scheme,
                    port = port,
                    request_timeout = self.__RequestTimeout,
                    timeout = self.__RequestTimeout)
            else:
                self.es = Elasticsearch( hosts = hosts,
                    http_auth = auth,
                    scheme = scheme,
                    port = port,
                    request_timeout = self.__RequestTimeout,
                    timeout = self.__RequestTimeout)
        else:
            
            self.es = Elasticsearch( hosts = hosts,
                http_auth = auth,
                scheme = scheme,
                port = port,
                verify_certs = sslVerify,
                ssl_show_warn = sslWarn,
                request_timeout = self.__RequestTimeout,
                timeout = self.__RequestTimeout)

        self.sc = SecurityClient(self.es)
        self.ic = IndicesClient(self.es)
        self.ingestClient = IngestClient(self.es)

        try:
            self.es.info() # test connection

        except Exception as err:
            msg = "" if not hasattr(err, "error") else f" ({err.error})"
            etype = type(err).__name__
            raise ElasticClientConnectionException(f"Elastic client initialization failure, connection failed due to [{etype}]{msg}.") from err

        logging.info(f"Elastic client initialized.  Using host(s) {hosts}, port {port}, scheme {scheme}")

    def ResilientSearch(self, index, body, size=20, scrollTimeout=None, afterKeys=None, includeLockingInfo=False, requestTimeout=None):
        '''
        Calls ES search, trapping and then retrying for connection exceptions

        Parameters:
        index - Index to query.  Can use wildcards.
        body - Query body.  Expected to be json format.  If None passed, will return all data.
        size - Number of docs to return (also scroll size if using scroll).
        scrollTimeout - If included, sets the length of time for the scroll id to be persisted.  "30s" for example.
        afterKeys - If included, used as search_after keys in query.
        includeLockingInfo - If set, return sequence number and primary term with each result for use with locking updates.
        requestTimeout - If set, override elastic client default request timeout. "30" for example.

        NOTE: if scrollTimeout included, will create a scroll ID on the server, which can be expensive for larger data sets but 
        will also lock the results in consistency (new data inserted during scroll will not affect results).  For larger data
        pagination, leave scrollTimeout empty and use afterKeys instead.  If both are present both will be used which may cause an error.
        '''
        if self.__RetryCount == self.__RetryMaxAttempts:
            self.__RetryCount = 0
            raise ElasticClientException("Reached retry limit - see earlier logged errors for details.")
        
        try:
            if not body:
                body = {}
            if afterKeys and len(afterKeys) > 0:
                body['search_after'] = afterKeys
            if not requestTimeout:
                requestTimeout = self.__RequestTimeout
            rsp = self.es.search(body, index, size=size, scroll=scrollTimeout, seq_no_primary_term=includeLockingInfo, request_timeout=requestTimeout)
            self.__RetryCount = 0
            return rsp
            
        except (exceptions.NotFoundError) as e:
            if scrollTimeout:
                logging.error(f"Elasticsearch NotFound error thrown, likely scroll has expired. ({e})")
            return
        except (exceptions.RequestError) as e:
            if e.error == "search_phase_execution_exception":
                if 'error' in e.info.keys():
                    if 'root_cause' in e.info['error'].keys():
                        msg = f"RequestError: {e.info['error']['root_cause'][0]['index']}: {e.info['error']['root_cause'][0]['reason']}"
                    else:
                        msg = f"RequestError: {e.info['error']['caused_by']['reason']}"
                else:
                    msg = f"RequestError: status {e.info['status']} (unable to read error)"
                logging.error(msg)
            raise
        except (exceptions.ConnectionError, exceptions.ConnectionTimeout, exceptions.TransportError, ReadTimeoutError) as e:
            wait = self.__RetryDelaySecs
            msg = e.__str__()
            self.__RetryCount += 1
            logging.error(f"API connection error during search operation - [{type(e).__name__}] {msg}, retrying in {wait} secs...")
            time.sleep(wait)
            return self.ResilientSearch(index, body, size, scrollTimeout, afterKeys, includeLockingInfo)

    def BulkSendBatch(self, index=None, doc=None, docId=None, action="index", batchSize=None):
        '''
        Batches document updates/additions to send via the bulk API.
        
        :index: index upon which to operate (or None to flush remaining)
        :doc: document to send, not including "doc" node for updates (or None to flush remaining)
        :docId: document ID for updates (or index, but not required for additions)
        :action: update or index ('index' upserts, 'update' is a partial doc update that requires the doc ID to be included, None defaults to 'index')
        '''
        if action not in ['index', 'update'] and doc:
            raise ElasticClientArgumentException(f"Invalid action '{action}', must be index or update")
        if doc and '_id' in doc.keys():
            docId = doc['_id']
        if not docId and action == "update":
            raise ElasticClientArgumentException("Update action requires docId parameter, but docId parameter was empty/None")
        if batchSize:
            self.__BulkBatchSize = batchSize
        doc = doc['_source'] if doc and '_source' in doc.keys() else doc
        finishIt = True if not (doc and index) else False
        if doc and index:
            self.__BulkBatch.append(self.BulkInsertDocument(index, doc, docId, action))
        if len(self.__BulkBatch) >= self.__BulkBatchSize or (finishIt and len(self.__BulkBatch) > 0):
            logging.info("Bulk batch send %s items", len(self.__BulkBatch))
            self.BulkInsert(self.__BulkBatch)
            self.__BulkBatch = []

    def BulkInsertDocument(self, index, source, id=None, action=None):
        '''
        Generates a document suitable for bulk insert operations

        Parameters:
        index - the index in which to insert
        source - the document/object to insert
        id - optional unique document id (generates one if not present)
        action - create/index/delete/update (or None to default to index)
        '''
        if action and action not in ['create','delete','index','update']:
            raise ElasticClientValidationException(f"Bulk action '{action}' invalid/unknown.")
        if action in ['delete', 'update'] and not id:
            raise ElasticClientValidationException(f"Bulk action '{action}' requires parameter id to be included.")
        if not id:
            id = uuid.uuid4()
        doc = {
        '_index': index,
        '_id': id
        }
        if not action or action != 'delete':
            doc['_source'] = source
        if action:
            doc['_op_type'] = action
        return doc

    def BulkInsert(self, bulkActions, raiseErrors=True, statsOnly=True):
        '''
        Calls ES bulk insert, trapping and then retrying for connection exceptions
        '''
        if self.__RetryCount == self.__RetryMaxAttempts:
            self.__RetryCount = 0
            raise ElasticClientException("Reached retry limit - see earlier logged errors for details.")
        
        try:
            rsp = helpers.bulk(self.es, bulkActions, request_timeout=self.__BulkRequestTimeout, raise_on_error=raiseErrors, stats_only=statsOnly)
            self.__RetryCount = 0
            return rsp
            
        except NotFoundError:
            if raiseErrors == False:
                pass
        except (exceptions.ConnectionError, exceptions.ConnectionTimeout, exceptions.TransportError, ReadTimeoutError) as e:
            wait = self.__RetryDelaySecs
            msg = e.__str__()
            self.__RetryCount += 1
            logging.error(f"API connection error during bulk operation - [{type(e).__name__}] {msg}, retrying in {wait} secs...")
            time.sleep(wait)
            return self.BulkInsert(bulkActions, raiseErrors, statsOnly)

    def WaitForTask(self, taskId, waitKey=None, waitSec=3, maxWaitCycles=60, conflictsAreBad=False):
        '''
        Waits for an ES task to complete (provide task ID to monitor), then returns resulting response
        
        :taskId: The task ID to monitor
        :waitKey: Descriptive key used in logging output for longer tasks
        :waitSec: How many seconds to wait between status checks, defaults to 3 sec
        :maxWaitCycles: How many times to check before failing, defaults to 60
        :conflictsAreBad: Raise conflict error if conflicts are bad, mkay?
        '''
        cycles = 0
        quickCycles = 10
        waitKey = f" '{waitKey}'" if waitKey else ""
        while cycles <= maxWaitCycles + quickCycles:
            rsp = self.GetTask(taskId)
            if 'error' in rsp.keys() and rsp['error']:
                try:
                    logging.error("%s:%s", rsp['error'], rsp['error']['failed_shards'][0]['reason']['reason'])
                except:
                    pass
                raise ElasticClientSearchFailureException(rsp['error']['reason'])
            if conflictsAreBad and 'task' in rsp.keys() and rsp['task'] and 'status' in rsp['task'].keys() and rsp['task']['status'] and rsp['task']['status']['version_conflicts'] > 0:
                raise exceptions.ConflictError()
                # we should also attempt to cancel the task if still running
            if rsp['completed'] == True:
                return rsp
            total = "?"
            curr = "?"
            if 'task' in rsp.keys() and rsp['task'] and 'status' in rsp['task'].keys() and rsp['task']['status']:
                st = rsp['task']['status']
                curr = st['updated'] + st['deleted'] + st['created']
                total = st['total']
            if cycles >= quickCycles:  # only log data operation if it's not done instantly
                logging.info("Data task%s in progress, %s of %s processed", waitKey, curr, total)
            time.sleep(waitSec if cycles >= quickCycles else .1) # check a lot for the first few cycles
            cycles += 1
        raise ElasticClientTimeoutException(f"Requested task with id '{taskId}' failed to complete after {maxWaitCycles * waitSec} sec")

    def GetTask(self, taskId):
        '''
        Gets ES task information
        '''
        return self.es.tasks.get(taskId)


    def GetTaskCount(self):
        '''
        Gets ES task count (running)
        '''
        tasks = self.es.tasks.list()
        count = 0
        for node in tasks['nodes'].values():
            count += len(node['tasks'])
        return count

    def UpdateByQuery(self, index, queryBody, noWait=False, ignoreConflicts=False, retryConflicts=False, slices=None):
        '''
        Calls ES update_by_query, trapping and then retrying for connection exceptions

        Parameters:
        :index: the index from which to delete; wildcards are possible
        :queryBody: elasticsearch DSL request body (should include "query": {})
        :flushAfter: calls FlushIndex after the delete (no flush will be executed if wait=False)
        :wait: whether or not to wait for the delete operation to complete
        :timeout: API call timeout - if None, uses the default from config
        :ignoreMissingIndex: If False, raise error if index doesn't exist
        :ignoreConflictError: If False, raise ConflictError if occurs
        :slices: The number of slices to use for the delete by query operation. If None (default), slicing is disabled. Use "auto" for automatic slicing or specify an integer for manual slicing.

        Usage:
        UpdateByQuery("index", query) - use this for most queries (slicing is disabled)
        UpdateByQuery("index", query, False, False) - use this when deleting large amounts of data
        UpdateByQuery("index", query, slices="auto") - use this for automatic slicing
        UpdateByQuery("index", query, slices=1) - use this to manually specify the number of slices to use on a large query

        queryBody should look like a regular elasticsearch DSL query, and can include an update "script" as shown in this example:
        {
          "query": {
            "term": {
              "id": {
                "value": "2e411516-5605-4bfd-b612-636e5623d503"
              }
            }
          },
          "script": {
            "source": "ctx._source.saltminer.internal.queue_status = 'Pending'",
            "lang": "painless"
          }
        }

        To "bump" the data in an index, to re-run ingest pipelines for example, only include the "query".
        '''
        rsp = None
        if self.__RetryCount >= self.__RetryMaxAttempts:
            self.__RetryCount = 0
            raise ElasticClientException("Reached retry limit - see earlier logged errors for details.")
        
        conflicts = "proceed" if ignoreConflicts else "abort"
        
        try:
            if slices:
                rsp = self.es.update_by_query(index, queryBody, wait_for_completion=False, conflicts=conflicts, slices=slices)
            else:
                rsp = self.es.update_by_query(index, queryBody, wait_for_completion=False, conflicts=conflicts)
            if not noWait:
                rsp = self.WaitForTask(rsp['task'], f"{index} update by query", conflictsAreBad = not ignoreConflicts)
            self.__RetryCount = 0
            return rsp

        except exceptions.ConflictError as e:
            wait = self.__RetryConflictDelaySecs
            if ignoreConflicts == True:
                if not rsp:
                    rsp = True
                return rsp
            if retryConflicts == True:
                logging.debug("[%s] %s", type(e).__name__, e.__str__())
                logging.error(f"API conflict error during update by query operation, retrying in {wait} secs...")
            else:
                raise
            
        except (exceptions.ConnectionError, exceptions.ConnectionTimeout, exceptions.TransportError, ReadTimeoutError) as e:
            wait = self.__RetryDelaySecs
            logging.error("API connection error during update by query operation - [%s] %s, retrying in {wait} secs...", type(e).__name__, e.__str__())

        self.__RetryCount += 1
        time.sleep(wait)
        return self.UpdateByQuery(index, queryBody, noWait, ignoreConflicts, retryConflicts)
    
    def UpdateDocWithLocking(self, index, docId, doc, seq, pri):
        '''
        Calls ES update, trapping and then retrying for connection exceptions
        This call requires a doc node and can be used for partial updates
        '''
        if self.__RetryCount >= self.__RetryMaxAttempts:
            self.__RetryCount = 0
            raise ElasticClientException("Reached retry limit - see earlier logged errors for details.")
        
        try:
            rsp = self.es.update(index=index, id=docId, doc=doc, if_seq_no=seq, if_primary_term=pri, detect_noop=True)
            self.__RetryCount = 0
            return rsp

        except ConflictError as e:
            return { "result": "noop", "reason": e.info['error']['reason'] }

        except (exceptions.ConnectionError, exceptions.ConnectionTimeout, exceptions.TransportError, ReadTimeoutError) as e:
            wait = self.__RetryDelaySecs
            msg = e.__str__()
            self.__RetryCount += 1
            logging.error(f"API connection error during update with lock operation - [{type(e).__name__}] {msg}, retrying in {wait} secs...")
            time.sleep(wait)
            return self.UpdateDocWithLocking(index, docId, doc, seq, pri)

    def UpdateDoc(self, index, docId, doc):
        '''
        Calls ES update, trapping and then retrying for connection exceptions
        This call requires a doc node and can be used for partial updates
        '''
        return self.UpdateDocWithLocking(index, docId, doc, None, None)

    # https://elasticsearch-py.readthedocs.io/en/7.9.1/api.html#elasticsearch.client.IndicesClient.put_template
    def PutTemplate(self, templateName, body, create=None, includeTypeName=None, masterTimeout=None, order=None,
                    headers=None):
        if not self.ic.exists_template(templateName):
            self.ic.put_template(templateName, body, create=create, include_type_name=includeTypeName,
                                 master_timeout=masterTimeout, order=order, headers=headers)

    def GetIndex(self, indexName, sort=None, limit=1000):
        '''
        Simple query of a single index, returning up to [limit] (max of 1000) rows, optionally after sorting.

        sort parameter should be comma-separated list of <field>:<direction> pairs.

        '''
        if (limit < 1 or limit > 1000):
            limit = 1000
        if sort:
            return self.ResilientSearch(body={ "size": limit, "sort": sort }, index=indexName)
        else:
            return self.ResilientSearch(body={ "size": limit }, index=indexName)

    def RunQuery(self, indexToQuery, queryBody):
        self.__App.Deprecated("ElasticClient.RunQuery", "ElasticClient.RunQuery() is deprecated, please use Search() instead.")
        return self.ResilientSearch(indexToQuery, queryBody)

    def PingServer(self):
        return self.es.ping()

    def RunMatchQuery(self, index, filters):
        ''' 
        Searches an index based on field/value combinations sent in the filters parameter.
        
        Exact matches (==) only.
        '''
        if not type(filters) is dict:
            raise TypeError("Parameter filters should be a dict")
        if not len(filters) > 0:
            raise Exception("Parameter filters must include at least one filter")
        match = {}
        for f in filters.keys():
            match[f] = filters[f]
        return self.RunQuery(index, { "query": { "match": match } })

    def ClearScroll(self, scrollId):
        '''
        Calls ES clear scroll, trapping and then retrying for connection exceptions
        '''
        if self.__RetryCount >= self.__RetryMaxAttempts:
            self.__RetryCount = 0
            raise ElasticClientException("Reached retry limit - see earlier logged errors for details.")
        
        try:
            self.es.clear_scroll(scroll_id = scrollId)
            self.__RetryCount = 0
            
        except (exceptions.ConnectionError, exceptions.ConnectionTimeout, exceptions.TransportError, ReadTimeoutError) as e:
            wait = self.__RetryDelaySecs
            self.__RetryCount += 1
            logging.error(f"API connection error during bulk operation - [{type(e).__name__}] {e}, retrying in {wait} secs...")
            time.sleep(wait)
            self.ClearScroll(scrollId)
        
    def Scroll(self, scrollId, scrollTimeout):
        '''
        Calls ES scroll, trapping and then retrying for connection exceptions
        '''
        if self.__RetryCount >= self.__RetryMaxAttempts:
            self.__RetryCount = 0
            raise ElasticClientException("Reached retry limit - see earlier logged errors for details.")
        
        try:
            rsp = self.es.scroll(scroll_id = scrollId, scroll = scrollTimeout)
            self.__RetryCount = 0
            return rsp
            
        except (exceptions.ConnectionError, exceptions.ConnectionTimeout, exceptions.TransportError, ReadTimeoutError) as e:
            wait = self.__RetryDelaySecs
            self.__RetryCount += 1
            logging.error(f"API connection error during bulk operation - [{type(e).__name__}] {e}, retrying in {wait} secs...")
            time.sleep(wait)
            return self.Scroll(scrollId, scrollTimeout)

    def AddNativeUser(self, username, password, fullName, email=None, roles = []):
        '''
        Add native realm user - only usable if native realm is enabled
        '''
        body = {
            "password": password,
            "roles": roles,
            "email": email,
            "full_name": fullName
        }
        return self.sc.put_user(username, body)

    def DeleteNativeUser(self, username):
        '''
        Deletes specified native user
        '''
        return self.sc.delete_user(username)
        
    def GetRoleMappings(self, name = None):
        '''
        Return either a list of all role mappings or a single role mapping if passing in a name

        Parameters:
        name - Name of role mapping to find (or None to return all)
        '''
        return self.sc.get_role_mapping(name)

    def GetRoles(self, name = None):
        '''
        Return either a list of all roles or a single role if passing in a name

        Parameters:
        name - Name of role to find and return (or None to return all)
        '''
        return self.sc.get_role(name)

    def DeleteRoleMapping(self, name, refresh = True):
        '''
        Deletes role mapping by provided name

        Parameters:
        name - name of the role mapping to delete
        refresh - deprecated, do not use
        '''
        return self.sc.delete_role_mapping(name)

    def DeleteRole(self, name, refresh = True):
        '''
        Deletes role by provided name

        Parameters:
        name - name of the role to delete
        refresh - deprecated, do not use
        '''
        return self.sc.delete_role(name)


    # https://www.elastic.co/guide/en/elasticsearch/reference/current/security-api-put-role.html
    def Role(self, name, roleBody):
        '''
        Creates a new role (or updates existing)

        Parameters:
        name - Name of role to create/update
        roleBody - Role definition
        '''
        return self.sc.put_role(name, roleBody)

    def RoleFromTemplate(self, name, template, dictionary=None):
        '''
        Creates a new role using a template

        Parameters:
        name - Name of role to create/update
        template - template json (as string) to use, optionally with delimited values to replace (i.e. "<<blah>>")
        dictionary - dict of embedded tokens to find and values to use to replace them (don't include delimiters << >> in dict)
        '''
        if dictionary:
            for k in dictionary.keys():
                template = template.replace("<<" + k + ">>", dictionary[k])
        return self.Role(name, json.loads(template))

    # https://www.elastic.co/guide/en/elasticsearch/reference/current/security-api-put-role-mapping.html
    def RoleMapping(self, name, roles, dns=[], groups=[], usernames=[], any=True):
        '''
        Creates role mapping with the specified parameters

        Parameters:
        name - Name of role mapping (must be unique)
        roles - [] of role names
        dns - [] of distinguished names to map
        groups - [] of groups to map
        usernames - [] of usernames to map
        any - use logical OR when multiple usernames and/or groups specified
        '''
        if dns is None or groups is None or usernames is None:
            raise ElasticClientValidationException("DNs, groups, and usernames must be lists (even if empty).")
        if len(dns)  == 0 and len(groups) == 0 and len(usernames) == 0:
            raise ElasticClientValidationException("DNs, groups, and/or usernames must be specified.")
        body = {
                "roles": roles,
                "enabled": True,
                "rules": {
                }
            }
        fields = []
        for u in dns:
            fields.append({ "field": { "dn": u }})
        for g in groups:
            fields.append({ "field": { "groups": g }})
        for u in usernames:
            fields.append({ "field": { "username": u }})
        if any:
            body["rules"] = { "any": fields }
        else:
            body["rules"] = { "all": fields }
        return self.sc.put_role_mapping(name, body)
    
    def Get(self, index, docId, raiseNotFoundError=True):
        '''
        Returns document from specified index and doc id.  Equivalent to GET index/_doc/id
        '''
        if self.__RetryCount >= self.__RetryMaxAttempts:
            self.__RetryCount = 0
            raise ElasticClientException("Reached retry limit - see earlier logged errors for details.")
        
        try:
            rsp = self.es.get(index, docId)
            self.__RetryCount = 0
            return rsp

        except exceptions.NotFoundError as e:
            if raiseNotFoundError == False:
                return e.info
            raise
            
        except (exceptions.ConnectionError, exceptions.ConnectionTimeout, exceptions.TransportError, ReadTimeoutError) as e:
            wait = self.__RetryDelaySecs
            self.__RetryCount += 1
            logging.error(f"API connection error during bulk operation - [{type(e).__name__}] {e}, retrying in {wait} secs...")
            time.sleep(wait)
            return self.Get(index, docId)
    
    def Search(self, index, queryBody=None, size=20, navToData=True, includeLockingInfo=False, requestTimeout=None):
        '''
        Searches elastic for matching docs, returning a max of [size] results.  Not intended for paged results.

        Parameters:
        :index: the index to search
        :queryBody: elasticsearch query
        :size: the number of documents to return
        :navToData: returns response['hits']['hits'] to "jump" to the data portion - if you are doing aggregations, this value must be set to False to get buckets back
        :includeLockingInfo: if set, returns sequence number and primary terms in document results, used for locking operations
        :requestTimeout: If set, override elastic client default request timeout. "30" for example.
        '''
        response = self.ResilientSearch(index, queryBody, size, includeLockingInfo=includeLockingInfo, requestTimeout=requestTimeout)
        if navToData:
            if not response or not 'hits' in response.keys() or 'hits' not in response['hits'] or not response['hits']['hits']:
                return None
            return response['hits']['hits']
        else:
            return response
    
    def SearchScroll(self, index, queryBody=None, scrollSize=0, scrollTimeout="60s", includeLockingInfo=False):
        '''
        Searches Elastic for all matching documents, returning the first resultset in a "scroller" object.
        This object can return all hits, even over 10,000 results
        
        Parameters:
        index - the index to search
        queryBody - elasticsearch query
        scrollSize - the number of documents to return in a single API call
        scrollTimeout - how long to keep the search context alive
        includeLockingInfo - if set, returns sequence number and primary terms in document results, used for locking operations

        Usage:
        with es.SearchScroll('sscprojects', scrollSize=10) as scroller:
            while len(scroller.Results):
                for p in scroller.Results:
                    print(f"{p['_source']['project']['name']} - {p['_source']['name']}")
                scroller.GetNext()

        Notes: 
          For larger datasets, set scrollTimeout to None.  This will result in better performance and can exceed 10k results.
          Sort will be set to id automatically if no sort included.
        '''
        if scrollSize == 0:
            scrollSize = self.__DefaultScrollSize

        scroller = ElasticSearchUtilsScroller(self, index, scrollSize, scrollTimeout, queryBody)
        scroller.IncludeLockingInfo = includeLockingInfo
        return scroller
    
    def Count(self, index, queryBody=None):
        '''
        Searches Elastic for all matching documents, returning the count of the resultset.
        
        Parameters:
        index - the index to search
        queryBody - elasticsearch query

        Usage:
        count =  es.Count('sscprojects', query)
        '''
        count = self.es.count(queryBody, index)
        return count["count"]
    
    def SearchWithCursor(self, keyField, index, queryBody, scrollSize=0, scrollTimeout='10s'):
        
        '''
        [DEPRECATED] Searches Elastic for all sscprojects, adds them to a dictionary and returns them.
        Note: use SearchScroll() instead.  This method will be removed in a future release.
            
            more on dictionaries: https://www.w3schools.com/python/python_dictionaries.asp

        Supports scrolling for lists over 10,000

        #Usage:
        SSCProjects =  ESUtils.SearchWithCursor('ProjectVersionId', 'sscprojversions', query_obj)

        #Print all values in the dictionary, one by one:
        for Project in SSCProjects.values():
            print(Project)

        #Print all key names in the dictionary, one by one:
        for ProjectKey in SSCProjects():
            print(ProjectKey)

        if "12345" in SSCProjects:
            print("Yes, '12345' is one of the keys in the SSCProjects dictionary")

        '''
        self.__App.Deprecated("ElasticClient.SearchWithCursor", "ElasticClient.SearchWithCursor() has been deprecated, please use SearchScroll() instead.")

        if scrollSize == 0:
            scrollSize = self.__DefaultScrollSize

        resp = self.ResilientSearch(index, queryBody, scrollSize, scrollTimeout)
        _scroll_id = ""

        AllDocs = {}

        while resp and 'hits' in resp.keys() and 'hits' in resp['hits'].keys() and len(resp['hits']['hits']):
            # iterate over the docs
            for doc in resp['hits']['hits']:
                if keyField == '_id':
                    AllDocs[doc['_id']] = doc['_source']
                else:
                    AllDocs[doc['_source'][keyField]] = doc['_source']

            if '_scroll_id' in resp.keys() and resp['_scroll_id'] and resp['_scroll_id'] != _scroll_id:
                _scroll_id = resp['_scroll_id']
                # make a request using the Scroll API
                resp = self.Scroll(_scroll_id, scrollTimeout)
            else:
                resp = None

        try:
            if (_scroll_id):
                self.ClearScroll(_scroll_id)
        except Exception as e:
            logging.warning("Elasticsearch clear scroll request failed, ignoring this error: [%s] %s", type(e).__name__, e)
        return AllDocs


    def SearchWithSql(self, sql):

        iCount = 0
        query = {"query": sql,
            "fetch_size": 1000}

        print("Starting SQL")


        _sqlQueryResults = {
            "columns": [],
            "rows": []
        }
        moreRows = True
        while moreRows:
            iCount = iCount + 1
            response =  self.es.sql.query(query, request_timeout = self.__RequestTimeout)
            print('Back from SQL with {} block of 1000'.format(iCount))

            if iCount == 1:
                for col in response['columns']:
                    _sqlQueryResults['columns'].append(col['name'])

            for row in response['rows']:
                _sqlQueryResults["rows"].append(row)
            
            if 'cursor' in response.keys():
                query = {"cursor": response['cursor']}
            else:
                moreRows = False

        return _sqlQueryResults


    def DeleteById(self, index, idToRemove, timeout=None, ignoreMissingIndex=False, ignoreNotFound=False):
        if not timeout:
            timeout = str(self.__DeleteRequestTimeout) + "s"
        if self.__RetryCount >= self.__RetryMaxAttempts:
            self.__RetryCount = 0
            raise ElasticClientException("Reached retry limit - see earlier logged errors for details.")
        if not self.IndexExists(index):
            if ignoreMissingIndex:
                return
            else:
                raise ElasticClientException("Index '%s' does not exist.", index)

        try:
            logging.debug("Running delete by id for index '%s', id %s", index, idToRemove)
            rsp = self.es.delete(index, idToRemove)
            self.__RetryCount = 0
            return rsp

        except exceptions.NotFoundError as e:
            if ignoreNotFound:
                return
            else:
                raise ElasticClientException("Id '%s' not found in index '%s'.", idToRemove, index) from e

        except (exceptions.ConnectionError, exceptions.ConnectionTimeout, exceptions.TransportError, ReadTimeoutError) as e:
            wait = self.__RetryDelaySecs
            msg = "[no message]" if not 'keys' in dir(e) or "message" not in e.keys() else e.message
            self.__RetryCount += 1
            logging.error(f"API connection error during delete by id operation - [{type(e).__name__}] {msg}, retrying in {wait} secs...")
            time.sleep(wait)
            return self.DeleteById(index, idToRemove)
        

    def DeleteByMatchQuery(self, index, filters, ignoreMissingIndex=False):
        ''' Deletes from index based on one or more match conditions.  Filters parameter should be dict of field:value. '''
        if not type(filters) is dict:
            raise TypeError("Parameter filters should be a dict")
        if not len(filters) > 0:
            raise Exception("Parameter filters must include at least one filter")
        if not self.IndexExists(index):
            if ignoreMissingIndex:
                return
            else:
                raise ElasticClientException("Index '%s' does not exist.", index)
        filt = []
        for f in filters.keys():
            filt.append({ "match": { f: filters[f] } })
        q = {
              "query": {
                "bool": {
                  "must": [],
                  "filter": filt
                }
              }
            }
        return self.DeleteByQuery(index, q)

    def DeleteAllByQuery(self, index, timeout=None, ignoreMissingIndex=False):
        '''
        Deletes all data from an index without dropping the index.  Best suited for large volume indices, 
        as will not wait for completion and will not flush the index after delete is completed.
        '''
        self.DeleteByQuery(index, { "query": { "match_all": {} } }, False, False, timeout, ignoreMissingIndex)
    
    def DeleteByQuery(self, index, queryBody, flushAfter=True, wait=True, timeout=None, ignoreMissingIndex=False, ignoreConflictError=False, slices=None):
        '''
        Deletes from specified index using specified query body

        Parameters:
        :index: the index from which to delete; wildcards are possible
        :queryBody: elasticsearch DSL request body (should include "query": {})
        :flushAfter: calls FlushIndex after the delete (no flush will be executed if wait=False)
        :wait: whether or not to wait for the delete operation to complete
        :timeout: API call timeout - if None, uses the default from config
        :ignoreMissingIndex: If False, raise error if index doesn't exist
        :ignoreConflictError: If False, raise ConflictError if occurs
        :slices: The number of slices to use for the delete by query operation. If None (default), slicing is disabled. Use "auto" for automatic slicing or specify an integer for manual slicing.

        Usage:
        DeleteByQuery("index", query) - use this for most queries where slicing is disabled
        DeleteByQuery("index", query, False, False) - use this when deleting large amounts of data
        DeleteByQuery("index", query, slices="auto") - use this for automatic slicing
        DeleteByQuery("index", query, slices=1) - use this to manually specify the number of slices
        '''
        conflicts = 'proceed' if ignoreConflictError else 'abort'
        if not self.IndexExists(index):
            if ignoreMissingIndex:
                return None
            else:
                raise ElasticClientException("Index '%s' does not exist.", index)
        if not timeout:
            timeout = self.__DeleteRequestTimeout
        timeout = int(timeout)
        if self.__RetryCount >= self.__RetryMaxAttempts:
            self.__RetryCount = 0
            raise ElasticClientException("Reached retry limit - see earlier logged errors for details.")

        try:
            logging.debug("Running delete by query for index '%s' with timeout '%s'", index, timeout)
            self.RefreshIndex(index)
            if slices:
                rsp = self.es.delete_by_query(index, queryBody, wait_for_completion=False, conflicts=conflicts, request_timeout=timeout, slices=slices)
            else:
                rsp = self.es.delete_by_query(index, queryBody, wait_for_completion=False, conflicts=conflicts, request_timeout=timeout)
            if wait:
                rsp = self.WaitForTask(rsp['task'])
            if flushAfter and wait:
                self.FlushIndex(index)
            self.__RetryCount = 0
            return rsp
        except (exceptions.ConnectionError, exceptions.ConnectionTimeout, exceptions.TransportError, ReadTimeoutError) as e:
            delay = self.__RetryDelaySecs
            msg = e.__str__()
            self.__RetryCount += 1
            logging.error("API connection error during delete by query operation - [%s] %s, retrying in %s secs...", type(e).__name__, msg, delay)
            time.sleep(delay)
            return self.DeleteByQuery(index, queryBody, flushAfter, wait, timeout, ignoreMissingIndex)

    def ExecuteEnrichPolicy(self, policy):
        try:
            logging.info("Attempting to execute enrichment policy '%s'", policy)
            self.es.enrich.execute_policy(policy, wait_for_completion=False)
            logging.info("Enrichment policy '%s' execution requested successfully", policy)
        except Exception as e:
            logging.warning("Unable to execute enrich policy %s: [%s] %s", policy, type(e).__name__, e)

    def UpdateWithLocking(self, index, doc, docId, seqNo, priTerm, timeout=None):
        '''
        Requires full document, calls es index API
        '''
        if not timeout:
            timeout = str(self.__IndexRequestTimeout) + "s"
        if self.__RetryCount >= 3:
            self.__RetryCount = 0
            raise ElasticClientException("Reached retry limit - see earlier logged errors for details.")

        try:
            logging.debug("Updating document in index '%s'", index)
            rsp = self.es.index(index=index, id=docId, body=doc, if_primary_term=priTerm, if_seq_no=seqNo, timeout=timeout)
            self.__RetryCount = 0
            return rsp

        except ConflictError as e:
            return { "result": "noop", "reason": e.info['error']['reason'] }
        except (exceptions.ConnectionError, exceptions.ConnectionTimeout) as e:
            wait = self.__RetryDelaySecs
            msg = e.__str__()
            self.__RetryCount += 1
            logging.error(f"API connection error during update operation - [{type(e).__name__}] {msg}, retrying in {wait} secs...")
            time.sleep(wait)
            return self.Index(index, doc, docId, seqNo, priTerm, timeout)

    def IndexWithId(self, index, docId, doc, timeout=None, createOnly=False):
        if not timeout:
            timeout = str(self.__IndexRequestTimeout) + "s"
        if self.__RetryCount >= self.__RetryMaxAttempts:
            self.__RetryCount = 0
            raise ElasticClientException("Reached retry limit - see earlier logged errors for details.")

        try:
            logging.debug("Inserting document into index '%s'", index)
            op_type = None if not createOnly else 'create'
            rsp = self.es.index(index=index, id=docId, body=doc, op_type=op_type, timeout=timeout)
            self.__RetryCount = 0
            return rsp

        except ConflictError:
            return { "result": "noop" }
        except (exceptions.ConnectionError, exceptions.ConnectionTimeout) as e:
            wait = self.__RetryDelaySecs
            msg = e.__str__()
            self.__RetryCount += 1
            logging.error(f"API connection error during index operation - [{type(e).__name__}] {msg}, retrying in {wait} secs...")
            time.sleep(wait)
            return self.IndexWithId(index, docId, doc, timeout, createOnly)

    def Index(self, index, doc, timeout=None, createOnly=False):
        return self.IndexWithId(index, None, doc, timeout, createOnly)

    def IndexExists(self, name):
        return self.es.indices.exists(name)

    def FlushIndex(self, index):
        try:
            self.es.indices.flush(index)
        except urllib3.exceptions.ReadTimeoutError:
            delay = self.__RetryDelaySecs
            logging.warning(f"Timeout occurred when attempting to flush index '{index}'.  Will retry after a {delay} sec delay.")
            time.sleep(delay)
            self.es.indices.flush(index)
    
    def RefreshIndex(self, index):
        try:
            self.es.indices.refresh(index)
        except urllib3.exceptions.ReadTimeoutError:
            delay = self.__RetryDelaySecs
            logging.warning(f"Timeout occurred when attempting to refresh index '{index}'.  Will retry after a {delay} sec delay.")
            time.sleep(delay)
            self.es.indices.refresh(index)

    def GetMapping(self, mappingName, default=None):
        mn = StringUtils.CamelCase(mappingName)
        fp1 = os.path.join(self.__MappingsPath, mn + ".json")
        fp2 = os.path.join(self.__MappingsTemplatePath, mn + ".json")
        fp = fp1
        if not os.path.exists(fp1):
            fp = fp2
            if not os.path.exists(fp2):
                if default:
                    logging.info("Mapping not found for mapping name '%s'. Paths searched: '%s', '%s'.  Returning provided default.", mappingName, fp1, fp2)
                    return default
                raise ElasticClientMappingException(f"Mapping not found for mappingName '{mappingName}'. Paths searched: '{fp1}', '{fp2}'")
        try:
            with open(fp) as json_data:
                return json.load(json_data)
        except Exception as e:
            raise ElasticClientMappingException(f"Error loading mapping for mappingName '{mappingName}' from '{fp}'") from e
    
    def GetIndexMapping(self, index):
        '''
        Gets mapping by index
        '''
        mapping =self.es.indices.get_mapping(index)
        return mapping
        
    def MapIndex(self, indexToMap, force):
        if type(force) is dict:
            raise ElasticClientMappingException("MapIndex no longer accepts mapping body.  See ElasticClient.py.")
        mapping = self.GetMapping(indexToMap)
        self.MapIndexWithMapping(indexToMap, mapping, force)

    def MapIndexWithMapping(self, indexToMap, mapping, force):
        if not type(mapping) is dict:
            raise ElasticClientMappingException("Invalid mapping body.  Requires dict.")

        exists = self.es.indices.exists(index=indexToMap)
        
        if force and exists:
            logging.warning(f"Re-mapping index '{indexToMap}'.  Data loss may occur.")
            self.es.indices.delete(index=indexToMap)
            exists = False

        if not exists:
            response = self.es.indices.create(index=indexToMap, body=mapping )
            msg = 'Mapping {} - {}'.format(indexToMap, response)
            logging.debug(msg)

        if exists:
            msg = 'Mapping {} - already exists (use force=True to overwrite)'.format(indexToMap)
            logging.debug(msg)


    def GetAlias(self, index, name=None, params=None, allow_no_indices= True):
        '''
        Returns an alias. 
        https://www.elastic.co/guide/en/elasticsearch/reference/7.17/indices-aliases.html

        '''
        logging.info('Finding alias for %s', index)
        aliasData = self.ic.get_alias(index= index, name=name, params=params, allow_no_indices=allow_no_indices)
        if len(aliasData[index]['aliases']) >= 1:
                return aliasData
        else:
            logging.info('There are no aliases for the index named: %s', index)
            return None

    def PutAlias(self, inIndex, aliasName, aliasBody, force):
        '''Creates / Updates an alias for the given index'''

        forceNeededToCreate = True  # Assume it exists and we would need to force 

        try:
            self.es.indices.get_alias(index=inIndex, name=aliasName)
        except exceptions.NotFoundError:
            forceNeededToCreate = False

        if force == False and forceNeededToCreate == True:
            #It exists and we were not asked to force the update.  Just exit.
            return
        if aliasBody == '':
            self.es.indices.put_alias(index=inIndex, name=aliasName)
        else:
            self.es.indices.put_alias(index=inIndex, name=aliasName, body=aliasBody)


    def IndexSetRefreshInterval(self, index, interval):
        _settings = {"index": {"refresh_interval": interval}}
        self.__PutSettings(index, _settings)


    def PutSettings(self, index, settings):
        self.es.indices.put_settings(index=index, body=settings)


    def DeleteIndex(self, index):
        exists = self.es.indices.exists(index=index)
        if exists:
            self.es.indices.delete(index=index)


    def CloneIndex(self, srcIndex, destIndex, overwrite=False):
        if self.IndexExists(destIndex):
            if overwrite:
                self.DeleteIndex(destIndex)
            else:
                raise ElasticClientException(f"Destination index '{destIndex}' already exists.  Set overwrite to enable replacing it.")
        if not self.IndexExists(srcIndex):
            raise ElasticClientException(f"Source index '{srcIndex}' does not exist.")
        self.es.indices.clone(index=srcIndex, target=destIndex)


    def Reindex(self, srcIndex, destIndex, destPipeline=None, block=True):
        '''
        :srcIndex: Source index (copy from)
        :destIndex: Destination index (copy to)
        :destPipeline: Destination pipeline
        :block: If set, block thread until reindex is complete.  Otherwise do not block, returning task object.
        '''
        if not self.IndexExists(srcIndex):
            raise ElasticClientException(f"Source index '{srcIndex}' does not exist.")
        body = { "source": { "index": srcIndex } , "dest": { "index": destIndex } }
        if destPipeline:
            body['dest']['pipeline'] = destPipeline
        return self.es.reindex(body, wait_for_completion=block)
    
    def GetPipeline(self, pipelineName = None, error_trace=None, filter_path=None, human=None, master_timeout=None, pretty=None, summary=None):
        '''
        https://elasticsearch-py.readthedocs.io/en/latest/api/ingest-pipelines.html
        Returns a pipeline by the name also known as the id. If you omit the id parameter it returns all pipelines
        example on how to get pipeline by Name: 
        _Es = ElasticClient(app.Settings)
        data = helper.GetPipeline(pipelineName = "saltminer-issues-risk-roller-pipeline")

        too easy
        '''
        return self.ingestClient.get_pipeline(id=pipelineName, error_trace=error_trace, filter_path=filter_path, human=human, master_timeout=master_timeout, pretty= pretty, summary = summary)

    def PutPipeline(self, id, body):
        '''
        https://elasticsearch-py.readthedocs.io/en/latest/api/ingest-pipelines.html
        Creates or updates a pipeline. Must specify the id to update a specific pipeline
        The on_failure parameter specifies what to do on the failure of the specific pipeline and it will stop the pipeline at the 
        processor that failed and run the on failure handler. In order to add on failure parameters to the processors you must specify
        this within the processor itself. 
        '''
        self.ingestClient.put_pipeline(id, body)#, on_failure= on_failure, version=version)

class ElasticSearchUtilsScroller(object):
    def __init__(self, elasticClient, index, scrollSize, scrollTimeout, queryBody=None):
        if type(elasticClient).__name__ != "ElasticClient":
            raise TypeError(f"Type of elasticClient must be 'ElasticClient', not '{type(elasticClient).__name__}'")
        defSort = [ { "id": "asc" } ]
        if not queryBody:
            queryBody = { "sort": defSort }
        if not "sort" in queryBody.keys():
            queryBody["sort"] = defSort
        self.__Es = elasticClient
        self.ScrollId = None
        self.Index = index
        self.ScrollSize = scrollSize
        self.ScrollTimeout = scrollTimeout
        self.AfterKeys = None
        self.QueryBody = queryBody
        self.IncludeLockingInfo = False
        self.TotalHits = 0
        self.__First = True
        self.GetNext()
        logging.debug(f"Scroller init")

    def __enter__(self):
        return self
    def __exit__(self, exc_type, exc_value, exc_traceback):
        self.Clear()

    def GetNext(self):
        if not self.__First and not len(self.Results):  # don't call the API for more results if already empty
            return
        self.__First = False

        if self.ScrollId:
            # make a request using the Scroll API
            result = self.__Es.Scroll(self.ScrollId, self.ScrollTimeout)
        else:
            result = self.__Es.ResilientSearch(self.Index, self.QueryBody, self.ScrollSize, self.ScrollTimeout, self.AfterKeys, self.IncludeLockingInfo)

        if result:
            self.Results = result['hits']['hits']
            if 'total' in result['hits'].keys():
                rel = result['hits']['total']['relation']
                if rel == 'eq':
                    self.TotalHits = result['hits']['total']['value']
                elif rel == 'gte':
                    uq = json.loads(json.dumps(self.QueryBody))
                    uq.pop('sort', '')
                    uq.pop('search_after', '')
                    uq.pop('_source', '')
                    self.TotalHits = self.__Es.Count(self.Index, uq)
                else:
                    self.TotalHits = 0 # unexpected
            else:
                self.TotalHits = 0
            self.ScrollId = result['_scroll_id'] if '_scroll_id' in result.keys() else None
            self.AfterKeys = self.Results[len(self.Results) - 1]['sort'] if len(self.Results) > 0 and not self.ScrollId else None
            logging.debug("Scroller GetNext: results found")
        else:
            self.Results = []
            logging.debug("Scroller GetNext: no results")

    def Generator(self):
        '''
        Returns all results as a generator (iterable).
        '''
        while self.Results:
            for r in self.Results:
                yield r
            self.GetNext()

    def GetAll(self):
        '''
        Assembles all results into a single collection in memory.
        Please use responsibly.
        '''
        lst = []
        while self.Results:
            lst += self.Results
            self.GetNext()
        return lst

    def Clear(self):
        '''
        Resets scroller (and clears scroll in elasticsearch)
        '''
        if not self.ScrollId:
            return
        self.Results = None
        self.TotalHits = 0
        try:
            self.__Es.ClearScroll(self.ScrollId)
        except Exception as e:
            logging.warning("Failed to clear scroll in elastic for scroller: %s", e)
        self.ScrollId = None

class ElasticClientException(Exception):
    pass
class ElasticClientValidationException(ElasticClientException):
    pass
class ElasticClientArgumentException(ElasticClientException):
    pass
class ElasticClientMappingException(ElasticClientException):
    pass
class ElasticClientConnectionException(ElasticClientException):
    pass
class ElasticClientTimeoutException(ElasticClientException):
    pass
class ElasticClientSearchFailureException(ElasticClientException):
    pass