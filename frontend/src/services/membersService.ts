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
