import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { MapPin, Package, Pencil, Radio, Trash2, Warehouse } from "lucide-react";
import StationPageHeader from "../components/stations/StationPageHeader";
import { formatCoordinates, formatStationAddress, formatStationDate } from "../components/stations/stationFormatters";
import { stationService } from "../services/stations/stationService";
import type { OrganizationPickupStation } from "../types/stations";

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between border-b border-slate-100 py-3 last:border-0">
      <span className="text-sm text-slate-500">{label}</span>
      <span className="text-sm font-medium text-slate-800">{value}</span>
    </div>
  );
}

export default function PickupStationDetailsPage() {
  const { claimId = "" } = useParams();
  const navigate = useNavigate();
  const [station, setStation] = useState<OrganizationPickupStation | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    stationService.getStation(claimId)
      .then(setStation)
      .catch(() => setError("Paketomat ni bil najden."));
  }, [claimId]);

  if (error) return <p className="p-6 text-sm text-red-600">{error}</p>;
  if (!station) return <p className="p-6 text-sm text-slate-500">Nalagam paketomat ...</p>;

  const serviceable = station.lockers.filter((locker) => locker.isServiceable).length;

  const deleteStation = async () => {
    const confirmed = window.confirm(
      `Ali ste prepričani, da želite odstraniti paketomat ${station.serialNumber} iz organizacije?`,
    );
    if (!confirmed) return;

    setIsDeleting(true);
    setError(null);
    try {
      await stationService.deleteStation(station.claimId);
      navigate("/paketniki");
    } catch {
      setError("Paketomata ni bilo mogoče odstraniti. Poskusite znova.");
      setIsDeleting(false);
    }
  };

  return (
    <div className="space-y-5 p-6">
      <div className="flex items-center justify-between">
        <StationPageHeader title={station.serialNumber} subtitle="Podrobnosti paketomata in registrirane lokacije." />
        <div className="flex items-center gap-2">
          <button type="button" onClick={deleteStation} disabled={isDeleting} className="flex items-center gap-2 rounded-xl border border-red-200 bg-white px-4 py-2.5 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-60">
            <Trash2 size={16} />
            {isDeleting ? "Odstranjujem ..." : "Odstrani paketomat"}
          </button>
          <Link to={`/paketniki/${station.claimId}/uredi`} className="flex items-center gap-2 rounded-xl bg-accent px-4 py-2.5 text-sm font-medium text-white hover:bg-accent-dark">
            <Pencil size={16} />
            Uredi lokacijo
          </Link>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-4">
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <Warehouse size={20} className="mb-3 text-emerald-600" />
          <p className="text-xs uppercase tracking-wide text-slate-400">Status</p>
          <p className="mt-1 font-semibold text-emerald-700">{station.releasedAt === null ? "Aktiven" : "Sproščen"}</p>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <Package size={20} className="mb-3 text-blue-500" />
          <p className="text-xs uppercase tracking-wide text-slate-400">Predalčki</p>
          <p className="mt-1 font-semibold text-slate-900">{serviceable}/{station.lockers.length} delujočih</p>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <Radio size={20} className="mb-3 text-indigo-500" />
          <p className="text-xs uppercase tracking-wide text-slate-400">Dodano organizaciji</p>
          <p className="mt-1 font-semibold text-slate-900">{formatStationDate(station.claimedAt)}</p>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h3 className="mb-2 flex items-center gap-2 font-bold text-slate-900"><Warehouse size={17} className="text-accent" /> Paketomat</h3>
          <DetailRow label="Serijska številka" value={station.serialNumber} />
          <DetailRow label="ID postaje" value={<span className="font-mono text-xs">{station.stationId}</span>} />
          <DetailRow label="Datum registracije" value={formatStationDate(station.createdAt)} />
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h3 className="mb-2 flex items-center gap-2 font-bold text-slate-900"><MapPin size={17} className="text-accent" /> Lokacija</h3>
          <DetailRow label="Naslov" value={formatStationAddress(station.location)} />
          <DetailRow label="Koordinate" value={<span className="font-mono text-xs">{formatCoordinates(station.location)}</span>} />
          <DetailRow label="ID povezave" value={<span className="font-mono text-xs">{station.claimId}</span>} />
        </div>
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 px-5 py-4">
          <h3 className="font-bold text-slate-900">Predalčki</h3>
          <p className="text-xs text-slate-400">Stanje predalčkov registriranega fizičnega paketomata</p>
        </div>
        {station.lockers.length === 0 ? (
          <p className="px-5 py-8 text-center text-sm text-slate-500">Podatki o predalčkih še niso na voljo.</p>
        ) : (
          <div className="grid grid-cols-8 gap-3 p-5">
            {station.lockers.map((locker) => (
              <div key={locker.id} className={`rounded-xl border px-3 py-2 text-center ${locker.isServiceable ? "border-emerald-100 bg-emerald-50 text-emerald-700" : "border-red-100 bg-red-50 text-red-600"}`}>
                <p className="text-xs font-semibold">{locker.lockerNumber}</p>
                <p className="mt-0.5 text-[10px]">{locker.isServiceable ? "Delujoč" : "Nedelujoč"}</p>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
