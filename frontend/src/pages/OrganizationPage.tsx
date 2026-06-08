import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Building2, Users } from "lucide-react";
import { getOrgInfo, type OrgInfo } from "../services/orgService";

export default function OrganizationPage() {
  const [info, setInfo] = useState<OrgInfo | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getOrgInfo()
      .then(setInfo)
      .catch(() => setError("Podatkov o organizaciji ni bilo mogoče naložiti."));
  }, []);

  return (
    <div className="space-y-5 p-6">
      <div className="flex gap-2 border-b border-slate-200 pb-1">
        <Link
          to="/organizacija"
          className="px-3 py-2 text-sm font-medium text-accent border-b-2 border-accent"
        >
          Pregled
        </Link>
        <Link
          to="/organizacija/clani"
          className="px-3 py-2 text-sm font-medium text-slate-500 hover:text-slate-900 border-b-2 border-transparent"
        >
          <span className="flex items-center gap-1.5"><Users size={14} />Zaposleni</span>
        </Link>
      </div>

      {error ? (
        <p className="text-sm text-red-600">{error}</p>
      ) : info === null ? (
        <div className="animate-pulse space-y-3">
          <div className="h-28 rounded-2xl bg-slate-100" />
        </div>
      ) : (
        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="flex items-center gap-3 border-b border-slate-100 bg-slate-50 px-6 py-4">
            <Building2 size={18} className="text-slate-500" />
            <h3 className="font-semibold text-slate-900">{info.name}</h3>
          </div>
          <div className="divide-y divide-slate-100">
            <div className="flex items-center justify-between px-6 py-4">
              <span className="text-sm text-slate-500">Davčna številka</span>
              <span className="text-sm font-medium text-slate-900">{info.taxNumber}</span>
            </div>
            <div className="flex items-center justify-between px-6 py-4">
              <span className="text-sm text-slate-500">Matična številka</span>
              <span className="text-sm font-medium text-slate-900">{info.registrationNumber}</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
