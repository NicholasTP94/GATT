from __future__ import annotations

import hashlib
import json
import re
from typing import Any

from pydantic import ValidationError

from ..exporters import write_netcdf_export
from ..models import ExportRecord, IngestResponse, SessionIndexRecord, TelemetrySessionEnvelope, ValidationMessage
from ..storage import SQLiteTelemetryStore
from ..storage import InvalidFilenameError as StorageInvalidFilenameError
from ..validation import validate_envelope
from .errors import SessionNotFoundError, TelemetryPayloadError


SAFE_EXPORT_ID_STEM = re.compile(r"^[A-Za-z0-9._-]+$")


class TelemetryService:
    def __init__(self, store: SQLiteTelemetryStore) -> None:
        self.store = store

    def ingest_session(self, raw_payload: dict[str, Any]) -> IngestResponse:
        try:
            envelope = TelemetrySessionEnvelope.model_validate(raw_payload)
        except ValidationError as exc:
            raise TelemetryPayloadError(_json_safe_errors(exc.errors())) from exc

        warnings = validate_envelope(envelope)
        self.store.save_session(envelope, raw_payload, warning_count=len(warnings))
        session = envelope.session
        exports = self._create_auto_netcdf_export(session.session_id, raw_payload, warnings)

        if len(warnings) > 0:
            self.store.update_session_warning_count(session.session_id, len(warnings))

        return IngestResponse(
            accepted=True,
            session_id=session.session_id,
            schema_version=envelope.schema_version,
            event_count=len(session.events),
            state_count=len(session.states),
            performance_sample_count=len(session.performance_samples),
            warnings=warnings,
            exports=exports,
        )

    def list_sessions(self) -> list[SessionIndexRecord]:
        return self.store.list_sessions()

    def get_session(self, session_id: str) -> SessionIndexRecord:
        record = self.store.get_session_record(session_id)
        if record is None:
            raise SessionNotFoundError(session_id)
        return record

    def get_raw_session(self, session_id: str) -> dict[str, Any]:
        try:
            return self.store.get_raw_payload(session_id)
        except KeyError:
            raise SessionNotFoundError(session_id) from None

    def _create_auto_netcdf_export(
        self,
        session_id: str,
        raw_payload: dict[str, Any],
        warnings: list[ValidationMessage],
    ) -> list[ExportRecord]:
        filename = f"{session_id}.nc"

        try:
            path = self.store.export_path(filename)
            write_netcdf_export([raw_payload], path)
        except (RuntimeError, StorageInvalidFilenameError) as exc:
            warnings.append(
                ValidationMessage(
                    code="netcdf_export_failed",
                    path="exports.netcdf",
                    message=f"NetCDF export was not created: {exc}",
                )
            )
            return []

        record = ExportRecord(
            export_id=f"{_safe_export_id_stem(session_id)}_netcdf",
            format="netcdf",
            session_ids=[session_id],
            path=str(path),
            download_url="",
        )
        record.download_url = f"/exports/{record.export_id}/download"
        return [self.store.save_export_record(record)]


def _json_safe_errors(errors: list[dict[str, Any]]) -> list[dict[str, Any]]:
    return json.loads(json.dumps(errors, default=str))


def _safe_export_id_stem(session_id: str) -> str:
    if session_id and session_id not in {".", ".."} and SAFE_EXPORT_ID_STEM.match(session_id):
        return session_id
    digest = hashlib.sha256(session_id.encode("utf-8")).hexdigest()
    return f"session_{digest}"
