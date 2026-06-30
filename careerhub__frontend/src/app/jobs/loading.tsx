// src/app/jobs/loading.tsx
// Assignment 3.1 — Part 5: route-level loading UI for /jobs.
//
// Next.js shows this automatically while the page's async fetch is pending. It
// reuses the PAIRED JobListSkeleton (six JobCardSkeletons) so the loading state
// matches the real grid exactly and there is no layout shift when the cards
// arrive — a spinner or blank page would not give that shape.

import { JobListSkeleton } from "@/components/JobCardSkeleton";

export default function Loading() {
  return (
    <div>
      <div className="mb-8">
        <div className="h-9 w-56 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="mt-2 h-4 w-72 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
      <JobListSkeleton count={6} />
    </div>
  );
}
