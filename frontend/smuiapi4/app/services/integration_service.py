import json
from collections import defaultdict
from app.services.es_client import get_es_client

INDEX = "sys_config"
SECTION_AVAILABLE = "integrations-available"
SECTION_CONFIGURED = "integrations-configured"
RESERVED_PROPERTIES = {"_metadata", "icon"}
DEFAULT_ICON = "/smui4/icons/integrations/default.svg"


def get_available_adapters():
    es = get_es_client()
    result = es.search(
        index=INDEX,
        query={"term": {"section": SECTION_AVAILABLE}},
        size=1000,
    )
    hits = [hit["_source"] for hit in result["hits"]["hits"]]
    if not hits:
        return []

    grouped = defaultdict(list)
    for doc in hits:
        grouped[doc["subsection"]].append(doc)

    adapters = []
    for adapter_name, docs in grouped.items():
        metadata_doc = next((d for d in docs if d["property"] == "_metadata"), None)
        icon_doc = next((d for d in docs if d["property"] == "icon"), None)
        fields = [d for d in docs if d["property"] not in RESERVED_PROPERTIES]

        description = ""
        if metadata_doc:
            try:
                meta = json.loads(metadata_doc["value"])
                description = meta.get("description", "")
            except (json.JSONDecodeError, TypeError):
                description = metadata_doc.get("description", "")

        adapters.append({
            "name": adapter_name,
            "description": description,
            "icon": icon_doc["value"] if icon_doc else DEFAULT_ICON,
            "fields": fields,
        })

    return adapters


def get_adapter_template(adapter):
    es = get_es_client()
    result = es.search(
        index=INDEX,
        query={
            "bool": {
                "must": [
                    {"term": {"section": SECTION_AVAILABLE}},
                    {"term": {"subsection": adapter}},
                ]
            }
        },
        size=1000,
    )
    docs = [hit["_source"] for hit in result["hits"]["hits"]]
    fields = [d for d in docs if d["property"] not in RESERVED_PROPERTIES]
    return {"adapter": adapter, "fields": fields}


def get_configured_integrations():
    es = get_es_client()
    result = es.search(
        index=INDEX,
        query={"term": {"section": SECTION_CONFIGURED}},
        size=1000,
    )
    hits = [hit["_source"] for hit in result["hits"]["hits"]]
    if not hits:
        return []

    grouped = defaultdict(list)
    for doc in hits:
        grouped[doc["subsection"]].append(doc)

    return [
        {"instance": instance_name, "properties": props}
        for instance_name, props in grouped.items()
    ]


def get_instance_settings(instance):
    es = get_es_client()
    result = es.search(
        index=INDEX,
        query={
            "bool": {
                "must": [
                    {"term": {"section": SECTION_CONFIGURED}},
                    {"term": {"subsection": instance}},
                ]
            }
        },
        size=1000,
    )
    properties = [hit["_source"] for hit in result["hits"]["hits"]]
    return {"instance": instance, "properties": properties}


def update_instance_settings(instance, updates):
    es = get_es_client()
    operations = []
    for item in updates:
        doc_id = f"{SECTION_CONFIGURED}_{instance}_{item['property']}"
        operations.append({"update": {"_index": INDEX, "_id": doc_id}})
        operations.append({
            "doc": {
                "id": doc_id,
                "section": SECTION_CONFIGURED,
                "subsection": instance,
                "property": item["property"],
                "value": item["value"],
                "value_type": item.get("value_type", "string"),
                "label": item.get("label", ""),
                "description": item.get("description", ""),
            },
            "doc_as_upsert": True,
        })
    es.bulk(operations=operations, refresh=True)


