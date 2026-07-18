import { Fragment, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useRawSession, useSession } from "../api/queries";
import { apiUrl } from "../api/client";
import type {
  TelemetryEvent,
  TelemetryStateSample,
} from "../api/types";
import { Badge, CategoryBadge } from "../components/Badge";
import { ErrorState, LoadingState, EmptyState } from "../components/States";
import { PropertyChips, PropertyGrid } from "../components/PropertyList";
import { PerformanceCharts } from "../components/charts/PerformanceChart";
import { EventTimeline } from "../components/charts/EventTimeline";
import {
  categoryColor,
  formatDateTime,
  formatDuration,
  formatTimestamp,
  shortId,
} from "../lib/format";

type Tab = "overview" | "events" | "states" | "performance" | "timeline";

const TABS: { id: Tab; label: string }[] = [
  { id: "overview", label: "Overview" },
  { id: "events", label: "Events" },
  { id: "states", label: "States" },
  { id: "performance", label: "Performance" },
  { id: "timeline", label: "Timeline" },
];

export function SessionDetailPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const index = useSession(sessionId);
  const raw = useRawSession(sessionId);
  const [tab, setTab] = useState<Tab>("overview");

  if (index.isLoading || raw.isLoading) return <LoadingState label="Loading session" />;
  if (index.isError) return <ErrorState error={index.error} onRetry={() => index.refetch()} />;
  if (raw.isError) return <ErrorState error={raw.error} onRetry={() => raw.refetch()} />;

  const record = index.data;
  const session = raw.data?.session;
  if (!record || !session) return <ErrorState error={{ message: "Session payload missing" }} />;

  const counts = {
    overview: undefined,
    events: session.events.length,
    states: session.states.length,
    performance: session.performance_samples.length,
    timeline: undefined,
  } as Record<Tab, number | undefined>;

  return (
    <div className="space-y-6">
      {/* Breadcrumb + header */}
      <div>
        <Link
          to="/"
          className="label-eyebrow inline-flex items-center gap-1.5 text-muted transition-colors hover:text-signal"
        >
          <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="m15 18-6-6 6-6" />
          </svg>
          All sessions
        </Link>

        <div className="mt-3 flex flex-wrap items-start justify-between gap-4">
          <div className="min-w-0">
            <h2 className="flex items-center gap-3 text-xl font-semibold tracking-tight text-ink-bright">
              {session.game_id}
              {record.warning_count > 0 && (
                <Badge tone="amber" dot>
                  {record.warning_count} warnings
                </Badge>
              )}
            </h2>
            <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 font-mono text-xs text-muted">
              <span className="text-muted-2">{session.scene_name ?? "no scene"}</span>
              <span className="text-line-bright">·</span>
              <span title={session.session_id}>{shortId(session.session_id, 18)}</span>
              <span className="text-line-bright">·</span>
              <span>schema {raw.data?.schema_version}</span>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <DownloadButton
              href={apiUrl(`/telemetry/sessions/${sessionId}/export/json`)}
              label="JSON"
            />
            <DownloadButton
              href={apiUrl(`/telemetry/sessions/${sessionId}/export/netcdf`)}
              label="NetCDF"
            />
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex flex-wrap gap-1 border-b border-line">
        {TABS.map((t) => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={`relative -mb-px flex items-center gap-2 px-4 py-2.5 text-sm transition-colors ${
              tab === t.id
                ? "text-signal"
                : "text-muted-2 hover:text-ink"
            }`}
          >
            {t.label}
            {counts[t.id] !== undefined && (
              <span
                className={`stat-num rounded-md px-1.5 py-0.5 text-[11px] ${
                  tab === t.id ? "bg-signal/15 text-signal" : "bg-panel-2 text-muted"
                }`}
              >
                {counts[t.id]}
              </span>
            )}
            {tab === t.id && (
              <span className="absolute inset-x-0 -bottom-px h-0.5 bg-signal" style={{ boxShadow: "0 0 10px #a3e635" }} />
            )}
          </button>
        ))}
      </div>

      <div className="animate-fade-up">
        {tab === "overview" && <OverviewTab record={record} session={session} generatedAt={raw.data?.generated_at_utc} />}
        {tab === "events" && <EventsTab events={session.events} />}
        {tab === "states" && <StatesTab states={session.states} />}
        {tab === "performance" && <PerformanceCharts samples={session.performance_samples} />}
        {tab === "timeline" && <EventTimeline events={session.events} />}
      </div>
    </div>
  );
}

function DownloadButton({ href, label }: { href: string; label: string }) {
  return (
    <a
      href={href}
      className="focus-ring flex items-center gap-1.5 rounded-lg border border-line bg-panel px-3 py-2 text-xs text-muted-2 transition-colors hover:border-signal/40 hover:text-signal"
    >
      <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <path d="M12 3v12m0 0 4-4m-4 4-4-4M5 21h14" />
      </svg>
      {label}
    </a>
  );
}

function InfoRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-4 bg-panel px-3 py-2.5">
      <dt className="label-eyebrow normal-case tracking-normal">{label}</dt>
      <dd className="truncate font-mono text-xs text-ink">{value}</dd>
    </div>
  );
}

function OverviewTab({
  record,
  session,
  generatedAt,
}: {
  record: import("../api/types").SessionIndexRecord;
  session: import("../api/types").TelemetrySession;
  generatedAt?: string;
}) {
  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div className="panel p-4">
        <h3 className="mb-3 text-sm font-semibold text-ink-bright">Session</h3>
        <dl className="grid grid-cols-1 gap-px overflow-hidden rounded-lg border border-line bg-line">
          <InfoRow label="Session id" value={<span title={session.session_id}>{shortId(session.session_id, 22)}</span>} />
          <InfoRow label="Game id" value={session.game_id} />
          <InfoRow label="Scene" value={session.scene_name ?? "—"} />
          <InfoRow label="Unity version" value={session.unity_version ?? "—"} />
          <InfoRow label="Tool version" value={session.tool_version ?? "—"} />
          <InfoRow label="Started" value={formatDateTime(session.started_at_utc)} />
          <InfoRow label="Ended" value={formatDateTime(session.ended_at_utc)} />
          <InfoRow
            label="Duration"
            value={
              <span className="text-signal">
                {formatDuration(session.started_at_utc, session.ended_at_utc)}
              </span>
            }
          />
          <InfoRow label="Generated" value={formatDateTime(generatedAt)} />
          <InfoRow label="Stored" value={formatDateTime(record.stored_at_utc)} />
        </dl>
      </div>

      <div className="space-y-4">
        <div className="panel p-4">
          <h3 className="mb-3 text-sm font-semibold text-ink-bright">Volume</h3>
          <div className="grid grid-cols-3 gap-px overflow-hidden rounded-lg border border-line bg-line">
            {[
              { label: "events", value: session.events.length, color: "text-cyan" },
              { label: "states", value: session.states.length, color: "text-violet" },
              { label: "perf", value: session.performance_samples.length, color: "text-amber" },
            ].map((c) => (
              <div key={c.label} className="bg-panel px-3 py-4 text-center">
                <p className={`stat-num text-2xl font-semibold ${c.color}`}>{c.value}</p>
                <p className="label-eyebrow mt-1">{c.label}</p>
              </div>
            ))}
          </div>
        </div>

        <div className="panel p-4">
          <h3 className="mb-3 text-sm font-semibold text-ink-bright">
            Metadata
            <span className="ml-2 font-mono text-xs font-normal text-muted">
              {session.metadata.length}
            </span>
          </h3>
          <PropertyGrid properties={session.metadata} />
        </div>
      </div>
    </div>
  );
}

