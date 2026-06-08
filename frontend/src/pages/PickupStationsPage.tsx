import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Eye, MapPin, Package, Pencil, Warehouse } from "lucide-react";
import type { OrganizationPickupStation } from "../types/stations";
import { stationService } from "../services/stations/stationService";
import { formatCoordinates, formatStationAddress, formatStationDate } from "../components/stations/stationFormatters";

function StatCard({ label, value, icon }: { label: string; value: number; icon: React.ReactNode }) {
  return (
    <div className="flex items-center gap-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">{icon}</div>
      <div>
        <p className="text-2xl font-extrabold tracking-tight text-slate-900">{value}</p>
        <p className="text-sm text-slate-500">{label}</p>
      </div>
    </div>
  );
}

function TableSkeleton() {
  return (
    <div className="animate-pulse space-y-3 p-5">
      {Array.from({ length: 3 }).map((_, index) => (
        <div key={index} className="h-12 rounded-xl bg-slate-100" />
      ))}
    </div>
  );
}

export default function PickupStationsPage() {
  const [stations, setStations] = useState<OrganizationPickupStation[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    stationService.getStations()
      .then(setStations)
      .catch(() => setError("Paketomatov ni bilo mogoče naložiti."));
  }, []);

  const totalLockers = stations?.reduce((sum, station) => sum + station.lockers.length, 0) ?? 0;
  const serviceableLockers = stations?.reduce(
    (sum, station) => sum + station.lockers.filter((locker) => locker.isServiceable).length,
    0,
  ) ?? 0;

  return (
    <div className="space-y-5 p-6">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <StatCard label="Aktivni paketomati" value={stations?.length ?? 0} icon={<Warehouse size={21} />} />
        <StatCard label="Vsi predalčki" value={totalLockers} icon={<Package size={21} />} />
        <StatCard label="Delujoči predalčki" value={serviceableLockers} icon={<Package size={21} />} />
      </div>

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 px-5 py-4">
          <h3 className="text-lg font-bold text-slate-900">Paketomati</h3>
          <p className="text-xs text-slate-400">Aktivne lokacije organizacije</p>
        </div>

        {error ? (
          <p className="px-5 py-8 text-center text-sm text-red-600">{error}</p>
        ) : stations === null ? (
          <TableSkeleton />
        ) : stations.length === 0 ? (
          <div className="flex flex-col items-center px-6 py-14 text-center">
            <MapPin size={32} className="mb-3 text-slate-300" />
            <h3 className="font-semibold text-slate-800">Organizacija še nima paketomatov</h3>
            <p className="mt-1 text-sm text-slate-500">Dodajte prvi paketomat.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-slate-50 text-left text-[11px] font-semibold tracking-wide text-slate-400">
                  <th className="px-5 py-3">SERIJSKA ŠTEVILKA</th>
                  <th className="px-5 py-3">LOKACIJA</th>
                  <th className="px-5 py-3">KOORDINATE</th>
                  <th className="px-5 py-3">PREDALČKI</th>
                  <th className="px-5 py-3">DODANO</th>
                  <th className="px-5 py-3 text-right">AKCIJE</th>
                </tr>
              </thead>
              <tbody>
                {stations.map((station) => {
                  const serviceable = station.lockers.filter((locker) => locker.isServiceable).length;
                  return (
                    <tr key={station.claimId} className="border-t border-slate-100 hover:bg-slate-50">
                      <td className="px-5 py-4 font-semibold text-slate-900">{station.serialNumber}</td>
                      <td className="px-5 py-4 text-slate-600">{formatStationAddress(station.location)}</td>
                      <td className="px-5 py-4 font-mono text-xs text-slate-500">{formatCoordinates(station.location)}</td>
                      <td className="px-5 py-4 text-slate-600">{serviceable}/{station.lockers.length} delujočih</td>
                      <td className="px-5 py-4 text-slate-500">{formatStationDate(station.claimedAt)}</td>
                      <td className="px-5 py-4">
                        <div className="flex justify-end gap-2">
                          <Link to={`/paketniki/${station.claimId}`} className="flex items-center gap-1.5 rounded-lg border border-slate-200 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-white">
                            <Eye size={14} />
                            Pregled
                          </Link>
                          <Link to={`/paketniki/${station.claimId}/uredi`} className="flex items-center gap-1.5 rounded-lg border border-slate-200 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-white">
                            <Pencil size={14} />
                            Uredi
                          </Link>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
