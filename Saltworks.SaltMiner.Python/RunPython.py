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

import logging
import subprocess
import sys
from pathlib import Path
from Core.Application import Application

app = Application(loggingInstance='RunPython')

def main():
  logging.info(f'RunPython arguments: {sys.argv}')

  a = sys.argv
  mod = Path(a[1]).stem

  a[0] = 'python3'
  a.insert(1, '-m')
  a[2] = f'Custom.{mod}' 

  try:
    subprocess.run(a)
  except Exception as e:
    prog = Path(__file__).name
    logging.critical("[%s] Exception: [%s] %s", prog, type(e).__name__, e)
    raise


if __name__ == "__main__":
    main()
