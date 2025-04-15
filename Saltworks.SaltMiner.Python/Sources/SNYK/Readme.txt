# SnykAdapter

The `SnykAdapter` is responsible for pulling, formatting, and sending Snyk security data (scans, assets, and issues) into Saltminer for processing and analysis.

## 🧭 Overview

The adapter follows a structured ETL flow:

1. **Get Sync State**  
   Retrieve previously ingested Snyk project "last_updated" from Saltminer to avoid reprocessing unchanged data.

2. **Fetch Projects**  
   Iterate through Snyk projects and identify ones that have been updated since the last recorded sync.

3. **Sync Issues Flow (Triggered per Updated Project)**  
   Issues are only pulled and processed if a project has updates. The order of operations is always:

   1. **Queue Scan**
   2. **Queue Asset**
   3. **Queue Issues**
   4. **Finalize Queue**

## 🔄 Detailed Flow

### 1. `GET_SYNC`

- Pull an aggregation of `snyk_last_updated` timestamps from Saltminer. 
- Store this in a lookup dictionary (`prj_version_last_updated`) to determine what projects need syncing.

### 2. Project and Issue Retrieval

- Use `get_snyk_projects_generator()` to retrieve project documents.
   -This uses the /rest/orgs/{org_id}/projects endpoint from Snyk to produce documents.
- Use the `project_id` to check the last update value from `prj_version_last_updated`.
- If the project has been updated, call `get_snyk_issues_generator(project, last_updated)` to retrieve issues.
   -This uses the /rest/orgs/{org_id}/issues endpoint from Snyk to produce documents

### 3. `SYNC_ISSUES`

For each updated project:

#### 🧪 Scan

- Map and validate a scan document using a DTO to ensure all required Saltminer fields are present.
- Send to `queue_scans` with `queue_status = "Loading". This is done automatically using the AddQueueScan function.

#### 🖥️ Asset

- Map an asset document using the `id` returned from `queue_scan` to populate:
  - `['Saltminer']['Internal']['QueueScanId']`
- Send to `queue_assets` using the AddQueueAsset function. 

#### 🐞 Issues

- Map each issue using both `queue_scan` and `queue_asset` IDs to populate:
  - `['Saltminer']['QueueScanId']`
  - `['Saltminer']['QueueAssetId']`
- Send to `queue_issues` using the AddQueueIssue function.

#### ✅ Finalize

- Call `finalizeQueue()` to change the scan’s `queue_status` to `"Pending"`.
- This signals Saltminer to move data from the queue indices to final indices.

## 📦 Final Saltminer Indices

Data ends up in the following final indices (based on the source adapter):

- Issues: `issues_app_saltworks.snyk_snyk1`
- Assets: `assets_app_saltworks.snyk_snyk1`
- Scans: `scans_app_saltworks.snyk_snyk1`


## 🛠 Development Notes

- All mappers should raise clear errors if required fields are missing.
- Do not skip scan or asset queuing — Saltminer requires a strict data hierarchy.
- Timestamps should be UTC and ISO8601 formatted where applicable.
- We use the python library "pydantic" do create our DTOs for validation. For more information on this library please see https://pypi.org/project/pydantic/

## 🧪 Testing

When testing this adapter:
- Mock Snyk responses using project stubs with `last_updated` timestamps.
- Use Saltminer’s dev environment to verify data lands in the correct indices.
- Check the queue_scans' `queue_status` field transitions from `"Loading"` to `"Pending"` as expected. 
   -When the data has moved to the final index "Pending" will be transitioned to "Completed". 

