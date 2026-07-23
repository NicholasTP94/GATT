import json

from fastapi import FastAPI
from fastapi.exceptions import RequestValidationError
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

from .api import api_router
from .config import settings
from .storage import SQLiteTelemetryStore


def create_app(store: SQLiteTelemetryStore | None = None) -> FastAPI:
    app = FastAPI(title=settings.app_name, version="0.1.0")
    app.state.store = store or SQLiteTelemetryStore(settings.data_dir, settings.db_path)
    app.add_middleware(
        CORSMiddleware,
        allow_origins=settings.cors_origins,
        allow_methods=["*"],
        allow_headers=["*"],
    )
    app.add_exception_handler(RequestValidationError, request_validation_exception_handler)
    app.include_router(api_router)
    return app


async def request_validation_exception_handler(_request, exc: RequestValidationError) -> JSONResponse:
    errors = json.loads(json.dumps(exc.errors(), default=str))
    return JSONResponse(
        status_code=422,
        content={"detail": {"code": "invalid_request", "errors": errors}},
    )
