import { LayoutDashboard, type LucideIcon } from "lucide-react";
import sloveniaBadge from "../assets/slovenia-badge.png";
import { NavLink } from "react-router-dom";

interface NavItem {
  icon: LucideIcon;
  label: string;
  path: string;
}

const NAV_ITEMS: NavItem[] = [
  { icon: LayoutDashboard, label: "Nadzorna plošča", path: "/dashboard" },
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
  return (
    <aside className="fixed inset-y-0 left-0 flex w-20 flex-col items-center border-r border-slate-200 bg-white">
      {/* logo — height matches topbar */}
      <div className="flex h-[72px] w-full shrink-0 flex-col items-center justify-center gap-0.5">
        <img src={sloveniaBadge} alt="Slovenija" className="h-7 w-7 object-contain" />
        <span className="text-[9px] font-bold tracking-tight text-accent">ePrevzem</span>
      </div>

      {/* nav */}
      <nav className="mt-1 flex flex-1 flex-col items-center gap-0.5">
        {NAV_ITEMS.map((item) => (
          <NavButton key={item.path} item={item} />
        ))}
      </nav>

      {/* user avatar */}
      <div className="mb-4 flex h-10 w-10 items-center justify-center rounded-full bg-accent/10">
        <span className="text-xs font-semibold text-accent">JN</span>
      </div>
    </aside>
  );
}
