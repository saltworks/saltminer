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
import csv
import platform

from Core.FodClient import FodClient
from Utility.ProgressLogger import ProgressLogger
from Utility.DImport import DImport

class AuthHelperException(Exception):
    pass

class AuthHelperCustomAttribute(object):
    def __init__(self):
        pass
    def GetAttribute(self, app):
        '''
        Given application app, returns attribute to be used for sorting out roles (i.e. first 10 chars of name for UAID)

        Override this for the customer as needed
        '''
        pass

class AuthHelper(object):
    def __init__(self, appSettings, sourceName, authConfigName="FodAuth", mainConfigName="Main"):
        self.__Es = appSettings.Application.GetElasticClient()
        self.__Fod = FodClient(appSettings, sourceName)
        self.__ReportsFolder = appSettings.Get(mainConfigName, 'ReportsFolder')  # used to output CSV report
        self.__CustomerCode = appSettings.Get(mainConfigName, 'CustomerCode')
        self.__GlobalRoleName = appSettings.Get(authConfigName, 'GlobalRoleName', '') # if set, creates a role mapping for all users created to the specified global role
        self.__App = appSettings.Application
        self.__AuthConfigName = authConfigName
        self.__UniversalRoles = appSettings.Get(authConfigName, 'UniversalRoles', ['Security Lead'])
        self.__Mode = appSettings.Get(authConfigName, "Mode", "")
        if not self.__Mode or self.__Mode.lower() not in ["attribute", "custom"]:
            raise AuthHelperException("Invalid 'Mode' in settings, expected one of these values: ['Attribute', 'Custom']")

        
    def __GetSetting(self, key, default=None):
        return self.__App.Settings.Get(self.__AuthConfigName, key, default)

    def __UniversalRoleHandling(self, roleUsers):
        '''
        roleUsers structure expected:
        { "Role": { "groups": [], "users": [ "joe@domain.com", "jill@domain.com" ] }
        '''
        es = self.__Es

        # Config
        rolePrefix = self.__GetSetting('UniversalRolePrefix', '')
        roleMappingPrefix = self.__GetSetting('UniversalRoleMappingPrefix', '')
        roleTemplate = self.__GetSetting('UniversalSecurityRoleTemplate', '')
        if not rolePrefix or not roleMappingPrefix or not roleTemplate:
            raise AuthHelperException(f"One or more settings invalid in settings file {self.__AuthConfigName}.  This feature requires UniversalRolePrefix, UniversalRoleMappingPrefix, and UniversalSecurityRoleTemplate.")

        # Add needed roles & role mappings
        rnames = []
        logging.info("Adding/updating elasticsearch universal roles & role mappings")
        for u in roleUsers.keys():
            name = u.lower().replace(" ", "-") # i.e. 'security-lead'
            rnames.append(name)
            logging.debug(f"Adding role '{rolePrefix}{name}'")
            es.RoleFromTemplate(f"{rolePrefix}{name}", roleTemplate)
            roles = [f"{rolePrefix}{name}"]
            if self.__GlobalRoleName:
                roles.append(self.__GlobalRoleName)
            es.RoleMapping(f"{roleMappingPrefix}{name}", roles, roleUsers[u]['users'], roleUsers[u]['groups'])

        # Find and remove roles & mappings that shouldn't be present
        logging.info("Removing unneeded elasticsearch universal roles/role mappings")
        rList = es.GetRoles()
        for r in rList.keys():
            ra = r.replace(rolePrefix, "")
            if r.startswith(rolePrefix) and ra not in rnames:
                logging.debug(f"Removing role '{r}' and role mapping '{roleMappingPrefix + ra}'")
                es.DeleteRole(r)
                es.DeleteRoleMapping(roleMappingPrefix + ra)

    def __RoleHandling(self, pl, attrUsers):
        '''
        attrUsers structure expected:
        { "RoleKeyValue": { 
            "groups": [], 
            "users": [ "cn=blah,ou=my ou,dc=domain,dc=local", "cn=blah2,ou=my ou,dc=domain,dc=local" ], 
            "tdict": { "field1": "value1", "field2": "value2" } }
        '''
        es = self.__Es

        # Config
        rolePrefix = self.__GetSetting('RolePrefix')
        roleMappingPrefix = self.__GetSetting('RoleMappingPrefix')
        roleTemplate = self.__GetSetting('SecurityRoleTemplate')
        if not rolePrefix or not roleMappingPrefix or not roleTemplate:
            raise AuthHelperException(f"One or more settings invalid in settings file {self.__AuthConfigName}.  This feature requires: RolePrefix, RoleMappingPrefix, SecurityRoleTemplate.")

        haveWork = True
        if not attrUsers or len(attrUsers.keys()) == 0:
            logging.info("No roles to map.")
            haveWork = False

        if haveWork:
            # Add needed roles
            logging.info("Adding/updating elasticsearch roles")
            pl.Progress(1, "Adding/updating elasticsearch roles", len(attrUsers))
            count = 1
            for a in attrUsers.keys():
                if count == 1 or count % 100 == 0:
                    pl.Progress(count, "Adding/updating elasticsearch roles")
                logging.debug(f"Adding role '{rolePrefix}{a}'")
                tdict = {}
                if 'tdict' in attrUsers[a].keys() and attrUsers[a]['tdict']:
                    tdict = attrUsers[a]['tdict']
                es.RoleFromTemplate(f"{rolePrefix}{a}", roleTemplate, tdict)
                count += 1

            # Adding needed role mappings
            logging.info("Adding/updating elasticsearch role mappings")
            pl.Progress(1, "Adding/updating elasticsearch role mappings", len(attrUsers))
            count = 1
            for a in attrUsers.keys():
                if count == 1 or count % 100 == 0:
                    pl.Progress(count, "Adding/updating elasticsearch role mappings")
                logging.debug(f"Adding role mapping '{roleMappingPrefix}{a}'")
                roles = [f"{rolePrefix}{a}"]
                if self.__GlobalRoleName:
                    roles.append(self.__GlobalRoleName)
                es.RoleMapping(f"{roleMappingPrefix}{a}", roles, attrUsers[a]['users'], attrUsers[a]['groups'])
                count += 1
        # end if

        # Find and remove roles & mappings that shouldn't be present
        logging.info("Removing unneeded elasticsearch roles/role mappings")
        rList = es.GetRoles()
        pl.Progress(1, "Removing unneeded elasticsearch roles & mappings", len(rList.keys()))
        count = 1
        for r in rList.keys():
            if count == 1 or count % 100 == 0:
                pl.Progress(count, "Looking for (and removing) unneeded elasticsearch roles & mappings")
            ra = r.replace(rolePrefix, "")
            if r.startswith(rolePrefix) and ra not in attrUsers.keys():
                logging.debug(f"Removing role '{r}' and role mapping '{roleMappingPrefix + ra}'")
                es.DeleteRoleMapping(roleMappingPrefix + ra)
                es.DeleteRole(r)
            count += 1

    def __GetUsersGroupsForApplications(self, appList, pl):
        '''
        Pull users/groups for the selected applications

        appList expected to look like this (other attribs fine if present but will be dropped):
        [ { "applicationId": "10077", "attributeValue": "Key To Differentiate Kibana Roles" } ]

        attrs should be a list of attributes to include other than "applicationId" in the new list

        returns new list because target structure is different
        '''
        
        # We first get a userId -> email dict together, since email is needed and isn't found in the app user access endpoint results
        logging.info("Retrieving user list")
        allUsers = {}
        sUsers = self.__Fod.GetUsers(isSuspended=False, orderBy='userId', fields='userId,email', scroller=True)
        while sUsers.Results:
            for u in sUsers.Results:
                allUsers[u['userId']] = u['email']
            sUsers.GetNext()
        logging.info("%s users retrieved", len(allUsers.keys()))
        
        pl.Start("Fod API auth calls", len(appList))
        count = 1
        for e in appList:
            logging.debug(f"Calling Fod auth entities endpoint for pv {e['applicationId']}")
            if count == 1 or count % 100 == 0:
                pl.Progress(count, "Getting users/groups for selected applications")
            users = self.__Fod.GetApplicationUserAccess(e['applicationId']).Content['users']
            e['users'] = []
            for u in users:
                # lookup email in allUsers dict (skip if not found - may be suspended user)
                if u['userId'] in allUsers.keys():
                    e['users'].append(allUsers[u['userId']])
            e['groups'] = []
            # Groups not supported at this time
            count += 1
        
        # Rearrange data into list format we need - the new format looks similar, but we make sure each attribute has all eligible users/groups assigned, not just a single pv
        #   from: { "applicationId": "10077", "attribute": "RoleKeyValue", "users": ["joe@domain.com", "jill@domain.com"], "groups": [] }
        #   to:   { "RoleKeyValue": { "groups": [], "users": ["joe@domain.com", "jill@domain.com"], "tdict": { "rkey": "10077" } }
        logging.info("Arranging data into optimized format")
        pl.Progress(1, "Optimizing data", len(appList))
        attrUsers = {}
        count = 1
        for e in appList:
            if count == 1 or count % 10 == 0:
                pl.Progress(count, "Data optimization")
            a = e['attribute']
            if a:
                if a not in attrUsers.keys():
                    attrUsers[a] = { "users": [], "groups": [], "tdict": { "rkey": a } }
                for u in e['users']:
                    if u not in attrUsers[a]['users']:
                        attrUsers[a]['users'].append(u)
                for u in e['groups']:
                    if u not in attrUsers[a]['groups']:
                        attrUsers[a]['groups'].append(u)
            count += 1
        # eliminate empties (no users/groups assigned)
        newD = {}
        for a in attrUsers.keys():
            if len(attrUsers[a]['users']) > 0 or len(attrUsers[a]['groups']) > 0:
                newD[a] = attrUsers[a]
        return newD

    def __GetApplicationsWithAttribute(self, attr):
        '''
        Returns list of Fod app ids and their attribute values for the given attribute name

        Parameters:
        attr - the attribute name to find
        esClient - elasticsearch client to use to run the query
        '''
        body = { 
            "_source": ["applicationId","attributes"],
            "sort": ["applicationId"]
        }
        appList = []
        scroller = self.__Es.SearchScroll('fodapplications', body, 200, None)
        while len(scroller.Results) > 0:
            for i in scroller.Results:
                if 'attributes' in i['_source'].keys() and attr in i['_source']['attributes'].keys():
                    appList.append({"applicationId": i['_source']['applicationId'], "attribute": i['_source']['attributes'][attr]})
            scroller.GetNext()
        return appList

    def __GetApplicationsWithCustomAttribute(self, custAttrClass):
        '''
        Returns list of Fod application ids and their attribute values based on a customized function

        Parameters:
        custAttrClass - class inheriting from AuthHelperCustomAttribute and implementing its one method

        '''
        if not isinstance(custAttrClass, AuthHelperCustomAttribute):
            raise AuthHelperException("Class of type AuthHelperCustomAttribute (or a child) required for parameter custAttrClass")
        appList = []
        scroller = self.__Es.SearchScroll('fodapplications', None, 200, None)
        while len(scroller.Results) > 0:
            for i in scroller.Results:
                a = custAttrClass.GetAttribute(i['_source'])
                if a:
                    appList.append({"applicationId": i['_source']['applicationId'], "attribute": a})
            scroller.GetNext()
        return appList

    def __GetUniversalRoleAuthEntities(self):
        '''
        Returns a list of roles and their LDAP groups and users with universal access
        { "Role": { "groups": [], "users": [ "joe@domain.com", "jill@domain.com" ] }
        '''
        roles = '|'.join(self.__UniversalRoles)
        lst = self.__Fod.GetUsers(isSuspended=False, roleName=roles, orderBy='roleName', fields='email,roleName')
        outList = {}
        # Users
        for item in lst.Content['items']:
            role = item['roleName']
            if role not in outList.keys():
                outList[role] = { "groups": [], "users": [] }
            outList[role]['users'].append(item['email'])
        # Groups?
        return outList


    def __WriteUserProjectVersionAssignmentCsv(self, file, overwrite=False):
        '''
        Writes a list of Fod project version ids, projects, and versions and their assigned users to CSV
        '''

        # This hasn't been implemented - can see SSC AuthHelper for starting point
        raise AuthHelperException("Not yet implemented")
    
    def UserProjectAssignmentCsv(self, reportFilename = "UserProjectAssignments.csv", overwrite = True):
        msg = 'Starting CSV report of all user project version assignments'
        logging.info(msg)
        self.__WriteUserProjectVersionAssignmentCsv(self.__ReportsFolder + reportFilename, overwrite)
        msg = "CSV report of all user project version assignments complete"
        logging.info(msg)
        
    def Sync(self):
        '''
        Syncs permissions from FOD to SM by calling appropriate methods as configured.
        '''
        self.SyncAuthUniversal()  # self-cancels if switch not "on" in settings
        if self.__Mode.lower() == "custom":
            self.SyncAuthWithCustomAttribute()
        else:
            self.SyncAuthWithAttribute()
        
    def SyncAuthUniversal(self):
        '''
        Syncs permissions from Fod to SM by creating roles to match "universal" roles from Fod and syncing LDAP members

        Settings required:
        AutoUniversalAccessSync - switch that turns this feature on or off
        UniversalRolePrefix - the prefix to use when creating a new role in elasticsearch (the Fod role name will be used as well)
        UniversalRoleMappingPrefix - the prefix to use when creating a new role mapping in elasticsearch (the Fod role name will be used as well)
        '''
        
        # Config
        if not self.__GetSetting('AutoUniversalAccessSync', False):
            logging.info('Universal access sync disabled in config ("AuthUniversalAccessSync" missing or not set to true).  Aborting sync.')
            return  # settings switch disabled/missing, skip universal
        if not self.__GetSetting('UniversalRolePrefix', "") or not self.__GetSetting('UniversalRoleMappingPrefix') or not self.__GetSetting('UniversalSecurityRoleTemplate'):
            raise AuthHelperException(f"Setting(s) missing for this feature in the {self.AuthConfigName} config.  Requires these: UniversalRolePrefix, UniversalRoleMappingPrefix, UniversalSecurityRoleTemplate")

        # Start / initial list pull
        logging.info('Starting authentication sync from Fod to SaltMiner (universal method).')
        authEntities = self.__GetUniversalRoleAuthEntities()
        logging.info("Found %s role(s) with universal access.", len(authEntities.keys()))

        self.__UniversalRoleHandling(authEntities)
        logging.info('Finished authentication sync from Fod to SaltMiner (universal method).')
        
    def SyncAuthWithAttribute(self):
        '''
        Syncs permissions from Fod to SM by pulling the values of a Fod custom attribute, then assigning roles based on that value

        Settings required:
        SyncAttribute - the Fod custom attribute to use
        RolePrefix - the prefix to use when creating a new role in elasticsearch (the attribute value will be used as well)
        RoleMappingPrefix - the prefix to use when creating a new role mapping in elasticsearch (the attribute value will be used as well)
        '''

        # Config
        attribute = self.__GetSetting('SyncAttribute', "")
        if not attribute or not self.__GetSetting('RolePrefix') or not self.__GetSetting('RoleMappingPrefix') or not self.__GetSetting('SecurityRoleTemplate'):
            raise AuthHelperException(f"Setting(s) missing for this feature in the {self.__AuthConfigName} config.  Requires these: SyncAttribute, RolePrefix, RoleMappingPrefix, SecurityRoleTemplate")

        # Start / initial list pull
        logging.info('Starting authentication sync from Fod to SaltMiner (attribute method)')
        pl = ProgressLogger(self.__Es)
        pl.WriteToElastic = False
        appList = self.__GetApplicationsWithAttribute(attribute)
        logging.info("Found %s application(s) with the selected attribute.", len(appList))

        # Pull users/groups for the selected ProjectVersionIds
        attrUsers = self.__GetUsersGroupsForApplications(appList, pl)

        self.__RoleHandling(pl, attrUsers)

        logging.info('Finished authentication sync from Fod to SaltMiner (attribute method).')

    def SyncAuthWithCustomAttribute(self):
        '''
        Syncs permissions from Fod to SM by applying a custom method (via a custom class), then assigning roles based on that value

        Settings required:
        RolePrefix - the prefix to use when creating a new role in elasticsearch (the attribute value will be used as well)
        RoleMappingPrefix - the prefix to use when creating a new role mapping in elasticsearch (the attribute value will be used as well)

        Also required: custom class inheriting from AuthHelperCustomAttribute and named "AuthHelperCustomAttribute{customer code from settings}", 
        located in the AuthHelperCustom package folder
        '''

        # Config
        classFactory = DImport.Import(f"AuthHelperCustom.FodAuthHelperCustomAttribute{self.__CustomerCode}", "AuthHelperCustomAttribute", "AuthHelperCustom")
        cac = classFactory()
        if not self.__GetSetting('RolePrefix') or not self.__GetSetting('RoleMappingPrefix') or not self.__GetSetting('SecurityRoleTemplate'):
            raise AuthHelperException(f"Setting(s) missing for this feature in the {self.__AuthConfigName} config.  Requires these: RolePrefix, RoleMappingPrefix, SecurityRoleTemplate")

        # Start / initial list pull
        logging.info('Starting authentication sync from Fod to SaltMiner (custom attribute method).')
        pl = ProgressLogger(self.__Es)
        pl.WriteToElastic = False
        appList = self.__GetApplicationsWithCustomAttribute(cac)
        logging.info("Found applications to process.", len(appList))

        # Pull users/groups for the selected ProjectVersionIds
        attrUsers = self.__GetUsersGroupsForApplications(appList, pl)

        self.__RoleHandling(pl, attrUsers)
        logging.info('Finished authentication sync from Fod to SaltMiner (custom attribute method).')

