import { useState, useRef, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import {
  ChevronDown,
  Shield,
  ShieldCheck,
  Lock,
  Building2,
  QrCode,
  Package,
  Bell,
  LogOut,
  Menu,
  X,
} from "lucide-react";
import sloveniaLogo from "../assets/slovenia-badge.png";
import sloveniaLogoWhite from "../assets/slovenia-badge-white.png";
import sitrustLogo from "../assets/sitrust-logo.png";

function LoginDropdown({ variant }: { variant: "navbar" | "hero" }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  useEffect(() => {
    function handleOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node))
        setOpen(false);
    }
    document.addEventListener("mousedown", handleOutside);
    return () => document.removeEventListener("mousedown", handleOutside);
  }, []);

  const isNavbar = variant === "navbar";

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className={
          isNavbar
            ? "flex items-center gap-2 rounded-xl border border-[#1a3d2b] px-4 py-2.5 text-sm font-semibold text-[#1a3d2b] transition hover:bg-[#1a3d2b]/5"
            : "flex items-center gap-2 rounded-xl border border-slate-300 bg-white px-5 py-3 text-sm font-semibold text-slate-700 transition hover:border-slate-400 hover:bg-slate-50"
        }
      >
        <Building2 size={15} />
        Prijava za organizacije
        <ChevronDown
          size={14}
          className={`transition-transform duration-200 ${open ? "rotate-180" : ""}`}
        />
      </button>

      {open && (
        <div className="absolute right-0 top-full z-50 mt-2 w-60 overflow-hidden rounded-xl border border-slate-100 bg-white shadow-xl">
          <button
            onClick={() => {
              navigate("/prijava");
              setOpen(false);
            }}
            className="flex w-full flex-col items-start px-4 py-3.5 text-left transition hover:bg-slate-50"
          >
            <span className="text-sm font-semibold text-slate-900">
              Sem zaposleni v organizaciji
            </span>
            <span className="mt-0.5 text-xs text-slate-500">
              Prijava z e-pošto in geslom
            </span>
          </button>
          <div className="border-t border-slate-100" />
          <button
            onClick={() => {
              navigate("/prijava/skrbnik");
              setOpen(false);
            }}
            className="flex w-full flex-col items-start px-4 py-3.5 text-left transition hover:bg-slate-50"
          >
            <span className="text-sm font-semibold text-slate-900">
              Sem skrbnik
            </span>
            <span className="mt-0.5 text-xs text-slate-500">
              Upravljanje organizacije in paketnikov
            </span>
          </button>
        </div>
      )}
    </div>
  );
}

