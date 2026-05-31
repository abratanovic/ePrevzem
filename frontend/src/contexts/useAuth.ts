import { createContext, useContext } from "react";

export interface AuthUser {
  id: string;
  role: "OrganizationAdmin" | "Employee";
  mustChangePassword: boolean;
  firstName: string;
  lastName: string;
  email: string | null;
  organizationId: string | null;
  organizationName: string | null;
}

export interface AuthState {
  user: AuthUser | null;
  token: string | null;
}

export interface AuthContextValue extends AuthState {
  login: (email: string, password: string) => Promise<{ mustChangePassword: boolean }>;
  logout: () => void;
  clearMustChangePassword: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}
