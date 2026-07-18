import type { ReactNode } from "react";

type Tone = "neutral" | "signal" | "cyan" | "amber" | "danger" | "violet";

const toneClasses: Record<Tone, string> = {
  neutral: "border-line-bright/60 bg-panel-2 text-muted-2",
  signal: "border-signal/30 bg-signal/10 text-signal",
  cyan: "border-cyan/30 bg-cyan/10 text-cyan",
  amber: "border-amber/30 bg-amber/10 text-amber",
  danger: "border-danger/30 bg-danger/10 text-danger",
  violet: "border-violet/30 bg-violet/10 text-violet",
};

export function Badge({
  children,
  tone = "neutral",
  dot,
  className = "",
}: {
  children: ReactNode;
  tone?: Tone;
  dot?: boolean;
  className?: string;
}) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-md border px-2 py-0.5 font-mono text-[11px] leading-5 ${toneClasses[tone]} ${className}`}
    >
      {dot && (
        <span
          className="h-1.5 w-1.5 rounded-full bg-current"
          style={{ boxShadow: "0 0 8px currentColor" }}
        />
      )}
      {children}
    </span>
  );
}

export function CategoryBadge({ category }: { category: string }) {
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-md border px-2 py-0.5 font-mono text-[11px] leading-5"
      style={{
        // tinted from the category color
        color: "var(--cat)",
        borderColor: "color-mix(in srgb, var(--cat) 35%, transparent)",
        backgroundColor: "color-mix(in srgb, var(--cat) 12%, transparent)",
      }}
    >
      <span
        className="h-1.5 w-1.5 rounded-full"
        style={{ background: "var(--cat)", boxShadow: "0 0 8px var(--cat)" }}
      />
      {category}
    </span>
  );
}
