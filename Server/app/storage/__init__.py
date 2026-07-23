from .sqlite_store import InvalidFilenameError, SQLiteTelemetryStore

FileTelemetryStore = SQLiteTelemetryStore

__all__ = ["FileTelemetryStore", "InvalidFilenameError", "SQLiteTelemetryStore"]
