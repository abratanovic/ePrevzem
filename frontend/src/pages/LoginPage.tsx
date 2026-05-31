import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Eye, EyeOff, Lock, Mail, FileText, Bell, History } from "lucide-react";
import AuthLayout from "../components/auth/AuthLayout";
import { useAuth } from "../contexts/useAuth";

export default function LoginPage() {
  const navigate = useNavigate();
  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [emailError, setEmailError] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);

  const validate = () => {
    let ok = true;
    setEmailError(null);
    setPasswordError(null);
    if (!email) { setEmailError("E-poštni naslov je obvezen."); ok = false; }
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { setEmailError("Vnesite veljaven e-poštni naslov."); ok = false; }
    if (!password) { setPasswordError("Geslo je obvezno."); ok = false; }
    return ok;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    setError(null);
    setLoading(true);
    try {
      const { mustChangePassword } = await login(email, password);
      navigate(mustChangePassword ? "/sprememba-gesla" : "/dashboard");
    } catch {
      setError("Napačen e-poštni naslov ali geslo.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout
      leftHeadline={<>Vaši dokumenti,<br />na voljo varno</>}
      leftSubtitle="Prijavite se in spremljajte svoje prevzeme dokumentov iz pametnih paketnikov."
      leftBullets={[
        { icon: <FileText size={15} />, text: "Pregled aktivnih prevzemov" },
        { icon: <Bell size={15} />, text: "Obvestila o rokih prevzema" },
        { icon: <History size={15} />, text: "Zgodovina prevzemov" },
      ]}
    >
      <div className="space-y-8">
        <div className="space-y-1">
          <h2 className="text-2xl font-bold text-slate-900">Prijava člana</h2>
          <p className="text-sm text-slate-500">Vpišite e-poštni naslov, povezan z vašim računom ePrevzem.</p>
        </div>

        <form onSubmit={handleSubmit} noValidate className="space-y-5">
          {/* Email */}
          <div className="space-y-1.5">
            <label htmlFor="email" className="block text-sm font-medium text-slate-700">E-poštni naslov</label>
            <div className="relative">
              <Mail size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                id="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="ana.kovac@email.si"
                className={`w-full rounded-xl border bg-white py-3 pl-10 pr-4 text-sm text-slate-900 placeholder-slate-400 outline-none transition focus:ring-2 ${emailError ? "border-red-400 focus:ring-red-200" : "border-slate-200 focus:border-[#1a3d2b] focus:ring-[#1a3d2b]/10"}`}
              />
            </div>
            {emailError && <p className="text-xs text-red-500">{emailError}</p>}
          </div>

          {/* Password */}
          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <label htmlFor="password" className="block text-sm font-medium text-slate-700">Geslo</label>
              <a href="#" className="text-xs font-medium text-[#1a3d2b] hover:underline">Pozabljeno geslo?</a>
            </div>
            <div className="relative">
              <Lock size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                id="password"
                type={showPassword ? "text" : "password"}
                autoComplete="current-password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={`w-full rounded-xl border bg-white py-3 pl-10 pr-10 text-sm text-slate-900 placeholder-slate-300 outline-none transition focus:ring-2 ${passwordError ? "border-red-400 focus:ring-red-200" : "border-slate-200 focus:border-[#1a3d2b] focus:ring-[#1a3d2b]/10"}`}
              />
              <button
                type="button"
                onClick={() => setShowPassword((v) => !v)}
                className="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
              >
                {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
            {passwordError && <p className="text-xs text-red-500">{passwordError}</p>}
          </div>

          {error && (
            <p className="rounded-lg bg-red-50 px-4 py-2.5 text-sm text-red-600">{error}</p>
          )}

          <button
            type="submit"
            disabled={loading}
            className="flex w-full items-center justify-center gap-2 rounded-xl py-3 text-sm font-semibold text-white transition disabled:opacity-60"
            style={{ backgroundColor: "#1a3d2b" }}
          >
            {loading ? (
              <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
              </svg>
            ) : (
              <span>→ Prijava</span>
            )}
          </button>

        </form>
      </div>
    </AuthLayout>
  );
}
