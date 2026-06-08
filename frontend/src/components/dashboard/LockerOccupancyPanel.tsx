import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Package } from "lucide-react";
import type { LockerStation } from "../../types/dashboard";
import { getLockerOccupancy } from "../../services/dashboardService";
import { useAuth } from "../../contexts/useAuth";

const HIGH_OCCUPANCY_THRESHOLD = 0.85;

function OccupancyBar({ used, total }: { used: number; total: number }) {
  const ratio = used / total;
  const isHigh = ratio >= HIGH_OCCUPANCY_THRESHOLD;
  const pct = Math.round(ratio * 100);
  return (
    <div className="h-2 w-full overflow-hidden rounded-full bg-slate-100">
      <div
        className={`h-full rounded-full transition-all ${isHigh ? "bg-orange-400" : "bg-accent"}`}
        style={{ width: `${pct}%` }}
      />
    </div>
  );
}

function PanelSkeleton() {
  return (
    <div className="animate-pulse space-y-5 p-5 pt-0">
      {Array.from({ length: 3 }).map((_, i) => (
        <div key={i} className="space-y-2">
          <div className="flex justify-between">
            <div className="h-4 w-36 rounded bg-slate-200" />
            <div className="h-4 w-12 rounded bg-slate-200" />
          </div>
          <div className="h-2 w-full rounded-full bg-slate-200" />
        </div>
      ))}
      <div className="h-10 w-full rounded-xl bg-slate-200" />
    </div>
  );
}

export default function LockerOccupancyPanel() {
  const { user } = useAuth();
  const [stations, setStations] = useState<LockerStation[] | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    getLockerOccupancy().then(setStations).catch(() => setError(true));
  }, []);

  return (
    <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="border-b border-slate-100 px-5 py-4">
        <h2 className="text-lg font-bold text-slate-900">Zasedenost paketnikov</h2>
        <p className="text-xs text-slate-400">
          {stations ? `${stations.length} lokacije · ${stations.reduce((s, l) => s + l.total, 0)} predalčkov` : " "}
        </p>
      </div>

      {error ? (
        <p className="px-5 pb-5 text-sm text-red-600">Zasedenosti paketnikov ni bilo mogoče naložiti.</p>
      ) : stations === null ? (
        <PanelSkeleton />
      ) : stations.length === 0 ? (
        <p className="px-5 pb-5 text-sm text-slate-500">Organizacija še nima aktivnih paketnikov.</p>
      ) : (
        <div className="px-5 pb-5 space-y-4">
          {stations.map((station) => {
            const isHigh = station.used / station.total >= HIGH_OCCUPANCY_THRESHOLD;
            return (
              <div key={station.id} className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium text-slate-700">{station.name}</span>
                  <span className={`text-sm font-semibold tabular-nums ${isHigh ? "text-orange-500" : "text-slate-600"}`}>
                    {station.used}/{station.total}
                  </span>
                </div>
                <OccupancyBar used={station.used} total={station.total} />
              </div>
            );
          })}

          {user?.role === "OrganizationAdmin" && (
            <Link
              to="/paketniki"
              className="mt-2 flex w-full items-center justify-center gap-2 rounded-xl border border-slate-200 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
            >
              <Package size={16} className="text-accent" />
              Upravljaj paketnike
            </Link>
          )}
        </div>
      )}
    </div>
  );
}
