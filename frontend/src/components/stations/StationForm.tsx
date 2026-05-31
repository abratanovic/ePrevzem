import { useState, type FormEvent } from "react";
import { LocateFixed, Save } from "lucide-react";
import type { ClaimPickupStationRequest } from "../../types/stations";
import { SLOVENIAN_POSTAL_CODES } from "../../data/slovenianPostalCodes";
import StationMapPicker from "./StationMapPicker";

interface StationFormValues {
  latitude: number | "";
  longitude: number | "";
  address: string;
  houseNumber: string;
  zipCode: string;
  city: string;
  serialNumber: string;
}

interface StationFormProps {
  initialValues?: StationFormValues;
  submitLabel: string;
  lockSerialNumber?: boolean;
  onCancel: () => void;
  onSubmit: (values: ClaimPickupStationRequest) => Promise<void>;
}

const EMPTY_VALUES: StationFormValues = {
  serialNumber: "",
  latitude: "",
  longitude: "",
  address: "",
  houseNumber: "",
  zipCode: "",
  city: "",
};

type FormErrors = Partial<Record<keyof StationFormValues, string>>;

function validate(values: StationFormValues): FormErrors {
  const errors: FormErrors = {};
  if (!values.serialNumber.trim()) errors.serialNumber = "Serijska številka je obvezna.";
  if (!values.address.trim()) errors.address = "Ulica je obvezna.";
  if (!values.houseNumber.trim()) errors.houseNumber = "Hišna številka je obvezna.";
  if (!values.zipCode.trim()) errors.zipCode = "Poštna številka je obvezna.";
  if (!values.city.trim()) errors.city = "Kraj je obvezen.";
  if (values.latitude === "") errors.latitude = "Zemljepisna širina je obvezna.";
  else if (values.latitude < -90 || values.latitude > 90) errors.latitude = "Širina mora biti med -90 in 90.";
  if (values.longitude === "") errors.longitude = "Zemljepisna dolžina je obvezna.";
  else if (values.longitude < -180 || values.longitude > 180) errors.longitude = "Dolžina mora biti med -180 in 180.";
  return errors;
}

function Field({
  id,
  label,
  value,
  error,
  disabled,
  type = "text",
  step,
  onChange,
}: {
  id: keyof StationFormValues;
  label: string;
  value: string | number;
  error?: string;
  disabled?: boolean;
  type?: "text" | "number";
  step?: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="space-y-1.5">
      <span className="block text-sm font-medium text-slate-700">{label}</span>
      <input
        id={id}
        type={type}
        step={step}
        value={value}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        className={`w-full rounded-xl border bg-white px-3.5 py-2.5 text-sm text-slate-900 outline-none transition disabled:bg-slate-100 disabled:text-slate-500 ${error ? "border-red-400 focus:ring-2 focus:ring-red-100" : "border-slate-200 focus:border-accent focus:ring-2 focus:ring-accent/10"}`}
      />
      {error && <span className="block text-xs text-red-500">{error}</span>}
    </label>
  );
}

