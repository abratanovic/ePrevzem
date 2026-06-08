import { Link } from "react-router-dom";
import { ArrowLeft, Building2, Mail, ShieldCheck, User, KeyRound } from "lucide-react";
import { useAuth } from "../contexts/useAuth";

function InfoRow({ icon, label, value }: { icon: React.ReactNode; label: string; value: string | null | undefined }) {
  return (
    <div className="flex items-start gap-4 py-4 border-b border-slate-100 last:border-0">
      <div className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-surface text-accent">
        {icon}
      </div>
      <div className="min-w-0">
        <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</p>
        <p className="mt-0.5 text-sm font-semibold text-slate-900 truncate">
          {value ?? <span className="text-slate-400 font-normal">—</span>}
        </p>
      </div>
    </div>
  );
}

const ROLE_LABELS: Record<string, string> = {
  OrganizationAdmin: "Skrbnik organizacije",
  Employee: "Zaposleni",
};

export default function ProfilePage() {
  const { user } = useAuth();

  if (!user) return null;

  const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase() || "?";
  const fullName = `${user.firstName} ${user.lastName}`;
  const roleLabel = ROLE_LABELS[user.role] ?? user.role;

  return (
    <div className="flex min-h-full justify-center p-6">
      <div className="w-full max-w-2xl space-y-5">
        <Link
          to="/dashboard"
          className="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-600 hover:bg-slate-50"
          aria-label="Nazaj na nadzorno ploščo"
        >
          <ArrowLeft size={18} />
        </Link>

        {/* Header card */}
        <div className="rounded-2xl bg-accent px-8 py-8 text-white">
          <div className="flex items-center gap-5">
            <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-white/10 text-xl font-bold text-white ring-2 ring-white/20">
              {initials}
            </div>
            <div>
              <h1 className="text-2xl font-bold">{fullName}</h1>
              <p className="mt-1 text-sm text-white/60">{roleLabel}</p>
              {user.organizationName && (
                <span className="mt-2 inline-flex items-center gap-1.5 rounded-full bg-white/10 px-3 py-1 text-xs font-medium text-white/80">
                  <Building2 size={11} />
                  {user.organizationName}
                </span>
              )}
            </div>
          </div>
        </div>

        {/* Info card */}
        <div className="rounded-2xl border border-slate-100 bg-white px-6 py-2 shadow-sm">
          <p className="pt-4 pb-2 text-xs font-semibold uppercase tracking-widest text-slate-400">
            Osebni podatki
          </p>
          <InfoRow icon={<User size={16} />} label="Ime in priimek" value={fullName} />
          <InfoRow icon={<Mail size={16} />} label="E-poštni naslov" value={user.email} />
          <InfoRow icon={<ShieldCheck size={16} />} label="Vloga" value={roleLabel} />
          {user.organizationName && (
            <InfoRow icon={<Building2 size={16} />} label="Organizacija" value={user.organizationName} />
          )}
          <div className="py-4">
            <p className="text-[10px] font-medium uppercase tracking-wide text-slate-400">ID računa</p>
            <p className="mt-0.5 font-mono text-xs text-slate-400 break-all">{user.id}</p>
          </div>
        </div>

        {/* Security card */}
        <div className="rounded-2xl border border-slate-100 bg-white px-6 py-2 shadow-sm">
          <p className="pt-4 pb-2 text-xs font-semibold uppercase tracking-widest text-slate-400">
            Varnost
          </p>
          <div className="flex items-center justify-between py-4">
            <div className="flex items-center gap-4">
              <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-surface text-accent">
                <KeyRound size={16} />
              </div>
              <div>
                <p className="text-sm font-semibold text-slate-900">Geslo</p>
                <p className="text-xs text-slate-400">Spremenite geslo za dostop do računa</p>
              </div>
            </div>
            <a
              href="/sprememba-gesla"
              className="rounded-xl border border-accent/30 px-4 py-2 text-xs font-semibold text-accent transition hover:bg-accent/5"
            >
              Spremenite geslo
            </a>
          </div>
        </div>
      </div>
    </div>
  );
}
