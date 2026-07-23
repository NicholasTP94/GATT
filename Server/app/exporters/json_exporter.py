from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from ..models import utc_now_iso


def write_json_export(payloads: list[dict[str, Any]], output_path: Path) -> Path:
    output_path.parent.mkdir(parents=True, exist_ok=True)

    if len(payloads) == 1:
        export_payload: dict[str, Any] = payloads[0]
    else:
        export_payload = {
            "schema_version": "1.0",
            "payload_type": "telemetry_session_export",
            "generated_at_utc": utc_now_iso(),
            "session_count": len(payloads),
            "sessions": payloads,
        }

    output_path.write_text(json.dumps(export_payload, indent=2, sort_keys=True), encoding="utf-8")
    return output_path

