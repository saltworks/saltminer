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

class CancelTracker(object):
    def __init__(self, initVal=False):
        self.__Cancel = initVal

    @property
    def Cancel(self):
        return self.__Cancel

    @Cancel.setter
    def Cancel(self, value):
        self.__Cancel = value
