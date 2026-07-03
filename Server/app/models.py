from __future__ import annotations

from datetime import datetime, timezone
from typing import Any, Literal
from uuid import uuid4

from pydantic import BaseModel, ConfigDict, Field, field_validator


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def parse_utc(value: str) -> datetime:
    normalized = value.replace("Z", "+00:00")
    parsed = datetime.fromisoformat(normalized)
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed.astimezone(timezone.utc)


class TelemetryProperty(BaseModel):
    model_config = ConfigDict(extra="allow")

    key: str
    type: str
    string_value: str | None = None
    number_value: float | None = None
    bool_value: bool | None = None


class TelemetryPosition(BaseModel):
    model_config = ConfigDict(extra="allow")

    x: float
    y: float
    z: float


class TelemetryEvent(BaseModel):
    model_config = ConfigDict(extra="allow")

    event_type: str
    event_id: str
    timestamp_seconds: float
    frame_index: int
    category: str
    name: str
    object_id: str | None = None
    object_type: str | None = None
    has_position: bool = False
    position: TelemetryPosition | None = None
    has_success: bool = False
    success: bool = False
    properties: list[TelemetryProperty] = Field(default_factory=list)


class TelemetryStateSample(BaseModel):
    model_config = ConfigDict(extra="allow")

    timestamp_seconds: float
    name: str
    object_id: str | None = None
    properties: list[TelemetryProperty] = Field(default_factory=list)


class TelemetryPerformanceSample(BaseModel):
    model_config = ConfigDict(extra="allow")

    timestamp_seconds: float
    fps: float | None = None
    frame_time_ms: float | None = None
    total_allocated_memory_bytes: int | None = None
    total_reserved_memory_bytes: int | None = None
    mono_used_memory_bytes: int | None = None


class TelemetrySession(BaseModel):
    model_config = ConfigDict(extra="allow")

    session_id: str
    game_id: str
    scene_name: str | None = None
    unity_version: str | None = None
    tool_version: str | None = None
    started_at_utc: str | None = None
    ended_at_utc: str | None = None
    metadata: list[TelemetryProperty] = Field(default_factory=list)
    events: list[TelemetryEvent] = Field(default_factory=list)
    states: list[TelemetryStateSample] = Field(default_factory=list)
    performance_samples: list[TelemetryPerformanceSample] = Field(default_factory=list)


class TelemetrySessionEnvelope(BaseModel):
    model_config = ConfigDict(extra="allow")

    schema_version: str
    payload_type: Literal["telemetry_session"]
    generated_at_utc: str
    session: TelemetrySession

    @field_validator("generated_at_utc")
    @classmethod
    def generated_at_must_be_iso8601(cls, value: str) -> str:
        parse_utc(value)
        return value


class ValidationMessage(BaseModel):
    severity: Literal["info", "warning", "error"] = "warning"
    code: str
    path: str
    message: str


class SessionIndexRecord(BaseModel):
    session_id: str
    game_id: str
    scene_name: str | None = None
    schema_version: str
    generated_at_utc: str
    started_at_utc: str | None = None
    ended_at_utc: str | None = None
    event_count: int
    state_count: int
    performance_sample_count: int
    warning_count: int = 0
    stored_at_utc: str
    raw_path: str


class ExportRecord(BaseModel):
    export_id: str = Field(default_factory=lambda: uuid4().hex)
    format: Literal["json", "netcdf"]
    session_ids: list[str]
    path: str
    created_at_utc: str = Field(default_factory=utc_now_iso)
    download_url: str


class IngestResponse(BaseModel):
    accepted: bool
    session_id: str
    schema_version: str
    event_count: int
    state_count: int
    performance_sample_count: int
    warnings: list[ValidationMessage] = Field(default_factory=list)
    exports: list[ExportRecord] = Field(default_factory=list)


class ExportRequest(BaseModel):
    session_ids: list[str] = Field(min_length=1)
    filename: str | None = None
