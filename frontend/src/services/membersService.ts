const API_BASE = `${import.meta.env.VITE_API_URL ?? ""}/api/org/members`;

function authHeaders(): Record<string, string> {
  const token = localStorage.getItem("access_token");
  return token ? { Authorization: `Bearer ${token}`, "Content-Type": "application/json" } : { "Content-Type": "application/json" };
}

export interface AddMemberResponse {
  employeeId: string;
  initialPassword: string;
  provisioningCode: string;
  provisioningCodeExpiresAt: string;
}

export async function addMember(firstName: string, lastName: string, email: string): Promise<AddMemberResponse> {
  const res = await fetch(API_BASE, {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify({ firstName, lastName, email }),
  });
  if (!res.ok) throw res;
  return res.json();
}

export interface Member {
  id: string;
  firstName: string;
  lastName: string;
  email: string | null;
  status: "Active" | "Disabled";
  roles: string[];
  lastLoginAt: string | null;
}

export async function getMembers(): Promise<Member[]> {
  const res = await fetch(API_BASE, {
    headers: { Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}` },
  });
  if (!res.ok) throw res;
  return res.json();
}

export async function disableEmployee(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/${id}/disable`, {
    method: "PATCH",
    headers: { Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}` },
  });
  if (!res.ok) throw res;
}

export async function enableEmployee(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/${id}/enable`, {
    method: "PATCH",
    headers: { Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}` },
  });
  if (!res.ok) throw res;
}
