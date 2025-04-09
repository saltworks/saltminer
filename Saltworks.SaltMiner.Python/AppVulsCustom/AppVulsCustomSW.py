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

class AppVulsCustom:
    '''
    Custom handler class for all sources
    '''

    def __init__(self, appSettings, sourceName):
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        self.__SourceName = sourceName

    @property
    def SourceName(self):
        return self.__SourceName

    def CustomUpdateAppVersion(self, appVersion, systemfrom, cancelTrk):
        '''
        Called once per app version, before attributes, scans, or issues for all sources.  Use to customize the app version, 
        or setup custom structures before the other entities are processed.
        Called before AppVulsXXXCustom.

        Parameters:
        appVersion - the application version object as built so far from the source
        systemfrom - SSC or FOD
        cancelTrk - set .Cancel to True if we should skip this application version for some reason (skip logged automatically)
        '''
        pass

    def CustomUpdateAttributes(self, appVersion, attributes, systemfrom, cancelTrk):
        '''
        Called once per app version, after the app version is created, but before scans or issues, all sources.
        Use to modify the attributes before scans or issues are processed.
        Called before AppVulsXXXCustom.

        Parameters:
        appVersion - the application version object 
        attributes - the application version attributes loaded from the source
        systemfrom - SSC or FOD
        cancelTrk - set .Cancel to True if we should skip this application version for some reason (skip logged automatically)
        '''
        pass

    def CustomBeforeScanUpdates(self, appVersion, attributes, systemfrom, isDelete):
        '''
        Called once per app version, before processing scan updates.
        Use to perform custom setup actions before processing scans, like deleting certain selected history for example
        Called before AppVulsXXXCustom.

        Parameters:
        appVersion - the application version object 
        attributes - the application version attributes loaded from the source
        systemfrom - SSC or FOD
        isDelete - whether the scan update is a delete operation
        '''
        pass

    def CustomUpdateScan(self, appVersion, attributes, scan, systemfrom, cancelTrk):
        '''
        Called for each scan recorded for the application version (could be more than one), for all sources.
        Use to modify the scan information before it is written and before issues are processed.
        Called before AppVulsXXXCustom.

        Parameters:
        appVersion - the application version object 
        attributes - the application version attributes after loading and any customizations
        scan - the scan object as loaded from the source
        systemfrom - SSC or FOD
        cancelTrk - set .Cancel to True if we should skip this scan for some reason (skip logged automatically)
        '''
        pass

    def CustomBeforeIssueUpdate(self, appVersion, attributes, assessmentType, systemfrom, cancelTrk):
        '''
        Called once per issue, before processing the next issue update.
        Use to customize attributes or cancel the next issue update.
        Called before AppVulsXXXCustom.

        Parameters:
        appVersion - the application version object 
        attributes - the application version attributes loaded from the source
        assessmentType - the assessment type determined for this issue
        systemfrom - SSC or FOD
        cancelTrk - set .Cancel to True if we should skip this issue for some reason (skip logged automatically)
        '''
        pass

    def CustomUpdateIssue(self, appVersion, attributes, assessmentType, srcIssue, issue, systemfrom, cancelTrk):
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
        systemfrom - SSC or FOD
        cancelTrk - set .Cancel to True if we should skip this issue for some reason (skip logged automatically)
        '''
        pass
