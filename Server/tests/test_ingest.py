from __future__ import annotations

import copy
import json
from pathlib import Path

from fastapi.testclient import TestClient

from app.factory import create_app
from app.storage import SQLiteTelemetryStore


ROOT = Path(__file__).resolve().parents[2]
SAMPLE = ROOT / "Assets" / "GenreAgnosticTelemetry" / "Samples" / "ClickerGame" / "TelemetryOutput" / "clicker_session_20260517_162250_3b73320d3eae4b4bb8b900f4af9f2049.json"


def make_client(tmp_path: Path) -> tuple[TestClient, SQLiteTelemetryStore]:
    store = SQLiteTelemetryStore(tmp_path)
    return TestClient(create_app(store=store)), store


def load_sample(session_id: str | None = None) -> dict:
    payload = json.loads(SAMPLE.read_text(encoding="utf-8-sig"))
    if session_id is not None:
        payload = copy.deepcopy(payload)
        payload["session"]["session_id"] = session_id
    return payload


def test_health_endpoint(tmp_path: Path) -> None:
    client, _ = make_client(tmp_path)

    response = client.get("/health")

    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


def test_ingest_writes_sqlite_metadata_and_raw_payload(tmp_path: Path) -> None:
    client, store = make_client(tmp_path)
    payload = load_sample()

    response = client.post("/telemetry/sessions", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["accepted"] is True
    assert body["session_id"] == payload["session"]["session_id"]
    assert body["event_count"] == len(payload["session"]["events"])
    assert (tmp_path / "telemetry.sqlite3").exists()
    assert len(body["exports"]) == 1
    assert body["exports"][0]["format"] == "netcdf"
    assert body["exports"][0]["session_ids"] == [body["session_id"]]
    assert body["exports"][0]["path"].endswith(f"{body['session_id']}.nc")
    assert Path(body["exports"][0]["path"]).exists()

    record = store.get_session_record(body["session_id"])
    assert record is not None
    assert record.warning_count == len(body["warnings"])
    assert Path(record.raw_path).exists()

    exports = store.list_exports()
    assert len(exports) == 1
    assert exports[0].export_id == body["exports"][0]["export_id"]


def test_duplicate_ingest_replaces_existing_session_record(tmp_path: Path) -> None:
    client, store = make_client(tmp_path)
    payload = load_sample(session_id="duplicate-session")

    first_response = client.post("/telemetry/sessions", json=payload)
    assert first_response.status_code == 200
    first_export = first_response.json()["exports"][0]
    first_export_path = Path(first_export["path"])
    assert first_export_path.exists()

    replacement = copy.deepcopy(payload)
    replacement["session"]["events"] = replacement["session"]["events"][:1]
    replacement_response = client.post("/telemetry/sessions", json=replacement)

    assert replacement_response.status_code == 200
    records = store.list_sessions()
    assert len(records) == 1
    assert records[0].session_id == "duplicate-session"
    assert records[0].event_count == 1
    assert store.get_raw_payload("duplicate-session")["session"]["events"] == replacement["session"]["events"]

    replacement_export = replacement_response.json()["exports"][0]
    assert replacement_export["export_id"] == first_export["export_id"]
    assert replacement_export["path"] == first_export["path"]
    assert first_export_path.exists()
    assert len(store.list_exports()) == 1


def test_list_get_raw_and_json_export(tmp_path: Path) -> None:
    client, _ = make_client(tmp_path)
    payload = load_sample()
    response = client.post("/telemetry/sessions", json=payload)
    assert response.status_code == 200
    body = response.json()

    list_response = client.get("/telemetry/sessions")
    assert list_response.status_code == 200
    assert [record["session_id"] for record in list_response.json()] == [body["session_id"]]

    metadata_response = client.get(f"/telemetry/sessions/{body['session_id']}")
    assert metadata_response.status_code == 200
    assert metadata_response.json()["session_id"] == body["session_id"]

    raw_response = client.get(f"/telemetry/sessions/{body['session_id']}/raw")
    assert raw_response.status_code == 200
    assert raw_response.json()["session"]["session_id"] == body["session_id"]

    export_response = client.get(f"/telemetry/sessions/{body['session_id']}/export/json")
    assert export_response.status_code == 200
    assert export_response.json()["session"]["session_id"] == body["session_id"]


def test_multi_session_json_export_record_flow(tmp_path: Path) -> None:
    client, _ = make_client(tmp_path)
    first = load_sample(session_id="session-one")
    second = load_sample(session_id="session-two")
    assert client.post("/telemetry/sessions", json=first).status_code == 200
    assert client.post("/telemetry/sessions", json=second).status_code == 200

    create_response = client.post(
        "/exports/json",
        json={"session_ids": ["session-one", "session-two"], "filename": "combined.json"},
    )

    assert create_response.status_code == 200
    export_record = create_response.json()
    assert export_record["format"] == "json"
    assert export_record["session_ids"] == ["session-one", "session-two"]
    assert export_record["download_url"] == f"/exports/{export_record['export_id']}/download"
    assert Path(export_record["path"]).exists()

    list_response = client.get("/exports")
    assert list_response.status_code == 200
    listed_exports = list_response.json()
    assert export_record["export_id"] in [record["export_id"] for record in listed_exports]
    assert len([record for record in listed_exports if record["format"] == "netcdf"]) == 2

    get_response = client.get(f"/exports/{export_record['export_id']}")
    assert get_response.status_code == 200
    assert get_response.json()["export_id"] == export_record["export_id"]

    download_response = client.get(export_record["download_url"])
    assert download_response.status_code == 200
    body = download_response.json()
    assert body["payload_type"] == "telemetry_session_export"
    assert body["session_count"] == 2


def test_missing_session_returns_404(tmp_path: Path) -> None:
    client, _ = make_client(tmp_path)

    response = client.get("/telemetry/sessions/missing/raw")

    assert response.status_code == 404
    assert response.json()["detail"]["code"] == "session_not_found"


def test_export_missing_session_returns_404(tmp_path: Path) -> None:
    client, _ = make_client(tmp_path)

    response = client.post("/exports/json", json={"session_ids": ["missing"]})

    assert response.status_code == 404
    assert response.json()["detail"]["code"] == "session_not_found"


def test_invalid_export_filename_returns_400(tmp_path: Path) -> None:
    client, _ = make_client(tmp_path)
    payload = load_sample(session_id="filename-test")
    assert client.post("/telemetry/sessions", json=payload).status_code == 200

    response = client.post(
        "/exports/json",
        json={"session_ids": ["filename-test"], "filename": "../escape.json"},
    )

    assert response.status_code == 400
    assert response.json()["detail"]["code"] == "invalid_export_filename"


def test_invalid_export_request_returns_structured_422(tmp_path: Path) -> None:
    client, _ = make_client(tmp_path)

    response = client.post("/exports/json", json={"session_ids": []})

    assert response.status_code == 422
    assert response.json()["detail"]["code"] == "invalid_request"


def test_missing_netcdf_dependency_returns_503_only_for_netcdf(tmp_path: Path, monkeypatch) -> None:
    client, _ = make_client(tmp_path)
    payload = load_sample(session_id="netcdf-dependency")
    assert client.post("/telemetry/sessions", json=payload).status_code == 200

    def fail_netcdf_export(*args, **kwargs):
        raise RuntimeError("NetCDF export requires the netCDF4 Python package.")

    monkeypatch.setattr("app.services.exports.write_netcdf_export", fail_netcdf_export)

    json_response = client.post(
        "/exports/json",
        json={"session_ids": ["netcdf-dependency"], "filename": "still-works.json"},
    )
    assert json_response.status_code == 200

    netcdf_response = client.post(
        "/exports/netcdf",
        json={"session_ids": ["netcdf-dependency"], "filename": "missing.nc"},
    )
    assert netcdf_response.status_code == 503
    assert netcdf_response.json()["detail"]["code"] == "export_unavailable"


def test_ingest_accepts_session_when_auto_netcdf_export_fails(tmp_path: Path, monkeypatch) -> None:
    client, store = make_client(tmp_path)
    payload = load_sample(session_id="auto-netcdf-failure")

    def fail_netcdf_export(*args, **kwargs):
        raise RuntimeError("NetCDF export requires the netCDF4 Python package.")

    monkeypatch.setattr("app.services.telemetry.write_netcdf_export", fail_netcdf_export)

    response = client.post("/telemetry/sessions", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["accepted"] is True
    assert body["session_id"] == "auto-netcdf-failure"
    assert body["exports"] == []
    assert any(warning["code"] == "netcdf_export_failed" for warning in body["warnings"])

    record = store.get_session_record("auto-netcdf-failure")
    assert record is not None
    assert record.warning_count == len(body["warnings"])
    assert Path(record.raw_path).exists()
    assert store.list_exports() == []
