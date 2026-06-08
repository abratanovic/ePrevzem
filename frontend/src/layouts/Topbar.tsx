import { Plus } from "lucide-react";
import { Link, useLocation } from "react-router-dom";
import { useAuth } from "../contexts/useAuth";

interface RouteHeader {
  title: string;
  subtitle?: string;
  action?: {
    label: string;
    to: string;
  };
}

const ROUTE_HEADERS: Record<string, RouteHeader> = {
  "/dashboard": {
    title: "Nadzorna plošča",
    subtitle: "Ključni prevzemi, roki in zasedenost paketnikov.",
    action: { label: "Dodaj prevzem", to: "/prevzemi/dodaj" },
  },
  "/prevzemi": {
    title: "Prevzemi",
    subtitle: "Zgodovina in trenutno stanje vseh prevzemov.",
    action: { label: "Dodaj prevzem", to: "/prevzemi/dodaj" },
  },
  "/prevzemi/dodaj": {
    title: "Dodaj prevzem",
    subtitle: "Vnesite prejemnika, vsebino in ciljni paketomat.",
  },
  "/audit-log": {
    title: "Revizijska sled",
    subtitle: "Zabeležena dejanja v vaši organizaciji.",
  },
  "/profil": {
    title: "Moj profil",
  },
  "/paketniki": {
    title: "Paketomati",
    subtitle: "Lokacije in predalčki vaše organizacije.",
    action: { label: "Dodaj paketomat", to: "/paketniki/dodaj" },
  },
  "/paketniki/dodaj": {
    title: "Dodaj paketomat",
    subtitle: "Povežite fizični paketomat z organizacijo.",
  },
  "/organizacija": {
    title: "Organizacija",
    subtitle: "Osnovni podatki in zaposleni.",
  },
  "/organizacija/clani": {
    title: "Zaposleni",
    subtitle: "Upravljanje dostopov in vlog.",
  },
};

export default function Topbar() {
  const { pathname } = useLocation();
  const { user } = useAuth();
  const routeHeader = ROUTE_HEADERS[pathname]
    ?? (pathname.startsWith("/paketniki/") ? { title: "Paketomati", subtitle: "Podrobnosti lokacije in predalčkov." } : { title: "" });
  const subtitle = [routeHeader.subtitle, user?.organizationName].filter(Boolean).join(" · ");

  return (
    <header className="fixed inset-x-0 top-0 left-20 z-10 flex h-[72px] items-center gap-4 border-b border-slate-200 bg-white px-6">
      <div className="min-w-0 shrink-0">
        <h1 className="text-[22px] font-bold tracking-tight text-slate-900 leading-tight">
          {routeHeader.title}
        </h1>
        {subtitle && (
          <p className="text-xs text-slate-500 leading-tight">{subtitle}</p>
        )}
      </div>

      <div className="ml-auto flex shrink-0 items-center gap-2">
        {routeHeader.action && (
          <Link
            to={routeHeader.action.to}
            className="flex items-center gap-2 rounded-xl bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent-dark"
          >
            <Plus size={16} strokeWidth={2.5} />
            {routeHeader.action.label}
          </Link>
        )}
      </div>
    </header>
  );
}
