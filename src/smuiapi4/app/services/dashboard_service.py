def get_executive_dashboard():
    return {
        "kpis": [
            {"label": "Total Issues", "value": 1284, "change": "-8%", "icon": "bug", "color": "red"},
            {"label": "Critical Issues", "value": 47, "change": "-12%", "icon": "warning", "color": "orange"},
            {"label": "Assets Monitored", "value": 312, "change": "+5%", "icon": "server", "color": "blue"},
            {"label": "Compliance Score", "value": "87%", "change": "+2%", "icon": "shield", "color": "green"},
        ],
        "topIssues": [
            {"severity": "Critical", "count": 3, "name": "Remote Code Execution in Auth Service", "location": "auth-service", "status": "Open"},
            {"severity": "Critical", "count": 1, "name": "SQL Injection in Reporting API", "location": "report-api", "status": "In Progress"},
            {"severity": "High", "count": 12, "name": "Outdated TLS Configuration", "location": "api-gateway", "status": "Open"},
            {"severity": "High", "count": 8, "name": "Exposed Admin Endpoint", "location": "admin-portal", "status": "Open"},
        ],
        "recentActivity": [
            {"icon": "scan", "color": "blue", "title": "Full scan completed", "subtitle": "Production environment — 0 new critical findings", "time": "2 hours ago"},
            {"icon": "resolved", "color": "green", "title": "Issue resolved", "subtitle": "CVE-2024-1234 patched in payment-service", "time": "5 hours ago"},
            {"icon": "warning", "color": "orange", "title": "New high severity issue", "subtitle": "Insecure deserialization detected in order-service", "time": "8 hours ago"},
            {"icon": "policy", "color": "purple", "title": "Compliance report generated", "subtitle": "SOC2 quarterly report ready for review", "time": "1 day ago"},
        ],
    }


def get_development_dashboard():
    return {
        "kpis": [
            {"label": "Open SAST Issues", "value": 234, "change": "-5%", "icon": "code", "color": "red"},
            {"label": "Open DAST Issues", "value": 89, "change": "-3%", "icon": "globe", "color": "orange"},
            {"label": "Open OSS Issues", "value": 412, "change": "+2%", "icon": "package", "color": "yellow"},
            {"label": "Avg Days to Close", "value": 14, "change": "-18%", "icon": "clock", "color": "green"},
        ],
        "topIssues": [],
        "recentActivity": [],
    }


def get_security_dashboard():
    return {
        "kpis": [
            {"label": "Total Vulnerabilities", "value": 1284, "change": "-8%", "icon": "bug", "color": "red"},
            {"label": "Critical + High", "value": 203, "change": "-10%", "icon": "warning", "color": "orange"},
            {"label": "Assets Scanned", "value": 312, "change": "+5%", "icon": "server", "color": "blue"},
            {"label": "Compliance Score", "value": "87%", "change": "+2%", "icon": "shield", "color": "green"},
        ],
        "topIssues": [],
        "recentActivity": [],
    }


def get_operations_dashboard():
    return {
        "kpis": [
            {"label": "Active Scanners", "value": 18, "change": "0%", "icon": "activity", "color": "green"},
            {"label": "Scans Today", "value": 47, "change": "+15%", "icon": "scan", "color": "blue"},
            {"label": "Failed Scans", "value": 3, "change": "+1", "icon": "x-circle", "color": "red"},
            {"label": "Avg Scan Time", "value": "4m 32s", "change": "-8%", "icon": "clock", "color": "teal"},
        ],
        "topIssues": [],
        "recentActivity": [],
    }
