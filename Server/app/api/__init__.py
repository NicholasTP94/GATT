from fastapi import APIRouter

from .exports import router as exports_router
from .health import router as health_router
from .sessions import router as sessions_router


api_router = APIRouter()
api_router.include_router(health_router)
api_router.include_router(sessions_router)
api_router.include_router(exports_router)

__all__ = ["api_router"]