function EventsTab({ events }: { events: TelemetryEvent[] }) {
  const [query, setQuery] = useState("");
  const [category, setCategory] = useState<string>("all");
  const [expanded, setExpanded] = useState<string | null>(null);

  const categories = useMemo(
    () => Array.from(new Set(events.map((e) => e.category))).sort(),
    [events],
  );

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    return events.filter((e) => {
      if (category !== "all" && e.category !== category) return false;
      if (!q) return true;
      return (
        e.name.toLowerCase().includes(q) ||
        e.category.toLowerCase().includes(q) ||
        (e.object_id ?? "").toLowerCase().includes(q) ||
        e.event_type.toLowerCase().includes(q)
      );
    });
  }, [events, query, category]);

  if (!events.length) {
    return <div className="panel"><EmptyState title="No events recorded" /></div>;
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <input
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search events…"
          className="focus-ring w-full max-w-xs rounded-lg border border-line bg-panel px-3 py-2 text-sm text-ink placeholder:text-muted/70"
        />
        <div className="flex flex-wrap gap-1.5">
          <FilterChip active={category === "all"} onClick={() => setCategory("all")} label={`all · ${events.length}`} />
          {categories.map((c) => (
            <FilterChip
              key={c}
              active={category === c}
              onClick={() => setCategory(c)}
              label={c}
              color={categoryColor(c)}
            />
          ))}
        </div>
      </div>

      <div className="panel overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b border-line text-left">
                <th className="px-4 py-3 label-eyebrow font-normal">Time</th>
                <th className="px-4 py-3 label-eyebrow font-normal">Category</th>
                <th className="px-4 py-3 label-eyebrow font-normal">Name</th>
                <th className="px-4 py-3 label-eyebrow font-normal">Object</th>
                <th className="px-4 py-3 label-eyebrow font-normal text-center">Result</th>
                <th className="px-4 py-3 label-eyebrow font-normal text-right">Props</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((e) => {
                const isOpen = expanded === e.event_id;
                return (
                  <Fragment key={e.event_id}>
                    <tr
                      onClick={() =>
                        setExpanded(isOpen ? null : e.event_id)
                      }
                      className="cursor-pointer border-b border-line/50 transition-colors last:border-0 hover:bg-panel-2/40"
                      style={{ ["--cat" as string]: categoryColor(e.category) }}
                    >
                      <td className="whitespace-nowrap px-4 py-2.5 stat-num text-muted-2">
                        {formatTimestamp(e.timestamp_seconds)}
                      </td>
                      <td className="px-4 py-2.5">
                        <CategoryBadge category={e.category} />
                      </td>
                      <td className="px-4 py-2.5 font-medium text-ink">{e.name}</td>
                      <td className="px-4 py-2.5 font-mono text-xs text-muted-2">
                        {e.object_id ?? "—"}
                      </td>
                      <td className="px-4 py-2.5 text-center">
                        {e.has_success ? (
                          <Badge tone={e.success ? "signal" : "danger"}>
                            {e.success ? "ok" : "fail"}
                          </Badge>
                        ) : (
                          <span className="text-muted/50">—</span>
                        )}
                      </td>
                      <td className="px-4 py-2.5 text-right font-mono text-xs text-muted">
                        {e.properties.length > 0 ? (
                          <span className="inline-flex items-center gap-1">
                            {e.properties.length}
                            <svg
                              viewBox="0 0 24 24"
                              className={`h-3.5 w-3.5 transition-transform ${isOpen ? "rotate-180" : ""}`}
                              fill="none"
                              stroke="currentColor"
                              strokeWidth="2"
                            >
                              <path d="m6 9 6 6 6-6" />
                            </svg>
                          </span>
                        ) : (
                          "—"
                        )}
                      </td>
                    </tr>
                    {isOpen && e.properties.length > 0 && (
                      <tr className="border-b border-line/50 bg-void/40">
                        <td colSpan={6} className="px-4 py-3">
                          <div className="flex items-start gap-3">
                            <span className="label-eyebrow mt-0.5">properties</span>
                            <PropertyChips properties={e.properties} />
                          </div>
                          {e.has_position && e.position && (
                            <div className="mt-2 flex items-center gap-3">
                              <span className="label-eyebrow">position</span>
                              <span className="font-mono text-xs text-ink">
                                x {e.position.x.toFixed(2)} · y {e.position.y.toFixed(2)} · z {e.position.z.toFixed(2)}
                              </span>
                            </div>
                          )}
                        </td>
                      </tr>
                    )}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
        {filtered.length === 0 && (
          <EmptyState title="No events match" hint="Adjust your search or filter." />
        )}
      </div>
    </div>
  );
}

function StatesTab({ states }: { states: TelemetryStateSample[] }) {
  const [query, setQuery] = useState("");

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return states;
    return states.filter(
      (s) =>
        s.name.toLowerCase().includes(q) ||
        (s.object_id ?? "").toLowerCase().includes(q),
    );
  }, [states, query]);

  if (!states.length) {
    return <div className="panel"><EmptyState title="No state samples recorded" /></div>;
  }

  return (
    <div className="space-y-4">
      <input
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search states…"
        className="focus-ring w-full max-w-xs rounded-lg border border-line bg-panel px-3 py-2 text-sm text-ink placeholder:text-muted/70"
      />
      <div className="panel overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b border-line text-left">
                <th className="px-4 py-3 label-eyebrow font-normal">Time</th>
                <th className="px-4 py-3 label-eyebrow font-normal">Name</th>
                <th className="px-4 py-3 label-eyebrow font-normal">Object</th>
                <th className="px-4 py-3 label-eyebrow font-normal">Properties</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((s, i) => (
                <tr key={i} className="border-b border-line/50 align-top transition-colors last:border-0 hover:bg-panel-2/40">
                  <td className="whitespace-nowrap px-4 py-2.5 stat-num text-muted-2">
                    {formatTimestamp(s.timestamp_seconds)}
                  </td>
                  <td className="px-4 py-2.5 font-medium text-ink">{s.name}</td>
                  <td className="px-4 py-2.5 font-mono text-xs text-muted-2">
                    {s.object_id ?? "—"}
                  </td>
                  <td className="px-4 py-2.5">
                    <PropertyChips properties={s.properties} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {filtered.length === 0 && <EmptyState title="No states match" />}
      </div>
    </div>
  );
}

function FilterChip({
  active,
  onClick,
  label,
  color,
}: {
  active: boolean;
  onClick: () => void;
  label: string;
  color?: string;
}) {
  return (
    <button
      onClick={onClick}
      className={`inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1 font-mono text-[11px] transition-colors ${
        active
          ? "border-signal/40 bg-signal/10 text-signal"
          : "border-line bg-panel text-muted-2 hover:border-line-bright hover:text-ink"
      }`}
    >
      {color && (
        <span className="h-1.5 w-1.5 rounded-full" style={{ background: color }} />
      )}
      {label}
    </button>
  );
}
