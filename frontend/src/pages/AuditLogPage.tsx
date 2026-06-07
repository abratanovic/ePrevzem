import { useCallback, useEffect, useMemo, useState } from "react";
import {
  AlertCircle,
  CalendarDays,
  ClipboardList,
  FileText,
  Filter,
  Loader2,
  MapPin,
  RefreshCw,
  ShieldCheck,
} from "lucide-react";
import { getAuditActors, getAuditLog } from "../services/auditLogService";
import type {
  AuditAction,
  AuditActorOption,
  AuditLogDetails,
  AuditLogEntry,
  AuditLogFilters,
  AuditTargetKind,
} from "../types/auditLog";

const SL_MONTHS = ["jan", "feb", "mar", "apr", "maj", "jun", "jul", "avg", "sep", "okt", "nov", "dec"];

const ACTION_OPTIONS: Array<{ value: AuditAction; label: string }> = [
  { value: "PackageCreated", label: "Prevzem ustvarjen" },
  { value: "PackagePlaced", label: "Prevzem vložen" },
  { value: "PackagePickedUpByCitizen", label: "Prevzem prevzet" },
  { value: "PackageRemovedByEmployee", label: "Prevzem odstranjen" },
  { value: "PackageExpired", label: "Prevzem potekel" },
  { value: "PackageCancelled", label: "Prevzem preklican" },
  { value: "PackageDeleted", label: "Prevzem izbrisan" },
  { value: "ProvisioningCodeIssued", label: "Koda izdana" },
  { value: "ProvisioningCodeRedeemed", label: "Koda uporabljena" },
  { value: "EmployeeAccountCreated", label: "Zaposleni dodan" },
  { value: "EmployeeAccountDisabled", label: "Zaposleni onemogočen" },
  { value: "EmployeeAccountReenabled", label: "Zaposleni omogočen" },
  { value: "EmployeeAccountRoleGranted", label: "Vloga dodeljena" },
  { value: "EmployeeAccountRoleRevoked", label: "Vloga odvzeta" },
  { value: "LockerServiceabilityChanged", label: "Paketnik servisiran" },
  { value: "StationClaimed", label: "Paketomat prevzet" },
  { value: "StationReleased", label: "Paketomat sproščen" },
];

const TARGET_OPTIONS: Array<{ value: AuditTargetKind; label: string }> = [
  { value: "Package", label: "Prevzem" },
  { value: "ProvisioningCode", label: "Provisioning koda" },
  { value: "EmployeeAccount", label: "Zaposleni" },
  { value: "OrganizationAdminAccount", label: "Skrbnik" },
  { value: "Locker", label: "Predalček" },
  { value: "PickupStation", label: "Paketomat" },
  { value: "StationClaim", label: "Paketomat" },
  { value: "Organization", label: "Organizacija" },
  { value: "Delegation", label: "Pooblastilo" },
];

const TARGET_LABELS: Record<string, string> = {
  Package: "Prevzem",
  Placement: "Vložitev",
  Delegation: "Pooblastilo",
  EmployeeAccount: "Zaposleni",
  OrganizationAdminAccount: "Skrbnik",
  SystemAdmin: "Sistemski skrbnik",
  EmployeeDevice: "Naprava zaposlenega",
  CitizenUser: "Državljan",
  CitizenDevice: "Naprava državljana",
  CitizenActivationCode: "Aktivacijska koda",
  RefreshToken: "Varnostna seja",
  Locker: "Predalček",
  Organization: "Organizacija",
  PickupStation: "Paketomat",
  StationClaim: "Paketomat",
  ProvisioningCode: "Provisioning koda",
};

