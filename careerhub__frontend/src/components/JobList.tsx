// src/components/JobList.tsx
// Renders a list of JobCards. Pure presentational — gets data via props.

"use client";

import { JobListing } from "@/types";
import { JobCard } from "@/components/JobCard";

interface Props {
  jobs: JobListing[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}

export function JobList({ jobs, selectedId, onSelect }: Props) {
  // Friendly empty state in case the API returns an empty array.
  if (jobs.length === 0) {
    return (
      <p className="text-center text-slate-500 dark:text-slate-400">
        No jobs available right now. Check back soon.
      </p>
    );
  }

  return (
    <div className="space-y-3">
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
