import type { DashboardStats, LockerStation, PickupPage } from "../types/dashboard";
import { getRecentPickups as getPickups } from "./pickupsService";

const API_BASE = `${import.meta.env.VITE_API_URL ?? ""}/api/org/dashboard`;

function authHeaders(): HeadersInit {
  const token = localStorage.getItem("access_token");
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) throw response;
  return response.json();
}

export async function getDashboardStats(): Promise<DashboardStats> {
  return readJson<DashboardStats>(
    await fetch(`${API_BASE}/stats`, { headers: authHeaders() }),
  );
}

export async function getRecentPickups(page = 1, pageSize = 10): Promise<PickupPage> {
  void page;
  return getPickups(pageSize);
}

export async function getLockerOccupancy(): Promise<LockerStation[]> {
  return readJson<LockerStation[]>(
    await fetch(`${API_BASE}/locker-occupancy`, { headers: authHeaders() }),
  );
}
