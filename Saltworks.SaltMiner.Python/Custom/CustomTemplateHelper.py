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

# This is a template helper class that can be used to bootstrap your own helper class.
# 1. Make sure to include settings in the constructor/init.
# 2. Add a Run main method that can be called from the runner to kick off the main logic.
# 3. try...except blocks are useful for better flagging where an error happens, but not strictly necessary if you don't need to handle a known possible exception.
# 4. Logging should attempt to balance program responsiveness with "over sharing" (log status every 100 "things" instead of every "thing" for example).

# system imports
import logging
import os

# third party imports
# * none in this template *

# local imports 
from Core.ApplicationSettings import ApplicationSettings

class CustomTemplateHelper(object):

    def __init__(self, settings):
        if not isinstance(settings, ApplicationSettings):
            raise ValueError("Parameter 'settings' must be of type 'ApplicationSettings'.")

        # if you need an elastic client, you can set one as a special member of the class
        # self.__Es = settings.Application.GetElasticClient()
        self.__Prog = os.path.splitext(os.path.basename(__file__))[0]

    # Example internal class method
    def __CountTo3(self):
        c = 1
        c += 1
        c += 1
        return c

    # Example main "Run" method
    def Run(self):
        # SETUP
        try:
            # Setup goes here, like instantiation of dependent classes, lookups, initial queries, etc.

            # Logging in setup can helpfully indicate program and main area
            logging.info("[%s - Setup] Setting up...", self.__Prog)
        except Exception as e:
            logging.critical("[%s - Setup] Exception: [%s] %s", self.__Prog, type(e).__name__, e)
            raise

        # MAIN
        try:
            # Main logic goes here, often take this dataset and for each thing do something.
            c = self.__CountTo3()

            # Logging in main can helpfully indicate program and main area.  Start/Finish logging is in the runner.
            logging.info("[%s - Main] Counted to %s!", self.__Prog, c)
        except Exception as e:
            logging.critical("[%s - Main] Exception: [%s] %s", self.__Prog, type(e).__name__, e)
            raise
