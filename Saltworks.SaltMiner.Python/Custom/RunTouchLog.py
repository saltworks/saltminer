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

# Example calls from command prompt:
# python -m Custom.RunTouchLog          # initializes log file [yyyy.mm.dd].SaltMiner.log
# python -m Custom.RunTouchLog sample   # initializes log file [yyyy.mm.dd].SaltMiner.sample.log

import logging
import sys

from Core.Application import Application

app = None
if len(sys.argv) >= 2:
    app = Application(loggingInstance=str(sys.argv[1]))
else:
    app = Application()
    
logging.info("Log initialized")