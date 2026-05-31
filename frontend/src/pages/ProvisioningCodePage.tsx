import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { CheckCircle, Copy, Check, Smartphone, Shield, Clock, ArrowLeft } from "lucide-react";
import sloveniaLogo from "../assets/slovenia-badge.png";

function decodeJwtPayload(token: string): Record<string, string> | null {
  try {
    const parts = token.split(".");
    if (parts.length < 2) return null;
    const base64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
    return JSON.parse(atob(padded)) as Record<string, string>;
  } catch {
    return null;
  }
}

function generateProvisioningCode(token: string): string {
  const parts = token.split(".");
  if (parts.length < 3) return "XXXXX-XXXXX-XXXXX";
  const sig = parts[2].replace(/-/g, "").replace(/_/g, "").replace(/=/g, "");
  const chars = sig.replace(/[^A-Za-z0-9]/g, "").toUpperCase().slice(0, 15);
  const padded = chars.padEnd(15, "0");
  return `${padded.slice(0, 5)}-${padded.slice(5, 10)}-${padded.slice(10, 15)}`;
}

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

  const token = searchParams.get("token");

  useEffect(() => {
    if (!token) {
      navigate("/", { replace: true });
    }
  }, [token, navigate]);

  if (!token) return null;

  const payload = decodeJwtPayload(token);
  const firstName = payload?.firstName ?? "";
  const lastName = payload?.lastName ?? "";
  const code = generateProvisioningCode(token);

  const handleCopy = async () => {
    await navigator.clipboard.writeText(code);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

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
        <div className="mb-8 text-center">
          <div className="mx-auto mb-5 flex h-16 w-16 items-center justify-center rounded-full bg-[#1a3d2b]">
            <CheckCircle size={32} className="text-white" />
          </div>
          <h1 className="text-3xl font-bold text-slate-900">Registracija uspešna!</h1>
          {firstName ? (
            <p className="mt-2 text-base text-slate-600">
              Dobrodošli,{" "}
              <span className="font-semibold text-slate-900">
                {firstName} {lastName}
              </span>
              . Vaša identiteta je bila potrjena prek SI-PASS.
            </p>
          ) : (
            <p className="mt-2 text-base text-slate-600">
              Vaša identiteta je bila uspešno potrjena prek SI-PASS.
            </p>
          )}
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
                {code}
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
                Velja 24 ur
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
      </section>
    </div>
  );
}
