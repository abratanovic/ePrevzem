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
    <div className="flex items-center justify-between gap-4">
      <Link to={backTo} aria-label={`Nazaj: ${title}`} className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3.5 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50">
        <ArrowLeft size={16} />
        Nazaj
      </Link>
      <p className="text-sm text-slate-500">{subtitle}</p>
    </div>
  );
}
