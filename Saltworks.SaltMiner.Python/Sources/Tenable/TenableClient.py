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

from tenable.io import TenableIO


class TenableClient:

    def __init__(self, settings):
        self.access_key = settings.GetSource("TenableClient", "Access Key")
        self.secret_key = settings.GetSource("TenableClient", "Secret Key")
        self.severity_list = settings.GetSource("TenableClient", "VulnSeverities")
        self.tio = TenableIO(access_key=self.access_key, secret_key=self.secret_key)


    def get_scans_generator(self):
        yield from self.tio.scans.list()

    def get_assets_generator(self):
        yield from self.tio.exports.assets()
    
    def get_vuln_export_generator(self, scan_uuid):
        if scan_uuid == "None":
            yield from self.tio.exports.vulns(
            severity = self.severity_list,
            state = ["OPEN", "REOPENED", "FIXED"],
            include_unlicensed = True,
            since = 0000000000
            )
        else:
            yield from self.tio.exports.vulns(
                scan_uuid=scan_uuid,
                severity = self.severity_list,
                state = ["OPEN", "REOPENED", "FIXED"],
                include_unlicensed = True,
                since = 0000000000
                )
            
    # def get_vuln_export_generator(self, scan_uuid):
    #     yield from self.tio.exports.vulns(
    #         scan_uuid=scan_uuid,
    #         state = ["OPEN", "REOPENED", "FIXED"],
    #         severity = self.severity_list
    #         )-+
    

    
    


