import { ClipboardList, LayoutDashboard, LogOut, Package, Users, type LucideIcon } from "lucide-react";
import sloveniaBadge from "../assets/slovenia-badge.png";
import { Link, NavLink, useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../contexts/useAuth";
import { useState, useRef, useEffect } from "react";

interface NavItem {
  icon: LucideIcon;
  label: string;
  path: string;
}

const NAV_ITEMS: NavItem[] = [
  { icon: LayoutDashboard, label: "Nadzorna plošča", path: "/dashboard" },
  { icon: ClipboardList, label: "Revizijska sled", path: "/audit-log" },
];

const ADMIN_NAV_ITEMS: NavItem[] = [
  { icon: Package, label: "Paketomati", path: "/paketniki" },
  { icon: Users, label: "Organizacija", path: "/organizacija" },
];

function NavButton({ item }: { item: NavItem }) {
  return (
    <NavLink
      to={item.path}
      className={({ isActive }) =>
        [
          "group relative flex h-10 w-10 items-center justify-center rounded-xl transition-colors",
          isActive
            ? "bg-accent text-white"
            : "text-slate-400 hover:bg-slate-100 hover:text-slate-900",
        ].join(" ")
      }
    >
      <item.icon size={19} strokeWidth={1.75} />
      <span className="pointer-events-none absolute left-12 z-50 whitespace-nowrap rounded-md bg-slate-900 px-2.5 py-1.5 text-xs font-medium text-white opacity-0 shadow-lg transition-opacity group-hover:opacity-100">
        {item.label}
      </span>
    </NavLink>
  );
}

export default function Sidebar() {
  const { logout, user } = useAuth();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const isProfileActive = pathname === "/profil";
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleOutside(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setMenuOpen(false);
    }
    document.addEventListener("mousedown", handleOutside);
    return () => document.removeEventListener("mousedown", handleOutside);
  }, []);

  const handleLogout = () => {
    logout();
    navigate("/prijava", { replace: true });
  };

  const initials = user
    ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase() || "?"
    : "?";

  return (
    <aside className="fixed inset-y-0 left-0 flex w-20 flex-col items-center border-r border-slate-200 bg-white">
      {/* logo */}
      <Link
        to="/"
        className="flex h-18 w-full shrink-0 flex-col items-center justify-center gap-0.5 transition hover:bg-slate-50"
        aria-label="Nazaj na začetno stran"
      >
        <img src={sloveniaBadge} alt="Slovenija" className="h-7 w-7 object-contain" />
        <span className="text-[9px] font-bold tracking-tight text-accent">ePrevzem</span>
      </Link>

      {/* nav */}
      <nav className="mt-1 flex flex-1 flex-col items-center gap-2">
        {[...NAV_ITEMS, ...(user?.role === "OrganizationAdmin" ? ADMIN_NAV_ITEMS : [])].map((item) => (
          <NavButton key={item.path} item={item} />
        ))}
      </nav>

      {/* profile button */}
      <div ref={menuRef} className="relative mb-4">
        <button
          onClick={() => setMenuOpen((v) => !v)}
          className={`flex h-10 w-10 items-center justify-center rounded-full transition ring-2 ${isProfileActive ? "bg-accent text-white ring-accent/30" : "bg-accent/10 text-accent ring-transparent hover:ring-accent/30"}`}
        >
          <span className="text-xs font-semibold">{initials}</span>
        </button>

        {menuOpen && (
          <div className="absolute bottom-0 left-14 z-50 w-48 overflow-hidden rounded-xl border border-slate-100 bg-white shadow-xl">
            <div className="border-b border-slate-100 px-4 py-3">
              <p className="text-xs font-semibold text-slate-900 truncate">
                {user ? `${user.firstName} ${user.lastName}` : ""}
              </p>
              <p className="mt-0.5 text-[10px] text-slate-400 truncate">
                {user?.organizationName ?? (user?.role === "OrganizationAdmin" ? "Administrator" : "Zaposleni")}
              </p>
            </div>
            <button
              onClick={() => { navigate("/profil"); setMenuOpen(false); }}
              className="flex w-full items-center gap-2.5 px-4 py-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
            >
              Moj profil
            </button>
            <div className="border-t border-slate-100" />
            <button
              onClick={handleLogout}
              className="flex w-full items-center gap-2.5 px-4 py-3 text-sm font-medium text-red-500 transition hover:bg-red-50"
            >
              <LogOut size={15} />
              Odjava
            </button>
          </div>
        )}
      </div>
    </aside>
  );
}
