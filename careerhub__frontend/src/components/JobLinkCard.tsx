// src/components/JobLinkCard.tsx
// Assignment 2.1 — Part 3.
// A navigation card for the /jobs listing. Unlike JobCard (which is a
// selection component with an onClick), this wraps its content in a <Link> so
// clicking it changes the URL to /jobs/{id}.
//
// It has no event handlers and no state, so it is a Server Component — there is
// no "use client" directive. <Link> uses hooks internally, but those run inside
// next/link's OWN client boundary, not ours (see README, "Why JobLinkCard has
// no use client").

import Link from "next/link";
import { JobListing } from "@/types";
import { JobStatusBadge } from "@/components/JobStatusBadge";
import { MapPin } from "lucide-react";

interface Props {
  job: JobListing;
}

export function JobLinkCard({ job }: Props) {
  return (
    <Link
      href={`/jobs/${job.id}`}
      className="group relative flex h-full flex-col overflow-hidden rounded-xl border border-zinc-200 bg-white p-5 transition-all duration-200 hover:-translate-y-0.5 hover:border-zinc-300 hover:shadow-lg dark:border-zinc-800 dark:bg-zinc-950 dark:hover:border-zinc-700 dark:hover:shadow-lime-400/5"
    >
      {/* Lime accent bar that grows on hover. */}
      <span
        className="absolute left-0 top-0 h-full w-1 origin-top scale-y-0 bg-lime-400 transition-transform duration-200 group-hover:scale-y-100"
        aria-hidden
      />

      {/* Company eyebrow. */}
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

      <div className="flex-1" />

      {/* Employment-type badge (the already-written JobStatusBadge). */}
      <div className="mt-4 flex items-center justify-between">
        <JobStatusBadge type={job.type} />
        <span className="text-xs font-medium text-zinc-400 dark:text-zinc-500">
          {job.status}
        </span>
      </div>
    </Link>
  );
}
