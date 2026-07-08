from __future__ import annotations

from typing import Any

from fastapi import APIRouter, Depends, HTTPException
from fastapi.responses import FileResponse

from ..models import IngestResponse, SessionIndexRecord
from ..services import (
    ExportDependencyError,
    ExportService,
    InvalidExportFilenameError,
    SessionNotFoundError,
    TelemetryPayloadError,
    TelemetryService,
)
from .dependencies import get_export_service, get_telemetry_service


router = APIRouter(prefix="/telemetry/sessions", tags=["telemetry sessions"])


@router.post("", response_model=IngestResponse)
async def ingest_session(
    raw_payload: dict[str, Any],
    service: TelemetryService = Depends(get_telemetry_service),
) -> IngestResponse:
    try:
        return service.ingest_session(raw_payload)
    except TelemetryPayloadError as exc:
        raise HTTPException(
            status_code=422,
            detail={"code": "invalid_telemetry_payload", "errors": exc.errors},
        ) from exc


@router.get("", response_model=list[SessionIndexRecord])
def list_sessions(service: TelemetryService = Depends(get_telemetry_service)) -> list[SessionIndexRecord]:
    return service.list_sessions()


@router.get("/{session_id}", response_model=SessionIndexRecord)
def get_session(
    session_id: str,
    service: TelemetryService = Depends(get_telemetry_service),
) -> SessionIndexRecord:
    try:
        return service.get_session(session_id)
    except SessionNotFoundError as exc:
        raise HTTPException(
            status_code=404,
            detail={"code": "session_not_found", "message": "Session not found.", "session_id": session_id},
        ) from exc


@router.get("/{session_id}/raw")
def get_raw_session(
    session_id: str,
    service: TelemetryService = Depends(get_telemetry_service),
) -> dict[str, Any]:
    try:
        return service.get_raw_session(session_id)
    except SessionNotFoundError as exc:
        raise HTTPException(
            status_code=404,
            detail={"code": "session_not_found", "message": "Session not found.", "session_id": session_id},
        ) from exc


@router.get("/{session_id}/export/json")
def export_session_json(
    session_id: str,
    service: ExportService = Depends(get_export_service),
) -> FileResponse:
    return _create_export_response(service, "json", [session_id], f"{session_id}.json")


@router.get("/{session_id}/export/netcdf")
def export_session_netcdf(
    session_id: str,
    service: ExportService = Depends(get_export_service),
) -> FileResponse:
    return _create_export_response(service, "netcdf", [session_id], f"{session_id}.nc")


def _create_export_response(
    service: ExportService,
    export_format: str,
    session_ids: list[str],
    filename: str,
) -> FileResponse:
    try:
        path = service.write_export(export_format, session_ids, filename)
    except SessionNotFoundError as exc:
        raise HTTPException(
            status_code=404,
            detail={
                "code": "session_not_found",
                "message": f"Session not found: {exc.session_id}",
                "session_id": exc.session_id,
            },
        ) from exc
    except InvalidExportFilenameError as exc:
        raise HTTPException(
            status_code=400,
            detail={"code": "invalid_export_filename", "message": str(exc)},
        ) from exc
    except ExportDependencyError as exc:
        raise HTTPException(status_code=503, detail={"code": "export_unavailable", "message": str(exc)}) from exc

    return FileResponse(path, filename=filename)
