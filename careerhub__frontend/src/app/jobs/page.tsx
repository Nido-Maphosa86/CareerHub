// src/app/jobs/page.tsx
// Assignment 2.3 — Part 6: the candidate jobs listing with URL-driven filters.
//
// The page reads q, location, and status from searchParams (Next.js passes the
// URL query to page components) and filters the jobs in getJobs() before
// rendering. The fetch itself still uses next: { tags: ["jobs"] } — the cache
// stores the FULL unfiltered list, and we filter in JavaScript afterwards
// because the mock API does not support query-parameter filtering.

import { JobLinkCard } from "@/components/JobLinkCard";
import { JobFilters } from "@/components/JobFilters";
import { JobListing } from "@/types";

interface PagedResponse<T> {
  data: T[];
}

interface Filters {
  q: string;
  location: string;
  status: string;
}

// Fetch the full list (cached/tagged), then filter in JS.
async function getJobs(filters: Filters): Promise<JobListing[]> {
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs`, {
    next: { tags: ["jobs"] },
  });
  if (!res.ok) {
    throw new Error(`Failed to fetch jobs: ${res.status}`);
  }

  const json: PagedResponse<JobListing> = await res.json();
  let jobs = json.data;

  // Keyword: match title or company, case-insensitive.
  if (filters.q) {
    const q = filters.q.toLowerCase();
    jobs = jobs.filter(
      (j) =>
        j.title.toLowerCase().includes(q) ||
        j.companyName.toLowerCase().includes(q)
    );
  }

  // Location: case-insensitive substring.
  if (filters.location) {
    const loc = filters.location.toLowerCase();
    jobs = jobs.filter((j) => j.location.toLowerCase().includes(loc));
  }

  // Status: "open" keeps only non-closed jobs; "all" keeps everything.
  if (filters.status === "open") {
    jobs = jobs.filter((j) => j.status !== "Closed");
  }

  return jobs;
}

interface Props {
  searchParams: Promise<{ q?: string; location?: string; status?: string }>;
}

export default async function JobsPage({ searchParams }: Props) {
  const params = await searchParams;

  const filters: Filters = {
    q: params.q ?? "",
    location: params.location ?? "",
    status: params.status ?? "all",
  };

  const jobs = await getJobs(filters);

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-black tracking-tight text-zinc-900 dark:text-zinc-50 sm:text-4xl">
          Open positions
        </h1>
        <p className="mt-1 text-zinc-500 dark:text-zinc-400">
          {jobs.length} {jobs.length === 1 ? "role" : "roles"} match your filters — click a card to view and apply.
        </p>
      </div>

      {/* URL-driven filters. */}
      <JobFilters />

      {jobs.length === 0 ? (
        <div className="rounded-xl border border-dashed border-zinc-300 p-12 text-center dark:border-zinc-700">
          <p className="text-zinc-500 dark:text-zinc-400">
            No positions match your filters. Try clearing them.
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
