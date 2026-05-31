import type { StationAdapter } from "./stationAdapter";
import type {
  ClaimPickupStationRequest,
  OrganizationPickupStation,
  UpdatePickupStationLocationRequest,
} from "../../types/stations";
import { StationServiceError } from "./stationAdapter";

const API_BASE = `${import.meta.env.VITE_API_URL ?? ""}/api/org/stations`;

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

async function getStation(claimId: string): Promise<OrganizationPickupStation> {
  return readJson<OrganizationPickupStation>(
    await fetch(`${API_BASE}/${claimId}`, { headers: authHeaders() }),
  );
}

export const httpStationAdapter: StationAdapter = {
  async getStations() {
    return readJson<OrganizationPickupStation[]>(
      await fetch(API_BASE, { headers: authHeaders() }),
    );
  },

  async getStation(claimId) {
    return getStation(claimId);
  },

  async createStation(request: ClaimPickupStationRequest) {
    const response = await fetch(API_BASE, {
      method: "POST",
      headers: authHeaders(),
      body: JSON.stringify(request),
    });
    if (response.status === 404) throw new StationServiceError("unknown_station");
    if (response.status === 409) throw new StationServiceError("station_already_claimed");

    const claim = await readJson<{ claimId: string }>(response);
    return getStation(claim.claimId);
  },

  async updateStation(claimId, request: UpdatePickupStationLocationRequest) {
    return readJson<OrganizationPickupStation>(
      await fetch(`${API_BASE}/${claimId}`, {
        method: "PUT",
        headers: authHeaders(),
        body: JSON.stringify(request),
      }),
    );
  },

  async deleteStation(claimId) {
    const response = await fetch(`${API_BASE}/${claimId}`, {
      method: "DELETE",
      headers: authHeaders(),
    });
    if (!response.ok) throw response;
  },
};
