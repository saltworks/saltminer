''' --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
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
