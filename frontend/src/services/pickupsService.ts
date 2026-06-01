import type {
  CreatePickupRequest,
  Pickup,
  PickupPage,
  PickupRecipient,
  PickupStationOption,
} from "../types/dashboard";

const STORAGE_KEY = "eprevzem_mock_pickups";
const MOCK_DELAY_MS = 300;

interface StoredPickup extends Pickup {
  createdAt: string;
}

interface MockRecipient extends PickupRecipient {
  emso: string;
}

const RECIPIENTS: MockRecipient[] = [
  { id: "citizen-1", emso: "0101006500006", firstName: "Ana", lastName: "Kovač" },
  { id: "citizen-2", emso: "0202006500004", firstName: "Marko", lastName: "Zupan" },
  { id: "citizen-3", emso: "0303006500002", firstName: "Eva", lastName: "Horvat" },
];

const STATIONS: PickupStationOption[] = [
  { id: "station-feri", name: "FERI - glavni vhod", address: "Koroška cesta 46, 2000 Maribor" },
  { id: "station-uemb", name: "UE Maribor - avla", address: "Ulica heroja Staneta 1, 2000 Maribor" },
  { id: "station-knj", name: "Knjižnica UM", address: "Gospejna ulica 10, 2000 Maribor" },
];

function wait(ms = MOCK_DELAY_MS) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function readStoredPickups(): StoredPickup[] {
  try {
    const value = sessionStorage.getItem(STORAGE_KEY);
    return value ? JSON.parse(value) : [];
  } catch {
    return [];
  }
}

function writeStoredPickups(pickups: StoredPickup[]) {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(pickups));
}

function generateReference(): string {
  const sequence = Math.floor(Math.random() * 1_000_000);
  return `EP-${new Date().getFullYear()}-${sequence.toString().padStart(6, "0")}`;
}

export async function findRecipientByEmso(emso: string): Promise<PickupRecipient | null> {
  await wait();
  const recipient = RECIPIENTS.find((candidate) => candidate.emso === emso);
  if (!recipient) return null;
  const { id, firstName, lastName } = recipient;
  return { id, firstName, lastName };
}

export async function getAvailablePickupStations(): Promise<PickupStationOption[]> {
  await wait(200);
  return STATIONS;
}

export async function createPickup(request: CreatePickupRequest): Promise<Pickup> {
  await wait();
  const recipient = RECIPIENTS.find((candidate) => candidate.emso === request.recipientEmso);
  const station = STATIONS.find((candidate) => candidate.id === request.targetPickupStationId);

  if (!recipient || !station) {
    throw new Error("Invalid mock pickup request.");
  }

  const pickup: StoredPickup = {
    id: crypto.randomUUID(),
    reference: generateReference(),
    documentType: request.description.trim(),
    recipientName: `${recipient.firstName} ${recipient.lastName}`,
    locationName: station.name,
    status: "awaitingPlacement",
    deadlineAt: null,
    createdAt: new Date().toISOString(),
  };

  writeStoredPickups([pickup, ...readStoredPickups()]);
  return pickup;
}

export async function getRecentPickups(page = 1, pageSize = 10): Promise<PickupPage> {
  await wait(150);
  const pickups = readStoredPickups();
  return {
    items: pickups.slice((page - 1) * pageSize, page * pageSize),
    total: pickups.length,
  };
}
