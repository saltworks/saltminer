from tenable.io import TenableIO


class TenableClient:

    def __init__(self, settings):
        self.access_key = settings.Get("TenableClient", "Access Key")
        self.secret_key = settings.Get("TenableClient", "Secret Key")
        self.severity_list = settings.Get("TenableClient", "VulnSeverities")
        self.tio = TenableIO(access_key=self.access_key, secret_key=self.secret_key)


    def get_scans_generator(self):
        yield from self.tio.scans.list()

    def get_assets_generator(self):
        yield from self.tio.assets.list()
    
    def get_vuln_export_generator(self, scan_uuid):
        yield from self.tio.exports.vulns(
            scan_uuid=scan_uuid,
            state = ["OPEN", "REOPENED", "FIXED"],
            severity = self.severity_list
            )
    

    
    


