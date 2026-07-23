import type { TelemetryProperty } from "../api/types";

/** Resolve the display value of a TelemetryProperty regardless of its declared type. */
export function propertyValue(p: TelemetryProperty): string {
  if (p.string_value !== null && p.string_value !== undefined) return p.string_value;
  if (p.number_value !== null && p.number_value !== undefined)
    return formatNumber(p.number_value);
  if (p.bool_value !== null && p.bool_value !== undefined)
    return p.bool_value ? "true" : "false";
  return "—";
}

export function formatNumber(n: number, maxFractionDigits = 3): string {
  if (!Number.isFinite(n)) return "—";
  return n.toLocaleString(undefined, { maximumFractionDigits: maxFractionDigits });
}

export function formatBytes(bytes?: number | null): string {
  if (bytes === null || bytes === undefined) return "—";
  if (bytes === 0) return "0 B";
  const units = ["B", "KB", "MB", "GB", "TB"];
  const i = Math.min(
    units.length - 1,
    Math.floor(Math.log(Math.abs(bytes)) / Math.log(1024)),
  );
  const value = bytes / Math.pow(1024, i);
  return `${value.toFixed(value >= 100 || i === 0 ? 0 : 1)} ${units[i]}`;
}

export function megabytes(bytes?: number | null): number | null {
  if (bytes === null || bytes === undefined) return null;
  return bytes / (1024 * 1024);
}

/** seconds (float) -> "m:ss.mmm" or "s.mmm" */
export function formatTimestamp(seconds: number): string {
  if (!Number.isFinite(seconds)) return "—";
  const m = Math.floor(seconds / 60);
  const s = seconds - m * 60;
  if (m > 0) return `${m}:${s.toFixed(3).padStart(6, "0")}`;
  return `${s.toFixed(3)}s`;
}

/** Duration between two ISO timestamps as a compact human string. */
export function formatDuration(
  start?: string | null,
  end?: string | null,
): string {
  if (!start || !end) return "—";
  const ms = new Date(end).getTime() - new Date(start).getTime();
  if (!Number.isFinite(ms) || ms < 0) return "—";
  const totalSec = ms / 1000;
  if (totalSec < 60) return `${totalSec.toFixed(1)}s`;
  const m = Math.floor(totalSec / 60);
  const s = Math.round(totalSec - m * 60);
  return `${m}m ${s.toString().padStart(2, "0")}s`;
}

export function formatDateTime(iso?: string | null): string {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

export function relativeTime(iso?: string | null): string {
  if (!iso) return "—";
  const d = new Date(iso);
  const diff = Date.now() - d.getTime();
  if (!Number.isFinite(diff)) return "—";
  const sec = Math.round(diff / 1000);
  if (sec < 60) return `${sec}s ago`;
  const min = Math.round(sec / 60);
  if (min < 60) return `${min}m ago`;
  const hr = Math.round(min / 60);
  if (hr < 24) return `${hr}h ago`;
  const day = Math.round(hr / 24);
  return `${day}d ago`;
}

export function shortId(id: string, head = 8): string {
  if (id.length <= head + 3) return id;
  return `${id.slice(0, head)}…`;
}

/** Stable color for an event category (instrument palette). */
const CATEGORY_COLORS = [
  "#a3e635", // signal lime
  "#22d3ee", // cyan
  "#a78bfa", // violet
  "#fbbf24", // amber
  "#fb7185", // rose
  "#34d399", // emerald
  "#f472b6", // pink
  "#60a5fa", // blue
];

export function categoryColor(category: string): string {
  let hash = 0;
  for (let i = 0; i < category.length; i++) {
    hash = (hash * 31 + category.charCodeAt(i)) >>> 0;
  }
  return CATEGORY_COLORS[hash % CATEGORY_COLORS.length];
}
