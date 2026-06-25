from datetime import datetime
from typing import Any

from pydantic import BaseModel, Field


class TelemetryEvent(BaseModel):
    name: str
    timestamp: datetime
    properties: dict[str, Any] = Field(default_factory=dict)


class TelemetrySession(BaseModel):
    session_id: str
    started_at: datetime
    events: list[TelemetryEvent] = Field(default_factory=list)
