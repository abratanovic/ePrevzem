import { createContext, useContext, useState, type ReactNode } from "react";
import * as authService from "../services/authService";
import type { LoginResponse } from "../services/authService";

interface AuthUser {
  id: string;
  role: "OrganizationAdmin" | "Employee";
  mustChangePassword: boolean;
  firstName: string;
  lastName: string;
  email: string | null;
  organizationId: string | null;
  organizationName: string | null;
}

interface AuthState {
  user: AuthUser | null;
  token: string | null;
}

interface AuthContextValue extends AuthState {
  login: (email: string, password: string) => Promise<{ mustChangePassword: boolean }>;
  logout: () => void;
  clearMustChangePassword: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function parseJwtSub(token: string): string | null {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return payload.sub ?? null;
  } catch {
    return null;
  }
}

function isTokenExpired(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return payload.exp * 1000 < Date.now();
  } catch {
    return true;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => {
    const token = localStorage.getItem("access_token");
    const userJson = localStorage.getItem("auth_user");
    if (token && userJson && !isTokenExpired(token)) {
      try {
        return { token, user: JSON.parse(userJson) };
      } catch {
        /* fall through */
      }
    }
    return { token: null, user: null };
  });

  const login = async (email: string, password: string): Promise<{ mustChangePassword: boolean }> => {
    const response: LoginResponse = await authService.login(email, password);
    const id = parseJwtSub(response.accessToken) ?? "";
    const user: AuthUser = {
      id,
      role: response.role,
      mustChangePassword: response.mustChangePassword,
      firstName: response.firstName,
      lastName: response.lastName,
      email: response.email,
      organizationId: response.organizationId,
      organizationName: response.organizationName,
    };
    localStorage.setItem("access_token", response.accessToken);
    localStorage.setItem("auth_user", JSON.stringify(user));
    setState({ token: response.accessToken, user });
    return { mustChangePassword: response.mustChangePassword };
  };

  const logout = () => {
    authService.logout();
    setState({ token: null, user: null });
  };

  const clearMustChangePassword = () => {
    setState((prev) => {
      if (!prev.user) return prev;
      const updated = { ...prev.user, mustChangePassword: false };
      localStorage.setItem("auth_user", JSON.stringify(updated));
      return { ...prev, user: updated };
    });
  };

  return (
    <AuthContext.Provider value={{ ...state, login, logout, clearMustChangePassword }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}
