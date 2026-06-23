// src/lib/auth.tsx
// Lightweight auth: holds the JWT, persists it to localStorage, and exposes
// login/logout plus the decoded user. Wrap the app in <AuthProvider> and read
// it anywhere with useAuth().

"use client";

import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  ReactNode,
} from "react";
import { login as apiLogin } from "@/lib/api";
import { AuthUser, LoginRequest } from "@/types";

const TOKEN_KEY = "careerhub:token";

interface AuthContextValue {
  token: string | null;
  user: AuthUser | null;
  isAuthenticated: boolean;
  isApplicant: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

// Decode the payload of a JWT (no verification — display only). The browser
// never trusts this for security; the backend verifies the token on every call.
function decodeUser(token: string): AuthUser | null {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    // The backend puts the username in "sub" and the role in the standard
    // ClaimTypes.Role URI claim.
    const username: string = payload.sub ?? "user";
    const role: string =
      payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ??
      payload.role ??
      "";
    return { username, role };
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<AuthUser | null>(null);

  // Restore a saved token on mount.
  useEffect(() => {
    const saved = localStorage.getItem(TOKEN_KEY);
    if (saved) {
      setToken(saved);
      setUser(decodeUser(saved));
    }
  }, []);

  const login = useCallback(async (credentials: LoginRequest) => {
    const { token: newToken } = await apiLogin(credentials);
    localStorage.setItem(TOKEN_KEY, newToken);
    setToken(newToken);
    setUser(decodeUser(newToken));
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY);
    setToken(null);
    setUser(null);
  }, []);

  const value: AuthContextValue = {
    token,
    user,
    isAuthenticated: token !== null,
    isApplicant: user?.role === "Applicant",
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return ctx;
}
