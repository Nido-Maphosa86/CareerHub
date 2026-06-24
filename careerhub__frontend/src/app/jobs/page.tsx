// src/app/jobs/page.tsx
// Assignment 2.1 — Part 3: the candidate-facing jobs listing.
//
// This is a Server Component (async function, no "use client"). The fetch runs
// on the server, so the job data is baked into the HTML before it reaches the
// browser — there is no client-side request to the jobs API on page load.

//JObs page
import { JobLinkCard } from "@/components/JobLinkCard";
import { JobListing } from "@/types";

// The real CareerHub.Api wraps list responses in a pagination envelope.
interface PagedResponse<T> {
  data: T[];
}

export default async function JobsPage() {
  // cache: "no-store" forces a fresh server-side fetch on every request, so the
  // listing always reflects the current database (see README, item 1).
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs`, {
    cache: "no-store",
  });

  // Do not swallow errors — a bad response surfaces loudly.
  if (!res.ok) {
    throw new Error(`Failed to fetch jobs: ${res.status}`);
  }

  // Unwrap the paginated envelope to a plain array.
  const json: PagedResponse<JobListing> = await res.json();
  const jobs = json.data;

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-black tracking-tight text-zinc-900 dark:text-zinc-50 sm:text-4xl">
          Open positions
        </h1>
        <p className="mt-1 text-zinc-500 dark:text-zinc-400">
          {jobs.length} {jobs.length === 1 ? "role" : "roles"} available — click a card to view and apply.
        </p>
      </div>

      {jobs.length === 0 ? (
        // Clear empty state.
        <div className="rounded-xl border border-dashed border-zinc-300 p-12 text-center dark:border-zinc-700">
          <p className="text-zinc-500 dark:text-zinc-400">
            No open positions right now. Check back soon.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {jobs.map((job) => (
            <JobLinkCard key={job.id} job={job} />
          ))}
        </div>
      )}
    </div>
  );
}
