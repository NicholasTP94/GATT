import type { ReactNode } from "react";

export function LoadingState({ label = "Acquiring signal" }: { label?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-4 py-20 text-center">
      <div className="relative h-10 w-40 overflow-hidden rounded-full border border-line bg-panel">
        <div className="absolute inset-y-0 w-1/3 animate-sweep bg-gradient-to-r from-transparent via-signal/40 to-transparent" />
      </div>
      <p className="label-eyebrow animate-pulse-dot">{label}…</p>
    </div>
  );
}

export function ErrorState({
  error,
  onRetry,
}: {
  error: unknown;
  onRetry?: () => void;
}) {
  const message =
    (error as { message?: string })?.message ?? "Unknown signal error";
  return (
    <div className="flex flex-col items-center justify-center gap-4 py-16 text-center">
      <div className="flex h-12 w-12 items-center justify-center rounded-xl border border-danger/40 bg-danger/10 text-danger">
        <svg viewBox="0 0 24 24" className="h-6 w-6" fill="none" stroke="currentColor" strokeWidth="1.8">
          <path d="M12 9v4M12 17h.01M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" />
        </svg>
      </div>
      <div>
        <p className="font-medium text-ink-bright">Connection lost</p>
        <p className="mt-1 max-w-md font-mono text-xs text-muted">{message}</p>
        <p className="mt-1 max-w-md text-xs text-muted">
          Is the FastAPI backend running on{" "}
          <span className="font-mono text-muted-2">127.0.0.1:8000</span>?
        </p>
      </div>
      {onRetry && (
        <button
          onClick={onRetry}
          className="focus-ring rounded-lg border border-line-bright bg-panel-2 px-4 py-2 text-sm text-ink hover:border-signal/50 hover:text-signal"
        >
          Retry
        </button>
      )}
    </div>
  );
}

export function EmptyState({
  title,
  hint,
  icon,
}: {
  title: string;
  hint?: ReactNode;
  icon?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
      <div className="flex h-12 w-12 items-center justify-center rounded-xl border border-line bg-panel-2 text-muted">
        {icon ?? (
          <svg viewBox="0 0 24 24" className="h-6 w-6" fill="none" stroke="currentColor" strokeWidth="1.6">
            <path d="M3 12h4l2 5 4-12 2 7h6" />
          </svg>
        )}
      </div>
      <p className="font-medium text-ink-bright">{title}</p>
      {hint && <div className="max-w-md text-sm text-muted">{hint}</div>}
    </div>
  );
}
