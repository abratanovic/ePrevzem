import type { DashboardStats, Pickup, PickupPage, LockerStation, PickupDisplayStatus } from "../types/dashboard";

const EXPIRING_THRESHOLD_MS = 24 * 60 * 60 * 1000;

type RawStatus = "InLocker" | "PickedUp" | "Expired";

interface RawPickup {
  id: string;
  reference: string;
  documentType: string;
  recipientName: string;
  locationName: string;
  rawStatus: RawStatus;
  deadlineAt: string;
}

function toDisplayStatus(raw: RawPickup): PickupDisplayStatus {
  if (raw.rawStatus === "PickedUp") return "picked";
  if (raw.rawStatus === "Expired") return "expired";
  const msUntilDeadline = new Date(raw.deadlineAt).getTime() - Date.now();
  return msUntilDeadline <= EXPIRING_THRESHOLD_MS ? "expiring" : "ready";
}

const RAW_PICKUPS: RawPickup[] = [
  {
    id: "1",
    reference: "EP-2026-000123",
    documentType: "Osebna izkaznica",
    recipientName: "Ana Kovač",
    locationName: "FERI – glavni vhod",
    rawStatus: "InLocker",
    deadlineAt: new Date(Date.now() + 19 * 24 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: "2",
    reference: "EP-2026-000119",
    documentType: "Potni list",
    recipientName: "Marko Zupan",
    locationName: "UE Maribor – avla",
    rawStatus: "InLocker",
    deadlineAt: new Date(Date.now() + 12 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: "3",
    reference: "EP-2026-000118",
    documentType: "Vozniško dovoljenje",
    recipientName: "Eva Horvat",
    locationName: "FERI – glavni vhod",
    rawStatus: "PickedUp",
    deadlineAt: new Date(Date.now() - 18 * 24 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: "4",
    reference: "EP-2026-000112",
    documentType: "Diploma",
    recipientName: "Luka Krajnc",
    locationName: "Knjižnica UM",
    rawStatus: "InLocker",
    deadlineAt: new Date(Date.now() + 21 * 24 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: "5",
    reference: "EP-2026-000108",
    documentType: "Potrdilo o bivanju",
    recipientName: "Nina Mlakar",
    locationName: "UE Maribor – avla",
    rawStatus: "Expired",
    deadlineAt: new Date(Date.now() - 21 * 24 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: "6",
    reference: "EP-2026-000104",
    documentType: "Osebna izkaznica",
    recipientName: "Tjaša Bizjak",
    locationName: "FERI – glavni vhod",
    rawStatus: "PickedUp",
    deadlineAt: new Date(Date.now() - 23 * 24 * 60 * 60 * 1000).toISOString(),
  },
];

export async function getDashboardStats(): Promise<DashboardStats> {
  await new Promise((r) => setTimeout(r, 400));
  return {
    activePickups: 248,
    activePickupsTrend: 12,
    pendingPickups: 36,
    pendingExpiresToday: 6,
    occupiedLockers: 72,
    totalLockers: 120,
    expiredThisWeek: 5,
  };
}

export async function getRecentPickups(page = 1, pageSize = 10): Promise<PickupPage> {
  await new Promise((r) => setTimeout(r, 500));
  const items: Pickup[] = RAW_PICKUPS.slice((page - 1) * pageSize, page * pageSize).map(
    ({ rawStatus, ...rest }) => ({ ...rest, status: toDisplayStatus({ rawStatus, ...rest }) }),
  );
  return { items, total: RAW_PICKUPS.length };
}

export async function getLockerOccupancy(): Promise<LockerStation[]> {
  await new Promise((r) => setTimeout(r, 350));
  return [
    { id: "1", stationId: "FERI-01", name: "FERI – glavni vhod", used: 28, total: 48 },
    { id: "2", stationId: "UEM-01", name: "UE Maribor – avla", used: 31, total: 36 },
    { id: "3", stationId: "KNJ-01", name: "Knjižnica UM", used: 13, total: 44 },
  ];
}
