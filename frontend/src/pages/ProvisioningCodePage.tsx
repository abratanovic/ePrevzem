import { useEffect, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { CheckCircle, Copy, Check, Smartphone, Shield, Clock, ArrowLeft, Loader2, AlertCircle } from "lucide-react";
import sloveniaLogo from "../assets/slovenia-badge.png";
import { registerCitizen, type RegisterCitizenResponse } from "../services/authService";

const steps = [
  {
    num: "1",
    title: "Prenesite aplikacijo ePrevzem",
    desc: "Na svoji mobilni napravi odprite App Store ali Google Play in poiščite aplikacijo ePrevzem.",
  },
  {
    num: "2",
    title: "Vnesite aktivacijsko kodo",
    desc: "V aplikaciji izberite 'Aktiviraj napravo' in vnesite prikazano kodo. Koda je veljavna 24 ur od registracije.",
  },
  {
    num: "3",
    title: "Nastavite varnostno metodo",
    desc: "Izberite prstni odtis, Face ID ali PIN za potrditev identitete pri vsakem prevzemu dokumenta.",
  },
];

export default function ProvisioningCodePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [copied, setCopied] = useState(false);
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [data, setData] = useState<RegisterCitizenResponse | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const token = searchParams.get("token");
  const hasFired = useRef(false);

  useEffect(() => {
    if (!token) {
      navigate("/", { replace: true });
      return;
    }

    if (hasFired.current) return;
    hasFired.current = true;

    registerCitizen(token)
      .then((res) => { setData(res); setStatus("success"); })
      .catch(() => {
        setErrorMessage("Registracija ni uspela. Preverite svojo internetno povezavo in poskusite znova.");
        setStatus("error");
      });
  }, [token, navigate]);

  const handleCopy = async () => {
    if (!data) return;
    await navigator.clipboard.writeText(data.code);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleRetry = () => {
    setStatus("loading");
    setErrorMessage(null);
    if (!token) return;
    registerCitizen(token)
      .then((res) => { setData(res); setStatus("success"); })
      .catch(() => {
        setErrorMessage("Registracija ni uspela. Preverite svojo internetno povezavo in poskusite znova.");
        setStatus("error");
      });
  };

  const expiresAt = data ? new Date(data.expiresAt) : null;
  const expiresLabel = expiresAt
    ? expiresAt.toLocaleString("sl-SI", { day: "numeric", month: "long", hour: "2-digit", minute: "2-digit" })
    : null;

  return (
    <div className="min-h-screen bg-[#f5f0e8]">
      <nav className="sticky top-0 z-40 border-b border-slate-100 bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4 sm:px-6">
          <button onClick={() => navigate("/")} className="flex items-center gap-3">
            <img src={sloveniaLogo} alt="Republika Slovenija" className="h-8 w-8 sm:h-9 sm:w-9" />
            <div className="leading-tight">
              <p className="hidden text-[9px] font-semibold uppercase tracking-widest text-slate-400 sm:block">
                Republika Slovenija
              </p>
              <p className="text-base font-bold text-[#1a3d2b]">ePrevzem</p>
            </div>
          </button>

          <button
            onClick={() => navigate("/")}
            className="flex items-center gap-1.5 text-sm font-medium text-slate-500 transition hover:text-[#1a3d2b]"
          >
            <ArrowLeft size={15} />
            Domača stran
          </button>
        </div>
      </nav>

      <section className="mx-auto max-w-2xl px-4 py-12 sm:px-6">
        {status === "loading" && (
          <div className="flex flex-col items-center gap-4 py-24 text-center">
            <Loader2 size={40} className="animate-spin text-[#1a3d2b]" />
            <p className="text-base font-medium text-slate-600">Pripravljamo vašo aktivacijsko kodo…</p>
          </div>
        )}

        {status === "error" && (
          <div className="flex flex-col items-center gap-6 py-16 text-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-red-100">
              <AlertCircle size={32} className="text-red-600" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Prišlo je do napake</h1>
              <p className="mt-2 text-base text-slate-600">{errorMessage}</p>
            </div>
            <button
              type="button"
              onClick={handleRetry}
              className="rounded-xl bg-[#1a3d2b] px-6 py-3 text-sm font-semibold text-white transition hover:bg-[#153122]"
            >
              Poskusi znova
            </button>
          </div>
        )}

        {status === "success" && data && (
          <>
            <div className="mb-8 text-center">
              <div className="mx-auto mb-5 flex h-16 w-16 items-center justify-center rounded-full bg-[#1a3d2b]">
                <CheckCircle size={32} className="text-white" />
              </div>
              <h1 className="text-3xl font-bold text-slate-900">Registracija uspešna!</h1>
              <p className="mt-2 text-base text-slate-600">
                Dobrodošli,{" "}
                <span className="font-semibold text-slate-900">
                  {data.firstName} {data.lastName}
                </span>
                . Vaša identiteta je bila potrjena prek SI-PASS.
              </p>
            </div>

            <div className="mb-6 overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-slate-200">
              <div className="bg-[#1a3d2b] px-6 py-4">
                <p className="text-xs font-semibold uppercase tracking-widest text-white/60">
                  Aktivacijska koda
                </p>
                <p className="mt-0.5 text-sm text-white/80">
                  Vnesite jo v mobilno aplikacijo ePrevzem
                </p>
              </div>

              <div className="px-6 py-8">
                <div className="flex items-center justify-center gap-4">
                  <span className="select-all font-mono text-3xl font-bold tracking-[0.18em] text-slate-900 sm:text-4xl">
                    {data.code}
                  </span>
                  <button
                    type="button"
                    onClick={() => void handleCopy()}
                    title={copied ? "Kopirano!" : "Kopiraj kodo"}
                    className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-slate-200 transition hover:bg-slate-50"
                  >
                    {copied ? (
                      <Check size={18} className="text-[#1a3d2b]" />
                    ) : (
                      <Copy size={18} className="text-slate-400" />
                    )}
                  </button>
                </div>

                <div className="mt-6 flex flex-wrap justify-center gap-3">
                  <span className="flex items-center gap-1.5 rounded-full bg-amber-50 px-3 py-1.5 text-xs font-semibold text-amber-700 ring-1 ring-amber-200">
                    <Clock size={12} />
                    {expiresLabel ? `Velja do ${expiresLabel}` : "Velja 24 ur"}
                  </span>
                  <span className="flex items-center gap-1.5 rounded-full bg-slate-50 px-3 py-1.5 text-xs font-semibold text-slate-600 ring-1 ring-slate-200">
                    <Shield size={12} />
                    Enkratna uporaba
                  </span>
                </div>
              </div>
            </div>

            <div className="mb-6 rounded-2xl border border-[#1a3d2b]/10 bg-white px-6 py-5">
              <h2 className="text-sm font-bold text-slate-900">Kaj je aktivacijska koda?</h2>
              <p className="mt-2 text-sm leading-relaxed text-slate-600">
                Aktivacijska koda je varnostna koda, s katero povežete svojo mobilno napravo z
                računom ePrevzem. Ko jo vnesete v aplikacijo, bo vaša naprava edina, ki bo
                lahko odpirala vam namenjene predalnike v pametnih paketnikih. Koda je enkratna
                in preneha veljati po 24 urah — shranite jo ali jo takoj vnesite v aplikacijo.
              </p>
            </div>

            <div className="rounded-2xl bg-white px-6 py-6 shadow-sm ring-1 ring-slate-200">
              <div className="mb-5 flex items-center gap-2">
                <Smartphone size={18} className="text-[#1a3d2b]" />
                <h2 className="text-sm font-bold text-slate-900">Kako aktivirate napravo?</h2>
              </div>

              <div className="space-y-5">
                {steps.map(({ num, title, desc }) => (
                  <div key={num} className="flex gap-4">
                    <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-[#1a3d2b] text-xs font-bold text-white">
                      {num}
                    </div>
                    <div>
                      <p className="text-sm font-semibold text-slate-900">{title}</p>
                      <p className="mt-0.5 text-sm leading-relaxed text-slate-500">{desc}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="mt-8 text-center">
              <button
                type="button"
                onClick={() => navigate("/")}
                className="rounded-xl bg-[#1a3d2b] px-6 py-3 text-sm font-semibold text-white transition hover:bg-[#153122]"
              >
                Pojdi na domačo stran
              </button>
            </div>
          </>
        )}
      </section>
    </div>
  );
}
