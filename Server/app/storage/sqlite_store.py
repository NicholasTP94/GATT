from __future__ import annotations

import hashlib
import json
import re
import sqlite3
from pathlib import Path
from typing import Any

from ..models import ExportRecord, SessionIndexRecord, TelemetrySessionEnvelope, utc_now_iso


SAFE_FILE_STEM = re.compile(r"^[A-Za-z0-9._-]+$")


class InvalidFilenameError(ValueError):
    pass


class SQLiteTelemetryStore:
    def __init__(self, data_dir: Path, db_path: Path | None = None) -> None:
        self.data_dir = data_dir
        self.db_path = db_path or data_dir / "telemetry.sqlite3"
        self.sessions_dir = data_dir / "sessions"
        self.exports_dir = data_dir / "exports"
        self.sessions_dir.mkdir(parents=True, exist_ok=True)
        self.exports_dir.mkdir(parents=True, exist_ok=True)
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        self._initialize()

    def save_session(
        self,
        envelope: TelemetrySessionEnvelope,
        raw_payload: dict[str, Any],
        warning_count: int = 0,
    ) -> SessionIndexRecord:
        session = envelope.session
        raw_path = self.sessions_dir / f"{_safe_session_stem(session.session_id)}.json"
        _write_json_atomic(raw_path, raw_payload)

        record = SessionIndexRecord(
            session_id=session.session_id,
            game_id=session.game_id,
            scene_name=session.scene_name,
            schema_version=envelope.schema_version,
            generated_at_utc=envelope.generated_at_utc,
            started_at_utc=session.started_at_utc,
            ended_at_utc=session.ended_at_utc,
            event_count=len(session.events),
            state_count=len(session.states),
            performance_sample_count=len(session.performance_samples),
            warning_count=warning_count,
            stored_at_utc=utc_now_iso(),
            raw_path=str(raw_path),
        )

        with self._connect() as connection:
            connection.execute(
                """
                INSERT INTO sessions (
                    session_id,
                    game_id,
                    scene_name,
                    schema_version,
                    generated_at_utc,
                    started_at_utc,
                    ended_at_utc,
                    event_count,
                    state_count,
                    performance_sample_count,
                    warning_count,
                    stored_at_utc,
                    raw_path
                )
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT(session_id) DO UPDATE SET
                    game_id = excluded.game_id,
                    scene_name = excluded.scene_name,
                    schema_version = excluded.schema_version,
                    generated_at_utc = excluded.generated_at_utc,
                    started_at_utc = excluded.started_at_utc,
                    ended_at_utc = excluded.ended_at_utc,
                    event_count = excluded.event_count,
                    state_count = excluded.state_count,
                    performance_sample_count = excluded.performance_sample_count,
                    warning_count = excluded.warning_count,
                    stored_at_utc = excluded.stored_at_utc,
                    raw_path = excluded.raw_path
                """,
                (
                    record.session_id,
                    record.game_id,
                    record.scene_name,
                    record.schema_version,
                    record.generated_at_utc,
                    record.started_at_utc,
                    record.ended_at_utc,
                    record.event_count,
                    record.state_count,
                    record.performance_sample_count,
                    record.warning_count,
                    record.stored_at_utc,
                    record.raw_path,
                ),
            )

        return record

    def list_sessions(self) -> list[SessionIndexRecord]:
        with self._connect() as connection:
            rows = connection.execute(
                "SELECT * FROM sessions ORDER BY stored_at_utc ASC, session_id ASC"
            ).fetchall()
        return [_session_record_from_row(row) for row in rows]

    def get_session_record(self, session_id: str) -> SessionIndexRecord | None:
        with self._connect() as connection:
            row = connection.execute(
                "SELECT * FROM sessions WHERE session_id = ?",
                (session_id,),
            ).fetchone()
        if row is None:
            return None
        return _session_record_from_row(row)

    def get_raw_payload(self, session_id: str) -> dict[str, Any]:
        record = self.get_session_record(session_id)
        if record is None:
            raise KeyError(session_id)
        path = Path(record.raw_path)
        return json.loads(path.read_text(encoding="utf-8"))

    def save_export_record(self, record: ExportRecord) -> ExportRecord:
        with self._connect() as connection:
            connection.execute(
                """
                INSERT INTO exports (
                    export_id,
                    format,
                    session_ids_json,
                    path,
                    created_at_utc,
                    download_url
                )
                VALUES (?, ?, ?, ?, ?, ?)
                ON CONFLICT(export_id) DO UPDATE SET
                    format = excluded.format,
                    session_ids_json = excluded.session_ids_json,
                    path = excluded.path,
                    created_at_utc = excluded.created_at_utc,
                    download_url = excluded.download_url
                """,
                (
                    record.export_id,
                    record.format,
                    json.dumps(record.session_ids),
                    record.path,
                    record.created_at_utc,
                    record.download_url,
                ),
            )
        return record

    def update_session_warning_count(self, session_id: str, warning_count: int) -> None:
        with self._connect() as connection:
            connection.execute(
                "UPDATE sessions SET warning_count = ? WHERE session_id = ?",
                (warning_count, session_id),
            )

    def list_exports(self) -> list[ExportRecord]:
        with self._connect() as connection:
            rows = connection.execute(
                "SELECT * FROM exports ORDER BY created_at_utc ASC, export_id ASC"
            ).fetchall()
        return [_export_record_from_row(row) for row in rows]

    def get_export_record(self, export_id: str) -> ExportRecord | None:
        with self._connect() as connection:
            row = connection.execute(
                "SELECT * FROM exports WHERE export_id = ?",
                (export_id,),
            ).fetchone()
        if row is None:
            return None
        return _export_record_from_row(row)

    def export_path(self, filename: str) -> Path:
        safe_filename = _validate_export_filename(filename)
        return self.exports_dir / safe_filename

    def _connect(self) -> sqlite3.Connection:
        connection = sqlite3.connect(self.db_path)
        connection.row_factory = sqlite3.Row
        return connection

    def _initialize(self) -> None:
        with self._connect() as connection:
            connection.execute("PRAGMA journal_mode = WAL")
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS sessions (
                    session_id TEXT PRIMARY KEY,
                    game_id TEXT NOT NULL,
                    scene_name TEXT,
                    schema_version TEXT NOT NULL,
                    generated_at_utc TEXT NOT NULL,
                    started_at_utc TEXT,
                    ended_at_utc TEXT,
                    event_count INTEGER NOT NULL,
                    state_count INTEGER NOT NULL,
                    performance_sample_count INTEGER NOT NULL,
                    warning_count INTEGER NOT NULL DEFAULT 0,
                    stored_at_utc TEXT NOT NULL,
                    raw_path TEXT NOT NULL
                )
                """
            )
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS exports (
                    export_id TEXT PRIMARY KEY,
                    format TEXT NOT NULL,
                    session_ids_json TEXT NOT NULL,
                    path TEXT NOT NULL,
                    created_at_utc TEXT NOT NULL,
                    download_url TEXT NOT NULL
                )
                """
            )


