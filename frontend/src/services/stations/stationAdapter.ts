import type {
  ClaimPickupStationRequest,
  OrganizationPickupStation,
  UpdatePickupStationLocationRequest,
} from "../../types/stations";

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
