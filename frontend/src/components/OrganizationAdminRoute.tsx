import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../contexts/useAuth";

export default function OrganizationAdminRoute({ children }: { children: ReactNode }) {
  const { user } = useAuth();

  if (user?.role !== "OrganizationAdmin") {
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
}
