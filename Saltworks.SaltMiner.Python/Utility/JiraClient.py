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

import logging
import json
import time
from requests.auth import HTTPBasicAuth
from datetime import timedelta

from Core.RestClient import RestClient
from Core.Application import ElasticClient

from jira import JIRA

class JiraClient:
    def __init__(self, settings):
        self._Es = ElasticClient(settings)
        self.url = settings.GetSource('JiraImport', 'Url')
        self.userName = settings.GetSource('JiraImport', 'UserName')
        self.apiKey = settings.GetSource('JiraImport', 'ApiKey')
        self.jira = JIRA(self.url, basic_auth=(self.userName, self.apiKey))
        #self.jira_task_ids = self.getCurrentJiraIssues()

    def get_fields(self):
        fields = self.jira.fields()
        for field in fields:
            if field['name'] == 'CVE':
                print(field)

    def getTransitions(self, issue):
        issue =self.jira.issue(issue)
        return self.jira.transitions(issue)

    def findDirectCloseTransition(self, issue):
        for transition in self.getTransitions(issue):
            if transition['name'] == "Direct Close":
                return transition['id']
            
    def directCloseIssue(self, issue):
        transitionId = self.findDirectCloseTransition(issue)
        if transitionId == None:
            logging.debug('[Jira Client] No transition Id found for direct close, unable to close issue.')
        else:
            self.jira.transition_issue(issue, transitionId)

    def getCurrentJiraIssues(self, projectKey):
        update_list = []
        for issue in self.jira.search_issues(f'project={projectKey}', maxResults=10000):
            summary = issue.fields.summary
            if summary.startswith('SM'):
                update_list.append(issue.key)
        return update_list

    def addNewIssue(self, key, issuetype, project, issue, formattedIssue, addLabel= None, attachement = None, directClose = False, cve = None):
        fields = {
            "project":{"key": project},
            "summary": "SM-" + issue['summary'],
            "description": formattedIssue,
            "priority": {'name': str(issue['priority'])},
            "duedate": issue['due_date'],
            "issuetype": {'name': issuetype}
        }
        
        issue_creation = self.jira.create_issue(fields=fields)
        self.sendTaskIdToSM(issue = issue, sidecar_id= issue['jira_sidecar_id'], key=key, task_id = str(issue_creation))
        time.sleep(3)
        if addLabel:
                self.addIssueLabel(str(issue_creation), addLabel)
        if attachement:
            self.addAttachment(str(issue_creation), attachement, 'impacted_assets.csv')

        if cve:
            self.add_issue_cve_field(str(issue_creation), cve_value=cve)
            
        if directClose:
            self.directCloseIssue(str(issue_creation))
            self.closeTicketInSm(str(issue_creation))
            logging.info('[Jira Client Send] Closing Ticket: %s', str(issue_creation))

        
        logging.info("Issue created: %s", str(issue_creation))
        return str(issue_creation)
    
    def updateIssue(self, issue, formattedIssue, addLabel, attachment=None, directClose=False, cve=None):
        try:
            # Validate input
            if not isinstance(issue, dict):
                raise TypeError(f"Expected 'issue' to be a dictionary, got {type(issue)}")

            if "jiraIssueId" not in issue:
                raise KeyError("Key 'jiraIssueId' is missing in issue")
    
            jira_issue_id = issue["jiraIssueId"]
            if attachment:
                self.deleteAttachment(jira_issue_id)
                self.addAttachment(jira_issue_id, issue.get('attachment', ''), 'impacted_assets.csv')
            self.issueUpdate = self.jira.issue(jira_issue_id)

               
            status = self.issueUpdate.fields.status.name

            if status == "Closed":
                logging.info("Issue: %s, Closed, updating in SM", jira_issue_id)
                self.closeTicketInSm(jira_issue_id)
            else:
                if addLabel:
                    if issue.get("due_date"):
                        self.addIssueLabelAndDueDate(issue, addLabel, issue["due_date"], isNew=False)
                    else:
                        self.addIssueLabel(jira_issue_id, addLabel, isNew=False)

                fields_to_update = {}
                if 'summary' in self.issueUpdate.raw.get('fields', {}):
                    fields_to_update['summary'] = "SM-" + issue["summary"]

                else:
                    logging.warning("Field 'summary' not available for issue %s", jira_issue_id)


                if 'description' in self.issueUpdate.raw.get('fields', {}):
                    fields_to_update['description'] = formattedIssue

                else:
                    logging.warning("Field 'description' not available for issue %s", jira_issue_id)

                if fields_to_update:
                    self.issueUpdate.update(**fields_to_update)

                if cve:
                    self.add_issue_cve_field(jira_issue_id, cve_value=cve)


            # Handle direct close
                if directClose:
                    if issue['projectKey'] != 'DISC':
                        self.directCloseIssue(jira_issue_id)
                    self.closeTicketInSm(jira_issue_id)
                    logging.info("[Jira Client Update] Closing ticket: %s", jira_issue_id)


                logging.info("%s issue updated", jira_issue_id)
    
        except KeyError as e:
            logging.error("There was a KeyError updating issue: %s, error: %s", jira_issue_id, str(e))
        except TypeError as e:
            logging.error("There was a TypeError with issue: %s, error: %s", jira_issue_id, str(e))
        except Exception as e:
            logging.error("Unexpected error while updating issue: %s, error: %s", jira_issue_id, str(e)) 

    def updateIssueDetails(self, issue, updates, isNew = True):
        if isNew:
            issueUpdate = self.jira.issue(issue)
        else:
            issueUpdate = self.issueUpdate
        if "labels" in updates:
            updated_labels = issueUpdate.fields.labels
            updated_labels.append(updates["labels"])
            updates["labels"] = updated_labels
        if isNew:
            issueUpdate.update(fields=updates)

    def addIssueLabel(self, issue, label, isNew=True):
        self.updateIssueDetails(issue, {"labels": label}, isNew= isNew)

    def add_issue_cve_field(self, issue, cve_value, isNew= True):
        self.updateIssueDetails(issue, {"customfield_14309": cve_value}, isNew= isNew)

    def addIssueDueDate(self, issue, dueDate, isNew=True ):
        self.updateIssueDetails(issue, {"duedate": dueDate}, isNew= isNew)

    def addIssueLabelAndDueDate(self, issue, label, dueDate, isNew= True):
        self.updateIssueDetails(issue, {"labels": label, "duedate": dueDate}, isNew= isNew)

    def addAttachment(self, issue, attachment, filename):
        self.jira.add_attachment(issue= issue, attachment= attachment, filename= filename)

    def deleteAttachment(self, issueId):
        issueUpdate = self.jira.issue(issueId)
        if issueUpdate.fields.attachment != []:
            attachments = [item for item in issueUpdate.fields.attachment]
            for item in attachments:
                if item == "impacted_assets.csv":
                    self.jira.delete_attachment(item.id)

    def getProjects(self):
        projects = self.jira.projects()
        return projects

    def closeTicketInSm(self, task_id):
        query = {
            "script": {
                "source": "ctx._source.isClosed = true",
                "lang": "painless"
            },
            "query": {
                "term": {
                    "task_id": task_id
                }
            }
        }
        self._Es.UpdateByQuery("jira_task_id_sidecar", queryBody=query)
        


    def sendTaskIdToSM(self, issue, sidecar_id, key, task_id):
        if type(sidecar_id) == list:
            for uniqueId in sidecar_id:
                doc = {
                    "id":uniqueId,
                    "key":issue['doc'][key],
                    "task_id": task_id,
                    "due_date": issue['due_date'],
                    "isClosed": False
                }
                self._Es.Index(index= "jira_task_id_sidecar", doc=doc)
        else:
            doc = {
                    "id":sidecar_id ,
                    "key":issue['doc'][key],
                    "task_id": task_id,
                    "due_date": issue['due_date'],
                    "isClosed": False
                }
            self._Es.Index(index= "jira_task_id_sidecar", doc=doc)

    
    