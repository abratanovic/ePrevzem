export type PickupDisplayStatus = "ready" | "expiring" | "picked" | "expired";

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
  deadlineAt: string;
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
