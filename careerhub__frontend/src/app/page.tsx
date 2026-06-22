// src/app/page.tsx
// Main page. Loads jobs via TanStack Query, renders skeleton / error / grid.
// Selection persists to sessionStorage so refresh keeps the chosen card.

"use client";

import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { fetchJobs } from "@/lib/api";
import { JobList } from "@/components/JobList";
import { JobListSkeleton } from "@/components/JobCardSkeleton";
import { ThemeToggle } from "@/components/ThemeToggle";
import { AlertCircle } from "lucide-react";

const SELECTION_KEY = "careerhub:selectedJobId";

export default function Home() {
  // The id of the currently selected card, or null.
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // Restore the selection from sessionStorage on mount.
  useEffect(() => {
    const saved = sessionStorage.getItem(SELECTION_KEY);
    if (saved) setSelectedId(saved);
  }, []);

  // Persist (or clear) the selection whenever it changes.
  useEffect(() => {
    if (selectedId) {
      sessionStorage.setItem(SELECTION_KEY, selectedId);
    } else {
      sessionStorage.removeItem(SELECTION_KEY);
    }
  }, [selectedId]);

  // Click selects; clicking the same card again deselects.
  function handleSelect(id: string) {
    setSelectedId((current) => (current === id ? null : id));
  }

  // TanStack Query — fetch + cache the job list.
  const { data: jobs, isPending, isError, error } = useQuery({
    queryKey: ["jobs"],
    queryFn: fetchJobs,
  });

  return (
    <div className="min-h-screen">
      {/* Top bar */}
      <header className="border-b border-zinc-200 dark:border-zinc-800">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
          {/* Wordmark — "Hub" in lime, with a small lime square as the mark. */}
          <div className="flex items-center gap-2.5">
            <div className="flex h-8 w-8 items-center justify-center rounded-md bg-lime-400">
              <span className="text-base font-black text-black">C</span>
            </div>
            <span className="text-xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
              Career<span className="text-lime-500 dark:text-lime-400">Hub</span>
            </span>
          </div>
          <ThemeToggle />
        </div>
      </header>

      {/* Main content */}
      <main className="mx-auto max-w-6xl px-4 py-10">
        {/* Page heading + live job count */}
        <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
          <div>
            <h1 className="text-3xl font-black tracking-tight text-zinc-900 dark:text-zinc-50 sm:text-4xl">
              Open positions
            </h1>
            <p className="mt-1 text-zinc-500 dark:text-zinc-400">
              Find your next role.
            </p>
          </div>

          {/* Count pill — only shows once data is in. */}
          {jobs && (
            <div className="rounded-full border border-lime-300 bg-lime-50 px-4 py-1.5 text-sm font-semibold text-lime-700 dark:border-lime-400/30 dark:bg-lime-400/10 dark:text-lime-300">
              {jobs.length} {jobs.length === 1 ? "role" : "roles"} available
            </div>
          )}
        </div>

        {/* Branch 1: loading */}
        {isPending && <JobListSkeleton />}

        {/* Branch 2: error */}
        {isError && (
          <div className="flex items-start gap-3 rounded-xl border border-red-300 bg-red-50 p-5 text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
            <AlertCircle className="mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <p className="font-semibold">Could not load jobs.</p>
              <p className="mt-1 text-sm">
                {error?.message ?? "Something went wrong."}
              </p>
              <p className="mt-1 text-sm opacity-80">
                Make sure the CareerHub API is running on port 5000.
              </p>
            </div>
          </div>
        )}

        {/* Branch 3: success */}
        {jobs && (
          <JobList
            jobs={jobs}
            selectedId={selectedId}
            onSelect={handleSelect}
          />
        )}
      </main>
    </div>
  );
}
