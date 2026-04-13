''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import logging
import subprocess
import sys
from pathlib import Path
from Core.Application import Application

app = Application(loggingCustomTag='RunPython')

def main():
    subprocess.run(
        [
            sys.executable,
            "-m",
            "unittest",
            "UnitTests.AsyncHelperTests"
        ],
        check=True
    )

if __name__ == "__main__":
    main()