export default function StationForm({
  initialValues = EMPTY_VALUES,
  submitLabel,
  lockSerialNumber = false,
  onCancel,
  onSubmit,
}: StationFormProps) {
  const [values, setValues] = useState<StationFormValues>(initialValues);
  const [errors, setErrors] = useState<FormErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [locationMessage, setLocationMessage] = useState<string | null>(null);
  const [isLocating, setIsLocating] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const setValue = (field: keyof StationFormValues, value: string) => {
    setValues((current) => ({
      ...current,
      [field]: field === "latitude" || field === "longitude"
        ? (value === "" ? "" : Number(value))
        : value,
    }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  };

  const setZipCode = (zipCode: string) => {
    const city = SLOVENIAN_POSTAL_CODES[zipCode] ?? "";
    setValues((current) => ({ ...current, zipCode, city }));
    setErrors((current) => ({
      ...current,
      zipCode: undefined,
      ...(city ? { city: undefined } : {}),
    }));
  };

  const useCurrentLocation = () => {
    setLocationMessage(null);
    if (!navigator.geolocation) {
      setLocationMessage("Brskalnik ne podpira pridobivanja lokacije.");
      return;
    }

    setIsLocating(true);
    navigator.geolocation.getCurrentPosition(
      ({ coords }) => {
        setCoordinates({
          latitude: Number(coords.latitude.toFixed(6)),
          longitude: Number(coords.longitude.toFixed(6)),
        });
        setLocationMessage("Koordinati sta bili uspešno izpolnjeni.");
        setIsLocating(false);
      },
      () => {
        setLocationMessage("Lokacije ni bilo mogoče pridobiti. Koordinati lahko vnesete ročno.");
        setIsLocating(false);
      },
    );
  };

  const setCoordinates = ({ latitude, longitude }: { latitude: number; longitude: number }) => {
    setValues((current) => ({ ...current, latitude, longitude }));
    setErrors((current) => ({ ...current, latitude: undefined, longitude: undefined }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const nextErrors = validate(values);
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;

    setSubmitError(null);
    setIsSubmitting(true);
    try {
      await onSubmit({
        ...values,
        latitude: Number(values.latitude),
        longitude: Number(values.longitude),
        serialNumber: values.serialNumber.trim(),
      });
    } catch {
      setSubmitError("Paketomata ni bilo mogoče shraniti. Poskusite znova.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="mb-5">
          <h2 className="text-lg font-bold text-slate-900">Osnovni podatki</h2>
          <p className="text-sm text-slate-500">Serijska številka poveže organizacijo z registriranim fizičnim paketomatom.</p>
        </div>
        <Field
          id="serialNumber"
          label="Serijska številka"
          value={values.serialNumber}
          error={errors.serialNumber}
          disabled={lockSerialNumber}
          onChange={(value) => setValue("serialNumber", value)}
        />
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="mb-5 flex items-start justify-between gap-4">
          <div>
            <h2 className="text-lg font-bold text-slate-900">Lokacija paketomata</h2>
            <p className="text-sm text-slate-500">Vnesite naslov in natančni koordinati lokacije.</p>
          </div>
          <button
            type="button"
            onClick={useCurrentLocation}
            disabled={isLocating}
            className="flex shrink-0 items-center gap-2 rounded-xl border border-accent/25 px-3.5 py-2 text-sm font-medium text-accent transition hover:bg-accent/5 disabled:opacity-60"
          >
            <LocateFixed size={16} />
            {isLocating ? "Pridobivam lokacijo ..." : "Uporabi mojo lokacijo"}
          </button>
        </div>

        {locationMessage && (
          <p className="mb-4 rounded-xl bg-slate-50 px-4 py-3 text-sm text-slate-600">{locationMessage}</p>
        )}

        <div className="grid grid-cols-2 gap-4">
          <Field id="address" label="Ulica" value={values.address} error={errors.address} onChange={(value) => setValue("address", value)} />
          <Field id="houseNumber" label="Hišna številka" value={values.houseNumber} error={errors.houseNumber} onChange={(value) => setValue("houseNumber", value)} />
          <Field id="zipCode" label="Poštna številka" value={values.zipCode} error={errors.zipCode} onChange={setZipCode} />
          <Field id="city" label="Kraj" value={values.city} error={errors.city} onChange={(value) => setValue("city", value)} />
        </div>

        <div className="mt-5 space-y-3">
          <div>
            <h3 className="text-sm font-semibold text-slate-800">Natančna lokacija paketomata</h3>
            <p className="text-xs text-slate-500">Kliknite na zemljevid ali premaknite marker do vhoda oziroma mesta, kjer je paketomat postavljen.</p>
          </div>
          <StationMapPicker
            coordinates={values.latitude === "" || values.longitude === ""
              ? null
              : { latitude: values.latitude, longitude: values.longitude }}
            onChange={setCoordinates}
          />
          {(errors.latitude || errors.longitude) && (
            <p className="text-xs text-red-500">Na zemljevidu določite natančno lokacijo paketomata.</p>
          )}
          {values.latitude !== "" && values.longitude !== "" && (
            <p className="text-xs text-slate-400">
              Koordinate: <span className="font-mono">{values.latitude.toFixed(6)}, {values.longitude.toFixed(6)}</span>
            </p>
          )}
        </div>
      </div>

      {submitError && <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-600">{submitError}</p>}

      <div className="flex justify-end gap-3">
        <button type="button" onClick={onCancel} className="rounded-xl border border-slate-200 bg-white px-5 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50">
          Prekliči
        </button>
        <button type="submit" disabled={isSubmitting} className="flex items-center gap-2 rounded-xl bg-accent px-5 py-2.5 text-sm font-medium text-white hover:bg-accent-dark disabled:opacity-60">
          <Save size={16} />
          {isSubmitting ? "Shranjujem ..." : submitLabel}
        </button>
      </div>
    </form>
  );
}
