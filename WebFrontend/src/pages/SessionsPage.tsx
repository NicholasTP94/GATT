import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useSessions } from "../api/queries";
import type { SessionIndexRecord } from "../api/types";
import { StatCard } from "../components/StatCard";
import { Badge } from "../components/Badge";
import { EmptyState, ErrorState, LoadingState } from "../components/States";
import {
  formatDuration,
  formatNumber,
  relativeTime,
  shortId,
} from "../lib/format";

type SortKey =
  | "stored_at_utc"
  | "game_id"
  | "event_count"
  | "state_count"
  | "performance_sample_count";

export function SessionsPage() {
  const { data, isLoading, isError, error, refetch, isFetching } = useSessions();
  const [query, setQuery] = useState("");
  const [sortKey, setSortKey] = useState<SortKey>("stored_at_utc");
  const [sortDir, setSortDir] = useState<"asc" | "desc">("desc");
  const navigate = useNavigate();

  const totals = useMemo(() => {
    const sessions = data ?? [];
    return {
      sessions: sessions.length,
      events: sessions.reduce((a, s) => a + s.event_count, 0),
      states: sessions.reduce((a, s) => a + s.state_count, 0),
      perf: sessions.reduce((a, s) => a + s.performance_sample_count, 0),
      warnings: sessions.reduce((a, s) => a + (s.warning_count ?? 0), 0),
    };
  }, [data]);

  const rows = useMemo(() => {
    let sessions = [...(data ?? [])];
    const q = query.trim().toLowerCase();
    if (q) {
      sessions = sessions.filter(
        (s) =>
          s.game_id.toLowerCase().includes(q) ||
          (s.scene_name ?? "").toLowerCase().includes(q) ||
          s.session_id.toLowerCase().includes(q),
      );
    }
    sessions.sort((a, b) => {
      const av = a[sortKey];
      const bv = b[sortKey];
      let cmp: number;
      if (typeof av === "number" && typeof bv === "number") cmp = av - bv;
      else cmp = String(av).localeCompare(String(bv));
      return sortDir === "asc" ? cmp : -cmp;
    });
    return sessions;
  }, [data, query, sortKey, sortDir]);

  function toggleSort(key: SortKey) {
    if (key === sortKey) setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    else {
      setSortKey(key);
      setSortDir(key === "game_id" ? "asc" : "desc");
    }
  }

  if (isLoading) return <LoadingState label="Loading sessions" />;
  if (isError) return <ErrorState error={error} onRetry={() => refetch()} />;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold tracking-tight text-ink-bright">
            Recorded sessions
          </h2>
          <p className="mt-1 text-sm text-muted">
            Every telemetry session ingested by the backend.
          </p>
        </div>
        <button
          onClick={() => refetch()}
          className="focus-ring flex items-center gap-2 rounded-lg border border-line bg-panel px-3 py-2 text-sm text-muted-2 hover:border-signal/40 hover:text-signal"
        >
          <svg
            viewBox="0 0 24 24"
            className={`h-4 w-4 ${isFetching ? "animate-spin" : ""}`}
            fill="none"
            stroke="currentColor"
            strokeWidth="1.7"
            strokeLinecap="round"
          >
            <path d="M21 12a9 9 0 1 1-3-6.7M21 4v4h-4" />
          </svg>
          Refresh
        </button>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatCard index={0} label="Sessions" value={totals.sessions} accent="signal" />
        <StatCard index={1} label="Events" value={formatNumber(totals.events, 0)} accent="cyan" />
        <StatCard index={2} label="State samples" value={formatNumber(totals.states, 0)} accent="violet" />
        <StatCard
          index={3}
          label="Perf samples"
          value={formatNumber(totals.perf, 0)}
          accent="amber"
          hint={totals.warnings ? `${totals.warnings} warnings` : undefined}
        />
      </div>

      {/* Search */}
      <div className="relative max-w-md">
        <svg
          viewBox="0 0 24 24"
          className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.7"
        >
          <circle cx="11" cy="11" r="7" />
          <path d="m20 20-3.5-3.5" />
        </svg>
        <input
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Filter by game, scene, or session id…"
          className="focus-ring w-full rounded-lg border border-line bg-panel py-2 pl-9 pr-3 text-sm text-ink placeholder:text-muted/70"
        />
      </div>

      {/* Table */}
      {rows.length === 0 ? (
        <div className="panel">
          <EmptyState
            title={query ? "No matching sessions" : "No sessions yet"}
            hint={
              query ? (
                "Try a different search term."
              ) : (
                <>
                  Record a session in Unity and upload it, or POST a telemetry
                  envelope to{" "}
                  <span className="font-mono text-muted-2">
                    /telemetry/sessions
                  </span>
                  .
                </>
              )
            }
          />
        </div>
      ) : (
        <div className="panel overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-sm">
              <thead>
                <tr className="border-b border-line text-left">
                  <Th label="Game" colKey="game_id" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} />
                  <th className="px-4 py-3 label-eyebrow font-normal">Scene</th>
                  <Th label="Events" colKey="event_count" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="right" />
                  <Th label="States" colKey="state_count" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="right" />
                  <Th label="Perf" colKey="performance_sample_count" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="right" />
                  <th className="px-4 py-3 label-eyebrow font-normal text-right">Duration</th>
                  <Th label="Stored" colKey="stored_at_utc" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="right" />
                </tr>
              </thead>
              <tbody>
                {rows.map((s) => (
                  <Row key={s.session_id} session={s} onOpen={() => navigate(`/sessions/${s.session_id}`)} />
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

function Th({
  label,
  colKey,
  sortKey,
  sortDir,
  onSort,
  align = "left",
}: {
  label: string;
  colKey: SortKey;
  sortKey: SortKey;
  sortDir: "asc" | "desc";
  onSort: (k: SortKey) => void;
  align?: "left" | "right";
}) {
  const active = sortKey === colKey;
  return (
    <th className={`px-4 py-3 ${align === "right" ? "text-right" : "text-left"}`}>
      <button
        onClick={() => onSort(colKey)}
        className={`label-eyebrow inline-flex items-center gap-1 font-normal transition-colors hover:text-ink ${
          active ? "text-signal" : ""
        }`}
      >
        {label}
        <span className={`text-[8px] ${active ? "opacity-100" : "opacity-30"}`}>
          {active ? (sortDir === "asc" ? "▲" : "▼") : "▼"}
        </span>
      </button>
    </th>
  );
}

function Row({
  session,
  onOpen,
}: {
  session: SessionIndexRecord;
  onOpen: () => void;
}) {
  return (
    <tr
      onClick={onOpen}
      className="group cursor-pointer border-b border-line/60 transition-colors last:border-0 hover:bg-signal/[0.04]"
    >
      <td className="px-4 py-3">
        <div className="flex flex-col">
          <span className="font-medium text-ink-bright group-hover:text-signal">
            {session.game_id}
          </span>
          <span className="font-mono text-[11px] text-muted">
            {shortId(session.session_id, 12)}
          </span>
        </div>
      </td>
      <td className="px-4 py-3 text-muted-2">{session.scene_name ?? "—"}</td>
      <td className="px-4 py-3 text-right stat-num text-ink">{session.event_count}</td>
      <td className="px-4 py-3 text-right stat-num text-ink">{session.state_count}</td>
      <td className="px-4 py-3 text-right stat-num text-ink">
        {session.performance_sample_count}
      </td>
      <td className="px-4 py-3 text-right stat-num text-muted-2">
        {formatDuration(session.started_at_utc, session.ended_at_utc)}
      </td>
      <td className="px-4 py-3 text-right">
        <div className="flex items-center justify-end gap-2">
          {session.warning_count > 0 && (
            <Badge tone="amber">{session.warning_count}⚠</Badge>
          )}
          <span className="stat-num text-muted-2">
            {relativeTime(session.stored_at_utc)}
          </span>
        </div>
      </td>
    </tr>
  );
}
