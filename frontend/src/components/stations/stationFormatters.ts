import type { StationLocation } from "../../types/stations";

export function formatStationAddress(location: StationLocation): string {
  return `${location.address} ${location.houseNumber}, ${location.zipCode} ${location.city}`;
}

export function formatStationDate(iso: string): string {
  return new Intl.DateTimeFormat("sl-SI", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(new Date(iso));
}

export function formatCoordinates(location: StationLocation): string {
  return `${location.latitude.toFixed(6)}, ${location.longitude.toFixed(6)}`;
}
