import type {
  CreatePickupRequest,
  Pickup,
  PickupPage,
  PickupRecipient,
  PickupStationOption,
} from "../types/dashboard";

const API_BASE = `${import.meta.env.VITE_API_URL ?? ""}/api/org/pickups`;
const EXPIRING_THRESHOLD_MS = 24 * 60 * 60 * 1000;

type BackendPickupStatus =
  | "AwaitingPlacement"
  | "InLocker"
  | "PickedUp"
  | "NotPickedUp"
  | "AwaitingPersonalPickup"
  | "Cancelled";

interface BackendPickup {
  id: string;
  reference: string;
  description: string;
  recipientName: string;
  locationName: string;
  status: BackendPickupStatus;
  deadlineAt: string | null;
  createdAt: string;
}

export class PickupServiceError extends Error {
  code: "recipient_not_found" | "station_forbidden" | "creation_forbidden" | "deletion_forbidden" | "unknown";

  constructor(code: "recipient_not_found" | "station_forbidden" | "creation_forbidden" | "deletion_forbidden" | "unknown") {
    super(code);
    this.code = code;
  }
}

function authHeaders(): HeadersInit {
  const token = localStorage.getItem("access_token");
  return {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) throw response;
  return response.json();
}

function toPickup(pickup: BackendPickup): Pickup {
  let status: Pickup["status"];
  switch (pickup.status) {
    case "AwaitingPlacement": status = "awaitingPlacement"; break;
    case "InLocker":
      status = pickup.deadlineAt
        && new Date(pickup.deadlineAt).getTime() - Date.now() <= EXPIRING_THRESHOLD_MS
        ? "expiring"
        : "ready";
      break;
    case "PickedUp": status = "picked"; break;
    case "NotPickedUp": status = "expired"; break;
    case "AwaitingPersonalPickup": status = "awaitingPersonalPickup"; break;
    case "Cancelled": status = "cancelled"; break;
  }

  return {
    id: pickup.id,
    reference: pickup.reference,
    documentType: pickup.description,
    recipientName: pickup.recipientName,
    locationName: pickup.locationName,
    status,
    deadlineAt: pickup.deadlineAt,
    createdAt: pickup.createdAt,
  };
}

export async function findRecipientByEmso(emso: string): Promise<PickupRecipient | null> {
  const response = await fetch(`${API_BASE}/recipient-lookup`, {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify({ emso }),
  });
  if (response.status === 404) return null;
  return readJson<PickupRecipient>(response);
}

export async function getAvailablePickupStations(): Promise<PickupStationOption[]> {
  return readJson<PickupStationOption[]>(
    await fetch(`${API_BASE}/stations`, { headers: authHeaders() }),
  );
}

export async function createPickup(request: CreatePickupRequest): Promise<Pickup> {
  const response = await fetch(API_BASE, {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify(request),
  });
  if (response.status === 404) throw new PickupServiceError("recipient_not_found");
  if (response.status === 403) {
    const problem = await response.json().catch(() => null);
    throw new PickupServiceError(
      problem?.type === "urn:eprevzem:pickups:station-forbidden"
        ? "station_forbidden"
        : "creation_forbidden",
    );
  }
  return toPickup(await readJson<BackendPickup>(response));
}

export async function getRecentPickups(limit = 10): Promise<PickupPage> {
  const pickups = await readJson<BackendPickup[]>(
    await fetch(`${API_BASE}?limit=${limit}`, { headers: authHeaders() }),
  );
  return { items: pickups.map(toPickup), total: pickups.length };
}

export async function deletePickup(pickupId: string): Promise<void> {
  const response = await fetch(`${API_BASE}/${pickupId}`, {
    method: "DELETE",
    headers: authHeaders(),
  });
  if (response.status === 409) throw new PickupServiceError("deletion_forbidden");
  if (!response.ok) throw new PickupServiceError("unknown");
}
