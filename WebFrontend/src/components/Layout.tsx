import { NavLink, Outlet } from "react-router-dom";
import { useHealth } from "../api/queries";

function HealthPill() {
  const { data, isError, isLoading } = useHealth();
  const ok = !isError && !isLoading && (data?.status ?? "ok") !== undefined && !isError;

  const tone = isLoading
    ? { dot: "bg-amber", text: "text-amber", label: "probing" }
    : isError
      ? { dot: "bg-danger", text: "text-danger", label: "offline" }
      : { dot: "bg-signal", text: "text-signal", label: "online" };

  return (
    <div className="flex items-center gap-2 rounded-lg border border-line bg-panel/80 px-3 py-1.5">
      <span
        className={`h-2 w-2 rounded-full ${tone.dot} ${ok || isLoading ? "animate-pulse-dot" : ""}`}
        style={{ boxShadow: "0 0 8px currentColor" }}
      />
      <span className="label-eyebrow normal-case tracking-wide text-muted">backend</span>
      <span className={`font-mono text-xs ${tone.text}`}>{tone.label}</span>
    </div>
  );
}

const navItems = [
  {
    to: "/",
    label: "Sessions",
    end: true,
    icon: (
      <path d="M4 6h16M4 12h16M4 18h10" />
    ),
  },
  {
    to: "/exports",
    label: "Exports",
    end: false,
    icon: <path d="M12 3v12m0 0 4-4m-4 4-4-4M5 21h14" />,
  },
];

export function Layout() {
  return (
    <div className="flex min-h-screen">
      {/* Sidebar */}
      <aside className="sticky top-0 hidden h-screen w-60 shrink-0 flex-col border-r border-line bg-void/60 px-4 py-5 backdrop-blur-sm md:flex">
        <Brand />
        <nav className="mt-8 flex flex-col gap-1">
          <p className="label-eyebrow mb-2 px-2">Telemetry</p>
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                `group flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors ${
                  isActive
                    ? "bg-signal/10 text-signal shadow-[inset_0_0_0_1px_rgba(163,230,53,0.25)]"
                    : "text-muted-2 hover:bg-panel hover:text-ink"
                }`
              }
            >
              <svg
                viewBox="0 0 24 24"
                className="h-4 w-4"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.7"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                {item.icon}
              </svg>
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="mt-auto space-y-3">
          <div className="rounded-lg border border-line bg-panel/60 p-3">
            <p className="label-eyebrow">Data source</p>
            <p className="mt-1 font-mono text-[11px] text-muted-2">
              FastAPI · SQLite
            </p>
            <p className="font-mono text-[11px] text-muted">127.0.0.1:8000</p>
          </div>
          <p className="px-1 font-mono text-[10px] text-muted/60">
            Genre-Agnostic Telemetry · v0.1
          </p>
        </div>
      </aside>

      {/* Main */}
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-10 flex items-center justify-between gap-4 border-b border-line bg-base/80 px-5 py-3 backdrop-blur-md md:px-8">
          <div className="flex items-center gap-3 md:hidden">
            <Brand compact />
          </div>
          <div className="hidden md:block">
            <h1 className="text-sm font-semibold text-ink-bright">
              Telemetry Console
            </h1>
            <p className="label-eyebrow normal-case tracking-normal text-muted">
              Browse · inspect · visualize recorded sessions
            </p>
          </div>
          <HealthPill />
        </header>

        <main className="mx-auto w-full max-w-7xl flex-1 px-5 py-6 md:px-8 md:py-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

function Brand({ compact = false }: { compact?: boolean }) {
  return (
    <div className="flex items-center gap-2.5">
      <div className="relative flex h-8 w-8 items-center justify-center rounded-lg border border-signal/40 bg-signal/10">
        <svg viewBox="0 0 24 24" className="h-4.5 w-4.5 text-signal" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
          <path d="M3 12h3l2 6 4-14 3 9 2-3h4" />
        </svg>
        <span className="absolute inset-0 rounded-lg shadow-glow" />
      </div>
      {!compact && (
        <div className="leading-tight">
          <p className="font-mono text-sm font-semibold tracking-tight text-ink-bright">
            GATT
          </p>
          <p className="font-mono text-[10px] uppercase tracking-[0.2em] text-muted">
            console
          </p>
        </div>
      )}
    </div>
  );
}
