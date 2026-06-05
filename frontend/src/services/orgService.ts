const API_BASE = `${import.meta.env.VITE_API_URL ?? ""}/api/org`;

function authHeaders(): Record<string, string> {
  const token = localStorage.getItem("access_token");
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export interface OrgInfo {
  name: string;
  taxNumber: string;
  registrationNumber: string;
}

export async function getOrgInfo(): Promise<OrgInfo> {
  const res = await fetch(`${API_BASE}/info`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw res;
  return res.json();
}
