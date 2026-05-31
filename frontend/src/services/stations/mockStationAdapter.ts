import type { StationAdapter } from "./stationAdapter";
import { StationServiceError } from "./stationAdapter";
import type {
  ClaimPickupStationRequest,
  OrganizationPickupStation,
  StationLocker,
  StationLocation,
  UpdatePickupStationLocationRequest,
} from "../../types/stations";

const MOCK_DELAY_MS = 250;
const ORGANIZATION_ID = "1c26cbcb-cf13-4487-b035-623bf574cb9f";

interface MockStationCatalogEntry {
  stationId: string;
  serialNumber: string;
  createdAt: string;
  lockers: StationLocker[];
}

function makeLockers(stationNumber: number): StationLocker[] {
  return Array.from({ length: 10 }, (_, index) => ({
    id: `20000000-${stationNumber.toString().padStart(4, "0")}-4000-8000-${(index + 1).toString().padStart(12, "0")}`,
    lockerNumber: index + 1,
    isServiceable: true,
  }));
}

const stationCatalog: MockStationCatalogEntry[] = Array.from({ length: 10 }, (_, index) => {
  const stationNumber = index + 1;
  return {
    stationId: `10000000-0000-4000-8000-${stationNumber.toString().padStart(12, "0")}`,
    serialNumber: `EP-PM-${stationNumber.toString().padStart(3, "0")}`,
    createdAt: `2026-02-${(stationNumber + 9).toString().padStart(2, "0")}T08:00:00.000Z`,
    lockers: makeLockers(stationNumber),
  };
});

function getCatalogStation(serialNumber: string): MockStationCatalogEntry | undefined {
  return stationCatalog.find((station) => station.serialNumber === serialNumber);
}

function makeClaimedStation(
  serialNumber: string,
  claimId: string,
  location: StationLocation,
  claimedAt: string,
): OrganizationPickupStation {
  const station = getCatalogStation(serialNumber);
  if (!station) throw new Error(`Mock station '${serialNumber}' is not registered.`);

  return {
    claimId,
    stationId: station.stationId,
    organizationId: ORGANIZATION_ID,
    serialNumber: station.serialNumber,
    createdAt: station.createdAt,
    lockers: station.lockers,
    location,
    claimedAt,
    releasedAt: null,
  };
}

let stations: OrganizationPickupStation[] = [
  makeClaimedStation(
    "EP-PM-001",
    "3481748b-e5ab-4797-b488-32bcb907d893",
    {
      latitude: 46.5591,
      longitude: 15.6387,
      address: "Koroška cesta",
      houseNumber: "46",
      zipCode: "2000",
      city: "Maribor",
    },
    "2026-03-18T10:00:00.000Z",
  ),
  makeClaimedStation(
    "EP-PM-002",
    "989e6383-4a84-455f-bbf9-7c7493a05c7e",
    {
      latitude: 46.5576,
      longitude: 15.6459,
      address: "Ulica heroja Staneta",
      houseNumber: "1",
      zipCode: "2000",
      city: "Maribor",
    },
    "2026-02-25T11:45:00.000Z",
  ),
  makeClaimedStation(
    "EP-PM-003",
    "ab79fa50-e491-4717-b1d5-a0548a5d1555",
    {
      latitude: 46.5598,
      longitude: 15.645,
      address: "Gospejna ulica",
      houseNumber: "10",
      zipCode: "2000",
      city: "Maribor",
    },
    "2026-04-08T07:30:00.000Z",
  ),
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
    const serialNumber = request.serialNumber.trim();
    const catalogStation = getCatalogStation(serialNumber);
    if (!catalogStation) throw new StationServiceError("unknown_station");

    const isAlreadyClaimed = stations.some(
      (station) => station.serialNumber === serialNumber && station.releasedAt === null,
    );
    if (isAlreadyClaimed) throw new StationServiceError("station_already_claimed");

    const now = new Date().toISOString();
    const station: OrganizationPickupStation = {
      claimId: crypto.randomUUID(),
      stationId: catalogStation.stationId,
      organizationId: ORGANIZATION_ID,
      serialNumber: catalogStation.serialNumber,
      createdAt: catalogStation.createdAt,
      lockers: catalogStation.lockers,
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
