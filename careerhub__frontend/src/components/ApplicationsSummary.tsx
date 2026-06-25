// src/components/ApplicationsSummary.tsx
// Assignment 2.2 — Part 5: the "fast" streaming component on the dashboard.
//
// An async Server Component (no "use client"). It fetches its own stats, sums
// the application counts, and shows a single stat card. Because it does just
// one small fetch, it resolves quickly — so when wrapped in its own <Suspense>
// boundary, it can replace its skeleton before the slower table is ready.


//application count screen on dashboard
//calls the route handler applications stats// which lives in the frontend
//application summary Skeleton
import { Users } from "lucide-react";

interface JobStat {
  jobId: string;
  applicationCount: number;
}

// Fetches the stats endpoint on the frontend origin. cache: "no-store" keeps
// application numbers always fresh (candidates apply at any time).
async function getApplicationStats(): Promise<JobStat[]> {
  const res = await fetch(
    `${process.env.NEXT_PUBLIC_SITE_URL}/api/applications/stats`,
    { cache: "no-store" }// calls the frontend route handler to get the application stats, which in turn calls the backend jobs endpoint to get the application counts for each job
  );
  if (!res.ok) {
    throw new Error(`Failed to fetch application stats: ${res.status}`);
  }
  return res.json();
}

export async function ApplicationsSummary() {
  const stats = await getApplicationStats();

  // Total applications across all jobs.
  const total = stats.reduce((sum, s) => sum + s.applicationCount, 0);

  return (
    <div className="flex items-center gap-4 rounded-xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950">
      <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-lime-100 dark:bg-lime-400/10">
        <Users className="h-6 w-6 text-lime-600 dark:text-lime-400" />
      </div>
      <div>
        <div className="text-xs font-semibold uppercase tracking-widest text-zinc-500 dark:text-zinc-400">
          Total Applications
        </div>
        <div className="text-3xl font-black text-zinc-900 dark:text-zinc-50">
          {total}
        </div>
      </div>
    </div>
  );
}

// Suspense fallback — an animate-pulse block matching the card's dimensions.
export function ApplicationsSummarySkeleton() {
  return (
    <div className="flex items-center gap-4 rounded-xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950">
      <div className="h-12 w-12 animate-pulse rounded-lg bg-zinc-200 dark:bg-zinc-800" />
      <div className="space-y-2">
        <div className="h-3 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-8 w-16 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
    </div>
  );
}
