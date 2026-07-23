import os
from pathlib import Path

from pydantic import BaseModel, Field


def default_data_dir() -> Path:
    return Path(os.getenv("GATT_SERVER_DATA_DIR", Path(__file__).resolve().parents[1] / "data"))


def default_db_path() -> Path:
    return Path(os.getenv("GATT_SERVER_DB_PATH", default_data_dir() / "telemetry.sqlite3"))


def default_cors_origins() -> list[str]:
    raw = os.getenv("GATT_SERVER_CORS_ORIGINS")
    if raw:
        return [origin.strip() for origin in raw.split(",") if origin.strip()]
    return [
        "http://localhost:5173",
        "http://127.0.0.1:5173",
    ]


class Settings(BaseModel):
    app_name: str = "Genre Agnostic Telemetry Server"
    data_dir: Path = Field(default_factory=default_data_dir)
    db_path: Path = Field(default_factory=default_db_path)
    cors_origins: list[str] = Field(default_factory=default_cors_origins)


settings = Settings()
