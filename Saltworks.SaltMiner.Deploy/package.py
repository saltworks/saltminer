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

import os
import sys
import zipfile
import glob
import shutil
from datetime import datetime, timedelta, timezone
import requests

# Replace with your GitHub repository and token
REPO_OWNER = "saltworks"  # GitHub repository owner (e.g., "user" or "organization")
REPO_NAME = "saltminer"  # Repository name (e.g., "repository-name")
WORKFLOWS = {
    "api": {"name": "Saltworks.SaltMiner.DataApi (Legacy)", "id": None, "artifact": None},
    "data-templates": {"name": "Saltworks.SaltMiner.IndexTemplates (Legacy)", "id": None, "artifact": None},
    "jobmanager": {"name": "Saltworks.SaltMiner.JobManager (Legacy)", "id": None, "artifact": None},
    "manager": {"name": "Saltworks.SaltMiner.Manager (Legacy)", "id": None, "artifact": None},
    "servicemanager": {"name": "Saltworks.SaltMiner.ServiceManager (Legacy)", "id": None, "artifact": None},
    "sm25": {"name": "Saltworks.SaltMiner.Python (Legacy)", "id": None, "artifact": None},
    "ui-api": {"name": "Saltworks.SaltMiner.Ui.Api (Legacy)", "id": None, "artifact": None},
    "ui-web": {"name": "Saltworks.SaltMiner.Ui (Legacy)", "id": None, "artifact": None},
    "agent": {"name": "Saltworks.SaltMiner.SyncAgent (Legacy)", "id": None, "artifact": None}
}
OUTPUT_DIR = "./dist"  # Directory to save artifacts
REMOVE_ITEMS = ["appsettings*.json", "ConfigPath.json", "config*.json", "BurpFiles", "CxFlowFiles", "QualysReports", "SourceConfigs", "WebInspectFiles", "AgentSettings*.json", "Manager*.json", "README.md", "JobManagerSettings*.json", "TestHarness", "ServiceManagerSettings*.json", "Template/Fiserv/", "Template/Saltworks/"]
REMOVE_ROOT_ITEMS = ["package.py", "package-reqs.txt", "addins"]
ADDINS_PATH = "./addins"

class PackageException(Exception):
    """PackageException"""

def remove_root_stuff():
    """Remove root stuff"""
    for item in REMOVE_ROOT_ITEMS:
        lst = glob.glob(item)
        for f in lst:
            if os.path.isdir(f):
                shutil.rmtree(f)
            else:
                os.remove(f)

def unzip_file(zip_path, output_dir):
    """
    Unzips a ZIP file into the specified output directory.

    :param zip_path: Path to the ZIP file
    :param output_dir: Directory where the contents will be extracted
    """
    try:
        # Ensure the output directory exists
        os.makedirs(output_dir, exist_ok=True)

        # Open the ZIP file
        with zipfile.ZipFile(zip_path, 'r') as zip_ref:
            # Extract all files into the output directory
            zip_ref.extractall(output_dir)
            print(f"Contents extracted to: {output_dir}")
    except Exception as e:
        print(f"An error occurred while unzipping: {e}")

def populate_workflow_ids(headers):
    """Populate the 'id' fields in the WORKFLOWS dictionary."""
    try:
        url = f"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/actions/workflows"
        response = requests.get(url, headers=headers, timeout=60)
        response.raise_for_status()

        workflows = response.json().get("workflows", [])
        for key, value in WORKFLOWS.items():
            matching_workflow = next((wf for wf in workflows if wf['name'] == value['name']), None)
            if matching_workflow:
                value['id'] = matching_workflow['id']
            else:
                raise PackageException(f"Couldn't load workflow info for key '{key}', workflow '{value['name']}'")
    except requests.exceptions.HTTPError as e:
        raise PackageException("Unable to list workflows.") from e

def get_latest_artifact(workflowId, branch, headers):
    """Fetch the latest artifact for the specified workflow and branch."""
    # Get workflow runs filtered by branch
    url = f"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/actions/workflows/{workflowId}/runs"
    params = {"branch": branch, "status": "completed"}
    response = requests.get(url, headers=headers, params=params, timeout=60)
    response.raise_for_status()

    runs = response.json().get("workflow_runs", [])
    run = None
    for r in runs:
        if r['status'] == "completed" and r['conclusion'] == "success":
            run = r
            break
    if not run:
        print(f"No successfully completed runs found for workflow ID: {workflowId} on branch: {branch}")
        return None

    # Get artifacts for the latest run
    latest_run_id = run["id"]
    artifacts_url = f"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/actions/runs/{latest_run_id}/artifacts"
    response = requests.get(artifacts_url, headers=headers, timeout=60)
    response.raise_for_status()

    artifacts = response.json().get("artifacts", [])
    if artifacts:
        return artifacts[0]  # Return the first artifact (latest) for the run
    print(f"No artifacts found for workflow: {workflowId} on branch: {branch}")
    return None

