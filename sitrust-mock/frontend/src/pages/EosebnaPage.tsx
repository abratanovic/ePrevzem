import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { checkAuth, normalizeQrCodeImage } from "../lib/authService";

const steps = [
  "Na mobilni napravi odprite aplikacijo eOsebna.",
  "Z aplikacijo eOsebna skenirajte kodo QR.",
  "Prijavite se neposredno z aplikacijo ali s prisotnostjo osebne izkaznice.",
  "Nadaljujte po navodilih v aplikaciji.",
];

export default function EosebnaPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const attemptId = searchParams.get("attemptId");
  const qrCodeImage = searchParams.get("qrCodeImage");
  const fallbackRedirectUrl =
    searchParams.get("redirectUrl") ?? `${window.location.origin}/home`;
  const hasRequiredParams = Boolean(attemptId && qrCodeImage);

  const [status, setStatus] = useState<"pending" | "complete" | "error">(
    "pending",
  );
  const [error, setError] = useState<string | null>(null);

  const qrImageSrc = useMemo(() => {
    if (!qrCodeImage) {
      return null;
    }

    return normalizeQrCodeImage(qrCodeImage);
  }, [qrCodeImage]);

  useEffect(() => {
    if (!attemptId) {
      return;
    }

    let cancelled = false;

    const pollStatus = async () => {
      try {
        const result = await checkAuth(attemptId);

        if (cancelled) {
          return;
        }

        if (result.status === "pending") {
          setStatus("pending");
          return;
        }

        if (result.status === "complete") {
          setStatus("complete");
          window.location.href = result.redirectUrl || fallbackRedirectUrl;
        }
      } catch (err) {
        if (cancelled) {
          return;
        }

        setStatus("error");
        setError(
          err instanceof Error
            ? err.message
            : "Napaka pri preverjanju prijave.",
        );
      }
    };

    void pollStatus();

    const intervalId = window.setInterval(() => {
      void pollStatus();
    }, 2000);

    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, [attemptId, fallbackRedirectUrl]);

  return (
    <main className="min-h-screen bg-[#f7f5f3] text-slate-800">
      <header className="h-12 bg-[#2f2f2f]" />

      <section className="mx-auto max-w-5xl px-6 py-10">
        <div className="mb-8">
          <div className="inline-block text-left">
            <p className="text-sm font-semibold uppercase tracking-wide text-orange-500">
              SI-TRUST
            </p>
            <h1 className="text-4xl font-bold text-slate-700">SI-PASS</h1>
            <p className="text-sm text-slate-500">
              Državni center za storitve zaupanja
            </p>
          </div>
        </div>

        <div className="mb-8 rounded-[1.5rem] bg-white px-6 py-5 shadow-sm ring-1 ring-slate-200">
          <p className="text-sm text-slate-700">
            Če imate težave pri prijavi z mobilno aplikacijo eOsebna, jo{" "}
            <span className="font-semibold">nadgradite</span> na najnovejšo
            različico.
          </p>
        </div>

        <div className="rounded-[2rem] bg-white p-8 shadow-xl ring-1 ring-slate-200">
          <div className="mb-8 flex items-center justify-between">
            <h2 className="text-3xl font-bold text-slate-800">
              Prijava z mobilno aplikacijo eOsebna
            </h2>

            <button
              onClick={() => navigate("/sipass/login")}
              className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 hover:bg-slate-50"
            >
              Nazaj
            </button>
          </div>

          {!hasRequiredParams ? (
            <div className="mb-8 rounded-2xl border border-red-300 bg-red-50 px-5 py-4 text-sm text-red-700">
              Seja za prijavo ni bila pravilno inicializirana. Vrnite se na
              prijavno stran in začnite postopek znova.
            </div>
          ) : null}

          <div className="grid gap-8 lg:grid-cols-[1.4fr_0.9fr]">
            <div className="grid gap-4 sm:grid-cols-2">
              {steps.map((step, index) => (
                <div
                  key={step}
                  className="rounded-3xl bg-slate-100 p-5 shadow-sm ring-1 ring-slate-200"
                >
                  <div className="mb-4 flex h-10 w-10 items-center justify-center rounded-full bg-slate-700 text-sm font-bold text-white">
                    {index + 1}
                  </div>
                  <p className="text-base font-medium leading-7 text-slate-700">
                    {step}
                  </p>
                </div>
              ))}
            </div>

            <div className="flex items-center justify-center">
              {qrImageSrc ? (
                <img
                  src={qrImageSrc}
                  alt="QR koda za prijavo"
                  className="h-72 w-72 rounded-2xl border-4 border-slate-900 bg-white object-contain p-3 shadow-md"
                />
              ) : (
                <div className="flex h-72 w-72 items-center justify-center rounded-2xl border-4 border-slate-900 bg-white shadow-md">
                  <p className="text-sm text-slate-500">
                    QR koda trenutno ni na voljo.
                  </p>
                </div>
              )}
            </div>
          </div>

          <div className="mt-8 rounded-2xl bg-amber-50 px-5 py-4 text-sm text-amber-800 ring-1 ring-amber-200">
            {status === "pending" &&
              "Status: Čakanje na potrditev v mobilni aplikaciji."}
            {status === "complete" &&
              "Status: Prijava uspešna. Preusmerjanje..."}
            {status === "error" &&
              `Status: Prišlo je do napake. ${error ?? "Poskusite znova."}`}
          </div>
        </div>

        <div className="mt-8 rounded-[1.5rem] bg-slate-100 px-6 py-5 shadow-sm ring-1 ring-slate-200">
          <p className="text-lg font-semibold text-slate-700">
            Še nimate aplikacije eOsebna?
          </p>
        </div>
      </section>
    </main>
  );
}