export default function LandingPage() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const sipassUrl = import.meta.env.VITE_SIPASS_URL ?? "";

  const handleRegister = () => {
    const redirectUrl = `${window.location.origin}/registracija`;
    window.location.href = `${sipassUrl}/sipass/login?redirectUrl=${encodeURIComponent(redirectUrl)}`;
  };

  const navLinks = [
    { label: "O storitvi", href: "#storitev" },
    { label: "Kako deluje", href: "#kako-deluje" },
    { label: "Podpora", href: "#podpora" },
  ];

  return (
    <div className="min-h-screen bg-[#f5f0e8] text-slate-900">
      {/* Navbar */}
      <nav className="sticky top-0 z-40 border-b border-slate-100 bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4 sm:px-6">
          {/* Logo */}
          <div className="flex items-center gap-3">
            <img
              src={sloveniaLogo}
              alt="Republika Slovenija"
              className="h-8 w-8 sm:h-9 sm:w-9"
            />
            <div className="leading-tight">
              <p className="hidden text-[9px] font-semibold uppercase tracking-widest text-slate-400 sm:block">
                Republika Slovenija
              </p>
              <p className="text-base font-bold text-[#1a3d2b]">ePrevzem</p>
            </div>
          </div>

          {/* Desktop nav links */}
          <div className="hidden items-center gap-8 md:flex">
            {navLinks.map((l) => (
              <a
                key={l.href}
                href={l.href}
                className="text-sm font-medium text-slate-600 transition hover:text-[#1a3d2b]"
              >
                {l.label}
              </a>
            ))}
          </div>

          {/* Right side */}
          <div className="flex items-center gap-3">
            <button
              onClick={handleRegister}
              className="hidden items-center gap-2 rounded-xl bg-[#1a3d2b] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[#153122] sm:flex"
            >
              <img
                src={sitrustLogo}
                alt="SI-PASS"
                className="h-4 w-4 object-contain"
              />
              Registriraj se s SI-PASS
            </button>
            <div className="hidden sm:block">
              <LoginDropdown variant="navbar" />
            </div>
            {/* Hamburger */}
            <button
              onClick={() => setMobileMenuOpen((v) => !v)}
              className="flex h-9 w-9 items-center justify-center rounded-lg text-slate-600 transition hover:bg-slate-100 md:hidden"
            >
              {mobileMenuOpen ? <X size={20} /> : <Menu size={20} />}
            </button>
          </div>
        </div>

        {/* Mobile menu */}
        {mobileMenuOpen && (
          <div className="border-t border-slate-100 bg-white px-4 pb-4 pt-2 md:hidden">
            <div className="flex flex-col gap-1">
              {navLinks.map((l) => (
                <a
                  key={l.href}
                  href={l.href}
                  onClick={() => setMobileMenuOpen(false)}
                  className="rounded-lg px-3 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 hover:text-[#1a3d2b]"
                >
                  {l.label}
                </a>
              ))}
              <div className="mt-2 border-t border-slate-100 pt-3">
                <LoginDropdown variant="navbar" />
              </div>
            </div>
          </div>
        )}
      </nav>

      {/* Hero */}
      <section
        id="storitev"
        className="mx-auto max-w-6xl px-4 py-12 sm:px-6 lg:py-20"
      >
        <div className="grid gap-10 lg:grid-cols-2 lg:items-center lg:gap-12">
          {/* Left */}
          <div>
            <span className="mb-5 inline-flex items-center gap-2 rounded-full border border-[#1a3d2b]/20 bg-white px-4 py-2 text-sm font-semibold text-[#1a3d2b] shadow-sm">
              <ShieldCheck size={14} /> Zanesljiv prevzem dokumentov
            </span>

            <h1 className="text-4xl font-bold leading-tight tracking-tight text-slate-900 sm:text-5xl lg:text-6xl">
              Uradni dokumenti, pripravljeni za vas — brez čakalne vrste.
            </h1>

            <p className="mt-5 text-base leading-8 text-slate-600 sm:text-lg">
              Organizacija odloži vaš dokument v zaklenjeni predalček. Ko
              pridete, se identificirate s telefonom — predalček se odpre sam.
            </p>

            <div className="mt-7 flex flex-wrap gap-3">
              <button
                onClick={handleRegister}
                className="flex items-center gap-2 rounded-xl bg-[#1a3d2b] px-5 py-3 text-sm font-semibold text-white shadow-md transition hover:bg-[#153122]"
              >
                <img
                  src={sitrustLogo}
                  alt="SI-PASS"
                  className="h-5 w-5 object-contain"
                />
                Registriraj se s SI-PASS
              </button>
              <LoginDropdown variant="hero" />
            </div>

            {/* Who is this for */}
            <div className="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div className="rounded-xl border border-[#1a3d2b]/15 bg-white/70 px-4 py-3">
                <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-[#1a3d2b]">
                  <Package size={12} /> Jaz prevzemam dokument
                </div>
                <p className="mt-1 text-xs leading-relaxed text-slate-500">
                  Čakate pogodbo, osebni dokument ali uradni dopis? Enkrat se
                  registrirate z SI-PASS, potem je vse samodejno.
                </p>
              </div>
              <div className="rounded-xl border border-slate-200 bg-white/70 px-4 py-3">
                <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                  <Building2 size={12} /> Delam v organizaciji
                </div>
                <p className="mt-1 text-xs leading-relaxed text-slate-500">
                  Vaša organizacija prek ePrevzem dostavlja dokumente strankam?
                  Prijavite se kot zaposleni ali skrbnik.
                </p>
              </div>
            </div>

            <div className="mt-7 flex flex-wrap gap-5">
              <div className="flex items-center gap-2 text-sm font-medium text-slate-600">
                <Lock size={14} className="text-[#1a3d2b]" /> Zanesljiva dostava
              </div>
              <div className="flex items-center gap-2 text-sm font-medium text-slate-600">
                <Shield size={14} className="text-[#1a3d2b]" /> Preverjena
                identiteta
              </div>
              <div className="flex items-center gap-2 text-sm font-medium text-slate-600">
                <Building2 size={14} className="text-[#1a3d2b]" /> Za vse
                organizacije
              </div>
            </div>
          </div>

          {/* Right — decorative, desktop only */}
          <div className="hidden lg:flex lg:justify-center">
            <div className="relative flex h-96 w-96 items-center justify-center">
              <div className="absolute inset-0 rounded-3xl bg-[#dde9e3]" />
              <div className="absolute h-56 w-56 rounded-full border border-[#1a3d2b]/10" />
              <div className="absolute h-72 w-72 rounded-full border border-[#1a3d2b]/05" />
              <div className="relative z-10 flex h-24 w-24 items-center justify-center rounded-full bg-[#1a3d2b] shadow-2xl">
                <ShieldCheck size={40} className="text-white" />
              </div>
              <div className="absolute left-8 top-10 z-10 flex h-14 w-14 items-center justify-center rounded-2xl bg-white shadow-lg">
                <QrCode size={24} className="text-amber-500" />
              </div>
              <div className="absolute right-8 top-10 z-10 flex h-14 w-14 items-center justify-center rounded-2xl bg-white shadow-lg">
                <Lock size={24} className="text-slate-600" />
              </div>
              <div className="absolute bottom-10 right-8 z-10 flex h-14 w-14 items-center justify-center rounded-2xl bg-white shadow-lg">
                <Package size={24} className="text-blue-500" />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Why ePrevzem */}
      <section className="bg-white pb-8 pt-12">
        <div className="mx-auto max-w-6xl px-4 sm:px-6">
          <div className="rounded-2xl bg-[#f5f0e8] px-6 py-8 sm:px-8 sm:py-10 md:px-12">
            <div className="grid gap-6 md:grid-cols-2 md:items-center md:gap-8">
              <div>
                <p className="text-xs font-semibold uppercase tracking-widest text-[#1a3d2b]">
                  Zakaj ePrevzem?
                </p>
                <h2 className="mt-3 text-xl font-bold leading-snug text-slate-900 sm:text-2xl">
                  Nekateri dokumenti morajo biti izročeni osebno. Zdaj to ne
                  pomeni več čakanja v vrsti.
                </h2>
              </div>
              <div className="space-y-4 text-sm leading-relaxed text-slate-600">
                <p>
                  V Evropi — in pri nas — zakon za določene uradne dokumente
                  zahteva osebno izročitev. Osebne izkaznice, sodna pisma,
                  overjena potrdila in podobni dokumenti ne smejo biti poslani
                  po navadni pošti: izročeni morajo biti točno določeni osebi.
                </p>
                <p>
                  ePrevzem to reši. Organizacija dokument odloži v pametni
                  paketnik, sistem pa zagotovi, da ga lahko odpre samo prava
                  oseba — z digitalno potrjeno identiteto. Brez okenc, brez
                  vrst, brez omejenega delovnega časa.
                </p>
              </div>
            </div>
          </div>

          {/* How we guarantee the right person */}
          <div className="mt-10">
            <p className="text-xs font-semibold uppercase tracking-widest text-[#1a3d2b]">
              Kako zagotovimo, da dokument dobi prava oseba?
            </p>
            <p className="mt-2 text-sm text-slate-500">
              Vsak prevzem je zaščiten s štirimi plastmi preverjanja.
            </p>

            <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              {[
                {
                  num: "1",
                  title: "Organizacija odloži dokument",
                  desc: "Uslužbenec vstavi dokument v paketnik in ga dodeli točno določenemu prejemniku po imenu in priimku.",
                },
                {
                  num: "2",
                  title: "Registracija z uradno identiteto",
                  desc: "Prejemnik se enkrat registrira z eOsebno izkaznico ali prek SI-PASS — državno potrjenim načinom identifikacije. Sistem ve, kdo ste.",
                },
                {
                  num: "3",
                  title: "Aktivacija naprave",
                  desc: "Po registraciji prejmete kodo, s katero aktivirate svojo mobilno napravo. Samo ta naprava je vezana na vaš račun — nobena druga ne more dostopati.",
                },
                {
                  num: "4",
                  title: "Biometrija ali PIN ob vsakem prevzemu",
                  desc: "Ko pridete po dokument, aplikacija zahteva potrditev z prstnim odtisom, Face ID ali PIN-om. Šele nato se predalček odpre.",
                },
              ].map(({ num, title, desc }) => (
                <div
                  key={num}
                  className="relative rounded-xl border border-[#1a3d2b]/10 bg-white px-5 py-5"
                >
                  <div className="mb-3 flex h-7 w-7 items-center justify-center rounded-full bg-[#1a3d2b] text-xs font-bold text-white">
                    {num}
                  </div>
                  <h3 className="text-sm font-bold text-slate-900">{title}</h3>
                  <p className="mt-1.5 text-xs leading-relaxed text-slate-500">
                    {desc}
                  </p>
                  {num !== "4" && (
                    <div className="absolute -right-2 top-1/2 hidden -translate-y-1/2 text-slate-300 lg:block">
                      →
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* Kako deluje */}
      <section id="kako-deluje" className="bg-white pb-14 pt-10">
        <div className="mx-auto max-w-6xl px-4 sm:px-6">
          <h2 className="text-2xl font-bold text-slate-900 sm:text-3xl">
            Kako deluje
          </h2>
          <p className="mt-2 text-slate-500">
            Od obvestila do dokumenta v treh preprostih korakih.
          </p>

          <div className="mt-8 grid gap-4 sm:gap-6 md:grid-cols-3">
            {[
              {
                icon: <LogOut size={20} className="text-[#1a3d2b]" />,
                num: "01",
                title: "Prijava",
                desc: "Odprite ePrevzem in se prijavite s svojim računom SI-PASS. Vaši čakajoči prevzemi so takoj vidni.",
              },
              {
                icon: <Bell size={20} className="text-[#1a3d2b]" />,
                num: "02",
                title: "Obvestilo",
                desc: "Ko organizacija odloži dokument, dobite sporočilo z naslovom paketnika in rokom, do kdaj ga morate prevzeti.",
              },
              {
                icon: <Lock size={20} className="text-[#1a3d2b]" />,
                num: "03",
                title: "Prevzem",
                desc: "Pridete do paketnika, na telefonu potrdite svojo identiteto in predalček se odpre. Vzamete dokument — in to je to.",
              },
            ].map(({ icon, num, title, desc }) => (
              <div
                key={num}
                className="rounded-2xl border border-slate-100 p-5 shadow-sm sm:p-6"
              >
                <div className="flex items-start justify-between">
                  <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-[#f5f0e8]">
                    {icon}
                  </div>
                  <span className="text-2xl font-bold text-slate-200">
                    {num}
                  </span>
                </div>
                <h3 className="mt-4 text-base font-bold text-slate-900">
                  {title}
                </h3>
                <p className="mt-1.5 text-sm leading-relaxed text-slate-500">
                  {desc}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer id="podpora" className="bg-[#1a3d2b] text-white">
        <div className="mx-auto max-w-6xl px-4 py-12 sm:px-6 sm:py-14">
          <div className="grid gap-8 sm:grid-cols-2 md:grid-cols-4 md:gap-10">
            <div className="sm:col-span-2 md:col-span-1">
              <div className="flex items-center gap-3">
                <img
                  src={sloveniaLogoWhite}
                  alt="Republika Slovenija"
                  className="h-9 w-9"
                />
                <div className="leading-tight">
                  <p className="text-[9px] font-semibold uppercase tracking-widest text-white/50">
                    Republika Slovenija
                  </p>
                  <p className="text-base font-bold text-white">ePrevzem</p>
                </div>
              </div>
              <p className="mt-4 text-sm leading-relaxed text-white/60">
                Uradna storitev za zanesljiv prevzem dokumentov prek pametnih
                paketnikov.
              </p>
            </div>

            <div>
              <p className="mb-4 text-xs font-semibold uppercase tracking-widest text-white/40">
                Storitev
              </p>
              <ul className="space-y-2.5">
                {["O ePrevzem", "Kako deluje", "Pogosta vprašanja"].map(
                  (item) => (
                    <li key={item}>
                      <a
                        href="#"
                        className="text-sm text-white/70 transition hover:text-white"
                      >
                        {item}
                      </a>
                    </li>
                  ),
                )}
              </ul>
            </div>

            <div>
              <p className="mb-4 text-xs font-semibold uppercase tracking-widest text-white/40">
                Organizacije
              </p>
              <ul className="space-y-2.5">
                {["Postani partner", "Cenik", "Dokumentacija"].map((item) => (
                  <li key={item}>
                    <a
                      href="#"
                      className="text-sm text-white/70 transition hover:text-white"
                    >
                      {item}
                    </a>
                  </li>
                ))}
              </ul>
            </div>

            <div>
              <p className="mb-4 text-xs font-semibold uppercase tracking-widest text-white/40">
                Podpora
              </p>
              <ul className="space-y-2.5">
                {["Center za pomoč", "Kontakt", "Stanje sistema"].map(
                  (item) => (
                    <li key={item}>
                      <a
                        href="#"
                        className="text-sm text-white/70 transition hover:text-white"
                      >
                        {item}
                      </a>
                    </li>
                  ),
                )}
              </ul>
            </div>
          </div>

          <div className="mt-10 flex flex-col gap-2 border-t border-white/10 pt-8 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-xs text-white/40">© 2026 Republika Slovenija</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
