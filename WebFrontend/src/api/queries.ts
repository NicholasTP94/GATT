import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { api } from "./client";
import type {
  ExportFormat,
  ExportRecord,
  SessionIndexRecord,
  TelemetrySessionEnvelope,
} from "./types";

export const queryKeys = {
  health: ["health"] as const,
  sessions: ["sessions"] as const,
  session: (id: string) => ["session", id] as const,
  rawSession: (id: string) => ["session", id, "raw"] as const,
  exports: ["exports"] as const,
};

export function useHealth() {
  return useQuery({
    queryKey: queryKeys.health,
    queryFn: async () => {
      const { data } = await api.get<{ status?: string }>("/health");
      return data;
    },
    refetchInterval: 15000,
    retry: false,
  });
}

export function useSessions() {
  return useQuery({
    queryKey: queryKeys.sessions,
    queryFn: async () => {
      const { data } = await api.get<SessionIndexRecord[]>("/telemetry/sessions");
      return data;
    },
  });
}

export function useSession(sessionId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.session(sessionId ?? ""),
    enabled: Boolean(sessionId),
    queryFn: async () => {
      const { data } = await api.get<SessionIndexRecord>(
        `/telemetry/sessions/${sessionId}`,
      );
      return data;
    },
  });
}

export function useRawSession(sessionId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.rawSession(sessionId ?? ""),
    enabled: Boolean(sessionId),
    queryFn: async () => {
      const { data } = await api.get<TelemetrySessionEnvelope>(
        `/telemetry/sessions/${sessionId}/raw`,
      );
      return data;
    },
  });
}

export function useExports() {
  return useQuery({
    queryKey: queryKeys.exports,
    queryFn: async () => {
      const { data } = await api.get<ExportRecord[]>("/exports");
      return data;
    },
  });
}

export function useCreateExport() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (input: {
      format: ExportFormat;
      session_ids: string[];
      filename?: string;
    }) => {
      const endpoint = input.format === "json" ? "/exports/json" : "/exports/netcdf";
      const { data } = await api.post<ExportRecord>(endpoint, {
        session_ids: input.session_ids,
        filename: input.filename,
      });
      return data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.exports });
    },
  });
}
