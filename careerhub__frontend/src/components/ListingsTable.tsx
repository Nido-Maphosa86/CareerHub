// src/components/ListingsTable.tsx
// Assignment 2.2 — Parts 4, 5, 6: the employer listings table.
//
// An async Server Component. It is self-contained: it fetches BOTH jobs and
// stats internally (in parallel via Promise.all) rather than receiving them as
// props. That makes it droppable behind its own <Suspense> boundary on the
// dashboard. It joins each job to its application count and renders the table,
// including the Close action per row.

import Link from "next/link";
import { JobListing } from "@/types";
import { CloseJobButton } from "@/components/CloseJobButton";

interface PagedResponse<T> {
  data: T[];
}

interface JobStat {
  jobId: string;
  applicationCount: number;
}

// Jobs come from the real backend, tagged "jobs" so the close action can
// invalidate them (Part 3).
async function getJobs(): Promise<JobListing[]> {
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs`, {
    next: { tags: ["jobs"] },
  });
  if (!res.ok) {
    throw new Error(`Failed to fetch jobs: ${res.status}`);
  }
  const json: PagedResponse<JobListing> = await res.json();
  return json.data;
}

// Stats come from the frontend's own endpoint, always fresh.
async function getApplicationStats(): Promise<JobStat[]> {
  const res = await fetch(
    `${process.env.NEXT_PUBLIC_SITE_URL}/api/applications/stats`,
    { cache: "no-store" }
  );
  if (!res.ok) {
    throw new Error(`Failed to fetch application stats: ${res.status}`);
  }
  return res.json();
}

export async function ListingsTable() {
  // Part 4: fetch both sources in PARALLEL. Promise.all starts both fetches at
  // once and waits for the slower of the two — not one after the other.
  const [jobs, stats] = await Promise.all([getJobs(), getApplicationStats()]);

  // Build a quick id -> count lookup for the join.
  const countByJob = new Map(stats.map((s) => [s.jobId, s.applicationCount]));

  if (jobs.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-zinc-300 p-12 text-center dark:border-zinc-700">
        <p className="text-zinc-500 dark:text-zinc-400">No listings yet.</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-zinc-200 dark:border-zinc-800">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-zinc-200 bg-zinc-50 text-xs uppercase tracking-wider text-zinc-500 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-400">
          <tr>
            <th className="px-4 py-3 font-semibold">Title</th>
            <th className="px-4 py-3 font-semibold">Company</th>
            <th className="px-4 py-3 font-semibold">Location</th>
            <th className="px-4 py-3 font-semibold">Status</th>
            <th className="px-4 py-3 font-semibold">Applications</th>
            <th className="px-4 py-3 font-semibold">View</th>
            <th className="px-4 py-3 font-semibold">Action</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-zinc-100 dark:divide-zinc-800">
          {jobs.map((job) => (
            <tr
              key={job.id}
              className="bg-white transition-colors hover:bg-zinc-50 dark:bg-zinc-950 dark:hover:bg-zinc-900/50"
            >
              <td className="px-4 py-3 font-medium text-zinc-900 dark:text-zinc-100">
                {job.title}
              </td>
              <td className="px-4 py-3 text-zinc-600 dark:text-zinc-300">
                {job.companyName}
              </td>
              <td className="px-4 py-3 text-zinc-600 dark:text-zinc-300">
                {job.location}
              </td>
              <td className="px-4 py-3">
                <span
                  className={
                    job.status === "Closed"
                      ? "text-zinc-400 dark:text-zinc-500"
                      : "font-medium text-lime-600 dark:text-lime-400"
                  }
                >
                  {job.status}
                </span>
              </td>
              {/* Join: look up the count by job id, default to 0 if absent. */}
              <td className="px-4 py-3 font-medium text-zinc-900 dark:text-zinc-100">
                {countByJob.get(job.id) ?? 0}
              </td>
              <td className="px-4 py-3">
                <Link
                  href={`/jobs/${job.id}`}
                  className="font-medium text-lime-600 underline-offset-2 hover:underline dark:text-lime-400"
                >
                  View
                </Link>
              </td>
              <td className="px-4 py-3">
                <CloseJobButton jobId={job.id} currentStatus={job.status} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// Suspense fallback — five animate-pulse rows matching the table row height.
export function ListingsTableSkeleton() {
  return (
    <div className="overflow-hidden rounded-xl border border-zinc-200 dark:border-zinc-800">
      <div className="border-b border-zinc-200 bg-zinc-50 px-4 py-3 dark:border-zinc-800 dark:bg-zinc-900">
        <div className="h-3 w-24 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
      {Array.from({ length: 5 }).map((_, i) => (
        <div
          key={i}
          className="flex items-center gap-4 border-b border-zinc-100 px-4 py-3.5 last:border-0 dark:border-zinc-800"
        >
          <div className="h-4 flex-1 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 w-24 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 w-24 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 w-16 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      ))}
    </div>
  );
}
