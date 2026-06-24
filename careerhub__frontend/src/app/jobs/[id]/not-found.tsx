// src/app/jobs/[id]/not-found.tsx
// Assignment 2.1 — Part 4: the not-found boundary for the job detail route.
//
// notFound() in page.tsx renders this file. It is a Server Component (no
// "use client") and inherits the root layout automatically, so the app header
// and navigation remain in place around it.

import Link from "next/link";
import { SearchX } from "lucide-react";

// The JobNotFound component is a Server Component that displays a message when a job is not found. It provides a link to navigate back to the jobs listing page.
export default function JobNotFound() {
  return (
    <div className="mx-auto max-w-lg py-16 text-center">
      <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-lime-100 dark:bg-lime-400/10">
        <SearchX className="h-7 w-7 text-lime-600 dark:text-lime-400" />
      </div>

      <h1 className="text-2xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
        Job Not Found
      </h1>
      <p className="mt-2 text-zinc-500 dark:text-zinc-400">
        The job you&apos;re looking for doesn&apos;t exist, or it may have been
        removed. It might have closed since you last saw the link.
      </p>

      <Link
        href="/jobs"// back to jobs link
        className="mt-6 inline-flex items-center rounded-lg bg-lime-400 px-4 py-2.5 text-sm font-semibold text-black transition-colors hover:bg-lime-300"
      >
        Back to all jobs
      </Link>
    </div>
  );
}
