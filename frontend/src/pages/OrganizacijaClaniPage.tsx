import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { Plus, Copy, Check, AlertCircle, Loader2, X, Users, MoreHorizontal, ShieldOff, ShieldCheck, UserMinus, UserPlus, RefreshCw } from "lucide-react";
import {
  addMember, getMembers, disableEmployee, enableEmployee, revokeRole, grantRole, reissueProvisioningCode,
  type AddMemberResponse, type Member
} from "../services/membersService";

type ModalState =
  | { step: "closed" }
  | { step: "form" }
  | { step: "credentials"; data: AddMemberResponse; codeOnly?: boolean };

type ActionState = { memberId: string; action: string } | null;

function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false);
  const handle = async () => {
    await navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  return (
    <button onClick={() => void handle()} className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 hover:bg-slate-50">
      {copied ? <Check size={14} className="text-emerald-600" /> : <Copy size={14} className="text-slate-400" />}
    </button>
  );
}

function TableSkeleton() {
  return (
    <div className="animate-pulse space-y-3 p-5">
      {Array.from({ length: 3 }).map((_, i) => <div key={i} className="h-12 rounded-xl bg-slate-100" />)}
    </div>
  );
}

function RowDropdown({
  member,
  activeAction,
  onAction,
  onReissue,
}: {
  member: Member;
  activeAction: ActionState;
  onAction: (memberId: string, action: string, fn: () => Promise<void>) => void;
  onReissue: (member: Member) => void;
}) {
  const [pos, setPos] = useState<{ top: number; right: number } | null>(null);
  const btnRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!pos) return;
    function handler(e: MouseEvent) {
      if (
        btnRef.current && !btnRef.current.contains(e.target as Node) &&
        menuRef.current && !menuRef.current.contains(e.target as Node)
      ) setPos(null);
    }
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [pos]);

  const toggle = () => {
    if (pos) { setPos(null); return; }
    const rect = btnRef.current!.getBoundingClientRect();
    setPos({ top: rect.bottom + 4, right: window.innerWidth - rect.right });
  };

  const isLoading = activeAction?.memberId === member.id;

  return (
    <>
      <button
        ref={btnRef}
        onClick={toggle}
        className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-50 hover:text-slate-800"
      >
        <MoreHorizontal size={16} />
      </button>
      {pos && (
        <div
          ref={menuRef}
          style={{ position: "fixed", top: pos.top, right: pos.right }}
          className="z-50 w-60 overflow-hidden rounded-xl border border-slate-100 bg-white shadow-xl"
        >
          <button
            onClick={() => {
              onAction(member.id, "toggle", () =>
                member.status === "Active" ? disableEmployee(member.id) : enableEmployee(member.id)
              );
              setPos(null);
            }}
            disabled={isLoading}
            className="flex w-full items-center gap-3 px-4 py-2.5 text-left text-sm text-slate-700 hover:bg-slate-50 disabled:opacity-50"
          >
            {isLoading && activeAction?.action === "toggle"
              ? <Loader2 size={14} className="animate-spin text-slate-400" />
              : member.status === "Active"
                ? <ShieldOff size={14} className="text-slate-400" />
                : <ShieldCheck size={14} className="text-slate-400" />}
            {member.status === "Active" ? "Onemogoči račun" : "Omogoči račun"}
          </button>
          <div className="border-t border-slate-100" />
          {(["RecordManager", "Operator"] as const).map(role => {
            const hasRole = member.roles.includes(role);
            return (
              <button
                key={role}
                onClick={() => {
                  onAction(member.id, role, () =>
                    hasRole ? revokeRole(member.id, role) : grantRole(member.id, role)
                  );
                  setPos(null);
                }}
                disabled={isLoading}
                className="flex w-full items-center gap-3 px-4 py-2.5 text-left text-sm text-slate-700 hover:bg-slate-50 disabled:opacity-50"
              >
                {isLoading && activeAction?.action === role
                  ? <Loader2 size={14} className="animate-spin text-slate-400" />
                  : hasRole
                    ? <UserMinus size={14} className="text-slate-400" />
                    : <UserPlus size={14} className="text-slate-400" />}
                {hasRole ? `Odvzemi vlogo ${role}` : `Dodeli vlogo ${role}`}
              </button>
            );
          })}
          <div className="border-t border-slate-100" />
          <button
            onClick={() => {
              onReissue(member);
              setPos(null);
            }}
            disabled={isLoading}
            className="flex w-full items-center gap-3 px-4 py-2.5 text-left text-sm text-slate-700 hover:bg-slate-50 disabled:opacity-50"
          >
            {isLoading && activeAction?.action === "reissue"
              ? <Loader2 size={14} className="animate-spin text-slate-400" />
              : <RefreshCw size={14} className="text-slate-400" />}
            Znova izdaj kodo za provisioning
          </button>
        </div>
      )}
    </>
  );
}

