from __future__ import annotations

from pathlib import Path
from uuid import uuid4

from ..exporters import write_json_export, write_netcdf_export
from ..models import ExportRecord, ExportRequest
from ..storage import InvalidFilenameError as StorageInvalidFilenameError
from ..storage import SQLiteTelemetryStore
from .errors import (
    ExportDependencyError,
    ExportNotFoundError,
    InvalidExportFilenameError,
    SessionNotFoundError,
)


class ExportService:
    def __init__(self, store: SQLiteTelemetryStore) -> None:
        self.store = store

    def write_export(self, export_format: str, session_ids: list[str], filename: str) -> Path:
        payloads = []
        for session_id in session_ids:
            try:
                payloads.append(self.store.get_raw_payload(session_id))
            except KeyError:
                raise SessionNotFoundError(session_id) from None

        try:
            path = self.store.export_path(filename)
        except StorageInvalidFilenameError as exc:
            raise InvalidExportFilenameError(str(exc)) from exc

        try:
            if export_format == "json":
                return write_json_export(payloads, path)
            if export_format == "netcdf":
                return write_netcdf_export(payloads, path)
        except RuntimeError as exc:
            raise ExportDependencyError(str(exc)) from exc

        raise ValueError(f"Unsupported export format: {export_format}")

    def create_export(self, export_format: str, request: ExportRequest) -> ExportRecord:
        filename = request.filename or _default_export_filename(export_format)
        path = self.write_export(export_format, request.session_ids, filename)
        record = ExportRecord(
            format=export_format,
            session_ids=request.session_ids,
            path=str(path),
            download_url="",
        )
        record.download_url = f"/exports/{record.export_id}/download"
        return self.store.save_export_record(record)

    def list_exports(self) -> list[ExportRecord]:
        return self.store.list_exports()

    def get_export(self, export_id: str) -> ExportRecord:
        record = self.store.get_export_record(export_id)
        if record is None:
            raise ExportNotFoundError(export_id)
        return record


def _default_export_filename(export_format: str) -> str:
    extension = "nc" if export_format == "netcdf" else "json"
    return f"telemetry_export_{uuid4().hex}.{extension}"
