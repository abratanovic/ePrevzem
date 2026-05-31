const API_BASE = `${import.meta.env.VITE_API_URL ?? ""}/api/auth`;

export interface LoginResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  role: "OrganizationAdmin" | "Employee";
  mustChangePassword: boolean;
  firstName: string;
  lastName: string;
  email: string | null;
  organizationId: string | null;
  organizationName: string | null;
}

export async function login(email: string, password: string): Promise<LoginResponse> {
  const res = await fetch(`${API_BASE}/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) throw res;
  return res.json();
}

export async function changePassword(current: string, next: string): Promise<void> {
  const token = localStorage.getItem("access_token");
  const res = await fetch(`${API_BASE}/change-password`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify({ currentPassword: current, newPassword: next }),
  });
  if (!res.ok) throw res;
}

export function logout(): void {
  localStorage.removeItem("access_token");
  localStorage.removeItem("auth_user");
}

export interface RegisterCitizenResponse {
  firstName: string;
  lastName: string;
  code: string;
  expiresAt: string;
}

export async function registerCitizen(siTrustToken: string): Promise<RegisterCitizenResponse> {
  const res = await fetch(`${API_BASE}/citizen/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ siTrustToken }),
  });
  if (!res.ok) throw res;
  return res.json();
}
