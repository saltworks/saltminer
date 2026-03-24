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