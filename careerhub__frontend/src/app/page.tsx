// src/app/page.tsx
// Loads jobs via TanStack Query, renders the grid, and when a job is selected
// shows a selection panel plus either the application form (if logged in as an
// applicant) or a login prompt.

"use client";

import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { fetchJobs } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { JobList } from "@/components/JobList";
import { JobListSkeleton } from "@/components/JobCardSkeleton";
import { ThemeToggle } from "@/components/ThemeToggle";
import { JobStatusBadge } from "@/components/JobStatusBadge";
import { ApplicationForm } from "@/components/ApplicationForm";
import { LoginPanel } from "@/components/LoginPanel";
import { AuthStatus } from "@/components/AuthStatus";
import { AlertCircle, X, ShieldAlert } from "lucide-react";

const SELECTION_KEY = "careerhub:selectedJobId";

export default function Home() {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const { isAuthenticated, isApplicant } = useAuth();

  useEffect(() => {
    const saved = sessionStorage.getItem(SELECTION_KEY);
    if (saved) setSelectedId(saved);
  }, []);

  useEffect(() => {
    if (selectedId) {
      sessionStorage.setItem(SELECTION_KEY, selectedId);
    } else {
      sessionStorage.removeItem(SELECTION_KEY);
    }
  }, [selectedId]);

  function handleSelect(id: string) {
    setSelectedId((current) => (current === id ? null : id));
  }

  const { data: jobs, isPending, isError, error } = useQuery({
    queryKey: ["jobs"],
    queryFn: fetchJobs,
  });

  const selectedJob = jobs?.find((j) => j.id === selectedId) ?? null;

  return (
    <div className="min-h-screen">
      <header className="border-b border-zinc-200 dark:border-zinc-800">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex h-8 w-8 items-center justify-center rounded-md bg-lime-400">
              <span className="text-base font-black text-black">C</span>
            </div>
            <span className="text-xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
              Career<span className="text-lime-500 dark:text-lime-400">Hub</span>
            </span>
          </div>
          <div className="flex items-center gap-3">
            <AuthStatus />
            <ThemeToggle />
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-10">
        <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
          <div>
            <h1 className="text-3xl font-black tracking-tight text-zinc-900 dark:text-zinc-50 sm:text-4xl">
              Open positions
            </h1>
            <p className="mt-1 text-zinc-500 dark:text-zinc-400">
              Find your next role.
            </p>
          </div>

          {jobs && (
            <div className="rounded-full border border-lime-300 bg-lime-50 px-4 py-1.5 text-sm font-semibold text-lime-700 dark:border-lime-400/30 dark:bg-lime-400/10 dark:text-lime-300">
              {jobs.length} {jobs.length === 1 ? "role" : "roles"} available
            </div>
          )}
        </div>

        {isPending && <JobListSkeleton />}

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

        {jobs && (
          <JobList jobs={jobs} selectedId={selectedId} onSelect={handleSelect} />
        )}

        {/* Selection panel + apply area */}
        {!isPending && !isError && selectedJob && (
          <section className="mt-10 grid grid-cols-1 gap-6 lg:grid-cols-2">
            {/* Selection panel (from Assignment 1.2). */}
            <div className="rounded-xl border border-zinc-200 bg-white p-6 dark:border-zinc-800 dark:bg-zinc-950">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <div className="text-xs font-semibold uppercase tracking-widest text-lime-600 dark:text-lime-400">
                    {selectedJob.companyName}
                  </div>
                  <h2 className="mt-1 text-2xl font-bold text-zinc-900 dark:text-zinc-50">
                    {selectedJob.title}
                  </h2>
                </div>
                <button
                  type="button"
                  onClick={() => setSelectedId(null)}
                  aria-label="Clear selection"
                  className="rounded-md p-1 text-zinc-400 transition-colors hover:bg-zinc-100 hover:text-zinc-700 dark:hover:bg-zinc-800 dark:hover:text-zinc-200"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>

              <div className="mt-3 flex flex-wrap items-center gap-2">
                <JobStatusBadge type={selectedJob.type} />
                <span className="text-sm text-zinc-500 dark:text-zinc-400">
                  {selectedJob.location}
                </span>
              </div>

              <p className="mt-4 text-sm text-zinc-600 dark:text-zinc-300">
                {selectedJob.description}
              </p>

              <div className="mt-4 border-t border-zinc-100 pt-4 dark:border-zinc-800">
                <span className="text-base font-semibold text-zinc-900 dark:text-zinc-100">
                  {selectedJob.salaryDisplay}
                </span>
              </div>
            </div>

            {/* Apply area — gated on auth + role. */}
            {!isAuthenticated && <LoginPanel />}

            {isAuthenticated && !isApplicant && (
              <div className="flex items-start gap-3 rounded-xl border border-amber-300 bg-amber-50 p-6 text-amber-800 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-200">
                <ShieldAlert className="mt-0.5 h-5 w-5 shrink-0" />
                <div>
                  <p className="font-semibold">Applicant account required</p>
                  <p className="mt-1 text-sm">
                    You&apos;re logged in, but only applicant accounts can apply
                    for jobs. Log out and sign in as an applicant.
                  </p>
                </div>
              </div>
            )}

            {isAuthenticated && isApplicant && (
              <ApplicationForm
                listingId={selectedJob.id}
                jobTitle={selectedJob.title}
              />
            )}
          </section>
        )}
      </main>
    </div>
  );
}
