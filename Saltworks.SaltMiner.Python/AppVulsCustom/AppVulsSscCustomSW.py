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

from Core.SscClient import SscClient

class AppVulsSscCustom(object):
    '''
    Custom SSC handler class
    '''

    def __init__(self, appSettings, sourceName, sscClient=None):
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        if not sscClient:
            self.__Ssc = SscClient(appSettings, sourceName)
            self.__SscIsMine = True
        else:
            self.__Ssc = sscClient
            self.__SscIsMine = False
        self.__SourceName = sourceName

    @property
    def SourceName(self):
        return self.__SourceName
    
    def CustomUpdateAppVersion(self, appVersion, cancelTrk):
        '''
        Called once per app version, before attributes, scans, or issues for all sources.  Use to customize the app version, 
        or setup custom structures before the other entities are processed.
        Called after AppVulsCustom.

        Parameters:
        appVersion - the application version object as built so far from the source
        cancelTrk - set .Cancel to True if we should skip this application version for some reason (skip logged automatically)
        '''
        pass

    def CustomUpdateAttributes(self, appVersion, attributes, cancelTrk):
        '''
        Called once per app version, after the app version is created, but before scans or issues, all sources.
        Use to modify the attributes before scans or issues are processed.
        Called after AppVulsCustom.

        Parameters:
        appVersion - the application version object 
        attributes - the application version attributes loaded from the source
        cancelTrk - set .Cancel to True if we should skip this application version for some reason (skip logged automatically)
        '''
        pass

    def CustomBeforeScanUpdates(self, appVersion, attributes, isDelete):
        '''
        Called once per app version, before processing scan updates.
        Use to perform custom setup actions before processing scans, like deleting certain selected history for example
        Called after AppVulsCustom.

        Parameters:
        appVersion - the application version object 
        attributes - the application version attributes loaded from the source
        isDelete - whether the scan update is a delete operation
        '''
        pass

    def CustomUpdateScan(self, appVersion, attributes, scan, cancelTrk):
        '''
        Called for each scan recorded for the application version (could be more than one), for all sources.
        Use to modify the scan information before it is written and before issues are processed.
        Called after AppVulsCustom.

        Parameters:
        appVersion - the application version object 
        attributes - the application version attributes after loading and any customizations
        scan - the scan object as loaded from the source
        cancelTrk - set .Cancel to True if we should skip this scan for some reason (skip logged automatically)
        '''
        pass

    def CustomBeforeIssueUpdate(self, appVersion, attributes, assessmentType, cancelTrk):
        '''
        Called once per issue, before processing the next issue update.
        Use to customize attributes or cancel the next issue update.
        Called before AppVulsXXXCustom.

        Parameters:
        appVersion - the application version object 
        attributes - the application version attributes loaded from the source
        assessmentType - the assessment type determined for this issue
        cancelTrk - set .Cancel to True if we should skip this issue for some reason (skip logged automatically)
        '''
        pass

    def CustomUpdateIssue(self, appVersion, attributes, assessmentType, srcIssue, issue, cancelTrk):
        '''
        Called once per issue loaded for all sources.  Can be expensive as will be called LOTS.  
        Use to modify the current issue before it is written.
        Called before AppVulsXXXCustom.

        Parameters:
        appVersion - the application version object
        attributes - the application version attributes 
        assessmentType - the assessment type determined for this issue
        srcIssue - the issue loaded from the source
        issue - the issue as mapped
        cancelTrk - set .Cancel to True if we should skip this issue for some reason (skip logged automatically)
        '''
        pass

    def Cleanup(self):
        '''
        Cleans up anything needed, including the SSC client if initialized locally
        '''
        if self.__Ssc and self.__SscIsMine:
            self.__Ssc.Cleanup()
       
                
            

