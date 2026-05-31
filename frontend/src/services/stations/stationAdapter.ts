import type {
  ClaimPickupStationRequest,
  OrganizationPickupStation,
  UpdatePickupStationLocationRequest,
} from "../../types/stations";

export type StationServiceErrorCode = "unknown_station" | "station_already_claimed";

export class StationServiceError extends Error {
  readonly code: StationServiceErrorCode;

  constructor(code: StationServiceErrorCode) {
    super(code);
    this.name = "StationServiceError";
    this.code = code;
  }
}

export interface StationAdapter {
  getStations(): Promise<OrganizationPickupStation[]>;
  getStation(claimId: string): Promise<OrganizationPickupStation>;
  createStation(request: ClaimPickupStationRequest): Promise<OrganizationPickupStation>;
  updateStation(
    claimId: string,
    request: UpdatePickupStationLocationRequest,
  ): Promise<OrganizationPickupStation>;
  deleteStation(claimId: string): Promise<void>;
}
