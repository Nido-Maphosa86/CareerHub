// src/app/jobs/[id]/page.tsx
// Assignment 2.1 — Part 4: the job detail page.
//
// This is the Server/Client composition moment. The page is a Server Component:
// it fetches the single job on the server, decides what to render, and passes
// plain props to <ApplyPanel> (which wraps the Client Component ApplicationForm).
// The server does the data fetching; the client does the form state, validation,
// and mutation. Each does exactly what it is designed for.

import Link from "next/link";
import { notFound } from "next/navigation";
import { JobListing } from "@/types";
import { JobStatusBadge } from "@/components/JobStatusBadge";
import { ApplyPanel } from "@/components/ApplyPanel";
import { ArrowLeft, MapPin, Lock } from "lucide-react";


//JOb details screen
//2 tags jobs and job-${id} are used to cache the job details and the jobs list, so that when a job is closed or updated, the cache can be invalidated and the jobs list can be refreshed with the latest data.

interface Props {
  // Next.js 15: params is async and must be awaited.
  params: Promise<{ id: string }>;
}

export default async function JobDetailPage({ params }: Props) {
  const { id } = await params;

  // Assignment 2.2 — Part 3 + Stretch B: two cache tags.
  //  - "jobs": shared with the listing pages, so closing any job invalidates this too.
  //  - `job-${id}`: a per-job tag, so the close action can invalidate ONLY this
  //    job's detail page without forcing every other job detail to refetch.
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs/${id}`, {
    next: { tags: ["jobs", `job-${id}`] }, //when revalidate is called, it will invalidate the cache for this job and the jobs list
                                           //share the same "jobs" tag as the jobs list, so closing any job invalidates this too.
  });

  // 404 -> show the not-found boundary immediately; do not render a partial page.
  if (res.status === 404) {
    notFound();
  }

  // Any other non-OK status -> throw, surfacing the nearest error boundary.
  if (!res.ok) {
    throw new Error(`Failed to fetch job: ${res.status}`);
  }

  const job: JobListing = await res.json();

  const isClosed = job.status === "Closed";

  return (
    <div className="mx-auto max-w-3xl">
      {/* Back link above the content. */}
      <Link
        href="/jobs"
        className="mb-6 inline-flex items-center gap-1.5 text-sm font-medium text-zinc-500 transition-colors hover:text-lime-600 dark:text-zinc-400 dark:hover:text-lime-400"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to jobs
      </Link>

      {/* Job details. */}
      <div className="rounded-xl border border-zinc-200 bg-white p-6 dark:border-zinc-800 dark:bg-zinc-950 sm:p-8">
        <div className="text-xs font-semibold uppercase tracking-widest text-lime-600 dark:text-lime-400">
          {job.companyName}
        </div>
        <h1 className="mt-1 text-3xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
          {job.title}
        </h1>

        <div className="mt-3 flex flex-wrap items-center gap-3">
          <JobStatusBadge type={job.type} />
          <span className="flex items-center gap-1.5 text-sm text-zinc-500 dark:text-zinc-400">
            <MapPin className="h-3.5 w-3.5" />
            {job.location}
          </span>
          <span className="text-sm font-medium text-zinc-400 dark:text-zinc-500">
            {job.status}
          </span>
        </div>

        <p className="mt-6 whitespace-pre-line text-sm leading-relaxed text-zinc-600 dark:text-zinc-300">
          {job.description}
        </p>

        <div className="mt-6 border-t border-zinc-100 pt-4 dark:border-zinc-800">
          <span className="text-base font-semibold text-zinc-900 dark:text-zinc-100">
            {job.salaryDisplay}
          </span>
        </div>
      </div>

      {/* Apply area below the details. */}
      <div className="mt-6">
        {isClosed ? (
          // Closed jobs cannot be applied to — show a message instead of the form.
          <div className="flex items-start gap-3 rounded-xl border border-zinc-200 bg-zinc-50 p-6 text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900/50 dark:text-zinc-300">
            <Lock className="mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <p className="font-semibold text-zinc-800 dark:text-zinc-100">
                Applications closed
              </p>
              <p className="mt-1 text-sm">
                This listing is no longer accepting applications.
              </p>
            </div>
          </div>
        ) : (
          // Open job: the Client Component handles auth gating + the form.
          <ApplyPanel listingId={job.id} jobTitle={job.title} />
        )}
      </div>
    </div>
  );
}
