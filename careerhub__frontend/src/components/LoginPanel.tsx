// src/components/LoginPanel.tsx
// A small login form. Used to authenticate as a seeded applicant before
// applying. Calls auth.login(); shows server errors inline.

"use client";

import { useState } from "react";
import { useAuth } from "@/lib/auth";
import { cn } from "@/lib/utils";
import { LogIn } from "lucide-react";

export function LoginPanel() {
  const { login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isBusy, setIsBusy] = useState(false);

  async function handleSubmit() {
    setError(null);
    setIsBusy(true);
    try {
      await login({ username, password });
    } catch (e) {
      setError(e instanceof Error ? e.message : "Login failed.");
    } finally {
      setIsBusy(false);
    }
  }

  const inputBase =
    "w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 placeholder-zinc-400 focus:outline-none focus:ring-2 focus:ring-lime-400 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-100 dark:placeholder-zinc-500";

  return (
    <div className="rounded-xl border border-zinc-200 bg-white p-6 dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mb-1 flex items-center gap-2">
        <LogIn className="h-5 w-5 text-lime-500 dark:text-lime-400" />
        <h3 className="text-lg font-bold text-zinc-900 dark:text-zinc-50">
          Log in to apply
        </h3>
      </div>
      <p className="mb-4 text-sm text-zinc-500 dark:text-zinc-400">
        Use an applicant account, e.g.{" "}
        <span className="font-mono text-zinc-700 dark:text-zinc-300">
          applicant1
        </span>{" "}
        /{" "}
        <span className="font-mono text-zinc-700 dark:text-zinc-300">
          password123
        </span>
        .
      </p>

      {error && (
        <div className="mb-3 rounded-lg border border-red-300 bg-red-50 p-2.5 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
          {error}
        </div>
      )}

      <div className="space-y-3">
        <div>
          <label htmlFor="username" className="mb-1 block text-sm font-medium text-zinc-700 dark:text-zinc-300">
            Username
          </label>
          <input
            id="username"
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            className={inputBase}
            autoComplete="username"
          />
        </div>
        <div>
          <label htmlFor="password" className="mb-1 block text-sm font-medium text-zinc-700 dark:text-zinc-300">
            Password
          </label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") handleSubmit();
            }}
            className={inputBase}
            autoComplete="current-password"
          />
        </div>
        <button
          type="button"
          onClick={handleSubmit}
          disabled={isBusy || !username || !password}
          className={cn(
            "w-full rounded-lg px-4 py-2.5 text-sm font-semibold transition-colors",
            isBusy || !username || !password
              ? "cursor-not-allowed bg-zinc-200 text-zinc-400 dark:bg-zinc-800 dark:text-zinc-600"
              : "bg-lime-400 text-black hover:bg-lime-300"
          )}
        >
          {isBusy ? "Logging in…" : "Log in"}
        </button>
      </div>
    </div>
  );
}
