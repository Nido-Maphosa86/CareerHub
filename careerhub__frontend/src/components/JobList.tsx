// src/components/JobList.tsx
// Renders jobs in a responsive grid (cards side by side).
// 1 column on mobile, 2 on tablet, 3 on desktop.

"use client";

import { JobListing } from "@/types";
import { JobCard } from "@/components/JobCard";

interface Props {
  jobs: JobListing[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}

export function JobList({ jobs, selectedId, onSelect }: Props) {
  // Friendly empty state if the API returns no jobs.
  if (jobs.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-zinc-300 p-12 text-center dark:border-zinc-700">
        <p className="text-zinc-500 dark:text-zinc-400">
          No open positions right now. Check back soon.
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {jobs.map((job) => (
        <JobCard
          key={job.id}
          job={job}
          isSelected={selectedId === job.id}
          onSelect={onSelect}
        />
      ))}
    </div>
  );
}
