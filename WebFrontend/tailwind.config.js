/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        // Telemetry console palette — near-black blue-tinted base,
        // phosphor-lime signal accent, instrument cyan secondary.
        void: "#070a0d",
        base: "#0a0e12",
        panel: "#11161c",
        "panel-2": "#151b22",
        line: "#1e2730",
        "line-bright": "#2a3641",
        muted: "#6b7785",
        "muted-2": "#8a96a3",
        ink: "#c7d0d9",
        "ink-bright": "#eef3f7",
        signal: "#a3e635",
        "signal-dim": "#65840f",
        cyan: "#22d3ee",
        amber: "#fbbf24",
        danger: "#fb7185",
        violet: "#a78bfa",
      },
      fontFamily: {
        sans: ['"IBM Plex Sans"', "system-ui", "sans-serif"],
        mono: ['"IBM Plex Mono"', "ui-monospace", "monospace"],
      },
      boxShadow: {
        glow: "0 0 0 1px rgba(163,230,53,0.25), 0 0 24px -6px rgba(163,230,53,0.35)",
        panel: "0 1px 0 0 rgba(255,255,255,0.03) inset, 0 8px 30px -12px rgba(0,0,0,0.6)",
      },
      keyframes: {
        "fade-up": {
          "0%": { opacity: "0", transform: "translateY(8px)" },
          "100%": { opacity: "1", transform: "translateY(0)" },
        },
        "pulse-dot": {
          "0%, 100%": { opacity: "1" },
          "50%": { opacity: "0.35" },
        },
        sweep: {
          "0%": { transform: "translateX(-100%)" },
          "100%": { transform: "translateX(100%)" },
        },
      },
      animation: {
        "fade-up": "fade-up 0.5s cubic-bezier(0.22,1,0.36,1) both",
        "pulse-dot": "pulse-dot 1.6s ease-in-out infinite",
        sweep: "sweep 1.4s ease-in-out infinite",
      },
    },
  },
  plugins: [],
};
