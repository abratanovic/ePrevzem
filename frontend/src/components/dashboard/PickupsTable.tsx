import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { CheckCircle, Clock, Package, PackagePlus, Trash2, UserRoundCheck, XCircle } from "lucide-react";
import type { Pickup, PickupDisplayStatus, PickupPage } from "../../types/dashboard";
import { getRecentPickups } from "../../services/dashboardService";
import { cancelPickup, deletePickup, PickupServiceError } from "../../services/pickupsService";

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

interface PickupsTableProps {
  loadPickups?: () => Promise<PickupPage>;
  title?: string;
  subtitle?: string;
  showAllLink?: boolean;
  onPickupChanged?: () => void;
}

export default function PickupsTable({
  loadPickups = getRecentPickups,
  title = "Nedavni prevzemi",
  subtitle = "Zadnjih 10 ustvarjenih prevzemov",
  showAllLink = true,
  onPickupChanged,
}: PickupsTableProps) {
  const [data, setData] = useState<PickupPage | null>(null);
  const [error, setError] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [updatingId, setUpdatingId] = useState<string | null>(null);

  useEffect(() => {
    loadPickups().then(setData).catch(() => setError(true));
  }, [loadPickups]);

  async function handleDelete(row: Pickup) {
    if (!window.confirm(`Ali želite izbrisati prevzem ${row.reference}?`)) return;

    setDeleteError(null);
    setUpdatingId(row.id);
    try {
      await deletePickup(row.id);
      setData((current) => current === null
        ? current
        : {
            items: current.items.filter((pickup) => pickup.id !== row.id),
            total: current.total - 1,
          });
      onPickupChanged?.();
    } catch (deleteFailure) {
      setDeleteError(
        deleteFailure instanceof PickupServiceError && deleteFailure.code === "deletion_forbidden"
          ? "Izbrisati je mogoče samo prevzeme, ki še čakajo na vložitev v paketomat."
          : "Prevzema ni bilo mogoče izbrisati.",
      );
    } finally {
      setUpdatingId(null);
    }
  }

  async function handleCancel(row: Pickup) {
    if (!window.confirm(`Ali želite preklicati prevzem ${row.reference}? Zapis bo ostal v zgodovini.`)) return;

    setDeleteError(null);
    setUpdatingId(row.id);
    try {
      await cancelPickup(row.id);
      setData((current) => current === null
        ? current
        : {
            ...current,
            items: current.items.map((pickup) => pickup.id === row.id
              ? { ...pickup, status: "cancelled", canDelete: false, canCancel: false }
              : pickup),
          });
      onPickupChanged?.();
    } catch (cancelFailure) {
      setDeleteError(
        cancelFailure instanceof PickupServiceError && cancelFailure.code === "cancellation_forbidden"
          ? "Prevzema v trenutnem stanju ni mogoče preklicati."
          : "Prevzema ni bilo mogoče preklicati.",
      );
    } finally {
      setUpdatingId(null);
    }
  }

  return (
    <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="flex items-baseline justify-between border-b border-slate-100 px-5 py-4">
        <div>
          <h2 className="text-lg font-bold text-slate-900">{title}</h2>
          <p className="text-xs text-slate-400">{subtitle}</p>
        </div>
        {showAllLink && (
          <Link to="/prevzemi" className="text-sm font-medium text-accent hover:underline">
            Vsi prevzemi
          </Link>
        )}
      </div>

      {deleteError && (
        <p className="mx-5 mb-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{deleteError}</p>
      )}

      {error ? (
        <p className="px-5 py-8 text-center text-sm text-red-600">Prevzemov ni bilo mogoče naložiti.</p>
      ) : data === null ? (
        <TableSkeleton />
      ) : data.items.length === 0 ? (
        <p className="px-5 py-10 text-center text-sm text-slate-500">Organizacija še nima prevzemov.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-[920px] w-full text-sm">
            <thead>
              <tr className="bg-slate-50 text-left">
                {["REFERENCA", "DOKUMENT", "PREJEMNIK", "LOKACIJA", "STATUS", "ROK", "AKCIJE"].map((h) => (
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
                  <td className="px-5 py-3.5">
                    {row.canDelete ? (
                      <button
                        type="button"
                        onClick={() => void handleDelete(row)}
                        disabled={updatingId === row.id}
                        className="inline-flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50 disabled:cursor-wait disabled:opacity-50"
                        title="Izbriši prevzem"
                      >
                        <Trash2 size={14} />
                        {updatingId === row.id ? "Brisanje ..." : "Izbriši"}
                      </button>
                    ) : row.canCancel ? (
                      <button
                        type="button"
                        onClick={() => void handleCancel(row)}
                        disabled={updatingId === row.id}
                        className="inline-flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-medium text-orange-600 hover:bg-orange-50 disabled:cursor-wait disabled:opacity-50"
                        title="Prekliči prevzem"
                      >
                        <XCircle size={14} />
                        {updatingId === row.id ? "Preklic ..." : "Prekliči"}
                      </button>
                    ) : "—"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
