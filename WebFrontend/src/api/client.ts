import axios from "axios";

// In dev, requests go to /api and Vite proxies them to the FastAPI backend
// (see vite.config.ts). Override with VITE_API_BASE_URL for a deployed build.
const baseURL = import.meta.env.VITE_API_BASE_URL ?? "/api";

export const api = axios.create({
  baseURL,
  headers: { Accept: "application/json" },
  timeout: 20000,
});

// Absolute URL helper for plain <a> downloads (FileResponse endpoints).
export function apiUrl(path: string): string {
  const cleaned = path.startsWith("/") ? path : `/${path}`;
  return `${baseURL}${cleaned}`;
}
