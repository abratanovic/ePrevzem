import type { ReactNode } from "react";

interface StatCardProps {
  icon: ReactNode;
  iconBg?: string;
  value: ReactNode;
  label: string;
  trend?: string;
  indicator?: string;
  subtitle?: string;
}

export default function StatCard({ icon, iconBg = "bg-slate-50", value, label, trend, indicator, subtitle }: StatCardProps) {
  return (
    <div className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-start justify-between">
        <div className={`flex h-11 w-11 items-center justify-center rounded-xl ${iconBg}`}>
          {icon}
        </div>
        {trend && (
          <span className="text-sm font-medium text-emerald-600">{trend}</span>
        )}
        {indicator && (
          <span className="text-sm text-slate-400">{indicator}</span>
        )}
      </div>
      <div>
        <div className="text-3xl font-extrabold tracking-tight text-slate-900">{value}</div>
        <div className="mt-0.5 text-sm text-slate-500">{label}</div>
        {subtitle && (
          <div className="mt-0.5 text-xs text-slate-400">{subtitle}</div>
        )}
      </div>
    </div>
  );
}
