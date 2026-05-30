export default function LandingPage() {
  const sipassUrl = import.meta.env.VITE_SIPASS_URL ?? "";

  const handleRegister = () => {
    window.location.href = sipassUrl + "/sipass/login";
  };

  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top,_#dbeafe_0%,_#f8fafc_45%,_#e2e8f0_100%)] text-slate-900">
      <section className="mx-auto flex min-h-screen max-w-6xl items-center px-6 py-16">
        <div className="grid gap-12 lg:grid-cols-2 lg:items-center">
          <div className="max-w-2xl">
            <span className="mb-4 inline-flex rounded-full border border-blue-200 bg-white/80 px-4 py-2 text-sm font-semibold text-blue-700 shadow-sm">
              Pametni paketnik
            </span>

            <h1 className="text-5xl font-bold tracking-tight sm:text-6xl">
              ePrevzem
            </h1>

            <p className="mt-6 text-lg leading-8 text-slate-600">
              Varna spletna aplikacija za prevzem osebnih dokumentov preko
              pametnega paketnika z digitalno identifikacijo uporabnika.
            </p>

            <div className="mt-8 flex flex-wrap gap-4">
              <button
                onClick={handleRegister}
                className="rounded-2xl bg-slate-900 px-6 py-3 text-base font-semibold text-white shadow-lg transition hover:bg-slate-700"
              >
                Registriraj se
              </button>

              <button className="rounded-2xl border border-slate-300 bg-white px-6 py-3 text-base font-semibold text-slate-700 transition hover:border-slate-400 hover:bg-slate-50">
                Več informacij
              </button>
            </div>
          </div>
        </div>
      </section>
    </main>
  );
}
