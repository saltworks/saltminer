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

import logging
import os
import unittest

from Core.Application import Application
from Core.LoggingProvider import LoggingProvider

module = os.path.splitext(os.path.basename(__file__))[0]


class LoggingTests(unittest.TestCase):
    '''Tests for LoggingProvider — covers root logger (logging.info()) and
    per-thread named logger (get_thread_logger()) use cases.

    Requires a valid Application config (Logging.Folder at minimum).
    Does not require a running DataApi.
    '''

    @classmethod
    def setUpClass(cls):
        cls.app = Application()
        cls.logging_provider = cls.app.logging_provider

    # ------------------------------------------------------------------
    # Root logger (logging.info() / logging.debug() etc.)
    # ------------------------------------------------------------------

    def test_root_logger_info(self):
        '''logging.info() writes without raising.'''
        logging.info("[LoggingTests] test_root_logger_info: info message")
        print(f'[TEST SUCCESS] {module}:test_root_logger_info')

    def test_root_logger_debug(self):
        '''logging.debug() writes without raising.'''
        logging.debug("[LoggingTests] test_root_logger_debug: debug message")
        print(f'[TEST SUCCESS] {module}:test_root_logger_debug')

    def test_root_logger_warning(self):
        '''logging.warning() writes without raising.'''
        logging.warning("[LoggingTests] test_root_logger_warning: warning message")
        print(f'[TEST SUCCESS] {module}:test_root_logger_warning')

    def test_root_logger_error(self):
        '''logging.error() writes without raising.'''
        logging.error("[LoggingTests] test_root_logger_error: error message")
        print(f'[TEST SUCCESS] {module}:test_root_logger_error')

    def test_root_logger_exception(self):
        '''logging.exception() inside an except block writes without raising.'''
        try:
            raise ValueError("test exception for logging")
        except ValueError:
            logging.exception("[LoggingTests] test_root_logger_exception: exception message")
        print(f'[TEST SUCCESS] {module}:test_root_logger_exception')

    # ------------------------------------------------------------------
    # Named thread logger (logging_provider.get_thread_logger())
    # ------------------------------------------------------------------

    def test_get_thread_logger_returns_logger(self):
        '''get_thread_logger() returns a logging.Logger instance.'''
        logger = self.logging_provider.get_thread_logger(1)
        self.assertIsInstance(logger, logging.Logger)
        print(f'[TEST SUCCESS] {module}:test_get_thread_logger_returns_logger')

    def test_get_thread_logger_same_instance(self):
        '''get_thread_logger() returns the same instance for the same thread_id.'''
        logger_a = self.logging_provider.get_thread_logger(42)
        logger_b = self.logging_provider.get_thread_logger(42)
        self.assertIs(logger_a, logger_b)
        print(f'[TEST SUCCESS] {module}:test_get_thread_logger_same_instance')

    def test_get_thread_logger_different_ids(self):
        '''get_thread_logger() returns distinct loggers for different thread_ids.'''
        logger_1 = self.logging_provider.get_thread_logger(100)
        logger_2 = self.logging_provider.get_thread_logger(101)
        self.assertIsNot(logger_1, logger_2)
        print(f'[TEST SUCCESS] {module}:test_get_thread_logger_different_ids')

    def test_get_thread_logger_writes_info(self):
        '''Named thread logger writes info without raising.'''
        logger = self.logging_provider.get_thread_logger(2)
        logger.info("[LoggingTests] test_get_thread_logger_writes_info: info message")
        print(f'[TEST SUCCESS] {module}:test_get_thread_logger_writes_info')

    def test_get_thread_logger_writes_debug(self):
        '''Named thread logger writes debug without raising.'''
        logger = self.logging_provider.get_thread_logger(3)
        logger.debug("[LoggingTests] test_get_thread_logger_writes_debug: debug message")
        print(f'[TEST SUCCESS] {module}:test_get_thread_logger_writes_debug')

    def test_get_thread_logger_writes_warning(self):
        '''Named thread logger writes warning without raising.'''
        logger = self.logging_provider.get_thread_logger(4)
        logger.warning("[LoggingTests] test_get_thread_logger_writes_warning: warning message")
        print(f'[TEST SUCCESS] {module}:test_get_thread_logger_writes_warning')

    def test_get_thread_logger_not_propagating(self):
        '''Named thread logger has propagate=False so messages do not double-log.'''
        logger = self.logging_provider.get_thread_logger(5)
        self.assertFalse(logger.propagate)
        print(f'[TEST SUCCESS] {module}:test_get_thread_logger_not_propagating')

    def test_get_thread_logger_log_file_created(self):
        '''get_thread_logger() creates a log file on disk for the given thread_id.'''
        logger = self.logging_provider.get_thread_logger(999)
        logger.info("[LoggingTests] test_get_thread_logger_log_file_created: file creation check")
        log_folder = self.app.Settings.Get('Logging', 'Folder', './logs/')
        # At least one log file for thread 999 should exist
        log_files = [f for f in os.listdir(log_folder) if '999' in f and f.endswith('.log')]
        self.assertGreater(len(log_files), 0, f"Expected a log file containing '999' in {log_folder}")
        print(f'[TEST SUCCESS] {module}:test_get_thread_logger_log_file_created')


if __name__ == '__main__':
    unittest.main()
