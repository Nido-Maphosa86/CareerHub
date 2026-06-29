// src/app/jobs/[id]/page.tsx
// Assignment 2.1 — Part 4: the job detail page.
//
// This is the Server/Client composition moment. The page is a Server Component:
// it fetches the single job on the server, decides what to render, and passes
// plain props to <ApplyPanel> (which wraps the Client Component ApplicationForm).
// The server does the data fetching; the client does the form state, validation,
// and mutation. Each does exactly what it is designed for.


//job details page
//fetches the job and reads the session
import Link from "next/link";
import { notFound } from "next/navigation";
import { JobListing } from "@/types";
import { JobStatusBadge } from "@/components/JobStatusBadge";
import { ApplyPanel } from "@/components/ApplyPanel";
import { auth } from "@/auth";
import { ArrowLeft, MapPin, Lock, ShieldAlert } from "lucide-react";

interface Props {
  // Next.js 15: params is async and must be awaited.
  params: Promise<{ id: string }>;
}

export default async function JobDetailPage({ params }: Props) {
  const { id } = await params;

  // Assignment 2.3 — Part 5: read the session ALONGSIDE the job fetch with
  // Promise.all so the two run in parallel. This page stays public (employers
  // may VIEW it), but only candidates should see the application form — that
  // distinction is decided here, not in middleware.
  //
  // Assignment 2.2 — Part 3 + Stretch B: two cache tags on the job fetch.
  const [res, session] = await Promise.all([
    fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs/${id}`, {
      next: { tags: ["jobs", `job-${id}`] },
    }),
    auth(),
  ]);

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
  const role = session?.user?.role ?? null;
  const isSignedIn = !!session;
  const isEmployer = role === "employer";

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

      {/* Apply area below the details. Assignment 2.3 — Part 5 role gating:
            closed           -> "Applications closed" message
            employer         -> "Employers cannot apply" message, no form
            signed out       -> form with a "sign in to apply" note
            candidate        -> the application form renders normally        */}
      <div className="mt-6">
        {isClosed ? (
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
        ) : isEmployer ? (
          // Employers may view the job but cannot apply.
          <div className="flex items-start gap-3 rounded-xl border border-amber-300 bg-amber-50 p-6 text-amber-800 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-200">
            <ShieldAlert className="mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <p className="font-semibold">Employers cannot apply for jobs.</p>
              <p className="mt-1 text-sm">
                This account manages listings. Applications are for candidate accounts.
              </p>
            </div>
          </div>
        ) : !isSignedIn ? (
          // Signed out — show the form but note that signing in is required.
          <div>
            <div className="mb-4 rounded-xl border border-zinc-200 bg-zinc-50 px-4 py-3 text-sm text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900/50 dark:text-zinc-300">
              You must be signed in to apply.{" "}
              <Link
                href="/login"
                className="font-semibold text-lime-600 underline-offset-2 hover:underline dark:text-lime-400"
              >
                Sign in here.
              </Link>
            </div>
            <ApplyPanel listingId={job.id} jobTitle={job.title} />
          </div>
        ) : (
          // Candidate — the form renders normally.
          <ApplyPanel listingId={job.id} jobTitle={job.title} />
        )}
      </div>
    </div>
  );
}
