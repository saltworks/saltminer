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
import traceback

try:
    import RunSync

except Exception as e:
    
    error_msg = traceback.format_exc()
    print(error_msg)
    logging.critical(error_msg)
    
try:
    import RunPopulateAppVuls

except Exception as e:
    
    error_msg = traceback.format_exc()
    print(error_msg)
    logging.critical(error_msg)

try:
    import RunCustom

except Exception as e:
    
    error_msg = traceback.format_exc()
    print(error_msg)
    logging.critical(error_msg)
