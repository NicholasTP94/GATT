from __future__ import annotations

from typing import Any


class TelemetryPayloadError(ValueError):
    def __init__(self, errors: list[dict[str, Any]]) -> None:
        super().__init__("Telemetry payload is invalid.")
        self.errors = errors


class SessionNotFoundError(KeyError):
    def __init__(self, session_id: str) -> None:
        super().__init__(session_id)
        self.session_id = session_id


class ExportNotFoundError(KeyError):
    def __init__(self, export_id: str) -> None:
        super().__init__(export_id)
        self.export_id = export_id


class InvalidExportFilenameError(ValueError):
    pass


class ExportDependencyError(RuntimeError):
    pass
