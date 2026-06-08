import { useCallback, useEffect, useState } from "react";
import { Calendar, FileText, Hourglass, Package } from "lucide-react";
import { useLocation } from "react-router-dom";
import type { DashboardStats } from "../types/dashboard";
import { getDashboardStats } from "../services/dashboardService";
import StatCard from "../components/dashboard/StatCard";
import PickupsTable from "../components/dashboard/PickupsTable";
import LockerOccupancyPanel from "../components/dashboard/LockerOccupancyPanel";

function StatCardSkeleton() {
  return (
    <div className="animate-pulse rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="mb-4 h-11 w-11 rounded-xl bg-slate-200" />
      <div className="mb-2 h-9 w-24 rounded-lg bg-slate-200" />
      <div className="h-4 w-36 rounded bg-slate-200" />
    </div>
  );
}

export default function DashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [statsError, setStatsError] = useState(false);
  const location = useLocation();

  const loadStats = useCallback(() => {
    getDashboardStats()
      .then((dashboardStats) => {
        setStats(dashboardStats);
        setStatsError(false);
      })
      .catch(() => setStatsError(true));
  }, []);

  useEffect(() => {
    loadStats();
  }, [loadStats]);

  return (
    <div className="space-y-5 p-6">
      {location.state?.pickupCreated && (
        <p className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-700">
          Prevzem je bil uspešno dodan in čaka na vložitev v paketomat.
        </p>
      )}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        {statsError ? (
          <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-600 md:col-span-2 xl:col-span-4">
            Statistike nadzorne plošče ni bilo mogoče naložiti.
          </p>
        ) : stats === null ? (
          Array.from({ length: 4 }).map((_, i) => <StatCardSkeleton key={i} />)
        ) : (
          <>
            <StatCard
              icon={<FileText size={22} className="text-emerald-600" />}
              iconBg="bg-emerald-50"
              value={stats.activePickups}
              label="Aktivni prevzemi"
              trend={`+${stats.activePickupsTrend}`}
            />
            <StatCard
              icon={<Hourglass size={22} className="text-amber-500" />}
              iconBg="bg-amber-50"
              value={stats.pendingPickups}
              label="Čakajoči prevzemi"
              subtitle={`${stats.pendingExpiresToday} poteče danes`}
            />
            <StatCard
              icon={<Package size={22} className="text-blue-500" />}
              iconBg="bg-blue-50"
              value={`${stats.occupiedLockers}/${stats.totalLockers}`}
              label="Zasedeni predalčki"
              indicator={`→ ${stats.totalLockers === 0 ? 0 : Math.round((stats.occupiedLockers / stats.totalLockers) * 100)} %`}
            />
            <StatCard
              icon={<Calendar size={22} className="text-indigo-500" />}
              iconBg="bg-indigo-50"
              value={stats.expiredThisWeek}
              label="Poteklo ta teden"
              subtitle="vrnjeno pošiljatelju"
            />
          </>
        )}
      </div>

      <div className="grid gap-4 xl:grid-cols-[1fr_360px]">
        <PickupsTable onPickupChanged={loadStats} />
        <LockerOccupancyPanel />
      </div>
    </div>
  );
}
