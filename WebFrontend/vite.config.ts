import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

// The dev server proxies API calls to the FastAPI backend so the browser
// makes same-origin requests (no CORS needed in dev). The backend also has
// CORS enabled for http://localhost:5173 as a fallback for deployed builds.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, ".", "");
  const API_TARGET = env.GATT_API_TARGET || "http://127.0.0.1:8000";

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        "/api": {
          target: API_TARGET,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/api/, ""),
        },
      },
    },
  };
});
