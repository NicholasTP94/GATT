import { useMemo, useState } from "react";
import {
  useCreateExport,
  useExports,
  useSessions,
} from "../api/queries";
import { apiUrl } from "../api/client";
import type { ExportFormat } from "../api/types";
import { Badge } from "../components/Badge";
import { EmptyState, ErrorState, LoadingState } from "../components/States";
import { formatDateTime, relativeTime, shortId } from "../lib/format";

export function ExportsPage() {
  const exportsQuery = useExports();
  const sessionsQuery = useSessions();
  const createExport = useCreateExport();

  const [format, setFormat] = useState<ExportFormat>("json");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [feedback, setFeedback] = useState<string | null>(null);

  const sessions = sessionsQuery.data ?? [];

  const sessionLabel = useMemo(() => {
    const map = new Map<string, string>();
    for (const s of sessions) map.set(s.session_id, s.game_id);
    return map;
  }, [sessions]);

  function toggle(id: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  async function handleCreate() {
    if (selected.size === 0) return;
    setFeedback(null);
    try {
      const record = await createExport.mutateAsync({
        format,
        session_ids: Array.from(selected),
      });
      setFeedback(`Created ${format.toUpperCase()} export ${shortId(record.export_id, 8)}`);
      setSelected(new Set());
    } catch (err) {
      setFeedback(
        `Failed: ${(err as { message?: string })?.message ?? "unknown error"}`,
      );
    }
  }

  if (exportsQuery.isLoading) return <LoadingState label="Loading exports" />;
  if (exportsQuery.isError)
    return <ErrorState error={exportsQuery.error} onRetry={() => exportsQuery.refetch()} />;

  const exports = exportsQuery.data ?? [];

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold tracking-tight text-ink-bright">Exports</h2>
        <p className="mt-1 text-sm text-muted">
          Bundle one or more sessions into a downloadable JSON or NetCDF file.
        </p>
      </div>

      {/* Create export */}
      <div className="panel p-5">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <h3 className="text-sm font-semibold text-ink-bright">New export</h3>
          <div className="flex items-center gap-1 rounded-lg border border-line bg-base p-1">
            {(["json", "netcdf"] as ExportFormat[]).map((f) => (
              <button
                key={f}
                onClick={() => setFormat(f)}
                className={`rounded-md px-3 py-1.5 font-mono text-xs uppercase tracking-wide transition-colors ${
                  format === f
                    ? "bg-signal/15 text-signal"
                    : "text-muted hover:text-ink"
                }`}
              >
                {f}
              </button>
            ))}
          </div>
        </div>

        {sessionsQuery.isLoading ? (
          <p className="font-mono text-xs text-muted">Loading sessions…</p>
        ) : sessions.length === 0 ? (
          <EmptyState title="No sessions to export" />
        ) : (
          <>
            <div className="max-h-64 space-y-1.5 overflow-y-auto pr-1">
              {sessions.map((s) => {
                const checked = selected.has(s.session_id);
                return (
                  <label
                    key={s.session_id}
                    className={`flex cursor-pointer items-center gap-3 rounded-lg border px-3 py-2 transition-colors ${
                      checked
                        ? "border-signal/40 bg-signal/[0.06]"
                        : "border-line bg-base hover:border-line-bright"
                    }`}
                  >
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={() => toggle(s.session_id)}
                      className="peer sr-only"
                    />
                    <span
                      className={`flex h-4 w-4 items-center justify-center rounded border transition-colors ${
                        checked ? "border-signal bg-signal text-base" : "border-line-bright"
                      }`}
                    >
                      {checked && (
                        <svg viewBox="0 0 24 24" className="h-3 w-3" fill="none" stroke="currentColor" strokeWidth="3">
                          <path d="m5 12 5 5L20 7" />
                        </svg>
                      )}
                    </span>
                    <span className="flex-1 text-sm text-ink">{s.game_id}</span>
                    <span className="font-mono text-[11px] text-muted">
                      {s.event_count} ev · {s.scene_name ?? "—"}
                    </span>
                    <span className="font-mono text-[11px] text-muted/70">
                      {shortId(s.session_id, 8)}
                    </span>
                  </label>
                );
              })}
            </div>

            <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-center gap-3">
                <button
                  onClick={handleCreate}
                  disabled={selected.size === 0 || createExport.isPending}
                  className="focus-ring flex items-center gap-2 rounded-lg border border-signal/40 bg-signal/10 px-4 py-2 text-sm font-medium text-signal transition-colors hover:bg-signal/20 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  {createExport.isPending ? "Building…" : `Create ${format.toUpperCase()} export`}
                </button>
                <span className="font-mono text-xs text-muted">
                  {selected.size} selected
                </span>
              </div>
              {feedback && (
                <span className="font-mono text-xs text-muted-2">{feedback}</span>
              )}
            </div>
          </>
        )}
      </div>

      {/* Existing exports */}
      <div>
        <h3 className="mb-3 text-sm font-semibold text-ink-bright">
          Generated exports
          <span className="ml-2 font-mono text-xs font-normal text-muted">{exports.length}</span>
        </h3>

        {exports.length === 0 ? (
          <div className="panel">
            <EmptyState
              title="No exports yet"
              hint="Select sessions above and create your first export."
            />
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            {exports.map((ex) => (
              <div key={ex.export_id} className="panel flex items-center justify-between gap-4 p-4">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <Badge tone={ex.format === "json" ? "cyan" : "violet"}>
                      {ex.format.toUpperCase()}
                    </Badge>
                    <span className="font-mono text-xs text-muted-2">
                      {shortId(ex.export_id, 10)}
                    </span>
                  </div>
                  <p className="mt-2 truncate text-sm text-ink">
                    {ex.session_ids.length === 1
                      ? sessionLabel.get(ex.session_ids[0]) ?? shortId(ex.session_ids[0], 12)
                      : `${ex.session_ids.length} sessions`}
                  </p>
                  <p className="font-mono text-[11px] text-muted" title={formatDateTime(ex.created_at_utc)}>
                    {relativeTime(ex.created_at_utc)}
                  </p>
                </div>
                <a
                  href={apiUrl(`/exports/${ex.export_id}/download`)}
                  className="focus-ring flex shrink-0 items-center gap-1.5 rounded-lg border border-line bg-panel-2 px-3 py-2 text-xs text-muted-2 transition-colors hover:border-signal/40 hover:text-signal"
                >
                  <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M12 3v12m0 0 4-4m-4 4-4-4M5 21h14" />
                  </svg>
                  Download
                </a>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
