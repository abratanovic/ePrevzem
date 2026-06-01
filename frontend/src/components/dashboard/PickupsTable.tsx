import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { CheckCircle, Clock, Package, PackagePlus, UserRoundCheck, XCircle } from "lucide-react";
import type { Pickup, PickupDisplayStatus, PickupPage } from "../../types/dashboard";
import { getRecentPickups } from "../../services/dashboardService";

const SL_MONTHS = ["jan", "feb", "mar", "apr", "maj", "jun", "jul", "avg", "sep", "okt", "nov", "dec"];

function formatDeadline(iso: string | null): string {
  if (!iso) return "—";
  const d = new Date(iso);
  return `${d.getDate()}. ${SL_MONTHS[d.getMonth()]} ${d.getFullYear()}`;
}

const STATUS_CONFIG: Record<PickupDisplayStatus, { label: string; icon: typeof CheckCircle; className: string }> = {
  awaitingPlacement: { label: "Čaka na vložitev", icon: PackagePlus, className: "bg-slate-100 text-slate-600" },
  ready:    { label: "Prijavljen",   icon: CheckCircle, className: "bg-green-100 text-green-700" },
  expiring: { label: "Poteče kmalu", icon: Clock,        className: "bg-orange-100 text-orange-600" },
  picked:   { label: "Prevzeto",     icon: Package,      className: "bg-blue-100 text-blue-600" },
  expired:  { label: "Poteklo",      icon: XCircle,      className: "bg-red-100 text-red-600" },
  awaitingPersonalPickup: { label: "Čaka osebni prevzem", icon: UserRoundCheck, className: "bg-indigo-100 text-indigo-600" },
  cancelled: { label: "Preklicano", icon: XCircle, className: "bg-slate-100 text-slate-500" },
};

function StatusBadge({ status }: { status: PickupDisplayStatus }) {
  const { label, icon: Icon, className } = STATUS_CONFIG[status];
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium ${className}`}>
      <Icon size={12} strokeWidth={2} />
      {label}
    </span>
  );
}

function TableSkeleton() {
  return (
    <div className="animate-pulse space-y-3 p-4 pt-0">
      {Array.from({ length: 6 }).map((_, i) => (
        <div key={i} className="flex gap-4 py-3">
          <div className="h-4 w-28 rounded bg-slate-200" />
          <div className="h-4 w-36 rounded bg-slate-200" />
          <div className="h-4 w-24 rounded bg-slate-200" />
          <div className="h-4 w-32 rounded bg-slate-200" />
          <div className="h-6 w-24 rounded-full bg-slate-200" />
          <div className="h-4 w-20 rounded bg-slate-200" />
        </div>
      ))}
    </div>
  );
}

export default function PickupsTable() {
  const [data, setData] = useState<PickupPage | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    getRecentPickups().then(setData).catch(() => setError(true));
  }, []);

  return (
    <div className="rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="flex items-baseline justify-between px-5 pt-5 pb-3">
        <div>
          <h2 className="text-lg font-bold text-slate-900">Nedavni prevzemi</h2>
          <p className="text-xs text-slate-400">Zadnjih 30 dni</p>
        </div>
        <Link to="/prevzemi" className="text-sm font-medium text-accent hover:underline">
          Vsi prevzemi
        </Link>
      </div>

      {error ? (
        <p className="px-5 py-8 text-center text-sm text-red-600">Prevzemov ni bilo mogoče naložiti.</p>
      ) : data === null ? (
        <TableSkeleton />
      ) : data.items.length === 0 ? (
        <p className="px-5 py-10 text-center text-sm text-slate-500">Organizacija še nima prevzemov.</p>
      ) : (
        <table className="w-full text-sm">
          <thead>
            <tr className="border-t border-slate-100">
              {["REFERENCA", "DOKUMENT", "PREJEMNIK", "LOKACIJA", "STATUS", "ROK"].map((h) => (
                <th key={h} className="px-5 py-2.5 text-left text-[11px] font-semibold tracking-wide text-slate-400">
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.items.map((row: Pickup) => (
              <tr key={row.id} className="border-t border-slate-100 hover:bg-slate-50">
                <td className="px-5 py-3.5 text-sm text-slate-400">{row.reference}</td>
                <td className="px-5 py-3.5 font-semibold text-slate-900">{row.documentType}</td>
                <td className="px-5 py-3.5 text-slate-600">{row.recipientName}</td>
                <td className="px-5 py-3.5 text-slate-600">{row.locationName}</td>
                <td className="px-5 py-3.5">
                  <StatusBadge status={row.status} />
                </td>
                <td className={`px-5 py-3.5 ${row.status === "expiring" ? "font-medium text-orange-600" : "text-slate-500"}`}>
                  {formatDeadline(row.deadlineAt)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
