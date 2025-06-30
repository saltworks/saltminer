''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-06-30
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import sys, os, json, shutil

def get_all_file_paths(directory): 
  
    file_paths = [] 
  
    for root, directories, files in os.walk(directory): 
        for filename in files: 
            filepath = os.path.join(root, filename) 
            file_paths.append(filepath) 
  
    return file_paths

def remove_path(path):
    """
    Removes the given path. If it's a file, it will be deleted.
    If it's a directory, it will be removed along with all its contents.

    :param path: The path to be removed (file or directory)
    """
    try:
        if os.path.isfile(path):
            os.remove(path)
            print(f"File removed: {path}")
        elif os.path.isdir(path):
            # Remove the directory and its contents
            shutil.rmtree(path)
            print(f"Directory removed: {path}")
        else:
            print(f"Path does not exist: {path}")
    except Exception as e:
        print(f"Error removing path {path}: {e}")

def main():
    if len(sys.argv) < 2:
        print("Syntax:\npython3 RunPackageUtility path [exclusions=exclusions.json]\n\n:path: path to files\n:exclusions: Exclusions file")
        exit(1)
    pth = sys.argv[1]
    excl = "exclusions.json"
    if len(sys.argv) > 2:
        excl = sys.argv[2]
    exclusions = []

    try:
        failMsg = "Failed loading exclusions"
        print(f"Loading exclusions from {excl}.")
        with open(excl, 'r') as fr:
            exclusions = json.load(fr)

        print(f"Getting all file paths from path {pth}.")
        failMsg = "Failed to get all file paths"
        file_paths = get_all_file_paths(pth) 
        for file in file_paths:
            failMsg = f"Failed while pruning artifact files - current file: {file}"
            ok = True
            for ex in exclusions:
                if ex in file:
                    ok = False
                    break
            if not ok:
                remove_path(file)
        print("Pruned files successfully.")
    except Exception as e:
        print(f"{failMsg}: {e}")
        raise e

if __name__ == "__main__": 
    main() 