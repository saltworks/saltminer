''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import os
import unittest

from Core.EncryptionHelper import EncryptionHelper

module = os.path.splitext(os.path.basename(__file__))[0]


class EncryptionHelperTests(unittest.TestCase):
    """Unit tests for EncryptionHelper functionality."""

    def setUp(self):
        """[UnitTest] Set up test fixtures before each test method."""
        self.eh = EncryptionHelper()

    def test_main(self):
        """Test encryption and decryption functionality."""
        # Arrange
        s1 = "Hi there, how's it going?"
        s4 = "Wp9VM4!@$%^&*()_=+-5#"

        # Act
        s2 = self.eh.Encrypt(s1)
        s3 = self.eh.Decrypt(s2)
        s5 = self.eh.Encrypt(s4)
        s6 = self.eh.Decrypt(s5)

        # Assert
        self.assertTrue(s2.startswith("e$Fernet$"), "Encrypted value should start with 'e$Fernet$'")
        self.assertEqual(s3, s1, "Encrypted value doesn't match original")
        self.assertEqual(s4, s6, "Encrypted value with special chars doesn't match original")
        print(f"[TEST SUCCESS] {module}:test_main")


if __name__ == '__main__':
    unittest.main()