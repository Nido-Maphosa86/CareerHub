// src/components/AuthStatus.tsx
// Header control: shows who is logged in (with role) and a log-out button,
// or nothing special when logged out.

"use client";

import { useAuth } from "@/lib/auth";
import { LogOut, User } from "lucide-react";

export function AuthStatus() {
  const { isAuthenticated, user, logout } = useAuth();

  if (!isAuthenticated || !user) {
    return null;
  }

  return (
    <div className="flex items-center gap-3">
      <span className="hidden items-center gap-1.5 text-sm text-zinc-600 dark:text-zinc-300 sm:flex">
        <User className="h-3.5 w-3.5" />
        {user.username}
        {user.role && (
          <span className="rounded-full bg-lime-100 px-2 py-0.5 text-xs font-semibold text-lime-700 dark:bg-lime-400/10 dark:text-lime-300">
            {user.role}
          </span>
        )}
      </span>
      <button
        type="button"
        onClick={logout}
        className="inline-flex items-center gap-1.5 rounded-lg border border-zinc-200 px-3 py-1.5 text-sm font-medium text-zinc-700 transition-colors hover:border-zinc-300 dark:border-zinc-800 dark:text-zinc-300 dark:hover:border-zinc-700"
      >
        <LogOut className="h-3.5 w-3.5" />
        Log out
      </button>
    </div>
  );
}
