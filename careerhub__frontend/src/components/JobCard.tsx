// src/components/JobCard.tsx
// One job listing as a card, designed to sit in a grid.
// Clicking the card selects it (lime ring + accent corner).

"use client";

import { JobListing } from "@/types";
import { JobStatusBadge } from "@/components/JobStatusBadge";
import { cn } from "@/lib/utils";
import { MapPin, Users } from "lucide-react";

interface Props {
  job: JobListing;
  isSelected: boolean;
  onSelect: (id: string) => void;
}

export function JobCard({ job, isSelected, onSelect }: Props) {
  // Trim the ISO timestamp down to a readable date.
  const postedDate = new Date(job.postedAt).toLocaleDateString("en-ZA", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });

  return (
    <button
      type="button"
      onClick={() => onSelect(job.id)}
      className={cn(
        // Layout — full height so cards in a row line up.
        "group relative flex h-full flex-col overflow-hidden rounded-xl border p-5 text-left transition-all duration-200",
        // Surface — quiet by default.
        "border-zinc-200 bg-white hover:-translate-y-0.5 hover:border-zinc-300 hover:shadow-lg",
        "dark:border-zinc-800 dark:bg-zinc-950 dark:hover:border-zinc-700 dark:hover:shadow-lime-400/5",
        // Selected — the one place lime gets loud.
        isSelected &&
          "border-lime-500 ring-2 ring-lime-400 dark:border-lime-400 dark:ring-lime-400/60"
      )}
    >
      {/* Lime accent bar that grows on hover/selection. */}
      <span
        className={cn(
          "absolute left-0 top-0 h-full w-1 bg-lime-400 transition-transform duration-200",
          isSelected ? "scale-y-100" : "scale-y-0 group-hover:scale-y-100"
        )}
        aria-hidden
      />

      {/* Eyebrow — company name, the lime accent text. */}
      <div className="mb-1 text-xs font-semibold uppercase tracking-widest text-lime-600 dark:text-lime-400">
        {job.companyName}
      </div>

      {/* Title. */}
      <h3 className="text-lg font-bold leading-tight text-zinc-900 dark:text-zinc-50">
        {job.title}
      </h3>

      {/* Location. */}
      <div className="mt-2 flex items-center gap-1.5 text-sm text-zinc-500 dark:text-zinc-400">
        <MapPin className="h-3.5 w-3.5 shrink-0" />
        <span>{job.location}</span>
      </div>

      {/* Description — clamped so cards stay even height. */}
      <p className="mt-3 line-clamp-2 text-sm text-zinc-600 dark:text-zinc-300">
        {job.description}
      </p>

      {/* Spacer pushes the footer to the bottom. */}
      <div className="flex-1" />

      {/* Badge + applicant count. */}
      <div className="mt-4 flex items-center justify-between">
        <JobStatusBadge type={job.type} />
        <span className="flex items-center gap-1 text-xs text-zinc-400 dark:text-zinc-500">
          <Users className="h-3 w-3" />
          {job.applicationCount}
        </span>
      </div>

      {/* Footer — salary (emphasised) and posted date. */}
      <div className="mt-3 border-t border-zinc-100 pt-3 dark:border-zinc-800">
        <div className="flex items-center justify-between">
          <span className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">
            {job.salaryDisplay}
          </span>
          <span className="text-xs text-zinc-400 dark:text-zinc-500">
            {postedDate}
          </span>
        </div>
      </div>
    </button>
  );
}
