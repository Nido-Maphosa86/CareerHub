// src/components/JobCard.tsx
// One job listing rendered as a card.
// Clicking the card calls onSelect with the job id.

"use client";

import { JobListing } from "@/types";
import { JobStatusBadge } from "@/components/JobStatusBadge";
import { cn } from "@/lib/utils";

interface Props {
  job: JobListing;
  isSelected: boolean;
  onSelect: (id: string) => void;
}

export function JobCard({ job, isSelected, onSelect }: Props) {
  return (
    <button
      type="button"
      onClick={() => onSelect(job.id)}
      className={cn(
        // Base card styles — work in both light and dark mode.
        "w-full text-left rounded-lg border p-4 transition-all",
        "bg-white border-slate-200 hover:border-slate-400",
        "dark:bg-slate-900 dark:border-slate-700 dark:hover:border-slate-500",
        // Selected state — coloured ring around the card.
        isSelected && "ring-2 ring-emerald-500 border-emerald-500"
      )}
    >
      <div className="flex items-start justify-between gap-3">
        <div>
          <h3 className="font-semibold text-slate-900 dark:text-slate-100">
            {job.title}
          </h3>
          <p className="text-sm text-slate-600 dark:text-slate-400">
            {job.company} &middot; {job.location}
          </p>
        </div>
        <JobStatusBadge type={job.employmentType} />
      </div>

      <p className="mt-3 text-sm text-slate-700 dark:text-slate-300">
        {job.description}
      </p>

      <div className="mt-3 flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
        <span>{job.salary}</span>
        <span>Posted {job.postedAt}</span>
      </div>
    </button>
  );
}
