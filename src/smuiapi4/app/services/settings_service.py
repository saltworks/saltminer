from app.services.es_client import get_es_client

INDEX = "sys_config"


def get_settings_by_section(section, subsection=None):
    es = get_es_client()
    query = {"bool": {"must": [{"term": {"section": section}}]}}
    if subsection is not None:
        query["bool"]["must"].append({"term": {"subsection": subsection}})

    result = es.search(index=INDEX, query=query, size=1000)
    return [hit["_source"] for hit in result["hits"]["hits"]]


def update_settings(section, subsection, updates):
    es = get_es_client()
    operations = []
    for item in updates:
        doc_id = f"{section}_{subsection}_{item['property']}" if subsection else f"{section}_{item['property']}"
        operations.append({"update": {"_index": INDEX, "_id": doc_id}})
        operations.append({
            "doc": {
                "id": doc_id,
                "section": section,
                "subsection": subsection or "",
                "property": item["property"],
                "value": item["value"],
                "value_type": item.get("value_type", "string"),
                "label": item.get("label", ""),
                "description": item.get("description", ""),
            },
            "doc_as_upsert": True,
        })

    es.bulk(operations=operations, refresh=True)


def delete_setting(section, subsection, property_name):
    es = get_es_client()
    doc_id = f"{section}_{subsection}_{property_name}" if subsection else f"{section}_{property_name}"
    es.delete(index=INDEX, id=doc_id, refresh=True)
