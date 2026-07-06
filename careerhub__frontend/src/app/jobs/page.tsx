// src/app/jobs/page.tsx
// Assignment 2.3 — Part 6: URL-driven filters.
// Assignment 3.1 — Part 5: two distinct empty states.
// Assignment 3.3 — Part 2: static metadata for SEO.
//
// The page reads q, location, status from searchParams and filters in JS after
// a cached fetch. It now distinguishes TWO empty states:
//   State 1 — the database has no jobs at all (the UNFILTERED list is empty).
//   State 2 — the filters eliminated every result (unfiltered has jobs, the
//             filtered list is empty).
// The distinction is made SERVER-SIDE, because that is where we hold both counts:
// getJobs returns the filtered list AND a flag for whether the source was empty
// before filtering. The two states offer different actions: State 1 offers
// nothing (the user cannot conjure jobs); State 2 offers "Clear all filters".

import type { Metadata } from "next";
import { JobLinkCard } from "@/components/JobLinkCard";
import { JobFilters } from "@/components/JobFilters";
import { ClearFiltersButton } from "@/components/ClearFiltersButton";
import { JobListing } from "@/types";
import { SearchX, Inbox } from "lucide-react";

// ── Static metadata (Part 2, Step 1) ─────────────────────────────────────────
// Static is correct here: the listing page title and description do not change
// per-request. Individual job titles live on /jobs/[id] and use generateMetadata.
// The URL-driven filters (?q=...) change what is shown but not the page's
// identity for search engines — the canonical page is always "Browse Jobs".

//assignment3.3 defines the static metadata for the jobs page, which is combined with the layout's template to produce the final title and description for SEO.
export const metadata: Metadata = {
  title: "Browse Jobs",
  description:
    "Browse all open positions on CareerHub. Filter by keyword, location, or job type and apply in minutes.",
  openGraph: {
    title: "Browse Jobs | CareerHub",
    description:
      "Browse all open positions on CareerHub. Filter by keyword, location, or job type and apply in minutes.",
    type: "website",
  },
};

// ── Data fetching ─────────────────────────────────────────────────────────────
interface PagedResponse<T> {
  data: T[];
}

interface Filters {
  q: string;
  location: string;
  status: string;
}

interface JobsResult {
  jobs: JobListing[];
  databaseEmpty: boolean; // true when the source had zero jobs before filtering
}

async function getJobs(filters: Filters): Promise<JobsResult> {
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs`, {
    next: { tags: ["jobs"] },
  });
  if (!res.ok) {
    throw new Error(`Failed to fetch jobs: ${res.status}`);
  }

  const json: PagedResponse<JobListing> = await res.json();
  const all = json.data;

  // Whether the database itself is empty is decided BEFORE any filtering.
  const databaseEmpty = all.length === 0;

  let jobs = all;

  if (filters.q) {
    const q = filters.q.toLowerCase();
    jobs = jobs.filter(
      (j) =>
        j.title.toLowerCase().includes(q) ||
        j.companyName.toLowerCase().includes(q)
    );
  }
  if (filters.location) {
    const loc = filters.location.toLowerCase();
    jobs = jobs.filter((j) => j.location.toLowerCase().includes(loc));
  }
  if (filters.status === "open") {
    jobs = jobs.filter((j) => j.status !== "Closed");
  }

  return { jobs, databaseEmpty };
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

  const hasActiveFilters =
    !!filters.q || !!filters.location || filters.status === "open";

  const { jobs, databaseEmpty } = await getJobs(filters);

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

      <JobFilters />

      {jobs.length === 0 ? (
        databaseEmpty ? (
          // STATE 1 — nothing in the database. No action; the user cannot fix it.
          <div className="flex flex-col items-center gap-3 rounded-xl border border-dashed border-zinc-300 p-12 text-center dark:border-zinc-700">
            <Inbox className="h-8 w-8 text-zinc-400" />
            <p className="text-zinc-600 dark:text-zinc-300">
              No jobs are currently listed.
            </p>
            <p className="text-sm text-zinc-400 dark:text-zinc-500">
              Please check back soon.
            </p>
          </div>
        ) : (
          // STATE 2 — filters removed everything. Offer a way to clear them.
          <div className="flex flex-col items-center gap-3 rounded-xl border border-dashed border-zinc-300 p-12 text-center dark:border-zinc-700">
            <SearchX className="h-8 w-8 text-zinc-400" />
            <p className="text-zinc-600 dark:text-zinc-300">
              No jobs match your search.
            </p>
            {/* Filter summary so the user sees what they searched for. */}
            <p className="text-sm text-zinc-400 dark:text-zinc-500">
              {[
                filters.q && `keyword "${filters.q}"`,
                filters.location && `location "${filters.location}"`,
                filters.status === "open" && "open roles only",
              ]
                .filter(Boolean)
                .join(" · ")}
            </p>
            <div className="mt-2">
              <ClearFiltersButton />
            </div>
          </div>
        )
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {jobs.map((job) => (
            <JobLinkCard key={job.id} job={job} />
          ))}
        </div>
      )}

      {/* hasActiveFilters is computed for the summary above; referenced here to
          keep the value meaningful even when the grid renders. */}
      {hasActiveFilters && jobs.length > 0 && (
        <p className="mt-6 text-xs text-zinc-400 dark:text-zinc-500">
          Filters active — <span className="font-medium">{jobs.length}</span> shown.
        </p>
      )}
    </div>
  );
}