export default function OrganizacijaClaniPage() {
  const [modal, setModal] = useState<ModalState>({ step: "closed" });
  const [form, setForm] = useState({ firstName: "", lastName: "", email: "" });
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [members, setMembers] = useState<Member[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [activeAction, setActiveAction] = useState<ActionState>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const loadMembers = () => {
    setLoadError(null);
    getMembers()
      .then(setMembers)
      .catch(() => setLoadError("Zaposlenih ni bilo mogoče naložiti."));
  };

  useEffect(() => { loadMembers(); }, []);

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
      loadMembers();
    } catch {
      setFormError("Dodajanje zaposlenega ni uspelo. Preverite podatke in poskusite znova.");
    } finally {
      setSubmitting(false);
    }
  };

  const handleAction = async (memberId: string, action: string, fn: () => Promise<void>) => {
    setActiveAction({ memberId, action });
    setActionError(null);
    try {
      await fn();
      loadMembers();
    } catch {
      setActionError("Akcija ni uspela. Poskusite znova.");
    } finally {
      setActiveAction(null);
    }
  };

  const handleReissue = async (member: Member) => {
    setActiveAction({ memberId: member.id, action: "reissue" });
    setActionError(null);
    try {
      const result = await reissueProvisioningCode(member.id);
      setModal({
        step: "credentials",
        data: {
          employeeId: member.id,
          initialPassword: "",
          provisioningCode: result.provisioningCode,
          provisioningCodeExpiresAt: result.expiresAt,
        },
        codeOnly: true,
      });
    } catch {
      setActionError("Izdaja kode za provisioning ni uspela. Poskusite znova.");
    } finally {
      setActiveAction(null);
    }
  };

  return (
    <div className="space-y-5 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-900">Organizacija</h2>
          <p className="text-sm text-slate-500">Upravljanje zaposlenih.</p>
        </div>
        <button onClick={openAdd} className="flex items-center gap-2 rounded-xl bg-accent px-4 py-2.5 text-sm font-medium text-white hover:bg-accent-dark">
          <Plus size={16} strokeWidth={2.5} />
          Dodaj zaposlenega
        </button>
      </div>

      <div className="flex gap-2 border-b border-slate-200 pb-1">
        <Link to="/organizacija" className="px-3 py-2 text-sm font-medium text-slate-500 hover:text-slate-900 border-b-2 border-transparent">Pregled</Link>
        <Link to="/organizacija/clani" className="px-3 py-2 text-sm font-medium text-accent border-b-2 border-accent">Zaposleni</Link>
      </div>

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 px-5 py-4">
          <h3 className="text-lg font-bold text-slate-900">Zaposleni</h3>
        </div>

        {loadError ? (
          <p className="px-5 py-8 text-center text-sm text-red-600">{loadError}</p>
        ) : members === null ? (
          <TableSkeleton />
        ) : members.length === 0 ? (
          <div className="flex flex-col items-center px-6 py-14 text-center">
            <Users size={32} className="mb-3 text-slate-300" />
            <h3 className="font-semibold text-slate-800">Organizacija še nima zaposlenih</h3>
            <p className="mt-1 text-sm text-slate-500">Dodajte prvega zaposlenega z gumbom zgoraj.</p>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-slate-50 text-left text-[11px] font-semibold tracking-wide text-slate-400">
                    <th className="px-5 py-3">IME IN PRIIMEK</th>
                    <th className="px-5 py-3">E-POŠTA</th>
                    <th className="px-5 py-3">STATUS</th>
                    <th className="px-5 py-3">VLOGE</th>
                    <th className="px-5 py-3 text-right"></th>
                  </tr>
                </thead>
                <tbody>
                  {members.map(m => (
                    <tr key={m.id} className="border-t border-slate-100 hover:bg-slate-50">
                      <td className="px-5 py-4 font-medium text-slate-900">{m.firstName} {m.lastName}</td>
                      <td className="px-5 py-4 text-slate-600">{m.email ?? "—"}</td>
                      <td className="px-5 py-4">
                        {m.status === "Active" ? (
                          <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-semibold text-emerald-700 ring-1 ring-emerald-200">Aktiven</span>
                        ) : (
                          <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-500 ring-1 ring-slate-200">Onemogočen</span>
                        )}
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex flex-wrap gap-1">
                          {m.roles.map(r => (
                            <span key={r} className="rounded-full bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700 ring-1 ring-blue-100">{r}</span>
                          ))}
                        </div>
                      </td>
                      <td className="px-5 py-4 text-right">
                        <RowDropdown
                          member={m}
                          activeAction={activeAction}
                          onAction={handleAction}
                          onReissue={m => void handleReissue(m)}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {actionError && (
              <p className="px-5 py-3 text-sm text-red-600">{actionError}</p>
            )}
          </>
        )}
      </div>

      {modal.step !== "closed" && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-md rounded-2xl bg-white shadow-xl">
            {modal.step === "form" && (
              <>
                <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
                  <h3 className="font-semibold text-slate-900">Dodaj zaposlenega</h3>
                  <button onClick={() => setModal({ step: "closed" })} className="text-slate-400 hover:text-slate-700"><X size={18} /></button>
                </div>
                <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4 px-6 py-5">
                  <div>
                    <label className="mb-1 block text-xs font-medium text-slate-700">Ime</label>
                    <input className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-accent focus:ring-2 focus:ring-accent/20" value={form.firstName} onChange={e => setForm(f => ({ ...f, firstName: e.target.value }))} required />
                  </div>
                  <div>
                    <label className="mb-1 block text-xs font-medium text-slate-700">Priimek</label>
                    <input className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-accent focus:ring-2 focus:ring-accent/20" value={form.lastName} onChange={e => setForm(f => ({ ...f, lastName: e.target.value }))} required />
                  </div>
                  <div>
                    <label className="mb-1 block text-xs font-medium text-slate-700">E-poštni naslov</label>
                    <input type="email" className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-accent focus:ring-2 focus:ring-accent/20" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} required />
                  </div>
                  {formError && (
                    <div className="flex items-center gap-2 rounded-xl bg-red-50 px-3 py-2 text-sm text-red-600">
                      <AlertCircle size={14} />{formError}
                    </div>
                  )}
                  <button type="submit" disabled={submitting} className="flex w-full items-center justify-center gap-2 rounded-xl bg-accent py-2.5 text-sm font-medium text-white hover:bg-accent-dark disabled:opacity-60">
                    {submitting && <Loader2 size={14} className="animate-spin" />}
                    Dodaj
                  </button>
                </form>
              </>
            )}
            {modal.step === "credentials" && (
              <>
                <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
                  <h3 className="font-semibold text-slate-900">Poverilnice zaposlenega</h3>
                </div>
                <div className="space-y-4 px-6 py-5">
                  <div className="flex items-center gap-2 rounded-xl bg-amber-50 px-3 py-2 text-sm text-amber-700">
                    <AlertCircle size={14} />Te podatke si shranite zdaj — prikazani so samo enkrat.
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
                  <button onClick={() => setModal({ step: "closed" })} className="w-full rounded-xl border border-slate-200 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50">Zapri</button>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
