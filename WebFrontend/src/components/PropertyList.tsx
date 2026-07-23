import type { TelemetryProperty } from "../api/types";
import { propertyValue } from "../lib/format";

/** Compact inline chips for a set of telemetry properties. */
export function PropertyChips({ properties }: { properties: TelemetryProperty[] }) {
  if (!properties?.length) {
    return <span className="font-mono text-xs text-muted/60">—</span>;
  }
  return (
    <div className="flex flex-wrap gap-1.5">
      {properties.map((p, i) => (
        <span
          key={`${p.key}-${i}`}
          className="inline-flex items-center gap-1 rounded-md border border-line bg-panel-2/60 px-1.5 py-0.5 font-mono text-[11px]"
        >
          <span className="text-muted">{p.key}</span>
          <span className="text-line-bright">=</span>
          <span className="text-ink">{propertyValue(p)}</span>
        </span>
      ))}
    </div>
  );
}

/** Key/value definition grid (used for metadata + overview). */
export function PropertyGrid({ properties }: { properties: TelemetryProperty[] }) {
  if (!properties?.length) {
    return (
      <p className="font-mono text-xs text-muted">No metadata recorded.</p>
    );
  }
  return (
    <dl className="grid grid-cols-1 gap-px overflow-hidden rounded-lg border border-line bg-line sm:grid-cols-2">
      {properties.map((p, i) => (
        <div
          key={`${p.key}-${i}`}
          className="flex items-center justify-between gap-4 bg-panel px-3 py-2"
        >
          <dt className="font-mono text-xs text-muted">{p.key}</dt>
          <dd className="truncate font-mono text-xs text-ink" title={propertyValue(p)}>
            {propertyValue(p)}
          </dd>
        </div>
      ))}
    </dl>
  );
}
