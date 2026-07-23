from __future__ import annotations

import json
from pathlib import Path

import pytest

from app.exporters.netcdf_exporter import write_netcdf_export


ROOT = Path(__file__).resolve().parents[2]
SAMPLE = ROOT / "Assets" / "GenreAgnosticTelemetry" / "Samples" / "ClickerGame" / "TelemetryOutput" / "clicker_session_20260517_162250_3b73320d3eae4b4bb8b900f4af9f2049.json"


def test_netcdf_export_preserves_core_groups_and_specialized_fields(tmp_path: Path) -> None:
    netcdf4 = pytest.importorskip("netCDF4")
    payload = json.loads(SAMPLE.read_text(encoding="utf-8-sig"))
    output = tmp_path / "clicker.nc"

    write_netcdf_export([payload], output)

    with netcdf4.Dataset(output) as dataset:
        assert set(dataset.groups) >= {
            "session",
            "metadata",
            "events",
            "event_properties",
            "states",
            "state_properties",
            "performance",
            "raw",
        }
        assert len(dataset.groups["events"].dimensions["event"]) == len(payload["session"]["events"])

        property_group = dataset.groups["event_properties"]
        keys = [str(value) for value in property_group.variables["key"][:]]
        sources = [str(value) for value in property_group.variables["source"][:]]

        assert "score" in keys
        assert "combo_count" in keys
        assert "specialized_event_field" in sources
        assert "property_list" in sources

