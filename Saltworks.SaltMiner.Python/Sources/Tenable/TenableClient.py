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
    

    
    


