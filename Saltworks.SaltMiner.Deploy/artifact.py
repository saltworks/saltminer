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

import sys
import os
import json
import requests

class ApiException(Exception):
    pass

if len(sys.argv) < 5:
    print("Missing required arguments.  Usage:\n")
    print("python3 artifact.py [current version] [api key] [repository] [path] [out file name=artifact.zip] [out version file name=version.txt\n")
    exit(0)

authHdr = { "X-JFrog-Art-Api": sys.argv[2] }
baseUrl = "https://saltminer.jfrog.io/artifactory/"
cver = sys.argv[1]
repo = sys.argv[3]
path = sys.argv[4]
outfile = "artifact.zip"
verfile = "version.txt"
if len(sys.argv) >= 6:
  outfile = sys.argv[5]
if len(sys.argv) >= 7:
  verfile = sys.argv[6]
version = cver
uri = ""

# Get latest artifact version
try:
    url = baseUrl + "api/versions/" + repo + "/" + path + "?listFiles=1"
    r = requests.get(url, headers = authHdr)
    if r.status_code == 404:
        raise ApiException("No artifacts found")
    if r.status_code != 200:
        raise ApiException("Error calling Artifactory API: [{}] {}".format(r.status_code, r.reason))
    ro = json.loads(r.text)
    version = ro['version']
    uri = ro['artifacts'][0]['downloadUri']
    with open(verfile, "w") as f:
        f.write(version + "\n")
except Exception as ex:
    print("Error getting current version of artifact for repo '{}' and path '{}':[{}] {}".format(repo, path, type(ex).__name__, ex))
    print("Url called: " + url)
    exit(0)

if version == cver:
    print("No update needed")
    exit(0)

# Download latest artifact
try:
    df = requests.get(uri, headers = authHdr)
    if df.status_code != 200:
        raise ApiException("Error downloading artifact from Artifactory: [{}] {}".format(df.status_code, df.reason))
    print("Current Artifactory version for repo '{}' and path '{}' is '{}'".format(repo, path, version))
    with open(outfile, "wb") as f:
        f.write(df.content)
    print("Latest artifact retrieved, see {} and {}".format(verfile, outfile))
except Exception as ex:
    print("Error getting current version of artifact for repo '{}' and path '{}':[{}] {}".format(repo, path, type(ex).__name__, ex))

