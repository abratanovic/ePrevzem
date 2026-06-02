export type PickupDisplayStatus =
  | "awaitingPlacement"
  | "ready"
  | "expiring"
  | "picked"
  | "expired"
  | "awaitingPersonalPickup"
  | "cancelled";

export interface DashboardStats {
  activePickups: number;
  activePickupsTrend: number;
  pendingPickups: number;
  pendingExpiresToday: number;
  occupiedLockers: number;
  totalLockers: number;
  expiredThisWeek: number;
}

export interface Pickup {
  id: string;
  reference: string;
  documentType: string;
  recipientName: string;
  locationName: string;
  status: PickupDisplayStatus;
  deadlineAt: string | null;
  createdAt?: string;
  canDelete: boolean;
  canCancel: boolean;
}

export interface PickupPage {
  items: Pickup[];
  total: number;
}

export interface LockerStation {
  id: string;
  stationId: string;
  name: string;
  used: number;
  total: number;
}

export interface PickupRecipient {
  id: string;
  firstName: string;
  lastName: string;
}

export interface PickupStationOption {
  id: string;
  name: string;
  address: string;
}

export interface CreatePickupRequest {
  recipientEmso: string;
  targetPickupStationId: string;
  description: string;
}
