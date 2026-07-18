import {
  Area,
  AreaChart,
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { TelemetryPerformanceSample } from "../../api/types";
import { formatBytes, formatNumber, megabytes } from "../../lib/format";
import { ChartFrame, tooltipStyle } from "./ChartFrame";
import { EmptyState } from "../States";

const axisProps = {
  stroke: "#3a4651",
  tick: { fill: "#6b7785", fontSize: 11, fontFamily: '"IBM Plex Mono", monospace' },
  tickLine: false,
} as const;

function timeLabel(v: number) {
  return `${v.toFixed(1)}s`;
}

export function PerformanceCharts({
  samples,
}: {
  samples: TelemetryPerformanceSample[];
}) {
  if (!samples.length) {
    return (
      <EmptyState
        title="No performance samples"
        hint="This session did not record FPS / frame-time / memory telemetry."
      />
    );
  }

  const fpsData = samples.map((s) => ({
    t: s.timestamp_seconds,
    fps: s.fps ?? null,
    frame: s.frame_time_ms ?? null,
  }));

  const memData = samples.map((s) => ({
    t: s.timestamp_seconds,
    allocated: megabytes(s.total_allocated_memory_bytes),
    reserved: megabytes(s.total_reserved_memory_bytes),
    mono: megabytes(s.mono_used_memory_bytes),
  }));

  const hasFps = samples.some((s) => s.fps != null);
  const hasFrame = samples.some((s) => s.frame_time_ms != null);
  const hasMem = samples.some(
    (s) =>
      s.total_allocated_memory_bytes != null ||
      s.total_reserved_memory_bytes != null ||
      s.mono_used_memory_bytes != null,
  );

  const avgFps =
    fpsData.filter((d) => d.fps != null).reduce((a, d) => a + (d.fps ?? 0), 0) /
    Math.max(1, fpsData.filter((d) => d.fps != null).length);

  return (
    <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
      {hasFps && (
        <ChartFrame
          title="Frame rate"
          subtitle="frames per second over session time"
          right={
            <span className="font-mono text-xs text-muted">
              avg <span className="text-signal">{formatNumber(avgFps, 1)}</span> fps
            </span>
          }
        >
          <ResponsiveContainer width="100%" height={220}>
            <AreaChart data={fpsData} margin={{ top: 6, right: 12, left: -8, bottom: 0 }}>
              <defs>
                <linearGradient id="fpsFill" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#a3e635" stopOpacity={0.35} />
                  <stop offset="100%" stopColor="#a3e635" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid stroke="#1e2730" vertical={false} />
              <XAxis dataKey="t" tickFormatter={timeLabel} {...axisProps} />
              <YAxis {...axisProps} width={40} />
              <Tooltip
                {...tooltipStyle}
                labelFormatter={(v) => `t = ${timeLabel(Number(v))}`}
                formatter={(value) => [formatNumber(Number(value), 1), "fps"]}
              />
              <Area
                type="monotone"
                dataKey="fps"
                stroke="#a3e635"
                strokeWidth={1.8}
                fill="url(#fpsFill)"
                dot={false}
                isAnimationActive={false}
                connectNulls
              />
            </AreaChart>
          </ResponsiveContainer>
        </ChartFrame>
      )}

      {hasFrame && (
        <ChartFrame title="Frame time" subtitle="milliseconds per frame">
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={fpsData} margin={{ top: 6, right: 12, left: -8, bottom: 0 }}>
              <CartesianGrid stroke="#1e2730" vertical={false} />
              <XAxis dataKey="t" tickFormatter={timeLabel} {...axisProps} />
              <YAxis {...axisProps} width={40} unit="ms" />
              <Tooltip
                {...tooltipStyle}
                labelFormatter={(v) => `t = ${timeLabel(Number(v))}`}
                formatter={(value) => [`${formatNumber(Number(value), 2)} ms`, "frame time"]}
              />
              <Line
                type="monotone"
                dataKey="frame"
                stroke="#22d3ee"
                strokeWidth={1.8}
                dot={false}
                isAnimationActive={false}
                connectNulls
              />
            </LineChart>
          </ResponsiveContainer>
        </ChartFrame>
      )}

      {hasMem && (
        <ChartFrame
          title="Memory"
          subtitle="allocated · reserved · mono (MB)"
          right={<MemLegend />}
        >
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={memData} margin={{ top: 6, right: 12, left: -8, bottom: 0 }}>
              <CartesianGrid stroke="#1e2730" vertical={false} />
              <XAxis dataKey="t" tickFormatter={timeLabel} {...axisProps} />
              <YAxis {...axisProps} width={44} unit="M" />
              <Tooltip
                {...tooltipStyle}
                labelFormatter={(v) => `t = ${timeLabel(Number(v))}`}
                formatter={(value, name) => [
                  value == null ? "—" : `${formatNumber(Number(value), 1)} MB`,
                  String(name),
                ]}
              />
              <Line type="monotone" dataKey="reserved" name="reserved" stroke="#a78bfa" strokeWidth={1.6} dot={false} isAnimationActive={false} connectNulls />
              <Line type="monotone" dataKey="allocated" name="allocated" stroke="#fbbf24" strokeWidth={1.6} dot={false} isAnimationActive={false} connectNulls />
              <Line type="monotone" dataKey="mono" name="mono" stroke="#34d399" strokeWidth={1.6} dot={false} isAnimationActive={false} connectNulls />
            </LineChart>
          </ResponsiveContainer>
          <MemPeak samples={samples} />
        </ChartFrame>
      )}
    </div>
  );
}

function MemLegend() {
  const items = [
    { label: "reserved", color: "#a78bfa" },
    { label: "allocated", color: "#fbbf24" },
    { label: "mono", color: "#34d399" },
  ];
  return (
    <div className="flex items-center gap-3">
      {items.map((it) => (
        <span key={it.label} className="flex items-center gap-1.5 font-mono text-[11px] text-muted">
          <span className="h-2 w-2 rounded-full" style={{ background: it.color }} />
          {it.label}
        </span>
      ))}
    </div>
  );
}

function MemPeak({ samples }: { samples: TelemetryPerformanceSample[] }) {
  const peak = samples.reduce(
    (max, s) => Math.max(max, s.total_reserved_memory_bytes ?? 0),
    0,
  );
  if (!peak) return null;
  return (
    <p className="mt-2 font-mono text-[11px] text-muted">
      peak reserved <span className="text-violet">{formatBytes(peak)}</span>
    </p>
  );
}
