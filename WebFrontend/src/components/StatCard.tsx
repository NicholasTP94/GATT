import type { ReactNode } from "react";

export function StatCard({
  label,
  value,
  unit,
  accent = "signal",
  hint,
  index = 0,
}: {
  label: string;
  value: ReactNode;
  unit?: string;
  accent?: "signal" | "cyan" | "amber" | "violet";
  hint?: string;
  index?: number;
}) {
  const accentColor = {
    signal: "text-signal",
    cyan: "text-cyan",
    amber: "text-amber",
    violet: "text-violet",
  }[accent];

  const barColor = {
    signal: "bg-signal",
    cyan: "bg-cyan",
    amber: "bg-amber",
    violet: "bg-violet",
  }[accent];

  return (
    <div
      className="panel group relative animate-fade-up overflow-hidden p-4"
      style={{ animationDelay: `${index * 60}ms` }}
    >
      <div
        className={`absolute left-0 top-0 h-full w-[3px] ${barColor} opacity-50 transition-opacity group-hover:opacity-100`}
      />
      <p className="label-eyebrow">{label}</p>
      <p className="mt-2 flex items-baseline gap-1.5">
        <span className={`stat-num text-3xl font-semibold ${accentColor}`}>
          {value}
        </span>
        {unit && <span className="font-mono text-xs text-muted">{unit}</span>}
      </p>
      {hint && <p className="mt-1 font-mono text-[11px] text-muted">{hint}</p>}
    </div>
  );
}
