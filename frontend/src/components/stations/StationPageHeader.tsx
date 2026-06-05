import { ArrowLeft } from "lucide-react";
import { Link } from "react-router-dom";

export default function StationPageHeader({
  title,
  subtitle,
  backTo = "/paketniki",
}: {
  title: string;
  subtitle: string;
  backTo?: string;
}) {
  return (
    <div className="flex items-center gap-4">
      <Link to={backTo} className="flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-600 hover:bg-slate-50">
        <ArrowLeft size={18} />
      </Link>
      <div>
        <h2 className="text-xl font-bold text-slate-900">{title}</h2>
        <p className="text-sm text-slate-500">{subtitle}</p>
      </div>
    </div>
  );
}
