# New Source Adapter — [SOURCE NAME]

## Instructions for Claude Code

Before writing any code:
1. Read `docs/adapter-guide/ADAPTER_REFERENCE.md` in full
2. Read `Sources/SNYK/SnykAdapter.py` and `Sources/SNYK/SnykClient.py` as the primary reference implementation
3. Review the inputs below
4. Build a fully functional, production-ready source adapter following every convention in the reference document

---

## Adapter Inputs

| Field | Value |
|-------|-------|
| **Source Name** | `___` |
| **Instance Name** | `___` |
| **Assessment Type** | `___` |
| **Auth Method** | `___` |
| **Replace Issues** | `true` / `false` |
| **GUI URL Pattern** | `___` *(optional — e.g. `https://vendor.com/org/{org_id}/issue/{issue_id}`, or "returned by API")* |
| **API Documentation** | `___` |

---

## Notes

*(Add any special considerations here — known API quirks, pagination style, multi-tenant structure, date format specifics, etc.)*

---

## Deliverables

Create all of the following files:

- `Sources/<SourceName>/<SourceName>Client.py` — all API communication
- `Sources/<SourceName>/<SourceName>Adapter.py` — orchestration and mapping
- `Config/Sources/<SourceName>.json` — connection configuration
- `Run<SourceName>Adapter.py` — entry point script

The adapter must be complete and runnable. All standard fields defined in the reference doc must be populated. Source-specific attributes should be captured in `Saltminer.Attributes` on both assets and issues.
