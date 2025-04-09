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

from Utility.ProgressLogger import *
from Core.Application import Application
import time

es = Application().GetElasticClient()
p = ProgressLogger(es)
p.Start("test", 20)
for i in range(1,20,1):
    time.sleep(5)
    p.Progress(i)
p.Finish()

sys.exit()
#r = es.GetIndex(ProgressLogger.IndexName(), "id:ascending,timestamp:ascending")
#for x in r:
#    print("{}".format(x))
