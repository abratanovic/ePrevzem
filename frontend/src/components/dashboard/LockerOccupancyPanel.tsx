import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Package } from "lucide-react";
import type { LockerStation } from "../../types/dashboard";
import { getLockerOccupancy } from "../../services/dashboardService";

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
  const [stations, setStations] = useState<LockerStation[] | null>(null);

  useEffect(() => {
    getLockerOccupancy().then(setStations);
  }, []);

  return (
    <div className="rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="px-5 pt-5 pb-3">
        <h2 className="text-base font-semibold text-slate-900">Zasedenost paketnikov</h2>
        <p className="text-xs text-slate-400">
          {stations ? `${stations.length} lokacije · ${stations.reduce((s, l) => s + l.total, 0)} predalčkov` : " "}
        </p>
      </div>

      {stations === null ? (
        <PanelSkeleton />
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

          <Link
            to="/paketniki"
            className="mt-2 flex w-full items-center justify-center gap-2 rounded-xl border border-slate-200 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
          >
            <Package size={16} className="text-accent" />
            Upravljaj paketnike
          </Link>
        </div>
      )}
    </div>
  );
}
