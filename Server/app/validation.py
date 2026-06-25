from __future__ import annotations

import math
import re

from .models import TelemetrySessionEnvelope, ValidationMessage, parse_utc


SNAKE_CASE = re.compile(r"^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$")


def validate_envelope(envelope: TelemetrySessionEnvelope) -> list[ValidationMessage]:
    warnings: list[ValidationMessage] = []
    session = envelope.session

    _required(warnings, session.session_id, "session.session_id", "Session id is required.")
    _required(warnings, session.game_id, "session.game_id", "Game id is required.")
    _utc(warnings, envelope.generated_at_utc, "generated_at_utc")
    _utc(warnings, session.started_at_utc, "session.started_at_utc")
    _utc(warnings, session.ended_at_utc, "session.ended_at_utc")

    for index, item in enumerate(session.metadata):
        _property(warnings, item.key, item.type, f"session.metadata[{index}]")

    for index, event in enumerate(session.events):
        path = f"session.events[{index}]"
        _required(warnings, event.event_type, f"{path}.event_type", "Event type is required.")
        _required(warnings, event.event_id, f"{path}.event_id", "Event id is required.")
        _required(warnings, event.category, f"{path}.category", "Event category is required.")
        _required(warnings, event.name, f"{path}.name", "Event name is required.")
        _snake_case(warnings, event.event_type, f"{path}.event_type")
        _snake_case(warnings, event.category, f"{path}.category")
        _snake_case(warnings, event.name, f"{path}.name")
        _non_negative_finite(warnings, event.timestamp_seconds, f"{path}.timestamp_seconds")
        if event.frame_index < 0:
            _warning(warnings, "negative_frame_index", f"{path}.frame_index", "Frame index must not be negative.")
        if event.has_position and event.position is None:
            _warning(warnings, "position_required", f"{path}.position", "Position is required when has_position is true.")
        for property_index, item in enumerate(event.properties):
            _property(warnings, item.key, item.type, f"{path}.properties[{property_index}]")

    for index, state in enumerate(session.states):
        path = f"session.states[{index}]"
        _required(warnings, state.name, f"{path}.name", "State sample name is required.")
        _snake_case(warnings, state.name, f"{path}.name")
        _non_negative_finite(warnings, state.timestamp_seconds, f"{path}.timestamp_seconds")
        for property_index, item in enumerate(state.properties):
            _property(warnings, item.key, item.type, f"{path}.properties[{property_index}]")

    for index, sample in enumerate(session.performance_samples):
        _non_negative_finite(
            warnings,
            sample.timestamp_seconds,
            f"session.performance_samples[{index}].timestamp_seconds",
        )

    return warnings


def _required(warnings: list[ValidationMessage], value: str | None, path: str, message: str) -> None:
    if value is None or not value.strip():
        _warning(warnings, "required_field", path, message)


def _snake_case(warnings: list[ValidationMessage], value: str | None, path: str) -> None:
    if value and not SNAKE_CASE.match(value):
        _warning(warnings, "snake_case", path, f"{path} should use snake_case.")


def _utc(warnings: list[ValidationMessage], value: str | None, path: str) -> None:
    if not value:
        return
    try:
        parse_utc(value)
    except ValueError:
        _warning(warnings, "invalid_utc", path, f"{path} should be an ISO-8601 UTC timestamp.")


def _non_negative_finite(warnings: list[ValidationMessage], value: float, path: str) -> None:
    if value < 0:
        _warning(warnings, "negative_timestamp", path, f"{path} must not be negative.")
    if not math.isfinite(value):
        _warning(warnings, "invalid_number", path, f"{path} must be finite.")


def _property(warnings: list[ValidationMessage], key: str | None, property_type: str | None, path: str) -> None:
    _required(warnings, key, f"{path}.key", "Property key is required.")
    _snake_case(warnings, key, f"{path}.key")
    if property_type not in {"string", "number", "bool", "integer", "enum"}:
        _warning(warnings, "unsupported_property_type", f"{path}.type", "Property type is unsupported.")


def _warning(warnings: list[ValidationMessage], code: str, path: str, message: str) -> None:
    warnings.append(ValidationMessage(code=code, path=path, message=message))