const ACTION_LABELS: Record<string, string> = {
  PackageCreated: "Prevzem ustvarjen",
  PackagePlaced: "Prevzem vložen",
  PackagePickedUpByCitizen: "Prevzem prevzet",
  PackageRemovedByEmployee: "Prevzem odstranjen",
  PackageExpired: "Prevzem potekel",
  PackageRetrievedAfterExpiry: "Prevzem vrnjen po poteku",
  PackageMarkedPickedUpManually: "Prevzem ročno zaključen",
  PackageCancelled: "Prevzem preklican",
  PackageDeleted: "Prevzem izbrisan",
  DelegationCreated: "Pooblastilo ustvarjeno",
  DelegationRevoked: "Pooblastilo preklicano",
  DelegationUsedAtPickup: "Pooblastilo uporabljeno",
  ProvisioningCodeIssued: "Koda izdana",
  ProvisioningCodeRedeemed: "Koda uporabljena",
  EmployeeAccountCreated: "Zaposleni dodan",
  EmployeeAccountDisabled: "Zaposleni onemogočen",
  EmployeeAccountReenabled: "Zaposleni omogočen",
  EmployeeAccountLoggedIn: "Prijava zaposlenega",
  EmployeePasswordChanged: "Geslo zaposlenega spremenjeno",
  EmployeeAccountRoleGranted: "Vloga dodeljena",
  EmployeeAccountRoleRevoked: "Vloga odvzeta",
  EmployeeStationAccessGranted: "Dostop do paketomata dodeljen",
  EmployeeStationAccessRevoked: "Dostop do paketomata odvzet",
  EmployeeDeviceRegistered: "Naprava zaposlenega dodana",
  EmployeeDeviceRevoked: "Naprava zaposlenega preklicana",
  CitizenActivationCodeIssued: "Aktivacijska koda izdana",
  CitizenDeviceRegistered: "Naprava državljana dodana",
  CitizenDeviceRevoked: "Naprava državljana preklicana",
  OrganizationAdminAccountCreated: "Skrbnik dodan",
  OrganizationAdminAccountDisabled: "Skrbnik onemogočen",
  OrganizationAdminAccountReenabled: "Skrbnik omogočen",
  OrganizationAdminLoggedIn: "Prijava skrbnika",
  OrganizationAdminPasswordChanged: "Geslo skrbnika spremenjeno",
  CitizenOnboarded: "Državljan registriran",
  OrganizationCreated: "Organizacija ustvarjena",
  SystemAdminLoggedIn: "Prijava sistemskega skrbnika",
  SystemAdminLoginFailed: "Neuspešna prijava sistemskega skrbnika",
  SystemAdminPasswordChanged: "Geslo sistemskega skrbnika spremenjeno",
  RefreshTokenRotated: "Varnostna seja obnovljena",
  RefreshTokenChainRevoked: "Varnostna seja preklicana",
  PickupStationCreated: "Paketomat ustvarjen",
  StationClaimed: "Paketomat prevzet",
  StationReleased: "Paketomat sproščen",
  LockerCreated: "Predalček ustvarjen",
  LockerServiceabilityChanged: "Servisnost predalčka spremenjena",
  LockerOpened: "Predalček odprt",
};

type BadgeTone = "success" | "info" | "warning" | "danger" | "neutral";

function actionTone(action: AuditAction): BadgeTone {
  if (
    action === "PackagePickedUpByCitizen" ||
    action === "PackageMarkedPickedUpManually" ||
    action === "ProvisioningCodeRedeemed" ||
    action === "EmployeeAccountCreated" ||
    action === "EmployeeAccountReenabled" ||
    action === "StationClaimed"
  ) return "success";

  if (
    action === "PackageExpired" ||
    action === "PackageRemovedByEmployee" ||
    action === "PackageRetrievedAfterExpiry" ||
    action === "LockerServiceabilityChanged"
  ) return "warning";

  if (
    action === "PackageCancelled" ||
    action === "PackageDeleted" ||
    action === "EmployeeAccountDisabled" ||
    action === "EmployeeDeviceRevoked" ||
    action === "StationReleased"
  ) return "danger";

  if (action.startsWith("Package") || action.includes("Code") || action.includes("Locker")) return "info";
  return "neutral";
}

