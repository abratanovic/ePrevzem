import type { ReactNode } from "react";
import sloveniaBadge from "../../assets/slovenia-badge-white.png";

interface Bullet {
  icon: ReactNode;
  text: string;
}

interface AuthLayoutProps {
  leftHeadline: ReactNode;
  leftSubtitle: string;
  leftBullets: Bullet[];
  children: ReactNode;
}

export default function AuthLayout({ leftHeadline, leftSubtitle, leftBullets, children }: AuthLayoutProps) {
  return (
    <div className="flex min-h-screen">
      {/* Left panel */}
      <div
        className="relative hidden flex-col justify-between overflow-hidden px-12 py-10 lg:flex lg:w-[44%]"
        style={{ backgroundColor: "#1a3d2b" }}
      >
        {/* Decorative circle */}
        <div
          className="pointer-events-none absolute -bottom-32 -right-32 h-[480px] w-[480px] rounded-full opacity-10"
          style={{ border: "1.5px solid #ffffff" }}
        />
        <div
          className="pointer-events-none absolute -bottom-48 -right-48 h-[640px] w-[640px] rounded-full opacity-5"
          style={{ border: "1.5px solid #ffffff" }}
        />

        {/* Logo */}
        <div className="flex items-center gap-3">
          <img src={sloveniaBadge} alt="Slovenija" className="h-10 w-10 object-contain" />
          <div className="flex h-10 flex-col justify-center leading-tight">
            <span className="text-[10px] font-semibold uppercase tracking-widest text-white/60">Republika Slovenija</span>
            <span className="text-lg font-bold text-white">ePrevzem</span>
          </div>
        </div>

        {/* Headline + bullets */}
        <div className="space-y-8">
          <div className="space-y-4">
            <h1 className="text-4xl font-bold leading-tight text-white">{leftHeadline}</h1>
            <p className="text-base text-white/70">{leftSubtitle}</p>
          </div>
          <ul className="space-y-4">
            {leftBullets.map((b, i) => (
              <li key={i} className="flex items-center gap-3">
                <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-white/10 text-white/80">
                  {b.icon}
                </span>
                <span className="text-sm font-medium text-white/80">{b.text}</span>
              </li>
            ))}
          </ul>
        </div>

        {/* Footer */}
        <p className="text-xs text-white/40">© 2026 Republika Slovenija · ePrevzem</p>
      </div>

      {/* Right panel */}
      <div className="flex flex-1 flex-col bg-[#f5f4f0]">
        {/* Mobile logo */}
        <div className="flex items-center gap-2 px-6 py-5 lg:hidden" style={{ backgroundColor: "#1a3d2b" }}>
          <img src={sloveniaBadge} alt="Slovenija" className="h-8 w-8 object-contain" />
          <span className="text-base font-bold text-white">ePrevzem</span>
        </div>

        <div className="flex flex-1 items-center justify-center px-6 py-12">
          <div className="w-full max-w-[420px]">{children}</div>
        </div>
      </div>
    </div>
  );
}
