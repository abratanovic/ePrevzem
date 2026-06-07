import type { AuditActorOption, AuditLogEntry, AuditLogFilters } from "../types/auditLog";

const API_BASE = `${import.meta.env.VITE_API_URL ?? ""}/api/org/audit-log`;

function authHeaders(): HeadersInit {
  const token = localStorage.getItem("access_token");
  return {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

function buildQuery(filters: AuditLogFilters): string {
  const params = new URLSearchParams();

  if (filters.limit) params.set("limit", String(filters.limit));
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  if (filters.action) params.set("action", filters.action);
  if (filters.targetKind) params.set("targetKind", filters.targetKind);
  if (filters.actorKind && filters.actorId) {
    params.set("actorKind", filters.actorKind);
    params.set("actorId", filters.actorId);
  }

  const query = params.toString();
  return query ? `?${query}` : "";
}

export async function getAuditLog(filters: AuditLogFilters = {}): Promise<AuditLogEntry[]> {
  const response = await fetch(`${API_BASE}${buildQuery(filters)}`, {
    headers: authHeaders(),
  });

  if (!response.ok) throw response;
  return response.json();
}

export async function getAuditActors(): Promise<AuditActorOption[]> {
  const response = await fetch(`${API_BASE}/actors`, {
    headers: authHeaders(),
  });

  if (!response.ok) throw response;
  return response.json();
}
