// TypeScript mirror of the Pydantic models in Server/app/models.py.
// Kept intentionally close to the server shapes so the API client needs no mapping.

export interface TelemetryProperty {
  key: string;
  type: string; // "string" | "number" | "bool" | "integer" | "enum"
  string_value?: string | null;
  number_value?: number | null;
  bool_value?: boolean | null;
  // server allows extra fields (ConfigDict extra="allow")
  [extra: string]: unknown;
}

export interface TelemetryPosition {
  x: number;
  y: number;
  z: number;
}

export interface TelemetryEvent {
  event_type: string;
  event_id: string;
  timestamp_seconds: number;
  frame_index: number;
  category: string;
  name: string;
  object_id?: string | null;
  object_type?: string | null;
  has_position?: boolean;
  position?: TelemetryPosition | null;
  has_success?: boolean;
  success?: boolean;
  properties: TelemetryProperty[];
}

export interface TelemetryStateSample {
  timestamp_seconds: number;
  name: string;
  object_id?: string | null;
  properties: TelemetryProperty[];
}

export interface TelemetryPerformanceSample {
  timestamp_seconds: number;
  fps?: number | null;
  frame_time_ms?: number | null;
  total_allocated_memory_bytes?: number | null;
  total_reserved_memory_bytes?: number | null;
  mono_used_memory_bytes?: number | null;
}

export interface TelemetrySession {
  session_id: string;
  game_id: string;
  scene_name?: string | null;
  unity_version?: string | null;
  tool_version?: string | null;
  started_at_utc?: string | null;
  ended_at_utc?: string | null;
  metadata: TelemetryProperty[];
  events: TelemetryEvent[];
  states: TelemetryStateSample[];
  performance_samples: TelemetryPerformanceSample[];
}

export interface TelemetrySessionEnvelope {
  schema_version: string;
  payload_type: "telemetry_session";
  generated_at_utc: string;
  session: TelemetrySession;
}

export interface SessionIndexRecord {
  session_id: string;
  game_id: string;
  scene_name?: string | null;
  schema_version: string;
  generated_at_utc: string;
  started_at_utc?: string | null;
  ended_at_utc?: string | null;
  event_count: number;
  state_count: number;
  performance_sample_count: number;
  warning_count: number;
  stored_at_utc: string;
  raw_path: string;
}

export type ExportFormat = "json" | "netcdf";

export interface ExportRecord {
  export_id: string;
  format: ExportFormat;
  session_ids: string[];
  path: string;
  created_at_utc: string;
  download_url: string;
}