def download_artifact(headers, artifact_name, artifact_url, output_dir):
    """Download a specific artifact to the output directory."""
    response = requests.get(artifact_url, headers=headers, stream=True, timeout=60)
    response.raise_for_status()

    filename = os.path.join(output_dir, f"{artifact_name}.zip")
    with open(filename, "wb") as file:
        for chunk in response.iter_content(chunk_size=8192):
            file.write(chunk)
    print(f"Downloaded artifact: {artifact_name} to {filename}")
    return filename

def remove_stuff(path):
    """Remove stuff"""
    for item in REMOVE_ITEMS:
        lst = glob.glob(os.path.join(path, item))
        for f in lst:
            if os.path.isdir(f):
                shutil.rmtree(f)
            else:
                os.remove(f)    

def process_one(key, workflow, branch, headers):
    """process one"""
    failMsg = f"General failure processing workflow '{workflow['name']}'"
    outPath = os.path.join(OUTPUT_DIR, key)
    try:
        print(f"Processing workflow: {workflow['name']} on branch: {branch}")
        failMsg = f"Failed downloading latest artifact for workflow: '{workflow['name']}' and branch: {branch}"
        zFile = download_artifact(headers, workflow['name'], workflow['artifact'], OUTPUT_DIR)
        failMsg = f"Failed zip operation on downloaded artifact for workflow: '{workflow['name']}' and branch: {branch}"
        unzip_file(zFile, outPath)
        os.remove(zFile)
        failMsg = f"Failed cleaning artifact for workflow: '{workflow['name']}' and branch: {branch}"
        remove_stuff(outPath)
        failMsg = f"Failed adding additional addins of addition for workflow: '{workflow['name']}' and branch: {branch}"
        addPath = os.path.join(ADDINS_PATH, key)
        if not os.path.exists(addPath):
            print(f"No default addins found at '{addPath}', skipping addins for workflow: '{workflow['name']}' and branch: {branch}.")
        else:
            shutil.copytree(addPath, outPath, dirs_exist_ok=True)
    except requests.exceptions.HTTPError as e:
        raise PackageException(f"No app artifact found for '{key}' app.") from e
    except Exception as e:
        raise PackageException(failMsg) from e

def py_exit(code:int):
    os.environ["PYEXIT"] = str(code)
    with open('pyexit.txt', 'w') as f:
        f.write(str(code))
    exit(code)

def main():
    """main"""
    if len(sys.argv) < 3:
        print("Syntax:\npython package.py branch apikey lasthrs\n\n:branch: Branch from which to pull artifacts\n:apikey: API Token\n:lasthrs: How many hrs since last build (0 to ignore)")
        py_exit(1)

    # Parameters
    token = sys.argv[2]
    if not token:
        print('Missing auth token.')
        py_exit(1)
    else:
        print(f'Found token ending in {token[-4:]}')
    headers = {
        "Authorization": f"Bearer {token}",
        "Accept": "application/vnd.github+json"
    }
    branch = sys.argv[1]
    last_hrs = 0
    if len(sys.argv) > 3:
        last_hrs = int(sys.argv[3])
    if not last_hrs:
        last_hrs = 0

    # Ensure the output directory exists
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    # Get last artifact info and make sure we should be running
    now_minus = datetime.now(timezone.utc) - timedelta(hours=last_hrs)
    last_created = now_minus
    print('Retrieving artifact information...')
    populate_workflow_ids(headers)
    for key, item in WORKFLOWS.items():
        artifact = get_latest_artifact(item['id'], branch, headers)
        if not artifact:
            print(f"No artifact available for '{item['name']}'.  Build fails.")
            py_exit(1)
        item['artifact'] = artifact["archive_download_url"]
        ca = datetime.fromisoformat(artifact['created_at'])
        last_created = max(last_created, ca)

    if last_hrs > 0 and last_created <= now_minus:
        print(f"Last artifact created '{last_created}' which is older than the last build timeframe of {last_hrs} hrs ago ({now_minus}).  Stopping build.")
        py_exit(2)

    print("Artifact information retrieved, beginning build.")
    for key, item in WORKFLOWS.items():
        process_one(key, item, branch, headers)
    remove_root_stuff()
    print("Processing complete.")
    py_exit(0)

if __name__ == "__main__":
    main()
else:
    main()
