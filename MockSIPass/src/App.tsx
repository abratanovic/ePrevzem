export default function App() {
  return (
    <main className="min-h-screen bg-slate-100 text-slate-900">
      <section className="mx-auto flex min-h-screen max-w-6xl items-center px-6 py-16">
        <div className="max-w-2xl">
          <span className="mb-4 inline-block rounded-full bg-blue-100 px-4 py-2 text-sm font-semibold text-blue-700">
            Pametni paketnik
          </span>

          <h1 className="text-5xl font-bold tracking-tight sm:text-6xl">
            ePrevzem
          </h1>

          <p className="mt-6 text-lg leading-8 text-slate-600">
            Varna spletna aplikacija za prevzem osebnih dokumentov preko
            pametnega paketnika z digitalno identifikacijo uporabnika.
          </p>

          <div className="mt-8 flex gap-4">
            <button className="rounded-xl bg-slate-900 px-6 py-3 text-base font-semibold text-white transition hover:bg-slate-700">
              Registriraj se
            </button>
          </div>
        </div>
      </section>
    </main>
  );
}
