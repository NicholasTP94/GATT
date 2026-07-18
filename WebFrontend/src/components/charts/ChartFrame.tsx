import type { ReactNode } from "react";

export function ChartFrame({
  title,
  subtitle,
  children,
  right,
}: {
  title: string;
  subtitle?: string;
  children: ReactNode;
  right?: ReactNode;
}) {
  return (
    <div className="panel p-4">
      <div className="mb-3 flex items-start justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-ink-bright">{title}</h3>
          {subtitle && (
            <p className="label-eyebrow mt-0.5 normal-case tracking-normal">
              {subtitle}
            </p>
          )}
        </div>
        {right}
      </div>
      {children}
    </div>
  );
}

/** Shared dark tooltip styling for Recharts. */
export const tooltipStyle = {
  contentStyle: {
    background: "#0a0e12",
    border: "1px solid #2a3641",
    borderRadius: 10,
    fontFamily: '"IBM Plex Mono", monospace',
    fontSize: 12,
    color: "#eef3f7",
    boxShadow: "0 8px 30px -12px rgba(0,0,0,0.7)",
  },
  labelStyle: { color: "#6b7785", marginBottom: 4 },
  itemStyle: { color: "#c7d0d9" },
} as const;
