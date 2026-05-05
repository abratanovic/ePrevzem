import { useMemo } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";

const activePickups = [
  {
    title: "Osebna izkaznica",
    location: "Paketnik Maribor Center",
    deadline: "Prevzem do 5. maja 2026",
    status: "Pripravljeno za prevzem",
  },
  {
    title: "Prometno dovoljenje",
    location: "Paketnik Ljubljana BTC",
    deadline: "Prevzem do 8. maja 2026",
    status: "V pripravi",
  },
];

const activityItems = [
  "Uspešna identifikacija preko SI-PASS",
  "Prevzem dokumenta rezerviran v paketniku",
  "Obvestilo o pripravi dokumenta poslano uporabniku",
];

export default function EPrevzemHomePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const token = searchParams.get("token");
  const hasToken = useMemo(() => Boolean(token), [token]);

  const handleStartAuth = () => {
    const redirectUrl = `${window.location.origin}/home`;
    navigate(`/sipass/login?redirectUrl=${encodeURIComponent(redirectUrl)}`);
  };

  return (
    <main className="min-h-screen bg-slate-100 text-slate-900">
      <header className="border-b border-slate-200 bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <div>
            <p className="text-sm font-semibold uppercase tracking-[0.22em] text-blue-700">
              ePrevzem
            </p>
            <h1 className="text-2xl font-bold text-slate-900">
              Uporabniški portal za prevzem dokumentov
            </h1>
          </div>

          <button
            type="button"
            onClick={handleStartAuth}
            className="rounded-2xl bg-slate-900 px-5 py-3 text-sm font-semibold text-white transition hover:bg-slate-700"
          >
            Nova prijava
          </button>
        </div>
      </header>

      <section className="mx-auto max-w-6xl px-6 py-10">
        <div className="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
          <div className="space-y-6">
            <div className="rounded-3xl bg-white p-8 shadow-sm ring-1 ring-slate-200">
              <p className="text-sm font-semibold uppercase tracking-[0.22em] text-slate-500">
                Nadzorna plošča
              </p>
              <h2 className="mt-3 text-3xl font-bold text-slate-900">
                Upravljanje osebnih dokumentov in prevzemov
              </h2>
              <p className="mt-4 max-w-2xl text-base leading-7 text-slate-600">
                Na enem mestu spremljajte aktivne prevzeme, lokacije paketnikov,
                roke za prevzem in zgodovino varnostnih preverjanj.
              </p>

              <div className="mt-6 flex flex-wrap gap-3">
                <span className="rounded-full bg-emerald-100 px-4 py-2 text-sm font-semibold text-emerald-700">
                  {hasToken ? "Identifikacija zaključena" : "Pripravljen za prijavo"}
                </span>
                <span className="rounded-full bg-blue-100 px-4 py-2 text-sm font-semibold text-blue-700">
                  2 aktivna prevzema
                </span>
              </div>
            </div>

            <div className="rounded-3xl bg-white p-8 shadow-sm ring-1 ring-slate-200">
              <div className="flex items-center justify-between">
                <h3 className="text-2xl font-bold text-slate-900">
                  Aktivni prevzemi
                </h3>
                <button
                  type="button"
                  className="text-sm font-semibold text-blue-700 hover:underline"
                >
                  Preglej vse
                </button>
              </div>

              <div className="mt-6 grid gap-4">
                {activePickups.map((pickup) => (
                  <article
                    key={pickup.title}
                    className="rounded-2xl border border-slate-200 bg-slate-50 p-5"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-3">
                      <div>
                        <h4 className="text-lg font-bold text-slate-900">
                          {pickup.title}
                        </h4>
                        <p className="mt-1 text-sm text-slate-600">
                          {pickup.location}
                        </p>
                      </div>

                      <span className="rounded-full bg-white px-3 py-1 text-xs font-semibold text-slate-700 ring-1 ring-slate-200">
                        {pickup.status}
                      </span>
                    </div>

                    <p className="mt-4 text-sm font-medium text-slate-700">
                      {pickup.deadline}
                    </p>
                  </article>
                ))}
              </div>
            </div>
          </div>

          <div className="space-y-6">
            <div className="rounded-3xl bg-white p-8 shadow-sm ring-1 ring-slate-200">
              <h3 className="text-2xl font-bold text-slate-900">
                Zadnje aktivnosti
              </h3>

              <ul className="mt-6 space-y-4">
                {activityItems.map((item) => (
                  <li
                    key={item}
                    className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700"
                  >
                    {item}
                  </li>
                ))}
              </ul>
            </div>

            <div className="rounded-3xl bg-slate-900 p-8 text-white shadow-sm">
              <p className="text-sm font-semibold uppercase tracking-[0.22em] text-slate-300">
                Hiter dostop
              </p>
              <h3 className="mt-3 text-2xl font-bold">
                Potrebujete nov varen prevzem?
              </h3>
              <p className="mt-4 text-sm leading-7 text-slate-300">
                Zaženite novo identifikacijo in nadaljujte postopek prevzema
                dokumentov preko SI-PASS in mobilne aplikacije eOsebna.
              </p>

              <button
                type="button"
                onClick={handleStartAuth}
                className="mt-6 rounded-2xl bg-white px-5 py-3 text-sm font-semibold text-slate-900 transition hover:bg-slate-200"
              >
                Začni identifikacijo
              </button>
            </div>
          </div>
        </div>
      </section>
    </main>
  );
}
