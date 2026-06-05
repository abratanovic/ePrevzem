import { useEffect, useState, type FormEvent } from "react";
import { ArrowLeft, CheckCircle2, Save, Search } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import {
  createPickup,
  findRecipientByEmso,
  getAvailablePickupStations,
  PickupServiceError,
} from "../services/pickupsService";
import type { PickupRecipient, PickupStationOption } from "../types/dashboard";

const EMSO_PATTERN = /^\d{13}$/;

export default function AddPickupPage() {
  const navigate = useNavigate();
  const [emso, setEmso] = useState("");
  const [description, setDescription] = useState("");
  const [stationId, setStationId] = useState("");
  const [recipient, setRecipient] = useState<PickupRecipient | null>(null);
  const [stations, setStations] = useState<PickupStationOption[] | null>(null);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isCheckingRecipient, setIsCheckingRecipient] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    getAvailablePickupStations()
      .then(setStations)
      .catch(() => setStations([]));
  }, []);

  const updateEmso = (value: string) => {
    setEmso(value.replace(/\D/g, "").slice(0, 13));
    setRecipient(null);
    setErrors((current) => ({ ...current, emso: "", recipient: "" }));
  };

  const checkRecipient = async () => {
    if (!EMSO_PATTERN.test(emso)) {
      setErrors((current) => ({
        ...current,
        emso: "EMŠO mora vsebovati natanko 13 številk.",
      }));
      return;
    }

    setIsCheckingRecipient(true);
    setErrors((current) => ({ ...current, emso: "", recipient: "" }));
    try {
      const match = await findRecipientByEmso(emso);
      setRecipient(match);
      if (!match) {
        setErrors((current) => ({
          ...current,
          recipient:
            "Prejemnik ni registriran. Pred izdelavo prevzema mora opraviti registracijo prek SI-TRUST.",
        }));
      }
    } finally {
      setIsCheckingRecipient(false);
    }
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const nextErrors: Record<string, string> = {};
    if (!EMSO_PATTERN.test(emso))
      nextErrors.emso = "EMŠO mora vsebovati natanko 13 številk.";
    else if (!recipient) nextErrors.recipient = "Najprej preverite prejemnika.";
    if (!description.trim())
      nextErrors.description = "Opis vsebine je obvezen.";
    if (!stationId) nextErrors.stationId = "Izberite ciljni paketomat.";
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;

    setIsSubmitting(true);
    try {
      await createPickup({
        recipientEmso: emso,
        targetPickupStationId: stationId,
        description,
      });
      navigate("/dashboard", {
        state: { pickupCreated: true },
      });
    } catch (error) {
      if (
        error instanceof PickupServiceError &&
        error.code === "recipient_not_found"
      ) {
        setErrors({
          recipient:
            "Prejemnik ni registriran. Pred izdelavo prevzema mora opraviti registracijo prek SI-TRUST.",
        });
      } else if (
        error instanceof PickupServiceError &&
        error.code === "station_forbidden"
      ) {
        setErrors({
          stationId: "Izbrani paketomat ni več na voljo vaši organizaciji.",
        });
      } else if (
        error instanceof PickupServiceError &&
        error.code === "creation_forbidden"
      ) {
        setErrors({ submit: "Nimate pravic za izdelavo prevzema." });
      } else {
        setErrors({
          submit: "Prevzema ni bilo mogoče shraniti. Poskusite znova.",
        });
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-4xl space-y-5 p-6">
      <div className="flex items-center gap-4">
        <Link
          to="/dashboard"
          className="flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-600 hover:bg-slate-50"
        >
          <ArrowLeft size={18} />
        </Link>
        <div>
          <h2 className="text-xl font-bold text-slate-900">Dodaj prevzem</h2>
          <p className="text-sm text-slate-500">
            Vnesite podatke o prejemniku in vsebini za nov prevzem.
          </p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="mb-5">
            <h3 className="text-lg font-bold text-slate-900">Prejemnik</h3>
            <p className="text-sm text-slate-500">
              Prejemnik mora biti predhodno registriran prek SI-TRUST.
            </p>
          </div>
          <div className="flex items-start gap-3">
            <label className="flex-1 space-y-1.5">
              <span className="block text-sm font-medium text-slate-700">
                EMŠO prejemnika
              </span>
              <input
                value={emso}
                inputMode="numeric"
                placeholder="Na primer 0101006500006"
                onChange={(event) => updateEmso(event.target.value)}
                className={`w-full rounded-xl border bg-white px-3.5 py-2.5 text-sm text-slate-900 outline-none transition ${errors.emso ? "border-red-400 focus:ring-2 focus:ring-red-100" : "border-slate-200 focus:border-accent focus:ring-2 focus:ring-accent/10"}`}
              />
              {errors.emso && (
                <span className="block text-xs text-red-500">
                  {errors.emso}
                </span>
              )}
            </label>
            <button
              type="button"
              onClick={checkRecipient}
              disabled={isCheckingRecipient}
              className="mt-7 flex items-center gap-2 rounded-xl border border-accent/25 px-4 py-2.5 text-sm font-medium text-accent transition hover:bg-accent/5 disabled:opacity-60"
            >
              <Search size={16} />
              {isCheckingRecipient ? "Preverjam ..." : "Preveri prejemnika"}
            </button>
          </div>
          {recipient && (
            <div className="mt-4 flex items-center gap-3 rounded-xl bg-emerald-50 px-4 py-3 text-emerald-800">
              <CheckCircle2 size={19} />
              <div>
                <p className="text-sm font-semibold">
                  {recipient.firstName} {recipient.lastName}
                </p>
                <p className="text-xs text-emerald-700">
                  Prejemnik je registriran.
                </p>
              </div>
            </div>
          )}
          {errors.recipient && (
            <p className="mt-4 rounded-xl bg-red-50 px-4 py-3 text-sm text-red-600">
              {errors.recipient}
            </p>
          )}
        </section>

        <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="mb-5">
            <h3 className="text-lg font-bold text-slate-900">
              Podatki o prevzemu
            </h3>
            <p className="text-sm text-slate-500">
              Referenca bo ustvarjena samodejno. Rok začne teči ob vložitvi v
              predalček.
            </p>
          </div>
          <label className="space-y-1.5">
            <span className="block text-sm font-medium text-slate-700">
              Opis vsebine
            </span>
            <textarea
              rows={4}
              value={description}
              placeholder="Na primer osebna izkaznica"
              onChange={(event) => {
                setDescription(event.target.value);
                setErrors((current) => ({ ...current, description: "" }));
              }}
              className={`w-full resize-y rounded-xl border bg-white px-3.5 py-2.5 text-sm text-slate-900 outline-none transition ${errors.description ? "border-red-400 focus:ring-2 focus:ring-red-100" : "border-slate-200 focus:border-accent focus:ring-2 focus:ring-accent/10"}`}
            />
            {errors.description && (
              <span className="block text-xs text-red-500">
                {errors.description}
              </span>
            )}
          </label>
        </section>

        <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="mb-5">
            <h3 className="text-lg font-bold text-slate-900">
              Ciljni paketomat
            </h3>
            <p className="text-sm text-slate-500">
              Predalček bo določen pozneje ob fizični vložitvi prevzema.
            </p>
          </div>
          {stations === null ? (
            <div className="h-11 animate-pulse rounded-xl bg-slate-100" />
          ) : stations.length === 0 ? (
            <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-600">
              Paketomatov ni bilo mogoče naložiti.
            </p>
          ) : (
            <div className="grid gap-3">
              {stations.map((station) => (
                <label
                  key={station.id}
                  className={`flex cursor-pointer items-start gap-3 rounded-xl border px-4 py-3 transition ${stationId === station.id ? "border-accent bg-accent/5" : "border-slate-200 hover:bg-slate-50"}`}
                >
                  <input
                    type="radio"
                    name="station"
                    value={station.id}
                    checked={stationId === station.id}
                    onChange={() => {
                      setStationId(station.id);
                      setErrors((current) => ({ ...current, stationId: "" }));
                    }}
                    className="mt-1 accent-[var(--color-accent)]"
                  />
                  <span>
                    <span className="block text-sm font-semibold text-slate-900">
                      {station.name}
                    </span>
                    <span className="block text-xs text-slate-500">
                      {station.address}
                    </span>
                  </span>
                </label>
              ))}
            </div>
          )}
          {errors.stationId && (
            <span className="mt-2 block text-xs text-red-500">
              {errors.stationId}
            </span>
          )}
        </section>

        {errors.submit && (
          <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-600">
            {errors.submit}
          </p>
        )}

        <div className="flex justify-end gap-3">
          <Link
            to="/dashboard"
            className="rounded-xl border border-slate-200 bg-white px-5 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
          >
            Prekliči
          </Link>
          <button
            type="submit"
            disabled={isSubmitting}
            className="flex items-center gap-2 rounded-xl bg-accent px-5 py-2.5 text-sm font-medium text-white hover:bg-accent-dark disabled:opacity-60"
          >
            <Save size={16} />
            {isSubmitting ? "Shranjujem ..." : "Dodaj prevzem"}
          </button>
        </div>
      </form>
    </div>
  );
}
