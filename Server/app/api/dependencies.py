from fastapi import Request

from ..services import ExportService, TelemetryService
from ..storage import SQLiteTelemetryStore


def get_store(request: Request) -> SQLiteTelemetryStore:
    return request.app.state.store


def get_telemetry_service(request: Request) -> TelemetryService:
    return TelemetryService(get_store(request))


def get_export_service(request: Request) -> ExportService:
    return ExportService(get_store(request))
