from __future__ import annotations

import json
import math
from pathlib import Path
from typing import Any

from ..models import utc_now_iso


BASE_EVENT_KEYS = {
    "event_type",
    "event_id",
    "timestamp_seconds",
    "frame_index",
    "category",
    "name",
    "object_id",
    "object_type",
    "has_position",
    "position",
    "has_success",
    "success",
    "properties",
}


def write_netcdf_export(payloads: list[dict[str, Any]], output_path: Path) -> Path:
    try:
        from netCDF4 import Dataset
    except ImportError as exc:
        raise RuntimeError("NetCDF export requires the netCDF4 Python package.") from exc

    output_path.parent.mkdir(parents=True, exist_ok=True)
    sessions = [_session(payload) for payload in payloads]

    with Dataset(output_path, "w", format="NETCDF4") as dataset:
        dataset.setncattr("schema_version", "1.0")
        dataset.setncattr("payload_type", "telemetry_session_export")
        dataset.setncattr("generated_at_utc", utc_now_iso())
        dataset.setncattr("exporter_name", "genre_agnostic_telemetry_fastapi")
        dataset.setncattr("exporter_version", "0.1.0")
        dataset.setncattr("session_count", len(payloads))

        _write_session_group(dataset, payloads, sessions)
        _write_metadata_group(dataset, sessions)
        _write_events_group(dataset, sessions)
        _write_event_properties_group(dataset, sessions)
        _write_states_group(dataset, sessions)
        _write_state_properties_group(dataset, sessions)
        _write_performance_group(dataset, sessions)
        _write_raw_group(dataset, payloads)

    return output_path


def _session(payload: dict[str, Any]) -> dict[str, Any]:
    return payload.get("session") or {}


def _write_session_group(dataset: Any, payloads: list[dict[str, Any]], sessions: list[dict[str, Any]]) -> None:
    group = dataset.createGroup("session")
    group.createDimension("session", len(sessions))
    _string_var(group, "schema_version", "session", [payload.get("schema_version", "") for payload in payloads])
    _string_var(group, "payload_type", "session", [payload.get("payload_type", "") for payload in payloads])
    _string_var(group, "generated_at_utc", "session", [payload.get("generated_at_utc", "") for payload in payloads])
    for key in (
        "session_id",
        "game_id",
        "scene_name",
        "unity_version",
        "tool_version",
        "started_at_utc",
        "ended_at_utc",
    ):
        _string_var(group, key, "session", [_text(session.get(key)) for session in sessions])


def _write_metadata_group(dataset: Any, sessions: list[dict[str, Any]]) -> None:
    rows: list[dict[str, Any]] = []
    for session_index, session in enumerate(sessions):
        for item in session.get("metadata") or []:
            rows.append({"session_index": session_index, **item})

    group = dataset.createGroup("metadata")
    group.createDimension("metadata_property", len(rows))
    _int_var(group, "session_index", "metadata_property", [row["session_index"] for row in rows])
    _write_property_columns(group, "metadata_property", rows)


def _write_events_group(dataset: Any, sessions: list[dict[str, Any]]) -> None:
    rows = _event_rows(sessions)
    group = dataset.createGroup("events")
    group.createDimension("event", len(rows))
    _int_var(group, "session_index", "event", [row["session_index"] for row in rows])
    _int_var(group, "event_index", "event", [row["event_index"] for row in rows])
    _float_var(group, "timestamp_seconds", "event", [_number(row.get("timestamp_seconds")) for row in rows])
    _int_var(group, "frame_index", "event", [_integer(row.get("frame_index")) for row in rows])
    for key in ("event_type", "event_id", "category", "name", "object_id", "object_type"):
        _string_var(group, key, "event", [_text(row.get(key)) for row in rows])
    _bool_var(group, "has_position", "event", [bool(row.get("has_position")) for row in rows])
    _float_var(group, "position_x", "event", [_position(row, "x") for row in rows])
    _float_var(group, "position_y", "event", [_position(row, "y") for row in rows])
    _float_var(group, "position_z", "event", [_position(row, "z") for row in rows])
    _bool_var(group, "has_success", "event", [bool(row.get("has_success")) for row in rows])
    _bool_var(group, "success", "event", [bool(row.get("success")) for row in rows])


def _write_event_properties_group(dataset: Any, sessions: list[dict[str, Any]]) -> None:
    rows: list[dict[str, Any]] = []
    for row in _event_rows(sessions):
        for item in row.get("properties") or []:
            rows.append(
                {
                    "session_index": row["session_index"],
                    "event_index": row["event_index"],
                    "source": "property_list",
                    **item,
                }
            )
        for key, value in row.items():
            if key in BASE_EVENT_KEYS or key in {"session_index", "event_index"}:
                continue
            rows.append(
                {
                    "session_index": row["session_index"],
                    "event_index": row["event_index"],
                    "source": "specialized_event_field",
                    "key": key,
                    **_property_value(value),
                }
            )

    group = dataset.createGroup("event_properties")
    group.createDimension("event_property", len(rows))
    _int_var(group, "session_index", "event_property", [row["session_index"] for row in rows])
    _int_var(group, "event_index", "event_property", [row["event_index"] for row in rows])
    _string_var(group, "source", "event_property", [row.get("source", "") for row in rows])
    _write_property_columns(group, "event_property", rows)


