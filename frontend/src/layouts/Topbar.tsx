import { Bell, Plus, Search } from "lucide-react";
import { useLocation } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";

interface RouteMeta {
  title: string;
  subtitle: string;
}

const ROUTE_TITLES: Record<string, string> = {
  "/dashboard": "Nadzorna plošča",
  "/profil": "Moj profil",
};

const NOTIFICATION_COUNT = 4;

export default function Topbar() {
  const { pathname } = useLocation();
  const { user } = useAuth();
  const title = ROUTE_TITLES[pathname] ?? "";
  const subtitle = pathname === "/profil"
    ? ""
    : user?.organizationName
      ? `Pregled prevzemov in paketnikov · ${user.organizationName}`
      : "Pregled prevzemov in paketnikov";

  return (
    <header className="fixed inset-x-0 top-0 left-20 z-10 flex h-[72px] items-center gap-4 border-b border-slate-200 bg-white px-6">
      {/* page heading */}
      <div className="min-w-0 shrink-0">
        <h1 className="text-[22px] font-bold tracking-tight text-slate-900 leading-tight">
          {title}
        </h1>
        {subtitle && (
          <p className="text-xs text-slate-500 leading-tight">{subtitle}</p>
        )}
      </div>

      {/* right controls */}
      <div className="ml-auto flex shrink-0 items-center gap-2">
        {/* search */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={15} />
          <input
            type="search"
            placeholder="Iskanje prevzemov"
            className="w-64 rounded-xl border border-slate-200 bg-slate-50 py-2 pl-9 pr-4 text-sm text-slate-700 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-accent/20 focus:border-accent"
          />
        </div>

        {/* primary action */}
        <button
          type="button"
          className="flex items-center gap-2 rounded-xl bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent-dark"
        >
          <Plus size={16} strokeWidth={2.5} />
          Dodaj prevzem
        </button>

        {/* notification bell */}
        <button
          type="button"
          className="relative flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 text-slate-500 hover:bg-slate-100"
        >
          <Bell size={18} />
          {NOTIFICATION_COUNT > 0 && (
            <span className="absolute right-1.5 top-1.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-bold leading-none text-white">
              {NOTIFICATION_COUNT}
            </span>
          )}
        </button>
      </div>
    </header>
  );
}
