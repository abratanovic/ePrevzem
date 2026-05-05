import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import rsLogo from "../assets/rs-hd.png";
import rsCasLogo from "../assets/rs-CAS.png";
import euLogo from "../assets/evropski-sklad.jpg";
import { initiateAuth } from "../lib/authService";

const loginOptions = [
  "Uporabniško ime in geslo",
  "Mobilna aplikacija eOsebna",
  "Osebna izkaznica s čitalnikom kartic",
  "smsPASS",
  "Digitalno potrdilo",
  "Halcom One",
  "Rekono",
  "Prijava državljana EU",
  "Facebook",
  "Google",
  "Microsoft",
  "ArnesAAI",
];
function UserIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" className="h-6 w-6 fill-white">
      <path d="M12 12a4.5 4.5 0 1 0-4.5-4.5A4.5 4.5 0 0 0 12 12Zm0 2.25c-3.85 0-7 2.32-7 5.18 0 .32.26.57.58.57h12.84c.32 0 .58-.25.58-.57 0-2.86-3.15-5.18-7-5.18Z" />
    </svg>
  );
}

export default function SiPassLoginPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const redirectUrl =
    searchParams.get("redirectUrl") ?? `${window.location.origin}/home`;

  const handleOptionClick = async (option: string) => {
    if (option !== "Mobilna aplikacija eOsebna" || isSubmitting) {
      return;
    }

    try {
      setIsSubmitting(true);
      setError(null);

      const result = await initiateAuth(redirectUrl);
      const nextParams = new URLSearchParams({
        attemptId: result.attemptId,
        qrCodeImage: result.qrCodeImage,
        redirectUrl,
      });

      navigate(`/sipass/eosebna?${nextParams.toString()}`);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Prišlo je do napake pri začetku prijave.",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="min-h-screen bg-[#f9f8f6] text-[#4b4b4b]">
      <header className="h-[51px] bg-[#2f2d2c]">
        <div className="mx-auto flex h-full max-w-[1180px] items-center justify-between px-6">
          <div className="flex h-full items-center">
            <div className="flex h-full items-center pr-5">
              <img
                src={rsLogo}
                alt="Republika Slovenija"
                className="h-8 w-auto object-contain"
              />
            </div>

            <div className="h-10 w-px bg-[#5a5857]" />

            <div className="pl-5 pt-3 text-[12px] font-semibold tracking-tight text-[#aaa]">
              SI-PASS
            </div>
          </div>

          <div className="flex items-center gap-6">
            <div className="h-10 w-px bg-[#5a5857]" />
            <UserIcon />
          </div>
        </div>
      </header>

      <section className="relative overflow-hidden">
        <div className="pointer-events-none absolute inset-0">
          <div className="absolute left-1/2 top-0 h-[420px] w-[900px] -translate-x-1/2 bg-[linear-gradient(120deg,transparent_0%,transparent_33%,rgba(0,0,0,0.018)_33%,rgba(0,0,0,0.018)_43%,transparent_43%,transparent_52%,rgba(0,0,0,0.018)_52%,rgba(0,0,0,0.018)_62%,transparent_62%,transparent_100%)]" />
        </div>

        <div className="relative mx-auto flex min-h-[calc(100vh-51px)] max-w-[1180px] flex-col items-center px-6 pb-16 pt-[56px]">
          <div className="w-full max-w-[444px]">
            <div className="mb-[60px] flex justify-start">
              <img
                src={rsCasLogo}
                alt="SI-PASS"
                className="h-[50px] w-[716px] object-contain mx-auto -ml-4"
              />
            </div>

            <h2 className="mb-[25px] text-left text-[24px] font-light tracking-tight text-[#3d3d3d]">
              Prosimo, izberite želeni način prijave
            </h2>

            <div className="mb-6 border border-[#5d908f] bg-transparent px-[21px] py-[15px] text-[15px] leading-[1.15] text-[#4f807d]">
              Če imate težave pri prijavi z mobilno aplikacijo eOsebna, jo
              nadgradite na najnovejšo različico.
            </div>

            {error ? (
              <div className="mb-4 border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-700">
                {error}
              </div>
            ) : null}

            <div className="space-y-[3px]">
              {loginOptions.map((option) => {
                const isActive = option === "Mobilna aplikacija eOsebna";

                return (
                  <button
                    key={option}
                    type="button"
                    onClick={() => void handleOptionClick(option)}
                    disabled={isSubmitting && isActive}
                    className={`relative flex min-h-[42px] w-full items-center border border-[#dddddd] bg-[#ededed] px-[28px] text-left ${
                      isActive
                        ? "cursor-pointer hover:bg-[#e7e7e7]"
                        : "cursor-default"
                    } ${isSubmitting && isActive ? "opacity-70" : ""}`}
                  >
                    <span className="pr-5 text-[14px] font-bold leading-none text-[#4d7f7b]">
                      {option}
                    </span>
                    <span className="absolute right-0 top-0 flex h-[18px] w-[18px] items-center justify-center bg-[#cfcfcf] text-[11px] font-bold leading-none text-white">
                      i
                    </span>
                  </button>
                );
              })}
            </div>

            <div className="mt-7">
              <button
                type="button"
                className="relative flex min-h-[42px] w-full items-center border border-[#dddddd] bg-[#ededed] px-[28px] text-left"
              >
                <span className="pr-5 text-[10px] font-bold leading-none text-[#4d7f7b]">
                  Nič od navedenega
                </span>
                <span className="absolute right-0 top-0 flex h-[18px] w-[18px] items-center justify-center bg-[#cfcfcf] text-[11px] font-bold leading-none text-white">
                  i
                </span>
              </button>
            </div>
          </div>

          <div className="mt-20 flex flex-col items-center">
            <img
              src={euLogo}
              alt="Evropski socialni sklad"
              className="h-auto w-[120px] object-contain opacity-70"
            />
            <p className="mt-8 text-[14px] font-semibold text-[#b9b9b9]">
              1-5168eb76-283d-428b-bf0c-26ef9f025c0d
            </p>
          </div>
        </div>
      </section>

      <footer className="border-t border-[#ececec] bg-[#f0f0f0]">
        <div className="mx-auto flex max-w-[1180px] items-center justify-between px-6 py-3.5 text-[13px] text-[#4d8480]">
          <div className="flex items-center gap-6">
            <button type="button" className="hover:underline">
              Slovenščina
            </button>
            <span className="text-[#c4c4c4]">•</span>
            <button type="button" className="hover:underline">
              English
            </button>
          </div>

          <button type="button" className="hover:underline">
            Pomoč uporabnikom
          </button>

          <p className="text-[#3d3d3d]">© 2026 Republika Slovenija</p>
        </div>
      </footer>
    </main>
  );
}
