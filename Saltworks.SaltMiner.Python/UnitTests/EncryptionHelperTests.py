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

import sys
import os

from Core.EncryptionHelper import EncryptionHelper

module = os.path.splitext(os.path.basename(__file__))[0]

def MainTests():
    # Arrange
    eh = EncryptionHelper()

    # Act
    s1 = "Hi there, how's it going?"
    s4 = "Wp9VM4!@$%^&*()_=+-5#"
    s2 = eh.Encrypt(s1)
    s3 = eh.Decrypt(s2)
    s5 = eh.Encrypt(s4)
    s6 = eh.Decrypt(s5)

    # Assert
    try:
        assert s2.startswith("e$Fernet$"), "Encrypted value should start with 'e$Fernet$'"
        assert s3 == s1, "Encrypted value doesn't match original"
        assert s4 == s6, "Encrypted value with special chars doesn't match original"
        
        fn = sys._getframe().f_code.co_name
        print(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        print(f"[TEST FAILURE] {module}:{fn}: {e}")

MainTests()