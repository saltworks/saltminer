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

# Example calls from command prompt:
# python -m Custom.RunTouchLog          # initializes log file [yyyy.mm.dd].SaltMiner.log
# python -m Custom.RunTouchLog sample   # initializes log file [yyyy.mm.dd].SaltMiner.sample.log

import logging
import sys

from Core.Application import Application

app = None
if len(sys.argv) >= 2:
    app = Application(loggingCustomTag=str(sys.argv[1]))
else:
    app = Application()
    
logging.info("Log initialized")