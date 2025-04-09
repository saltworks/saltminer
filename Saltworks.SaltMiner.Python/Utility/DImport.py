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

import importlib

class DImport(object):
    """
    Used to dynamically import a class and then return a class factory
    """
    def __init__(self):
        pass

    @staticmethod
    def Import(moduleName, className, package = None):
        """
        Dynamically import a class and then return a class factory

        Usage: 
        (from Utility.DImport import DImport)
        classFactory = DImport.Import("moduleName", "className")
        instance = classFactory(prm1, prm2)
        """
        #__import__ method used to fetch module 
        module = importlib.import_module(moduleName, package) 
  
        # getting class as "attribute" of module  
        myclass = getattr(module, className)
        
        return myclass

