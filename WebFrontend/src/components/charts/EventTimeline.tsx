import { useMemo } from "react";
import {
  CartesianGrid,
  ResponsiveContainer,
  Scatter,
  ScatterChart,
  Tooltip,
  XAxis,
  YAxis,
  ZAxis,
} from "recharts";
import type { TelemetryEvent } from "../../api/types";
import { categoryColor, formatTimestamp } from "../../lib/format";
import { ChartFrame, tooltipStyle } from "./ChartFrame";
import { EmptyState } from "../States";

export function EventTimeline({ events }: { events: TelemetryEvent[] }) {
  const { categories, dataByCategory } = useMemo(() => {
    const cats = Array.from(new Set(events.map((e) => e.category))).sort();
    const byCat = cats.map((cat) => ({
      category: cat,
      color: categoryColor(cat),
      points: events
        .filter((e) => e.category === cat)
        .map((e) => ({
          t: e.timestamp_seconds,
          lane: cats.indexOf(cat),
          name: e.name,
          category: cat,
          success: e.has_success ? e.success : undefined,
        })),
    }));
    return { categories: cats, dataByCategory: byCat };
  }, [events]);

  if (!events.length) {
    return <EmptyState title="No events" hint="This session recorded no events." />;
  }

  return (
    <ChartFrame
      title="Event timeline"
      subtitle={`${events.length} events across ${categories.length} categories`}
    >
      <ResponsiveContainer width="100%" height={Math.max(180, categories.length * 44 + 60)}>
        <ScatterChart margin={{ top: 8, right: 16, left: 8, bottom: 8 }}>
          <CartesianGrid stroke="#1e2730" horizontal vertical={false} />
          <XAxis
            type="number"
            dataKey="t"
            name="time"
            tickFormatter={(v) => `${Number(v).toFixed(0)}s`}
            stroke="#3a4651"
            tick={{ fill: "#6b7785", fontSize: 11, fontFamily: '"IBM Plex Mono", monospace' }}
            tickLine={false}
          />
          <YAxis
            type="number"
            dataKey="lane"
            name="category"
            domain={[-0.5, categories.length - 0.5]}
            ticks={categories.map((_, i) => i)}
            tickFormatter={(v) => categories[Number(v)] ?? ""}
            width={110}
            stroke="#3a4651"
            tick={{ fill: "#8a96a3", fontSize: 11, fontFamily: '"IBM Plex Mono", monospace' }}
            tickLine={false}
          />
          <ZAxis range={[60, 60]} />
          <Tooltip
            {...tooltipStyle}
            cursor={{ stroke: "#2a3641", strokeDasharray: "3 3" }}
            content={({ active, payload }) => {
              if (!active || !payload?.length) return null;
              const p = payload[0].payload as {
                t: number;
                name: string;
                category: string;
                success?: boolean;
              };
              return (
                <div
                  style={tooltipStyle.contentStyle}
                  className="px-3 py-2"
                >
                  <div className="text-muted">{formatTimestamp(p.t)}</div>
                  <div className="text-ink-bright">
                    <span style={{ color: categoryColor(p.category) }}>{p.category}</span>
                    {" · "}
                    {p.name}
                  </div>
                  {p.success !== undefined && (
                    <div className={p.success ? "text-signal" : "text-danger"}>
                      {p.success ? "success" : "failure"}
                    </div>
                  )}
                </div>
              );
            }}
          />
          {dataByCategory.map((series) => (
            <Scatter
              key={series.category}
              data={series.points}
              fill={series.color}
              fillOpacity={0.85}
              isAnimationActive={false}
              shape="circle"
            />
          ))}
        </ScatterChart>
      </ResponsiveContainer>
    </ChartFrame>
  );
}
