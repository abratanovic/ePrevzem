import { Plus } from "lucide-react";
import { Link, useLocation } from "react-router-dom";
import { useAuth } from "../contexts/useAuth";

const ROUTE_TITLES: Record<string, string> = {
  "/dashboard": "Nadzorna plošča",
  "/prevzemi/dodaj": "Dodaj prevzem",
  "/profil": "Moj profil",
  "/paketniki": "Paketomati",
  "/organizacija": "Organizacija",
  "/organizacija/clani": "Organizacija",
};

export default function Topbar() {
  const { pathname } = useLocation();
  const { user } = useAuth();
  const title = ROUTE_TITLES[pathname] ?? (pathname.startsWith("/paketniki/") ? "Paketomati" : "");
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
        {/* primary action */}
        <Link
          to="/prevzemi/dodaj"
          className="flex items-center gap-2 rounded-xl bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent-dark"
        >
          <Plus size={16} strokeWidth={2.5} />
          Dodaj prevzem
        </Link>
      </div>
    </header>
  );
}
