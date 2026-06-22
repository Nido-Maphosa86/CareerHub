// src/app/page.tsx
// The main page. Loads jobs via TanStack Query, renders skeleton on pending,
// error UI on failure, and the real JobList on success.
// Selection is persisted to sessionStorage so refresh keeps the chosen card.

"use client";

import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { fetchJobs } from "@/lib/api";
import { JobList } from "@/components/JobList";
import { JobListSkeleton } from "@/components/JobCardSkeleton";
import { ThemeToggle } from "@/components/ThemeToggle";

const SELECTION_KEY = "careerhub:selectedJobId";

export default function Home() {
  // The id of the currently selected card, or null if nothing is selected.
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // Run once on mount: restore the selection from sessionStorage.
  useEffect(() => {
    const saved = sessionStorage.getItem(SELECTION_KEY);
    if (saved) setSelectedId(saved);
  }, []);

  // Whenever selectedId changes, persist (or clear) it.
  useEffect(() => {
    if (selectedId) {
      sessionStorage.setItem(SELECTION_KEY, selectedId);
    } else {
      sessionStorage.removeItem(SELECTION_KEY);
    }
  }, [selectedId]);

  // Clicking a card selects it. Clicking the same card again deselects.
  function handleSelect(id: string) {
    setSelectedId((current) => (current === id ? null : id));
  }

  // TanStack Query — fetches and caches the job list.
  const { data: jobs, isPending, isError, error } = useQuery({
    queryKey: ["jobs"],
    queryFn: fetchJobs,
  });

  return (
    <main className="mx-auto max-w-3xl px-4 py-10">
      <header className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
            CareerHub
          </h1>
          <p className="text-sm text-slate-600 dark:text-slate-400">
            Find your next role.
          </p>
        </div>
        <ThemeToggle />
      </header>

      <div>
        {/* Branch 1: still loading — show skeletons. */}
        {isPending && <JobListSkeleton />}

        {/* Branch 2: fetch failed — show a clear error message. */}
        {isError && (
          <div className="rounded-lg border border-red-300 bg-red-50 p-4 text-sm text-red-800 dark:border-red-800 dark:bg-red-950 dark:text-red-200">
            <p className="font-medium">Could not load jobs.</p>
            <p className="mt-1">{error?.message ?? "Unknown error."}</p>
          </div>
        )}

        {/* Branch 3: success — render the real list. */}
        {jobs && (
          <JobList
            jobs={jobs}
            selectedId={selectedId}
            onSelect={handleSelect}
          />
        )}
      </div>
    </main>
  );
}
