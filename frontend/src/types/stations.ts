export interface StationLocation {
  latitude: number;
  longitude: number;
  address: string;
  houseNumber: string;
  zipCode: string;
  city: string;
}

export interface StationLocker {
  id: string;
  lockerNumber: number;
  isServiceable: boolean;
}

export interface OrganizationPickupStation {
  claimId: string;
  stationId: string;
  organizationId: string;
  serialNumber: string;
  createdAt: string;
  lockers: StationLocker[];
  location: StationLocation;
  claimedAt: string;
  releasedAt: string | null;
}

export interface ClaimPickupStationRequest extends StationLocation {
  serialNumber: string;
}

export type UpdatePickupStationLocationRequest = StationLocation;
