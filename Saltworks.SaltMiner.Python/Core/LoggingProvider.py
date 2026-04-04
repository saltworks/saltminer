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
import sys
import datetime
from logging.handlers import RotatingFileHandler

import ecs_logging

_CONSOLE_FORMAT = '%(asctime)s [%(levelname)s] %(funcName)s in %(filename)s: %(message)s'
_CONSOLE_DATE_FORMAT = '%Y-%m-%d %H:%M'


class LoggingProviderException(Exception):
    '''Custom exception for logging provider errors.'''
    pass

class LoggingProviderConfigException(LoggingProviderException):
    '''Custom exception for logging provider configuration errors.'''
    pass

class LoggingFilter(logging.Filter):
    def __init__(self, name="", suppress_base_warnings=True):
        super().__init__(name)
        self._suppress_base_warnings = suppress_base_warnings

    def filter(self, record):
        filtered = ["base", "connectionpool", "_transport"]
        if self._suppress_base_warnings:
            return record.module not in filtered or record.levelno > logging.WARNING
        return record.module not in filtered or record.levelno > logging.INFO


class LoggingProvider:
    """Centralizes logging configuration for simple (root logger) and threaded (per-worker) use."""

    def __init__(self, app, custom_tag: str | None = None, suppress_base_warnings: bool = True):
        self._app = app
        self._suppress_base_warnings = suppress_base_warnings
        self._custom_tag = None
        self._level = logging.DEBUG
        self._log_folder = app.Settings.Get('Logging', 'Folder', './logs/')
        self._thread_loggers: dict[int, logging.Logger] = {}
        try:
            self.configure(custom_tag)
        except Exception as e:
            raise LoggingProviderConfigException(f"Failed to configure logging provider: {e}") from e

    def configure(self, custom_tag: str | None = None):
        """Configure the root logger for simple logging (logging.info() etc.)."""
        self._custom_tag = custom_tag or None
        if not os.path.exists(self._log_folder):
            os.makedirs(self._log_folder)
        self._level = self._parse_log_level(self._app.Settings.Get('Logging', 'LogLevel', 'DEBUG'))
        log_path = self._build_log_path(self._custom_tag)
        print(f'SaltMiner logging to: {log_path}')
        root = logging.getLogger()
        for handler in root.handlers[:]:
            handler.close()
            root.removeHandler(handler)
        root.setLevel(self._level)
        root.addHandler(self._make_console_handler())
        root.addHandler(self._make_file_handler(log_path))

    def get_thread_logger(self, thread_id: int) -> logging.Logger:
        """Return a named logger for a thread with its own dedicated file handler."""
        if thread_id in self._thread_loggers:
            return self._thread_loggers[thread_id]
        thread_tag = f"{self._custom_tag}-{thread_id}" if self._custom_tag else f"{thread_id}"
        if not os.path.exists(self._log_folder):
            os.makedirs(self._log_folder)
        log_path = self._build_log_path(thread_tag)
        logger = logging.getLogger(f"smpy-{thread_tag}")
        logger.setLevel(self._level)
        logger.propagate = False
        logger.addHandler(self._make_file_handler(log_path))
        logger.addHandler(self._make_console_handler())
        self._thread_loggers[thread_id] = logger
        return logger

    def _build_log_path(self, custom_tag: str | None) -> str:
        date_str = datetime.datetime.now().strftime('%Y%m%d')
        filename = f"smpy-{custom_tag}-{date_str}.log" if custom_tag else f"smpy-{date_str}.log"
        return os.path.join(self._log_folder, filename)

    def _make_file_handler(self, log_path: str) -> RotatingFileHandler:
        handler = RotatingFileHandler(
            log_path, 'a',
            maxBytes=self._app.Settings.Get('Logging', 'FileMaxBytes', 536870912),
            backupCount=self._app.Settings.Get('Logging', 'FileCountLimit', 9)
        )
        handler.setFormatter(self._make_file_formatter())
        handler.setLevel(self._level)
        handler.addFilter(LoggingFilter(suppress_base_warnings=self._suppress_base_warnings))
        return handler

    def _make_file_formatter(self) -> logging.Formatter:
        file_format = self._app.Settings.Get('Logging', 'FileFormat', None)
        if not file_format:
            file_format = 'simple'
        elif file_format.lower() not in ('ecs', 'simple'):
            print(f"{datetime.datetime.now():{_CONSOLE_DATE_FORMAT}} [PRELOG] [WARNING] Unknown Logging.FileFormat '{file_format}', defaulting to 'simple'")
            file_format = 'simple'
        if file_format.lower() == 'ecs':
            return ecs_logging.StdlibFormatter(
                stack_trace_limit=self._app.Settings.Get('Logging', 'StackTraceLimit', 3)
            )
        return logging.Formatter(_CONSOLE_FORMAT, _CONSOLE_DATE_FORMAT)

    def _make_console_handler(self) -> logging.StreamHandler:
        handler = logging.StreamHandler(sys.stdout)
        handler.setFormatter(logging.Formatter(_CONSOLE_FORMAT, _CONSOLE_DATE_FORMAT))
        handler.setLevel(self._level)
        handler.addFilter(LoggingFilter(suppress_base_warnings=self._suppress_base_warnings))
        return handler

    @staticmethod
    def _parse_log_level(log_level: str) -> int:
        return {
            'WARN': logging.WARN,
            'DEBUG': logging.DEBUG,
            'ERROR': logging.ERROR,
            'INFO': logging.INFO,
            'CRITICAL': logging.CRITICAL,
        }.get(log_level.upper(), logging.NOTSET)
