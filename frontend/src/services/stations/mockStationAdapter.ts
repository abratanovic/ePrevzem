import type { StationAdapter } from "./stationAdapter";
import type {
  ClaimPickupStationRequest,
  OrganizationPickupStation,
  StationLocker,
  UpdatePickupStationLocationRequest,
} from "../../types/stations";

const MOCK_DELAY_MS = 250;
const ORGANIZATION_ID = "1c26cbcb-cf13-4487-b035-623bf574cb9f";

function makeLockers(count: number, unavailable: number[] = []): StationLocker[] {
  return Array.from({ length: count }, (_, index) => ({
    id: crypto.randomUUID(),
    lockerNumber: index + 1,
    isServiceable: !unavailable.includes(index + 1),
  }));
}

let stations: OrganizationPickupStation[] = [
  {
    claimId: "3481748b-e5ab-4797-b488-32bcb907d893",
    stationId: "8976fbbf-ff0c-426e-93b7-b53818d38a28",
    organizationId: ORGANIZATION_ID,
    serialNumber: "EP-FERI-001",
    createdAt: "2026-03-14T09:30:00.000Z",
    lockers: makeLockers(48, [17]),
    location: {
      latitude: 46.5591,
      longitude: 15.6387,
      address: "Koroška cesta",
      houseNumber: "46",
      zipCode: "2000",
      city: "Maribor",
    },
    claimedAt: "2026-03-18T10:00:00.000Z",
    releasedAt: null,
  },
  {
    claimId: "989e6383-4a84-455f-bbf9-7c7493a05c7e",
    stationId: "22012198-f8cc-437f-bcbd-b667c4a6fedb",
    organizationId: ORGANIZATION_ID,
    serialNumber: "EP-UEM-002",
    createdAt: "2026-02-20T08:15:00.000Z",
    lockers: makeLockers(36),
    location: {
      latitude: 46.5576,
      longitude: 15.6459,
      address: "Ulica heroja Staneta",
      houseNumber: "1",
      zipCode: "2000",
      city: "Maribor",
    },
    claimedAt: "2026-02-25T11:45:00.000Z",
    releasedAt: null,
  },
  {
    claimId: "ab79fa50-e491-4717-b1d5-a0548a5d1555",
    stationId: "71e4b055-476f-45d7-a765-4157b5ef9a29",
    organizationId: ORGANIZATION_ID,
    serialNumber: "EP-KNJ-003",
    createdAt: "2026-04-06T12:00:00.000Z",
    lockers: makeLockers(44, [3, 29]),
    location: {
      latitude: 46.5598,
      longitude: 15.645,
      address: "Gospejna ulica",
      houseNumber: "10",
      zipCode: "2000",
      city: "Maribor",
    },
    claimedAt: "2026-04-08T07:30:00.000Z",
    releasedAt: null,
  },
];

function wait(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, MOCK_DELAY_MS));
}

function findStation(claimId: string): OrganizationPickupStation {
  const station = stations.find((item) => item.claimId === claimId);
  if (!station) throw new Error("Paketomat ni bil najden.");
  return station;
}

export const mockStationAdapter: StationAdapter = {
  async getStations() {
    await wait();
    return stations.filter((station) => station.releasedAt === null);
  },

  async getStation(claimId) {
    await wait();
    return findStation(claimId);
  },

  async createStation(request: ClaimPickupStationRequest) {
    await wait();
    const now = new Date().toISOString();
    const station: OrganizationPickupStation = {
      claimId: crypto.randomUUID(),
      stationId: crypto.randomUUID(),
      organizationId: ORGANIZATION_ID,
      serialNumber: request.serialNumber,
      createdAt: now,
      lockers: [],
      location: {
        latitude: request.latitude,
        longitude: request.longitude,
        address: request.address,
        houseNumber: request.houseNumber,
        zipCode: request.zipCode,
        city: request.city,
      },
      claimedAt: now,
      releasedAt: null,
    };
    stations = [station, ...stations];
    return station;
  },

  async updateStation(claimId, request: UpdatePickupStationLocationRequest) {
    await wait();
    const current = findStation(claimId);
    const updated = { ...current, location: { ...request } };
    stations = stations.map((station) => station.claimId === claimId ? updated : station);
    return updated;
  },

  async deleteStation(claimId) {
    await wait();
    const current = findStation(claimId);
    const released = { ...current, releasedAt: new Date().toISOString() };
    stations = stations.map((station) => station.claimId === claimId ? released : station);
  },
};
