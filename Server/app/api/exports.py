from pathlib import Path

from fastapi import APIRouter, Depends, HTTPException
from fastapi.responses import FileResponse

from ..models import ExportRecord, ExportRequest
from ..services import (
    ExportDependencyError,
    ExportNotFoundError,
    ExportService,
    InvalidExportFilenameError,
    SessionNotFoundError,
)
from .dependencies import get_export_service


router = APIRouter(prefix="/exports", tags=["exports"])


@router.post("/json", response_model=ExportRecord)
def create_json_export(
    request: ExportRequest,
    service: ExportService = Depends(get_export_service),
) -> ExportRecord:
    return _create_export_record(service, "json", request)


@router.post("/netcdf", response_model=ExportRecord)
def create_netcdf_export(
    request: ExportRequest,
    service: ExportService = Depends(get_export_service),
) -> ExportRecord:
    return _create_export_record(service, "netcdf", request)


@router.get("", response_model=list[ExportRecord])
def list_exports(service: ExportService = Depends(get_export_service)) -> list[ExportRecord]:
    return service.list_exports()


@router.get("/{export_id}", response_model=ExportRecord)
def get_export(
    export_id: str,
    service: ExportService = Depends(get_export_service),
) -> ExportRecord:
    try:
        return service.get_export(export_id)
    except ExportNotFoundError as exc:
        raise HTTPException(
            status_code=404,
            detail={"code": "export_not_found", "message": "Export not found.", "export_id": export_id},
        ) from exc


@router.get("/{export_id}/download")
def download_export(
    export_id: str,
    service: ExportService = Depends(get_export_service),
) -> FileResponse:
    try:
        record = service.get_export(export_id)
    except ExportNotFoundError as exc:
        raise HTTPException(
            status_code=404,
            detail={"code": "export_not_found", "message": "Export not found.", "export_id": export_id},
        ) from exc
    return FileResponse(record.path, filename=Path(record.path).name)


def _create_export_record(
    service: ExportService,
    export_format: str,
    request: ExportRequest,
) -> ExportRecord:
    try:
        return service.create_export(export_format, request)
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
