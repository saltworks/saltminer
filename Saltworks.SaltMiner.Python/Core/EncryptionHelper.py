''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-06-30
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

#!/usr/bin/env python
# -*- coding: utf-8 -*-
__copyright__ = "(C) 2020 Saltworks"

import os
import sys
import re
import json
import logging
from cryptography.fernet import Fernet

class EncryptionHelper(object):
    def __init__(self, keyFile = "saltworks.key"):
        self.__KeyFile = keyFile
        self.__FernetKey = self.__ReadSecret()

    def __VerifySecret(self):
        if self.__SecretExists():
            return True
        else:
            self.__WriteSecret()

    def __SecretExists(self):
        return os.path.isfile(self.__KeyFile) and os.stat(self.__KeyFile).st_size > 0

    def __WriteSecret(self, overwrite=False):
        path = self.__KeyFile
        if self.__SecretExists() and overwrite:
            os.chmod(path, 0o200)
        key = Fernet.generate_key()
        with open(path, 'w') as f:
            f.write(key.decode())
        os.chmod(path, 0o400)
        msg = "New encryption secret generated for application."
        logging.warning(msg)
        print(msg)

    def __ReadSecret(self):
        self.__VerifySecret()
        try:
            with open(self.__KeyFile, 'r') as f:
                key = f.readline().strip()
            return key
        except IOError:
            logging.error("Error retrieving decryption key.")
            raise
    
    @staticmethod
    def EncryptionTag():
        return "e$Fernet$"

    def Encrypt(self, value):
        try:
            cipher = Fernet(self.__FernetKey)
            return EncryptionHelper.EncryptionTag() + cipher.encrypt(value.encode()).decode()
        except ValueError as e:
            logging.error(f"Error encrypting: {e}")
            raise

    def Decrypt(self, encryptedValue):
        try:
            encVer = re.search('e\$(.*)\$.*', encryptedValue).group(1)
        except Exception as e:
            msg = f"Unable to determine encryption version of encrypted value ({e})"
            logging.error(msg)
            raise ValueError(msg)

        if encVer == 'Fernet':
            encryptedValue = encryptedValue.split(encVer + "$", 1)[1]
            try:
                cipher = Fernet(self.__FernetKey)
                decryptedValue = cipher.decrypt(encryptedValue.encode()).decode()
            except Exception as e:
                msg = f"Decryption error - {e}"
                logging.error(msg)
                raise ValueError(msg)
        else:
            msg = "Error decrypting.  Unsupported encryption version"
            logging.error(msg)
            raise Exception(msg)
        return decryptedValue