import { useState } from "react";
import { Link } from "react-router-dom";
import { Plus, Copy, Check, AlertCircle, Loader2, X } from "lucide-react";
import { addMember, type AddMemberResponse } from "../services/membersService";

type ModalState =
  | { step: "closed" }
  | { step: "form" }
  | { step: "credentials"; data: AddMemberResponse; codeOnly?: boolean };

function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false);
  const handle = async () => {
    await navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  return (
    <button
      onClick={() => void handle()}
      className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 hover:bg-slate-50"
    >
      {copied ? <Check size={14} className="text-emerald-600" /> : <Copy size={14} className="text-slate-400" />}
    </button>
  );
}

export default function OrganizacijaClaniPage() {
  const [modal, setModal] = useState<ModalState>({ step: "closed" });
  const [form, setForm] = useState({ firstName: "", lastName: "", email: "" });
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const openAdd = () => {
    setForm({ firstName: "", lastName: "", email: "" });
    setFormError(null);
    setModal({ step: "form" });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setFormError(null);
    try {
      const data = await addMember(form.firstName, form.lastName, form.email);
      setModal({ step: "credentials", data });
    } catch {
      setFormError("Dodajanje člana ni uspelo. Preverite podatke in poskusite znova.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-5 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-900">Organizacija</h2>
          <p className="text-sm text-slate-500">Upravljanje članov organizacije.</p>
        </div>
        <button
          onClick={openAdd}
          className="flex items-center gap-2 rounded-xl bg-accent px-4 py-2.5 text-sm font-medium text-white hover:bg-accent-dark"
        >
          <Plus size={16} strokeWidth={2.5} />
          Dodaj člana
        </button>
      </div>

      <div className="flex gap-2 border-b border-slate-200 pb-1">
        <Link to="/organizacija" className="px-3 py-2 text-sm font-medium text-slate-500 hover:text-slate-900 border-b-2 border-transparent">
          Pregled
        </Link>
        <Link to="/organizacija/clani" className="px-3 py-2 text-sm font-medium text-accent border-b-2 border-accent">
          Člani
        </Link>
      </div>

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 px-5 py-4">
          <h3 className="text-lg font-bold text-slate-900">Člani</h3>
        </div>
        <div className="flex flex-col items-center px-6 py-14 text-center">
          <p className="text-sm text-slate-500">Seznam članov bo prikazan v naslednji fazi.</p>
        </div>
      </div>

      {/* Modal */}
      {modal.step !== "closed" && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-md rounded-2xl bg-white shadow-xl">
            {modal.step === "form" && (
              <>
                <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
                  <h3 className="font-semibold text-slate-900">Dodaj člana</h3>
                  <button onClick={() => setModal({ step: "closed" })} className="text-slate-400 hover:text-slate-700"><X size={18} /></button>
                </div>
                <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4 px-6 py-5">
                  <div>
                    <label className="mb-1 block text-xs font-medium text-slate-700">Ime</label>
                    <input
                      className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-accent focus:ring-2 focus:ring-accent/20"
                      value={form.firstName}
                      onChange={e => setForm(f => ({ ...f, firstName: e.target.value }))}
                      required
                    />
                  </div>
                  <div>
                    <label className="mb-1 block text-xs font-medium text-slate-700">Priimek</label>
                    <input
                      className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-accent focus:ring-2 focus:ring-accent/20"
                      value={form.lastName}
                      onChange={e => setForm(f => ({ ...f, lastName: e.target.value }))}
                      required
                    />
                  </div>
                  <div>
                    <label className="mb-1 block text-xs font-medium text-slate-700">E-poštni naslov</label>
                    <input
                      type="email"
                      className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-accent focus:ring-2 focus:ring-accent/20"
                      value={form.email}
                      onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
                      required
                    />
                  </div>
                  {formError && (
                    <div className="flex items-center gap-2 rounded-xl bg-red-50 px-3 py-2 text-sm text-red-600">
                      <AlertCircle size={14} />
                      {formError}
                    </div>
                  )}
                  <button
                    type="submit"
                    disabled={submitting}
                    className="flex w-full items-center justify-center gap-2 rounded-xl bg-accent py-2.5 text-sm font-medium text-white hover:bg-accent-dark disabled:opacity-60"
                  >
                    {submitting && <Loader2 size={14} className="animate-spin" />}
                    Dodaj
                  </button>
                </form>
              </>
            )}

            {modal.step === "credentials" && (
              <>
                <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
                  <h3 className="font-semibold text-slate-900">Poverilnice člana</h3>
                </div>
                <div className="space-y-4 px-6 py-5">
                  <div className="flex items-center gap-2 rounded-xl bg-amber-50 px-3 py-2 text-sm text-amber-700">
                    <AlertCircle size={14} />
                    Te podatke si shranite zdaj — prikazani so samo enkrat.
                  </div>
                  {!modal.codeOnly && (
                    <div>
                      <p className="mb-1 text-xs font-medium text-slate-500">Začasno geslo</p>
                      <div className="flex items-center justify-between rounded-xl border border-slate-200 px-3 py-2">
                        <span className="font-mono text-sm font-semibold text-slate-900">{modal.data.initialPassword}</span>
                        <CopyButton text={modal.data.initialPassword} />
                      </div>
                    </div>
                  )}
                  <div>
                    <p className="mb-1 text-xs font-medium text-slate-500">Koda za provisioning</p>
                    <div className="flex items-center justify-between rounded-xl border border-slate-200 px-3 py-2">
                      <span className="font-mono text-sm font-semibold tracking-wider text-slate-900">{modal.data.provisioningCode}</span>
                      <CopyButton text={modal.data.provisioningCode} />
                    </div>
                  </div>
                  <button
                    onClick={() => setModal({ step: "closed" })}
                    className="w-full rounded-xl border border-slate-200 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
                  >
                    Zapri
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
