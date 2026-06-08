import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowLeft, Eye, EyeOff, Lock, History, KeyRound, ShieldCheck } from "lucide-react";
import AuthLayout from "../components/auth/AuthLayout";
import { useAuth } from "../contexts/useAuth";
import { changePassword } from "../services/authService";

interface PasswordStrength {
  minLength: boolean;
  hasNumber: boolean;
  hasUpperLower: boolean;
  hasSpecial: boolean;
}

function checkStrength(pw: string): PasswordStrength {
  return {
    minLength: pw.length >= 12,
    hasNumber: /\d/.test(pw),
    hasUpperLower: /[a-z]/.test(pw) && /[A-Z]/.test(pw),
    hasSpecial: /[!?@#$%^&*]/.test(pw),
  };
}

function strengthScore(s: PasswordStrength): number {
  return [s.minLength, s.hasNumber, s.hasUpperLower, s.hasSpecial].filter(Boolean).length;
}

function strengthLabel(score: number): string {
  if (score <= 1) return "Šibko geslo";
  if (score === 2) return "Zmerno geslo";
  if (score === 3) return "Dobro geslo";
  return "Močno geslo";
}

function strengthColor(score: number): string {
  if (score <= 1) return "bg-red-400";
  if (score === 2) return "bg-yellow-400";
  if (score === 3) return "bg-blue-400";
  return "bg-accent";
}

function StrengthBar({ score }: { score: number }) {
  return (
    <div className="flex gap-1">
      {[1, 2, 3, 4].map((level) => (
        <div
          key={level}
          className={`h-1 flex-1 rounded-full transition-all ${score >= level ? strengthColor(score) : "bg-slate-200"}`}
        />
      ))}
    </div>
  );
}

function Requirement({ met, label }: { met: boolean; label: string }) {
  return (
    <div className="flex items-center gap-1.5">
      <span className={`flex h-4 w-4 items-center justify-center rounded-full text-xs ${met ? "bg-accent text-white" : "border border-slate-300 text-transparent"}`}>
        {met ? "✓" : "○"}
      </span>
      <span className={`text-xs ${met ? "text-slate-700" : "text-slate-400"}`}>{label}</span>
    </div>
  );
}

export default function ChangePasswordPage() {
  const navigate = useNavigate();
  const { clearMustChangePassword, user } = useAuth();

  const [current, setCurrent] = useState("");
  const [newPw, setNewPw] = useState("");
  const [repeat, setRepeat] = useState("");
  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [showRepeat, setShowRepeat] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const strength = checkStrength(newPw);
  const score = strengthScore(strength);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!current) { setError("Vnesite začetno geslo."); return; }
    if (newPw.length < 8) { setError("Novo geslo mora imeti vsaj 8 znakov."); return; }
    if (newPw !== repeat) { setError("Gesli se ne ujemata."); return; }

    setLoading(true);
    try {
      await changePassword(current, newPw);
      clearMustChangePassword();
      navigate("/dashboard");
    } catch (err: unknown) {
      if (err instanceof Response && err.status === 401) {
        setError("Napačno trenutno geslo.");
      } else {
        setError("Napaka pri spremembi gesla. Poskusite znova.");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout
      leftHeadline={<>Zaščitite svoj<br />skrbniški račun</>}
      leftSubtitle="Ob prvi prijavi je obvezna zamenjava začetnega gesla, ki vam ga je dodelila organizacija."
      leftBullets={[
        { icon: <KeyRound size={15} />, text: "Najmanj 12 znakov" },
        { icon: <History size={15} />, text: "Edinstveno za ta račun" },
        { icon: <ShieldCheck size={15} />, text: "Šifrirano shranjeno" },
      ]}
    >
      <div className="space-y-8">
        {!user?.mustChangePassword && (
          <Link
            to="/profil"
            className="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-600 hover:bg-slate-50"
            aria-label="Nazaj na profil"
          >
            <ArrowLeft size={18} />
          </Link>
        )}

        <div className="space-y-1">
          <h2 className="text-2xl font-bold text-slate-900">Nastavite novo geslo</h2>
          <p className="text-sm text-slate-500">Iz varnostnih razlogov morate pred prvo uporabo zamenjati začetno geslo.</p>
        </div>

        {/* Info banner */}
        <div className="flex items-start gap-3 rounded-xl border border-blue-100 bg-blue-50 px-4 py-3">
          <span className="mt-0.5 text-blue-500">ℹ</span>
          <p className="text-sm text-blue-700">Prijavljeni ste z začetnim geslom. Nadaljujete šele po nastavitvi novega.</p>
        </div>

        <form onSubmit={handleSubmit} noValidate className="space-y-5">
          {/* Current password */}
          <div className="space-y-1.5">
            <label htmlFor="current" className="block text-sm font-medium text-slate-700">Začetno geslo</label>
            <div className="relative">
              <Lock size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                id="current"
                type={showCurrent ? "text" : "password"}
                autoComplete="current-password"
                placeholder="••••••••"
                value={current}
                onChange={(e) => setCurrent(e.target.value)}
                className="w-full rounded-xl border border-slate-200 bg-white py-3 pl-10 pr-10 text-sm text-slate-900 placeholder-slate-300 outline-none transition focus:border-accent focus:ring-2 focus:ring-accent/10"
              />
              <button type="button" onClick={() => setShowCurrent((v) => !v)} className="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
                {showCurrent ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>

          {/* New password */}
          <div className="space-y-1.5">
            <label htmlFor="newpw" className="block text-sm font-medium text-slate-700">Novo geslo</label>
            <div className="relative">
              <History size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                id="newpw"
                type={showNew ? "text" : "password"}
                autoComplete="new-password"
                placeholder="••••••••"
                value={newPw}
                onChange={(e) => setNewPw(e.target.value)}
                className="w-full rounded-xl border border-slate-200 bg-white py-3 pl-10 pr-10 text-sm text-slate-900 placeholder-slate-300 outline-none transition focus:border-accent focus:ring-2 focus:ring-accent/10"
              />
              <button type="button" onClick={() => setShowNew((v) => !v)} className="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
                {showNew ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
            {newPw && (
              <div className="space-y-1">
                <StrengthBar score={score} />
                <p className={`text-xs font-medium ${score >= 4 ? "text-accent" : "text-slate-500"}`}>{strengthLabel(score)}</p>
              </div>
            )}
          </div>

          {/* Repeat password */}
          <div className="space-y-1.5">
            <label htmlFor="repeat" className="block text-sm font-medium text-slate-700">Ponovite novo geslo</label>
            <div className="relative">
              <History size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                id="repeat"
                type={showRepeat ? "text" : "password"}
                autoComplete="new-password"
                placeholder="••••••••"
                value={repeat}
                onChange={(e) => setRepeat(e.target.value)}
                className="w-full rounded-xl border border-slate-200 bg-white py-3 pl-10 pr-10 text-sm text-slate-900 placeholder-slate-300 outline-none transition focus:border-accent focus:ring-2 focus:ring-accent/10"
              />
              <button type="button" onClick={() => setShowRepeat((v) => !v)} className="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
                {showRepeat ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>

          {/* Requirements grid */}
          {newPw && (
            <div className="grid grid-cols-2 gap-2 rounded-xl border border-slate-100 bg-slate-50 p-4">
              <Requirement met={strength.minLength} label="Najmanj 12 znakov" />
              <Requirement met={strength.hasUpperLower} label="Velika in mala črka" />
              <Requirement met={strength.hasNumber} label="Vsaj ena številka" />
              <Requirement met={strength.hasSpecial} label="Poseben znak (!?#)" />
            </div>
          )}

          {error && (
            <p className="rounded-lg bg-red-50 px-4 py-2.5 text-sm text-red-600">{error}</p>
          )}

          <button
            type="submit"
            disabled={loading}
            className="flex w-full items-center justify-center gap-2 rounded-xl bg-accent py-3 text-sm font-semibold text-white transition hover:bg-accent-dark disabled:opacity-60"
          >
            {loading ? (
              <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
              </svg>
            ) : (
              <span>✓ Shrani in nadaljuj</span>
            )}
          </button>
        </form>
      </div>
    </AuthLayout>
  );
}
