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

import logging
import os
import sys
from threading import RLock
from logging.handlers import QueueHandler, QueueListener, TimedRotatingFileHandler
from queue import Queue
from typing import Optional
import stat  # For file permission handling

class EnhancedQueueListener(QueueListener):
    """QueueListener with error handling"""
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self._fallback_handler = self._create_fallback_handler()

    def _create_fallback_handler(self):
        """Handler for logging subsystem errors"""
        handler = logging.StreamHandler(sys.stderr)
        handler.setLevel(logging.CRITICAL)
        handler.setFormatter(
            logging.Formatter('!!LOGGING FAILURE!! %(message)s')
        )
        return handler

    def handleError(self, record):
        """Override error handling to use fallback mechanism"""
        try:
            # Try to log the error through standard mechanisms first
            super().handleError(record)
        except Exception:
            # Ultimate fallback to stderr
            self._fallback_handler.handle(record)
            sys.stderr.write(f"Critical logging failure handling record: {record}\n")

class ThreadSafeLoggingSystem:
    """
    Logging system with Thread-safe features and Daily log rotation
    
    """
    
    def __init__(self):
        self.listeners = {}  # Track multiple listeners by log name
        self._lock = RLock()  # Add reentrant lock
        self._fallback_handler = None

    def setup_logging(
        self,
        log_name: str = "application",
        log_level: str = "INFO",
        retention_days: int = 3,
        log_dir: Optional[str] = None,
        utc_rotation: bool = True
    ) -> QueueListener:
        """
        Initialize thread-safe logging system with enhanced reliability
        
        Args:
            log_name: Base name for log files and console logger
            log_level: Console logging level (DEBUG, INFO, WARNING, ERROR, CRITICAL)
            retention_days: Number of days to keep rotated logs (min 1, max 30)
            log_dir: Directory for log files (default: current directory)
            utc_rotation: Use UTC time for log rotation (recommended for distributed systems)
            
        Returns:
            QueueListener instance that must be maintained during application lifecycle
            
        Raises:
            LoggingConfigurationError: For invalid parameters or unrecoverable errors
        """
        try:
            # Validate inputs
            self._validate_inputs(log_level, retention_days)
            
            # Create log directory if specified
            log_dir = self._ensure_log_directory(log_dir)
            
            # Configure core logging components
            log_queue, console_handler, file_handler = self._create_handlers(
                log_name, log_level, retention_days, log_dir, utc_rotation
            )

            # Set up queue listener pattern
            listener = self._create_listener(log_queue, console_handler, file_handler)
            
            # Configure root logger and ensure clean state
            self._configure_root_logger(log_queue)
            
            # Store listener for proper cleanup
            self.listeners[log_name] = listener
            
            # Set secure permissions on log file
            self._set_file_permissions(file_handler.baseFilename)
            
            logging.info(f"Logging system initialized for {log_name}")
            return listener
            
        except Exception as e:
            self._handle_setup_error(e)
            raise LoggingConfigurationError(f"Failed to initialize logging: {str(e)}") from e

    def _validate_inputs(self, log_level: str, retention_days: int):
        """Ensure parameters meet operational requirements"""
        valid_levels = {'DEBUG', 'INFO', 'WARNING', 'ERROR', 'CRITICAL'}
        if log_level.upper() not in valid_levels:
            raise ValueError(f"Invalid log level: {log_level}. Valid options: {valid_levels}")
            
        if not (1 <= retention_days <= 30):
            raise ValueError("Retention days must be between 1 and 30")

    def _ensure_log_directory(self, log_dir: Optional[str]) -> str:
        """Create and validate log directory with error handling"""
        if log_dir:
            try:
                os.makedirs(log_dir, exist_ok=True)
                if not os.access(log_dir, os.W_OK):
                    raise PermissionError(f"Write permission denied for log directory: {log_dir}")
                return log_dir
            except Exception as e:
                logging.error(f"Log directory error: {str(e)}")
                raise
        return os.getcwd()

    def _create_handlers(
        self,
        log_name: str,
        log_level: str,
        retention_days: int,
        log_dir: str,
        utc_rotation: bool
    ) -> tuple[Queue, logging.Handler, logging.Handler]:
        """Create and configure logging handlers with config settings"""
        try:
            # Create formatter with PID tracking
            formatter = logging.Formatter(
                '%(asctime)s.%(msecs)03d [%(process)d] %(levelname)-8s %(threadName)s %(message)s',
                datefmt='%Y-%m-%dT%H:%M:%S'
            )

            # Console handler with level filtering
            console_handler = logging.StreamHandler()
            console_handler.setLevel(log_level.upper())
            console_handler.setFormatter(formatter)

            # File handler with daily rotation and retention
            full_path = os.path.join(log_dir, f"{log_name}.log")
            file_handler = TimedRotatingFileHandler(
                filename=full_path,
                when='midnight',
                interval=1,
                backupCount=retention_days,
                encoding='utf-8',
                utc=utc_rotation
            )
            file_handler.suffix = "%Y-%m-%d"
            file_handler.setLevel(log_level.upper())  # File gets all levels
            file_handler.setFormatter(formatter)

            # Create thread-safe queue
            log_queue = Queue(-1)

            return log_queue, console_handler, file_handler

        except PermissionError as pe:
            logging.critical(f"File permission error: {str(pe)}")
            raise
        except Exception as e:
            logging.error(f"Handler creation failed: {str(e)}")
            raise

    def _create_listener(
        self,
        log_queue: Queue,
        console_handler: logging.Handler,
        file_handler: logging.Handler
    ) -> EnhancedQueueListener:
        """Initialize and start enhanced queue listener"""
        listener = EnhancedQueueListener(
            log_queue,
            console_handler,
            file_handler,
            respect_handler_level=True
        )
        listener.start()
        return listener

    def _configure_root_logger(self, log_queue: Queue):
        """Configure root logger with thread-safe settings"""
        root_logger = logging.getLogger()
        root_logger.setLevel(logging.DEBUG)
        root_logger.handlers = []
        root_logger.addHandler(QueueHandler(log_queue))

    def _set_file_permissions(self, filepath: str):
        """Set secure permissions (rw-r-----) on log files"""
        try:
            os.chmod(filepath, stat.S_IRUSR | stat.S_IWUSR | stat.S_IRGRP)
        except Exception as e:
            logging.warning(f"Couldn't set permissions on {filepath}: {str(e)}")

    def _handle_setup_error(self, error: Exception):
        """Emergency logging when primary setup fails"""
        if not self._fallback_handler:
            self._fallback_handler = logging.StreamHandler(sys.stderr)
            self._fallback_handler.setFormatter(
                logging.Formatter('!!EMERGENCY!! %(message)s')
            )
        logging.basicConfig(handlers=[self._fallback_handler], level=logging.CRITICAL)
        logging.critical("Logging system failure: %s", str(error))

    def shutdown(self):
        """Gracefully stop all logging components with flush"""

        with self._lock:
            # Create a static list of listener names to iterate over
            listener_names = list(self.listeners.keys())
        
        for name in listener_names:
            try:
                # Get listener from original dict (may have been removed)
                listener = self.listeners.get(name)
                if listener:
                    logging.debug(f"Stopping {name} logging subsystem")
                    listener.stop()
                    del self.listeners[name]
                if self.listeners:
                    sys.stderr.write(f"Warning: {len(self.listeners)} listeners remaining after shutdown\n")
            except Exception as e:
                sys.stderr.write(f"Error stopping listener {name}: {str(e)}\n")
        logging.shutdown()

class LoggingConfigurationError(Exception):
    """Specialized exception for logging configuration failures"""
    pass


logging_system = ThreadSafeLoggingSystem()
