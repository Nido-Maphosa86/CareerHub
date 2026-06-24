// src/app/jobs/[id]/error.tsx
// Assignment 2.1 — Stretch B: error boundary for the job detail route.
//
// error.tsx must be a Client Component: Next.js passes it `error` and a `reset`
// callback, and the reset interaction runs in the browser. This catches any
// error thrown by page.tsx that is NOT a 404 (404 goes to not-found.tsx).

"use client";

import { AlertTriangle } from "lucide-react";

interface Props {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function JobDetailError({ error, reset }: Props) {
  return (
    <div className="mx-auto max-w-lg py-16 text-center">
      <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-red-100 dark:bg-red-950/50">
        <AlertTriangle className="h-7 w-7 text-red-600 dark:text-red-400" />
      </div>

      <h1 className="text-2xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
        Something went wrong
      </h1>
      <p className="mt-2 text-sm text-zinc-500 dark:text-zinc-400">
        {error.message || "The job could not be loaded."}
      </p>

      <button
        type="button"
        onClick={reset}
        className="mt-6 inline-flex items-center rounded-lg bg-lime-400 px-4 py-2.5 text-sm font-semibold text-black transition-colors hover:bg-lime-300"
      >
        Try again
      </button>
    </div>
  );
}
