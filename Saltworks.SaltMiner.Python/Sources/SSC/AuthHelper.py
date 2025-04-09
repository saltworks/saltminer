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

from Core.SscClient import SscClient, SscClientException
from Utility.ProgressLogger import ProgressLogger
from Utility.DImport import DImport

class AuthHelperException(Exception):
    pass

class AuthHelperCustomAttribute(object):
    def __init__(self):
        pass
    def GetAttribute(self, pv):
        '''
        Given project version pv, returns attribute to be used for sorting out roles (i.e. first 10 chars of name for UAID)

        Override this for the customer as needed
        '''
        pass

class AuthHelper(object):
    def __init__(self, appSettings, sourceName, authConfigName="SscAuth", mainConfigName="Main"):
        self.__Es = appSettings.Application.GetElasticClient()
        self.__Ssc = SscClient(appSettings, sourceName)
        self.__ReportsFolder = appSettings.Get(mainConfigName, 'ReportsFolder')  # used to output CSV report
        self.__CustomerCode = appSettings.Get(mainConfigName, 'CustomerCode')
        self.__GlobalRoleName = appSettings.Get(authConfigName, 'GlobalRoleName', '') # if set, creates a role mapping for all users created to the specified global role
        self.__App = appSettings.Application
        self.__AuthConfigName = authConfigName
        self.__AssetIndex = appSettings.Get(authConfigName, 'AssetIndex', 'assets_app_saltworks.ssc_ssc1')
        self.__DebugMode = appSettings.Get(authConfigName, "DebugMode", False)
        if self.__DebugMode:
            logging.warning("************************************************************************")
            logging.warning("Debug Mode enabled for this feature - see DebugMode in config to change.")
            logging.warning("************************************************************************")
        self.__Mode = appSettings.Get(authConfigName, "Mode", "")
        if not self.__Mode or self.__Mode.lower() not in ["attribute", "custom"]:
            raise AuthHelperException("Invalid 'Mode' in settings, expected one of these values: ['Attribute', 'Custom']")

    def __GetSetting(self, key, default=None):
        return self.__App.Settings.Get(self.__AuthConfigName, key, default)

    def __UniversalRoleHandling(self, roleUsers):
        '''
        roleUsers structure expected:
        { "Role": { "groups": [], "users": [ "cn=blah,ou=my ou,dc=domain,dc=local", "cn=blah2,ou=my ou,dc=domain,dc=local" ] }
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
            name = u.lower().replace(" ", "-")
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

    def __GetUsersGroupsForProjectVersions(self, pvList, pl):
        '''
        Pull users/groups for the selected ProjectVersionIds

        pvList expected to look like this (other attribs fine if present but will be dropped):
        [ { "projectVersionId": "10077", "attributeValue": "Key To Differentiate Roles" } ]

        attrs should be a list of attributes to include other than "projectVersionId" in the new list

        returns new list instead of updating old because structure is different
        '''
        pl.Start("SSC API auth calls", len(pvList))
        count = 1
        for e in pvList:
            logging.debug(f"Calling SSC authentities endpoint for pv {e['projectVersionId']}")
            if count == 1 or count % 100 == 0:
                pl.Progress(count, "Getting users/groups for selected projectVersions")
            users = []
            try:
                users = self.__Ssc.GetProjectVersionUsers(e['projectVersionId'])
            except SscClientException as ex:
                logging.error("Failed to get users/groups for project version %s. Skipping...", e['projectVersionId'])
                continue
            e['users'] = []
            e['groups'] = []
            for u in users:
                if u['isLdap']:
                    if u['type'] == "Group":
                        e['groups'].append(u['ldapDn'])
                    else:
                        e['users'].append(u['ldapDn'])
                else:
                    e['users'].append(u['entityName'])
            count += 1
        
        # Rearrange data into list format we need - the new format looks similar, but we make sure each attribute has all eligible users/groups assigned, not just a single pv
        #   from: { "projectVersionId": "10077", "attribute": "RoleKeyValue", "users": ["cn=blah,ou=my ou,dc=domain,dc=local", "cn=blah2,ou=my ou,dc=domain,dc=local"], "groups": [] }
        #   to:   { "RoleKeyValue": { "groups": [], "users": [ "cn=blah,ou=my ou,dc=domain,dc=local", "cn=blah2,ou=my ou,dc=domain,dc=local"], "tdict": { "rkey": "10077" } }
        logging.info("Arranging data into optimized format")
        pl.Progress(1, "Optimizing data", len(pvList))
        attrUsers = {}
        count = 1
        for e in pvList:
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

    def __GetProjectVersionsWithAttribute(self, attr):
        '''
        Returns list of SSC project version ids and their attribute values for the given attribute name

        Parameters:
        attr - the attribute name to find
        esClient - elasticsearch client to use to run the query
        '''
        fld = "saltminer.asset.attributes." + attr
        query = { 
            "_source": ["saltminer.asset.version_id", fld],
            "query": { "exists": { "field": fld } },
            "sort": ["saltminer.asset.version_id"]
        }
        pvList = []
        scroller = self.__Es.SearchScroll(self.__AssetIndex, query, 500, None)
        while len(scroller.Results) > 0:
            for i in scroller.Results:
                if attr in i['_source']['saltminer']['asset']['attributes'].keys() and i['_source']['saltminer']['asset']['attributes'][attr]:
                    pvList.append({"projectVersionId": i['_source']['saltminer']['asset']['version_id'], "attribute": i['_source']['saltminer']['asset']['attributes'][attr]})
            logging.info("Loaded %s of %s project versions", len(pvList), scroller.TotalHits)
            if self.__DebugMode:
                break
            scroller.GetNext()
        # if debug set, only return 10 items
        if self.__DebugMode:
            debugList = []
            rng = 9 if len(pvList) > 10 else len(pvList) - 1
            for i in range(rng):
                debugList.append(pvList[i])
            return debugList
        return pvList

    def __GetProjectVersionsWithCustomAttribute(self, custAttrClass):
        '''
        Returns list of SSC project version ids and their attribute values based on a customized function

        Parameters:
        custAttrClass - class inheriting from AuthHelperCustomAttribute and implementing its one method

        '''
        if not isinstance(custAttrClass, AuthHelperCustomAttribute):
            raise AuthHelperException("Class of type AuthHelperCustomAttribute (or a child) required for parameter custAttrClass")
        logging.info('Pulling all project versions from SSC, this may take a while...')
        pvList = self._Ssc.GetProjectVersions()
        pvList2 = []
        for i in pvList:
            a = custAttrClass.GetAttribute(i['_source'])
            if a:
                pvList2.append({ "projectVersionId": i['id'], "attributeValue": a })
        return pvList2

    def __GetUniversalRoleAuthEntities(self):
        '''
        Returns a list of roles and their LDAP groups and users with universal access
        { "Role": { "groups": [], "users": [ "cn=blah,ou=my ou,dc=domain,dc=local", "cn=blah2,ou=my ou,dc=domain,dc=local" ] }
        '''
        list = self.__Ssc.GetUsers(True)
        outList = {}
        for ug in list.keys():
            if list[ug]['isLdap']:
                for r in list[ug]['_embed']['roles']:
                    if r['allApplicationRole']:
                        if not r['name'] in outList.keys():
                            outList[r['name']] = { "groups": [], "users": [] }
                        if list[ug]['type'].lower() == 'group':
                            outList[r['name']]['groups'].append(list[ug]['ldapDn'])
                        else:
                            outList[r['name']]['users'].append(list[ug]['ldapDn'])
        return outList


    def __WriteUserProjectVersionAssignmentCsv(self, file, overwrite=False):
        '''
        Writes a list of SSC project version ids, projects, and versions and their assigned users to CSV
        '''

        # Setup, initial pv list
        ssc = self.__Ssc
        es = self.__Es
        pl = ProgressLogger(es)
        pl.WriteToElastic = False
        logging.info('Pulling all project versions from SSC...')
        pvList = ssc.GetProjectVersions()
        logging.info("Found %s project version(s).", len(pvList))
        myList = []
        seenUsers = []
        count = 1

        # Retrieve pv user assignments (one pv at a time)
        pl.Start("SSC authEntities endpoint calls", len(pvList))
        for pv in pvList:
            logging.debug(f"Calling SSC authEntities endpoint for pv {pv['id']}")
            if count == 1 or count % 10 == 0:
                pl.Progress(count, "Getting user/group project version assignments")
            users = ssc.GetProjectVersionUsers(pv['id'])
            for u in users:
                myList.append({ "UserId": u['entityName'], "DisplayName": u['displayName'], "LdapDn": u['ldapDn'], "ProjectVersionId": pv['id'], "Project": pv['project']['name'], "Version": pv['name'] })
                if not u['entityName'] in seenUsers:
                    seenUsers.append(u['entityName'])
            count += 1
        pvList = None

        # Retrieve global roles
        logging.info("Retrieving global assignments and roles")
        userList = ssc.GetUsers(True)
        gUserList = []
        gUsersSeen = []
        noUserList = []
        for u in userList.keys():
            for r in userList[u]['_embed']['roles']:
                if r['allApplicationRole']:
                    if not u in gUsersSeen:
                        gUserList.append({ "UserId": userList[u]['entityName'], "DisplayName": userList[u]['displayName'], "LdapDn": userList[u]['ldapDn'], "ProjectVersionId": 0, "Project": "ALL", "Version": "ALL", "Roles": r['name'] })
                        gUsersSeen.append(u)
                    if not u in seenUsers:
                        seenUsers.append(u)
            if not u in seenUsers:
                noUserList.append({ "UserId": userList[u]['entityName'], "DisplayName": userList[u]['displayName'], "LdapDn": userList[u]['ldapDn'], "ProjectVersionId": -1, "Project": "NONE", "Version": "NONE", "Roles": "" })
        
        # Write csv file
        logging.info("Writing CSV file...")
        if platform.system().lower() == "windows":
            newline = ''
        else:
            newline = '\r\n'
        if overwrite:
            fflag = "w"
        else:
            fflag = "x"
        with open(file, fflag, newline=newline) as outfile:
            csvwriter = csv.writer(outfile)
            h = False
            for e in myList:
                roles = []
                for r in userList[e['UserId']]['_embed']['roles']:
                    roles.append(r['name'])
                e['Roles'] = '|'.join(roles)
                if not h:
                    csvwriter.writerow(e.keys())
                    h = True
                csvwriter.writerow(e.values())
            for e in gUserList:
                csvwriter.writerow(e.values())
            for e in noUserList:
                csvwriter.writerow(e.values())
    
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
        Syncs permissions from SSC to SM by creating roles to match "universal" roles from SSC and syncing LDAP members

        Settings required:
        AutoUniversalAccessSync - switch that turns this feature on or off
        UniversalRolePrefix - the prefix to use when creating a new role in elasticsearch (the SSC role name will be used as well)
        UniversalRoleMappingPrefix - the prefix to use when creating a new role mapping in elasticsearch (the SSC role name will be used as well)
        '''
        
        # Config
        if not self.__GetSetting('AutoUniversalAccessSync', False):
            logging.info('Universal access sync disabled in config ("AuthUniversalAccessSync" missing or not set to true).  Aborting sync.')
            return  # settings switch disabled/missing, skip universal
        if not self.__GetSetting('UniversalRolePrefix', "") or not self.__GetSetting('UniversalRoleMappingPrefix') or not self.__GetSetting('UniversalSecurityRoleTemplate'):
            raise AuthHelperException(f"Setting(s) missing for this feature in the {self.AuthConfigName} config.  Requires these: UniversalRolePrefix, UniversalRoleMappingPrefix, UniversalSecurityRoleTemplate")

        # Start / initial list pull
        logging.info('Starting authentication sync from SSC to SaltMiner (universal method).')
        authEntities = self.__GetUniversalRoleAuthEntities()
        logging.info("Found %s role(s) with universal access.", len(authEntities.keys()))

        self.__UniversalRoleHandling(authEntities)
        logging.info('Finished authentication sync from SSC to SaltMiner (universal method).')
        
    def SyncAuthWithAttribute(self):
        '''
        Syncs permissions from SSC to SM by pulling the values of a SSC custom attribute, then assigning roles based on that value

        Settings required:
        SyncAttribute - the SSC custom attribute to use
        RolePrefix - the prefix to use when creating a new role in elasticsearch (the attribute value will be used as well)
        RoleMappingPrefix - the prefix to use when creating a new role mapping in elasticsearch (the attribute value will be used as well)
        '''

        # Config
        attribute = self.__GetSetting('SyncAttribute', "")
        if not attribute or not self.__GetSetting('RolePrefix') or not self.__GetSetting('RoleMappingPrefix') or not self.__GetSetting('SecurityRoleTemplate'):
            raise AuthHelperException(f"Setting(s) missing for this feature in the {self.__AuthConfigName} config.  Requires these: SyncAttribute, RolePrefix, RoleMappingPrefix, SecurityRoleTemplate")

        # Start / initial list pull
        logging.info('Starting authentication sync from SSC to SaltMiner (attribute method)')
        pl = ProgressLogger(self.__Es)
        pl.WriteToElastic = False
        pvList = self.__GetProjectVersionsWithAttribute(attribute)
        logging.info("Found %s entries with the selected attribute (possibly less than total in query if %s was empty in some).", len(pvList), attribute)

        # Pull users/groups for the selected ProjectVersionIds
        attrUsers = self.__GetUsersGroupsForProjectVersions(pvList, pl)

        self.__RoleHandling(pl, attrUsers)

        logging.info('Finished authentication sync from SSC to SaltMiner (attribute method).')

    def SyncAuthWithCustomAttribute(self):
        '''
        Syncs permissions from SSC to SM by applying a custom method (via a custom class), then assigning roles based on that value

        Settings required:
        RolePrefix - the prefix to use when creating a new role in elasticsearch (the attribute value will be used as well)
        RoleMappingPrefix - the prefix to use when creating a new role mapping in elasticsearch (the attribute value will be used as well)

        Also required: custom class inheriting from AuthHelperCustomAttribute and named "AuthHelperCustomAttribute{customer code from settings}", 
        located in the AuthHelperCustom package folder
        '''

        # Config
        classFactory = DImport.Import(f"AuthHelperCustom.SscAuthHelperCustomAttribute{self.__CustomerCode}", "AuthHelperCustomAttribute", "AuthHelperCustom")
        cac = classFactory()
        if not self.__GetSetting('RolePrefix') or not self.__GetSetting('RoleMappingPrefix') or not self.__GetSetting('SecurityRoleTemplate'):
            raise AuthHelperException(f"Setting(s) missing for this feature in the {self.__AuthConfigName} config.  Requires these: RolePrefix, RoleMappingPrefix, SecurityRoleTemplate")

        # Start / initial list pull
        logging.info('Starting authentication sync from SSC to SaltMiner (custom attribute method).')
        pl = ProgressLogger(self.__Es)
        pl.WriteToElastic = False
        pvList = self.__GetProjectVersionsWithCustomAttribute(cac)
        logging.info("Found project versions to process.", len(pvList))

        # Pull users/groups for the selected ProjectVersionIds
        attrUsers = self.__GetUsersGroupsForProjectVersions(pvList, pl)

        self.__RoleHandling(pl, attrUsers)
        logging.info('Finished authentication sync from SSC to SaltMiner (custom attribute method).')