function badgeClass(tone: BadgeTone): string {
  switch (tone) {
    case "success": return "bg-emerald-50 text-emerald-700 ring-emerald-200";
    case "info": return "bg-blue-50 text-blue-700 ring-blue-200";
    case "warning": return "bg-amber-50 text-amber-700 ring-amber-200";
    case "danger": return "bg-red-50 text-red-700 ring-red-200";
    case "neutral": return "bg-slate-100 text-slate-600 ring-slate-200";
  }
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return `${d.getDate()}. ${SL_MONTHS[d.getMonth()]} ${d.getFullYear()}, ${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
}

function toStartOfDay(date: string): string | undefined {
  return date ? new Date(`${date}T00:00:00`).toISOString() : undefined;
}

function toEndOfDay(date: string): string | undefined {
  return date ? new Date(`${date}T23:59:59.999`).toISOString() : undefined;
}

function labelFor(value: string, labels: Record<string, string>): string {
  return labels[value] ?? value;
}

function shortId(id: string): string {
  return id.length > 8 ? `${id.slice(0, 8)}...` : id;
}

function actorOptionLabel(actor: AuditActorOption): string {
  const displayName = actor.displayName?.trim();
  const email = actor.email?.trim();

  if (displayName && email) return `${displayName} (${email})`;
  if (displayName) return displayName;
  if (email) return email;
  return `${actor.actorKind} ${shortId(actor.actorId)}`;
}

function actorIdFor(entry: AuditLogEntry): string | null {
  switch (entry.actorKind) {
    case "Citizen":
      return entry.actorCitizenUserId;
    case "Employee":
      return entry.actorEmployeeAccountId;
    case "OrganizationAdmin":
      return entry.actorOrganizationAdminAccountId;
    case "SystemAdmin":
      return entry.actorSystemAdminId;
    default:
      return entry.actorCitizenUserId
        ?? entry.actorEmployeeAccountId
        ?? entry.actorOrganizationAdminAccountId
        ?? entry.actorSystemAdminId;
  }
}

function actorOptionFromEntry(entry: AuditLogEntry): AuditActorOption | null {
  const actorId = actorIdFor(entry);
  if (!actorId || entry.actorKind === "System") {
    return null;
  }

  return {
    actorKind: entry.actorKind,
    actorId,
    displayName: entry.actorDisplayName,
    email: entry.actorEmail,
  };
}

function mergeActorOptions(
  primary: AuditActorOption[],
  fallbackEntries: AuditLogEntry[] | null,
): AuditActorOption[] {
  const byKey = new Map<string, AuditActorOption>();

  const add = (actor: AuditActorOption) => {
    byKey.set(`${actor.actorKind}:${actor.actorId}`, actor);
  };

  primary.forEach(add);
  fallbackEntries
    ?.map(actorOptionFromEntry)
    .filter((actor): actor is AuditActorOption => actor !== null)
    .forEach(add);

  return [...byKey.values()].sort((a, b) =>
    actorOptionLabel(a).localeCompare(actorOptionLabel(b), "sl"),
  );
}

function contextualTargetFor(entry: AuditLogEntry): { label: string; id: string | null } {
  switch (entry.action) {
    case "EmployeeAccountLoggedIn":
    case "OrganizationAdminLoggedIn":
    case "SystemAdminLoggedIn":
    case "SystemAdminLoginFailed":
      return { label: "Portal", id: null };
    case "EmployeePasswordChanged":
    case "OrganizationAdminPasswordChanged":
    case "SystemAdminPasswordChanged":
      return { label: "Račun", id: null };
    case "RefreshTokenRotated":
    case "RefreshTokenChainRevoked":
      return { label: "Varnostna seja", id: null };
    default:
      return { label: labelFor(entry.targetKind, TARGET_LABELS), id: entry.targetId };
  }
}

function DetailLine({ icon, text }: { icon: React.ReactNode; text: string }) {
  return (
    <span className="inline-flex min-w-0 items-center gap-1.5 text-slate-500">
      <span className="shrink-0 text-slate-400">{icon}</span>
      <span className="truncate">{text}</span>
    </span>
  );
}

function DetailsCell({ details }: { details: AuditLogDetails | null }) {
  const values = [
    details?.documentTitle,
    details?.organizationName,
    details?.lockerLabel,
    details?.location,
  ].filter((value): value is string => Boolean(value));

  if (values.length === 0) {
    return <span className="text-slate-400">Brez dodatnih podatkov</span>;
  }

  return (
    <div className="flex max-w-[420px] flex-wrap gap-x-3 gap-y-1">
      {details?.documentTitle && <DetailLine icon={<FileText size={13} />} text={details.documentTitle} />}
      {details?.organizationName && <DetailLine icon={<ShieldCheck size={13} />} text={details.organizationName} />}
      {details?.lockerLabel && <DetailLine icon={<ClipboardList size={13} />} text={details.lockerLabel} />}
      {details?.location && <DetailLine icon={<MapPin size={13} />} text={details.location} />}
    </div>
  );
}

function ActorCell({ entry }: { entry: AuditLogEntry }) {
  const displayName = entry.actorDisplayName?.trim();
  const email = entry.actorEmail?.trim();
  const actorId = actorIdFor(entry);
  const fallbackIdentity = actorId ? shortId(actorId) : null;
  const identity = displayName || fallbackIdentity || "Sistem";
  const identityTitle = displayName || actorId || identity;

  return (
    <div className="max-w-[210px]">
      <div className="truncate font-medium text-slate-700" title={identityTitle}>
        {identity}
      </div>
      {email && (
        <div className="mt-0.5 truncate text-xs text-slate-400" title={email}>
          {email}
        </div>
      )}
    </div>
  );
}

function TargetCell({ entry }: { entry: AuditLogEntry }) {
  const target = contextualTargetFor(entry);

  return (
    <div>
      <div className="font-medium text-slate-700">{target.label}</div>
      {target.id && (
        <div className="mt-0.5 max-w-[150px] truncate text-xs text-slate-400" title={target.id}>
          {target.id}
        </div>
      )}
    </div>
  );
}

function TableSkeleton() {
  return (
    <div className="animate-pulse space-y-3 p-5">
      {Array.from({ length: 8 }).map((_, i) => (
        <div key={i} className="grid grid-cols-[150px_190px_210px_150px_1fr] gap-4 border-b border-slate-100 pb-3">
          <div className="h-4 rounded bg-slate-200" />
          <div className="h-6 rounded-full bg-slate-200" />
          <div className="space-y-1">
            <div className="h-4 rounded bg-slate-200" />
            <div className="h-3 w-3/4 rounded bg-slate-200" />
          </div>
          <div className="h-4 rounded bg-slate-200" />
          <div className="h-4 rounded bg-slate-200" />
        </div>
      ))}
    </div>
  );
}

export default function AuditLogPage() {
  const [entries, setEntries] = useState<AuditLogEntry[] | null>(null);
  const [actors, setActors] = useState<AuditActorOption[]>([]);
  const [loadError, setLoadError] = useState(false);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState({
    limit: "50",
    from: "",
    to: "",
    action: "",
    targetKind: "",
    actorKind: "",
    actorId: "",
  });

  const queryFilters: AuditLogFilters = useMemo(() => ({
    limit: Number(filters.limit),
    from: toStartOfDay(filters.from),
    to: toEndOfDay(filters.to),
    action: filters.action as AuditAction | "",
    targetKind: filters.targetKind as AuditTargetKind | "",
  }), [filters.action, filters.from, filters.limit, filters.targetKind, filters.to]);

  const actorOptions = useMemo(
    () => mergeActorOptions(actors, entries),
    [actors, entries],
  );

  const displayedEntries = useMemo(() => {
    if (!filters.actorKind || !filters.actorId) {
      return entries;
    }

    return entries?.filter((entry) =>
      entry.actorKind === filters.actorKind && actorIdFor(entry) === filters.actorId,
    ) ?? null;
  }, [entries, filters.actorId, filters.actorKind]);

  const loadEntries = useCallback(async () => {
    try {
      setEntries(await getAuditLog(queryFilters));
      setLoadError(false);
    } catch {
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, [queryFilters]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadEntries();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [loadEntries]);

  useEffect(() => {
    getAuditActors()
      .then(setActors)
      .catch(() => setActors([]));
  }, []);

  const resetFilters = () => {
    setLoading(true);
    setLoadError(false);
    setFilters({ limit: "50", from: "", to: "", action: "", targetKind: "", actorKind: "", actorId: "" });
  };

  return (
    <div className="space-y-5 p-6">
      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <div className="mb-2 flex h-10 w-10 items-center justify-center rounded-xl bg-accent/10 text-accent">
              <ClipboardList size={21} strokeWidth={1.8} />
            </div>
            <h2 className="text-xl font-bold text-slate-900">Revizijska sled</h2>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              Pregled zabeleženih sprememb, prevzemov, paketomatov in uporabniških dogodkov v vaši organizaciji.
            </p>
          </div>
          <button
            type="button"
            onClick={() => {
              setLoading(true);
              setLoadError(false);
              void loadEntries();
            }}
            disabled={loading}
            className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3.5 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:cursor-wait disabled:opacity-60"
          >
            {loading ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
            Osveži
          </button>
        </div>

        <div className="mt-5 grid grid-cols-1 items-end gap-3 md:grid-cols-2 xl:grid-cols-[1.05fr_1fr_1fr_150px_150px_110px_auto]">
          <label className="space-y-1">
            <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">Dejanje</span>
            <select
              value={filters.action}
              onChange={(e) => {
                setLoading(true);
                setLoadError(false);
                setFilters((current) => ({ ...current, action: e.target.value }));
              }}
              className="h-10 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-accent focus:ring-2 focus:ring-accent/10"
            >
              <option value="">Vsa dejanja</option>
              {ACTION_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </label>

          <label className="space-y-1">
            <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">Tarča</span>
            <select
              value={filters.targetKind}
              onChange={(e) => {
                setLoading(true);
                setLoadError(false);
                setFilters((current) => ({ ...current, targetKind: e.target.value }));
              }}
              className="h-10 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-accent focus:ring-2 focus:ring-accent/10"
            >
              <option value="">Vse tarče</option>
              {TARGET_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </label>

          <label className="space-y-1">
            <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">Akter</span>
            <select
              value={filters.actorKind && filters.actorId ? `${filters.actorKind}:${filters.actorId}` : ""}
              onChange={(e) => {
                const [actorKind = "", actorId = ""] = e.target.value.split(":");
                setLoadError(false);
                setFilters((current) => ({ ...current, actorKind, actorId }));
              }}
              className="h-10 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-accent focus:ring-2 focus:ring-accent/10"
            >
              <option value="">Vsi akterji</option>
              {actorOptions.map((actor) => (
                <option
                  key={`${actor.actorKind}:${actor.actorId}`}
                  value={`${actor.actorKind}:${actor.actorId}`}
                >
                  {actorOptionLabel(actor)}
                </option>
              ))}
            </select>
          </label>

          <label className="space-y-1">
            <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">Od</span>
            <input
              type="date"
              value={filters.from}
              onChange={(e) => {
                setLoading(true);
                setLoadError(false);
                setFilters((current) => ({ ...current, from: e.target.value }));
              }}
              className="h-10 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-accent focus:ring-2 focus:ring-accent/10"
            />
          </label>

          <label className="space-y-1">
            <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">Do</span>
            <input
              type="date"
              value={filters.to}
              onChange={(e) => {
                setLoading(true);
                setLoadError(false);
                setFilters((current) => ({ ...current, to: e.target.value }));
              }}
              className="h-10 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-accent focus:ring-2 focus:ring-accent/10"
            />
          </label>

          <label className="space-y-1">
            <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">Limit</span>
            <select
              value={filters.limit}
              onChange={(e) => {
                setLoading(true);
                setLoadError(false);
                setFilters((current) => ({ ...current, limit: e.target.value }));
              }}
              className="h-10 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-accent focus:ring-2 focus:ring-accent/10"
            >
              <option value="25">25</option>
              <option value="50">50</option>
              <option value="100">100</option>
            </select>
          </label>

          <button
            type="button"
            onClick={resetFilters}
            className="inline-flex h-10 items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 text-sm font-medium text-slate-600 hover:bg-slate-50"
          >
            <Filter size={15} />
            Počisti
          </button>
        </div>
      </section>

      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        {loadError ? (
          <div className="m-5 flex items-center gap-3 rounded-xl border border-red-100 bg-red-50 px-4 py-3 text-sm text-red-700">
            <AlertCircle size={18} />
            Revizijske sledi ni bilo mogoče naložiti.
          </div>
        ) : loading && entries === null ? (
          <TableSkeleton />
        ) : displayedEntries === null || displayedEntries.length === 0 ? (
          <div className="flex flex-col items-center px-5 py-16 text-center">
            <CalendarDays size={34} className="mb-3 text-slate-300" />
            <p className="text-sm font-semibold text-slate-700">Ni zapisov za izbrane filtre.</p>
            <p className="mt-1 text-sm text-slate-400">Ko bo v organizaciji izvedeno dejanje, se bo prikazalo tukaj.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-[1040px] w-full text-sm">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-50/80">
                  {["ČAS", "DEJANJE", "AKTER", "TARČA", "PODROBNOSTI"].map((heading) => (
                    <th key={heading} className="px-5 py-3 text-left text-[11px] font-semibold tracking-wide text-slate-400">
                      {heading}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {displayedEntries.map((entry) => (
                  <tr key={entry.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                    <td className="whitespace-nowrap px-5 py-4 font-medium text-slate-700">
                      {formatDate(entry.occurredAt)}
                    </td>
                    <td className="px-5 py-4">
                      <span className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ${badgeClass(actionTone(entry.action))}`}>
                        {labelFor(entry.action, ACTION_LABELS)}
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      <ActorCell entry={entry} />
                    </td>
                    <td className="px-5 py-4">
                      <TargetCell entry={entry} />
                    </td>
                    <td className="px-5 py-4">
                      <DetailsCell details={entry.details} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