def _safe_session_stem(session_id: str) -> str:
    if session_id and session_id not in {".", ".."} and SAFE_FILE_STEM.match(session_id):
        return session_id
    digest = hashlib.sha256(session_id.encode("utf-8")).hexdigest()
    return f"session_{digest}"


def _validate_export_filename(filename: str) -> str:
    clean = filename.strip()
    if not clean:
        raise InvalidFilenameError("Export filename is required.")
    if clean in {".", ".."} or Path(clean).name != clean:
        raise InvalidFilenameError("Export filename must not contain directories.")
    if "/" in clean or "\\" in clean or "\x00" in clean:
        raise InvalidFilenameError("Export filename contains invalid characters.")
    return clean


def _write_json_atomic(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp_path = path.with_name(f"{path.name}.tmp")
    temp_path.write_text(json.dumps(payload, indent=2, sort_keys=True), encoding="utf-8")
    temp_path.replace(path)


def _session_record_from_row(row: sqlite3.Row) -> SessionIndexRecord:
    return SessionIndexRecord(
        session_id=row["session_id"],
        game_id=row["game_id"],
        scene_name=row["scene_name"],
        schema_version=row["schema_version"],
        generated_at_utc=row["generated_at_utc"],
        started_at_utc=row["started_at_utc"],
        ended_at_utc=row["ended_at_utc"],
        event_count=row["event_count"],
        state_count=row["state_count"],
        performance_sample_count=row["performance_sample_count"],
        warning_count=row["warning_count"],
        stored_at_utc=row["stored_at_utc"],
        raw_path=row["raw_path"],
    )


def _export_record_from_row(row: sqlite3.Row) -> ExportRecord:
    return ExportRecord(
        export_id=row["export_id"],
        format=row["format"],
        session_ids=json.loads(row["session_ids_json"]),
        path=row["path"],
        created_at_utc=row["created_at_utc"],
        download_url=row["download_url"],
    )
