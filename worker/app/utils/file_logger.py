"""
File-based logging utility for the invoice worker.
Routes logs to status-based subfolders: success/, warn/, fail/
Each service run creates timestamped .txt log files.
"""
import os
import logging
from datetime import datetime, timezone


# ─── Level-based filters ───────────────────────────────────────────

class SuccessFilter(logging.Filter):
    """Pass only DEBUG and INFO level logs."""
    def filter(self, record):
        return record.levelno <= logging.INFO


class WarnFilter(logging.Filter):
    """Pass only WARNING level logs."""
    def filter(self, record):
        return record.levelno == logging.WARNING


class FailFilter(logging.Filter):
    """Pass only ERROR and CRITICAL level logs."""
    def filter(self, record):
        return record.levelno >= logging.ERROR


# ─── Setup function ────────────────────────────────────────────────

def setup_file_logging(base_dir: str = None):
    """
    Configure file-based logging with status-categorized subfolders.

    Creates the directory structure:
        logs/logs_worker/success/
        logs/logs_worker/warn/
        logs/logs_worker/fail/

    Each run produces a timestamped .txt file in each subfolder.

    Args:
        base_dir: Base directory for logs. Defaults to project root's 'logs' folder.
    """
    if base_dir is None:
        # Navigate from worker/app/utils/ up to worker/ then to project root
        project_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
        base_dir = os.path.join(project_root, "logs", "logs_worker")

    # Create subdirectories
    subdirs = ["success", "warn", "fail"]
    for subdir in subdirs:
        os.makedirs(os.path.join(base_dir, subdir), exist_ok=True)

    # Timestamp for this run's log files
    timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%d_%H-%M-%S")

    # Common formatter
    formatter = logging.Formatter(
        "%(asctime)s | %(name)s | %(levelname)s | %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S"
    )

    # Filter + handler pairs
    handlers_config = [
        ("success", SuccessFilter()),
        ("warn", WarnFilter()),
        ("fail", FailFilter()),
    ]

    root_logger = logging.getLogger()

    for subdir, log_filter in handlers_config:
        filepath = os.path.join(base_dir, subdir, f"{timestamp}.txt")
        handler = logging.FileHandler(filepath, encoding="utf-8")
        handler.setLevel(logging.DEBUG)
        handler.setFormatter(formatter)
        handler.addFilter(log_filter)
        root_logger.addHandler(handler)

    logging.getLogger(__name__).info(
        "File logging initialized → %s (run: %s)", base_dir, timestamp
    )
