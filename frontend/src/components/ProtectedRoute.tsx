import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../contexts/useAuth";
import type { ReactNode } from "react";

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) {
    return <Navigate to="/prijava" state={{ from: location }} replace />;
  }

  if (user.mustChangePassword && location.pathname !== "/sprememba-gesla") {
    return <Navigate to="/sprememba-gesla" replace />;
  }

  return <>{children}</>;
}