def delete_instance(instance):
    es = get_es_client()
    result = es.search(
        index=INDEX,
        query={
            "bool": {
                "must": [
                    {"term": {"section": SECTION_CONFIGURED}},
                    {"term": {"subsection": instance}},
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


def instance_exists(instance_name):
    es = get_es_client()
    result = es.count(
        index=INDEX,
        query={
            "bool": {
                "must": [
                    {"term": {"section": SECTION_CONFIGURED}},
                    {"term": {"subsection": instance_name}},
                ]
            }
        },
    )
    return result["count"] > 0


def create_instance(adapter_name, instance_name):
    template = get_adapter_template(adapter_name)

    es = get_es_client()
    operations = []

    # Copy template fields with empty values
    for field in template["fields"]:
        doc_id = f"{SECTION_CONFIGURED}_{instance_name}_{field['property']}"
        operations.append({"update": {"_index": INDEX, "_id": doc_id}})
        operations.append({
            "doc": {
                "id": doc_id,
                "section": SECTION_CONFIGURED,
                "subsection": instance_name,
                "property": field["property"],
                "value": "",
                "value_type": field.get("value_type", "string"),
                "label": field.get("label", ""),
                "description": field.get("description", ""),
            },
            "doc_as_upsert": True,
        })

    # Add adapterName (read-only)
    doc_id = f"{SECTION_CONFIGURED}_{instance_name}_adapterName"
    operations.append({"update": {"_index": INDEX, "_id": doc_id}})
    operations.append({
        "doc": {
            "id": doc_id,
            "section": SECTION_CONFIGURED,
            "subsection": instance_name,
            "property": "adapterName",
            "value": adapter_name,
            "value_type": "string",
            "label": "Adapter Name",
            "description": "Read-only. Identifies which adapter this instance belongs to.",
        },
        "doc_as_upsert": True,
    })

    # Add schedule fields
    for prop, value, vtype, label in [
        ("enabled", "false", "boolean", "Enabled"),
        ("runEveryHours", "24", "integer", "Run Every (Hours)"),
        ("startingAt", "", "string", "Starting At"),
    ]:
        doc_id = f"{SECTION_CONFIGURED}_{instance_name}_{prop}"
        operations.append({"update": {"_index": INDEX, "_id": doc_id}})
        operations.append({
            "doc": {
                "id": doc_id,
                "section": SECTION_CONFIGURED,
                "subsection": instance_name,
                "property": prop,
                "value": value,
                "value_type": vtype,
                "label": label,
                "description": "",
            },
            "doc_as_upsert": True,
        })

    es.bulk(operations=operations, refresh=True)
    return get_instance_settings(instance_name)


ICON_PREFIX = "/smui4/icons/integrations/"
SYSTEM_PROPERTIES = {"adapterName", "enabled", "runEveryHours", "startingAt"}

STANDARD_FIELDS = [
    {"property": "baseUrl", "value_type": "string", "label": "Base URL", "description": "API endpoint for the integration"},
    {"property": "clientId", "value_type": "string", "label": "Client ID", "description": "OAuth/API client identifier"},
    {"property": "clientSecret", "value_type": "string", "label": "Client Secret", "description": "OAuth/API client secret"},
    {"property": "assetIdAttribute", "value_type": "string", "label": "Asset ID Attribute", "description": "Attribute name used to link findings to asset inventory"},
]


def _resolve_icon(icon_filename):
    if not icon_filename:
        return f"{ICON_PREFIX}default.svg"
    if icon_filename.startswith("/"):
        return icon_filename
    return f"{ICON_PREFIX}{icon_filename}"


def adapter_exists(adapter_name):
    es = get_es_client()
    result = es.count(
        index=INDEX,
        query={
            "bool": {
                "must": [
                    {"term": {"section": SECTION_AVAILABLE}},
                    {"term": {"subsection": adapter_name}},
                ]
            }
        },
    )
    return result["count"] > 0


def create_adapter(adapter_name, description, icon_filename):
    es = get_es_client()
    operations = []

    # _metadata
    doc_id = f"{SECTION_AVAILABLE}_{adapter_name}__metadata"
    operations.append({"update": {"_index": INDEX, "_id": doc_id}})
    operations.append({
        "doc": {
            "id": doc_id,
            "section": SECTION_AVAILABLE,
            "subsection": adapter_name,
            "property": "_metadata",
            "value": json.dumps({"description": description}),
            "value_type": "json",
            "label": adapter_name,
            "description": description,
        },
        "doc_as_upsert": True,
    })

    # icon
    doc_id = f"{SECTION_AVAILABLE}_{adapter_name}_icon"
    operations.append({"update": {"_index": INDEX, "_id": doc_id}})
    operations.append({
        "doc": {
            "id": doc_id,
            "section": SECTION_AVAILABLE,
            "subsection": adapter_name,
            "property": "icon",
            "value": _resolve_icon(icon_filename),
            "value_type": "string",
            "label": "Icon",
            "description": "Path to adapter icon",
        },
        "doc_as_upsert": True,
    })

    # Standard fields
    for field in STANDARD_FIELDS:
        doc_id = f"{SECTION_AVAILABLE}_{adapter_name}_{field['property']}"
        operations.append({"update": {"_index": INDEX, "_id": doc_id}})
        operations.append({
            "doc": {
                "id": doc_id,
                "section": SECTION_AVAILABLE,
                "subsection": adapter_name,
                "property": field["property"],
                "value": "",
                "value_type": field["value_type"],
                "label": field["label"],
                "description": field["description"],
            },
            "doc_as_upsert": True,
        })

    es.bulk(operations=operations, refresh=True)


def update_adapter_template(adapter, description, icon_filename, fields):
    es = get_es_client()

    # Get existing field docs (excluding _metadata and icon)
    result = es.search(
        index=INDEX,
        query={
            "bool": {
                "must": [
                    {"term": {"section": SECTION_AVAILABLE}},
                    {"term": {"subsection": adapter}},
                ]
            }
        },
        size=1000,
    )
    existing_docs = result["hits"]["hits"]
    existing_field_docs = [
        d for d in existing_docs
        if d["_source"]["property"] not in RESERVED_PROPERTIES
    ]

    new_property_names = {f["property"] for f in fields}
    operations = []

    # Delete removed fields
    for doc in existing_field_docs:
        if doc["_source"]["property"] not in new_property_names:
            operations.append({"delete": {"_index": INDEX, "_id": doc["_id"]}})

    # Upsert fields
    for field in fields:
        doc_id = f"{SECTION_AVAILABLE}_{adapter}_{field['property']}"
        operations.append({"update": {"_index": INDEX, "_id": doc_id}})
        operations.append({
            "doc": {
                "id": doc_id,
                "section": SECTION_AVAILABLE,
                "subsection": adapter,
                "property": field["property"],
                "value": field.get("value", ""),
                "value_type": field.get("value_type", "string"),
                "label": field.get("label", ""),
                "description": field.get("description", ""),
            },
            "doc_as_upsert": True,
        })

    # Upsert _metadata
    doc_id = f"{SECTION_AVAILABLE}_{adapter}__metadata"
    operations.append({"update": {"_index": INDEX, "_id": doc_id}})
    operations.append({
        "doc": {
            "id": doc_id,
            "section": SECTION_AVAILABLE,
            "subsection": adapter,
            "property": "_metadata",
            "value": json.dumps({"description": description}),
            "value_type": "json",
            "label": adapter,
            "description": description,
        },
        "doc_as_upsert": True,
    })

    # Upsert icon
    doc_id = f"{SECTION_AVAILABLE}_{adapter}_icon"
    operations.append({"update": {"_index": INDEX, "_id": doc_id}})
    operations.append({
        "doc": {
            "id": doc_id,
            "section": SECTION_AVAILABLE,
            "subsection": adapter,
            "property": "icon",
            "value": _resolve_icon(icon_filename),
            "value_type": "string",
            "label": "Icon",
            "description": "Path to adapter icon",
        },
        "doc_as_upsert": True,
    })

    es.bulk(operations=operations, refresh=True)


def get_instances_for_adapter(adapter):
    es = get_es_client()
    result = es.search(
        index=INDEX,
        query={
            "bool": {
                "must": [
                    {"term": {"section": SECTION_CONFIGURED}},
                    {"term": {"property": "adapterName"}},
                    {"term": {"value": adapter}},
                ]
            }
        },
        size=1000,
    )
    return [hit["_source"]["subsection"] for hit in result["hits"]["hits"]]


def delete_adapter_template(adapter):
    instances = get_instances_for_adapter(adapter)
    if instances:
        return {"blocked": True, "instance_count": len(instances), "instances": instances}

    es = get_es_client()
    result = es.search(
        index=INDEX,
        query={
            "bool": {
                "must": [
                    {"term": {"section": SECTION_AVAILABLE}},
                    {"term": {"subsection": adapter}},
                ]
            }
        },
        size=1000,
    )
    doc_ids = [hit["_id"] for hit in result["hits"]["hits"]]
    if doc_ids:
        operations = [{"delete": {"_index": INDEX, "_id": doc_id}} for doc_id in doc_ids]
        es.bulk(operations=operations, refresh=True)

    return {"deleted": True}


def propagate_template_to_instances(adapter, fields):
    instances = get_instances_for_adapter(adapter)
    if not instances:
        return {"instances_updated": 0}

    es = get_es_client()
    template_property_names = {f["property"] for f in fields}
    template_by_property = {f["property"]: f for f in fields}

    for instance_name in instances:
        # Get current instance properties
        result = es.search(
            index=INDEX,
            query={
                "bool": {
                    "must": [
                        {"term": {"section": SECTION_CONFIGURED}},
                        {"term": {"subsection": instance_name}},
                    ]
                }
            },
            size=1000,
        )
        instance_docs = result["hits"]["hits"]
        instance_properties = {d["_source"]["property"]: d for d in instance_docs}

        operations = []

        # Add missing fields (in template but not in instance)
        for prop_name in template_property_names:
            if prop_name not in instance_properties:
                field = template_by_property[prop_name]
                doc_id = f"{SECTION_CONFIGURED}_{instance_name}_{prop_name}"
                operations.append({"update": {"_index": INDEX, "_id": doc_id}})
                operations.append({
                    "doc": {
                        "id": doc_id,
                        "section": SECTION_CONFIGURED,
                        "subsection": instance_name,
                        "property": prop_name,
                        "value": "",
                        "value_type": field.get("value_type", "string"),
                        "label": field.get("label", ""),
                        "description": field.get("description", ""),
                    },
                    "doc_as_upsert": True,
                })

        # Remove extra fields (in instance but not in template, excluding system properties)
        for prop_name, doc in instance_properties.items():
            if prop_name not in template_property_names and prop_name not in SYSTEM_PROPERTIES:
                operations.append({"delete": {"_index": INDEX, "_id": doc["_id"]}})

        if operations:
            es.bulk(operations=operations, refresh=True)

    return {"instances_updated": len(instances)}