def _write_states_group(dataset: Any, sessions: list[dict[str, Any]]) -> None:
    rows: list[dict[str, Any]] = []
    for session_index, session in enumerate(sessions):
        for state_index, state in enumerate(session.get("states") or []):
            rows.append({"session_index": session_index, "state_index": state_index, **state})

    group = dataset.createGroup("states")
    group.createDimension("state", len(rows))
    _int_var(group, "session_index", "state", [row["session_index"] for row in rows])
    _int_var(group, "state_index", "state", [row["state_index"] for row in rows])
    _float_var(group, "timestamp_seconds", "state", [_number(row.get("timestamp_seconds")) for row in rows])
    _string_var(group, "name", "state", [_text(row.get("name")) for row in rows])
    _string_var(group, "object_id", "state", [_text(row.get("object_id")) for row in rows])


def _write_state_properties_group(dataset: Any, sessions: list[dict[str, Any]]) -> None:
    rows: list[dict[str, Any]] = []
    for session_index, session in enumerate(sessions):
        for state_index, state in enumerate(session.get("states") or []):
            for item in state.get("properties") or []:
                rows.append({"session_index": session_index, "state_index": state_index, **item})

    group = dataset.createGroup("state_properties")
    group.createDimension("state_property", len(rows))
    _int_var(group, "session_index", "state_property", [row["session_index"] for row in rows])
    _int_var(group, "state_index", "state_property", [row["state_index"] for row in rows])
    _write_property_columns(group, "state_property", rows)


def _write_performance_group(dataset: Any, sessions: list[dict[str, Any]]) -> None:
    rows: list[dict[str, Any]] = []
    for session_index, session in enumerate(sessions):
        for sample_index, sample in enumerate(session.get("performance_samples") or []):
            rows.append({"session_index": session_index, "sample_index": sample_index, **sample})

    group = dataset.createGroup("performance")
    group.createDimension("performance_sample", len(rows))
    _int_var(group, "session_index", "performance_sample", [row["session_index"] for row in rows])
    _int_var(group, "sample_index", "performance_sample", [row["sample_index"] for row in rows])
    for key in ("timestamp_seconds", "fps", "frame_time_ms"):
        _float_var(group, key, "performance_sample", [_number(row.get(key)) for row in rows])
    for key in ("total_allocated_memory_bytes", "total_reserved_memory_bytes", "mono_used_memory_bytes"):
        _int64_var(group, key, "performance_sample", [_integer(row.get(key)) for row in rows])


def _write_raw_group(dataset: Any, payloads: list[dict[str, Any]]) -> None:
    group = dataset.createGroup("raw")
    group.createDimension("session", len(payloads))
    _string_var(group, "json_payload", "session", [json.dumps(payload, sort_keys=True) for payload in payloads])


def _event_rows(sessions: list[dict[str, Any]]) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for session_index, session in enumerate(sessions):
        for event_index, event in enumerate(session.get("events") or []):
            rows.append({"session_index": session_index, "event_index": event_index, **event})
    return rows


def _write_property_columns(group: Any, dimension: str, rows: list[dict[str, Any]]) -> None:
    _string_var(group, "key", dimension, [_text(row.get("key")) for row in rows])
    _string_var(group, "type", dimension, [_text(row.get("type")) for row in rows])
    _string_var(group, "string_value", dimension, [_text(row.get("string_value")) for row in rows])
    _float_var(group, "number_value", dimension, [_number(row.get("number_value")) for row in rows])
    _bool_var(group, "bool_value", dimension, [bool(row.get("bool_value")) for row in rows])


def _property_value(value: Any) -> dict[str, Any]:
    if isinstance(value, bool):
        return {"type": "bool", "bool_value": value}
    if isinstance(value, int) and not isinstance(value, bool):
        return {"type": "integer", "number_value": value}
    if isinstance(value, float):
        return {"type": "number", "number_value": value}
    return {"type": "string", "string_value": _text(value)}


def _position(row: dict[str, Any], axis: str) -> float:
    position = row.get("position") or {}
    return _number(position.get(axis))


def _text(value: Any) -> str:
    if value is None:
        return ""
    return str(value)


def _number(value: Any) -> float:
    if value is None:
        return math.nan
    try:
        return float(value)
    except (TypeError, ValueError):
        return math.nan


def _integer(value: Any) -> int:
    if value is None:
        return -1
    try:
        return int(value)
    except (TypeError, ValueError):
        return -1


def _string_var(group: Any, name: str, dimension: str, values: list[str]) -> None:
    variable = group.createVariable(name, str, (dimension,))
    for index, value in enumerate(values):
        variable[index] = value


def _float_var(group: Any, name: str, dimension: str, values: list[float]) -> None:
    variable = group.createVariable(name, "f8", (dimension,), fill_value=math.nan)
    variable[:] = values


def _int_var(group: Any, name: str, dimension: str, values: list[int]) -> None:
    variable = group.createVariable(name, "i4", (dimension,), fill_value=-1)
    variable[:] = values


def _int64_var(group: Any, name: str, dimension: str, values: list[int]) -> None:
    variable = group.createVariable(name, "i8", (dimension,), fill_value=-1)
    variable[:] = values


def _bool_var(group: Any, name: str, dimension: str, values: list[bool]) -> None:
    variable = group.createVariable(name, "i1", (dimension,))
    variable[:] = [1 if value else 0 for value in values]
