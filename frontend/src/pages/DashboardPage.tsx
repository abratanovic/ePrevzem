import { useEffect, useState } from "react";
import { Calendar, FileText, Hourglass, Package } from "lucide-react";
import type { DashboardStats } from "../types/dashboard";
import { getDashboardStats } from "../services/dashboardService";
import StatCard from "../components/dashboard/StatCard";

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

  useEffect(() => {
    getDashboardStats().then(setStats);
  }, []);

  return (
    <div className="p-6 space-y-4">
      <div className="grid grid-cols-4 gap-4">
        {stats === null ? (
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
              indicator={`→ ${Math.round((stats.occupiedLockers / stats.totalLockers) * 100)} %`}
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
    </div>
  );
}
