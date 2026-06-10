from collections import defaultdict
from app.services.es_client import get_es_client

INDEX = "sys_config"
SECTION = "scanning"

AVAILABLE_SCANNERS = [
    {
        "name": "Nmap",
        "description": "Network exploration tool and security/port scanner",
        "icon": "mdi-radar",
        "color": "#1A7F64",
    },
    {
        "name": "Nessus",
        "description": "Comprehensive vulnerability scanner by Tenable",
        "icon": "mdi-radar",
        "color": "#E53935",
    },
    {
        "name": "OpenVAS",
        "description": "Open-source vulnerability scanning and management",
        "icon": "mdi-radar",
        "color": "#5CBBFF",
    },
    {
        "name": "Burp Suite",
        "description": "Web application security testing platform",
        "icon": "mdi-radar",
        "color": "#FF6F00",
    },
    {
        "name": "Nikto",
        "description": "Open-source web server scanner for vulnerabilities",
        "icon": "mdi-radar",
        "color": "#7B61FF",
    },
    {
        "name": "Metasploit",
        "description": "Penetration testing framework for exploit development",
        "icon": "mdi-radar",
        "color": "#2196F3",
    },
]


def get_all_scanners():
    es = get_es_client()
    result = es.search(
        index=INDEX,
        query={"term": {"section": SECTION}},
        size=1000,
    )
    hits = [hit["_source"] for hit in result["hits"]["hits"]]
    if not hits:
        return []

    grouped = defaultdict(list)
    for doc in hits:
        grouped[doc["subsection"]].append(doc)

    return [
        {"scanner": scanner, "properties": props}
        for scanner, props in grouped.items()
    ]


def get_scanner_settings(scanner):
    es = get_es_client()
    result = es.search(
        index=INDEX,
        query={
            "bool": {
                "must": [
                    {"term": {"section": SECTION}},
                    {"term": {"subsection": scanner}},
                ]
            }
        },
        size=1000,
    )
    properties = [hit["_source"] for hit in result["hits"]["hits"]]
    return {"scanner": scanner, "properties": properties}


def update_scanner_settings(scanner, updates):
    es = get_es_client()
    operations = []
    for item in updates:
        doc_id = f"{SECTION}_{scanner}_{item['property']}"
        operations.append({"update": {"_index": INDEX, "_id": doc_id}})
        operations.append({
            "doc": {
                "id": doc_id,
                "section": SECTION,
                "subsection": scanner,
                "property": item["property"],
                "value": item["value"],
                "value_type": item.get("value_type", "string"),
                "label": item.get("label", ""),
                "description": item.get("description", ""),
            },
            "doc_as_upsert": True,
        })
    es.bulk(operations=operations, refresh=True)


def delete_scanner(scanner):
    es = get_es_client()
    result = es.search(
        index=INDEX,
        query={
            "bool": {
                "must": [
                    {"term": {"section": SECTION}},
                    {"term": {"subsection": scanner}},
                ]
            }
        },
        size=1000,
    )
    doc_ids = [hit["_id"] for hit in result["hits"]["hits"]]
    if not doc_ids:
        return

    operations = [{"delete": {"_index": INDEX, "_id": doc_id}} for doc_id in doc_ids]
    es.bulk(operations=operations, refresh=True)


def get_scanning_jobs():
    es = get_es_client()
    result = es.search(
        index="scanning_jobs",
        query={"match_all": {}},
        size=1000,
    )
    return [hit["_source"] for hit in result["hits"]["hits"]]


def get_scan_schedules():
    es = get_es_client()
    result = es.search(
        index="scanning_schedules",
        query={"match_all": {}},
        size=1000,
    )
    return [hit["_source"] for hit in result["hits"]["hits"]]
