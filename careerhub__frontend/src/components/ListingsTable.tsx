// src/components/ListingsTable.tsx
// Assignment 2.2 — Parts 4, 5, 6: the employer listings.
// Assignment 2.3 — Part 7: now renders as a table OR a card grid, and can hide
// closed jobs, based on props supplied by the dashboard.
//
// It is still an async Server Component that fetches BOTH jobs and stats
// internally in parallel (Promise.all). What it CANNOT do is read the Zustand
// store — Zustand's useStore is a React hook and hooks only run in Client
// Components, while this runs on the server during streaming. So the view and
// showClosedJobs values are passed in as PROPS by a thin Client wrapper
// (DashboardView) that reads the store and forwards them here.

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

interface Props {
  view: "table" | "grid";
  showClosedJobs: boolean;
}

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

export async function ListingsTable({ view, showClosedJobs }: Props) {
  const [allJobs, stats] = await Promise.all([getJobs(), getApplicationStats()]);

  const countByJob = new Map(stats.map((s) => [s.jobId, s.applicationCount]));

  // Apply the "show closed jobs" preference. Same data, no new fetch.
  const jobs = showClosedJobs
    ? allJobs
    : allJobs.filter((j) => j.status !== "Closed");

  if (jobs.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-zinc-300 p-12 text-center dark:border-zinc-700">
        <p className="text-zinc-500 dark:text-zinc-400">No listings to show.</p>
      </div>
    );
  }

  // GRID VIEW — cards reusing the same jobs + stats.
  if (view === "grid") {
    return (
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {jobs.map((job) => (
          <div
            key={job.id}
            className="flex flex-col gap-3 rounded-xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950"
          >
            <div className="flex items-start justify-between gap-2">
              <h3 className="font-bold text-zinc-900 dark:text-zinc-100">
                {job.title}
              </h3>
              <span
                className={
                  job.status === "Closed"
                    ? "rounded-full bg-zinc-100 px-2 py-0.5 text-xs font-semibold text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400"
                    : "rounded-full bg-lime-100 px-2 py-0.5 text-xs font-semibold text-lime-700 dark:bg-lime-400/10 dark:text-lime-300"
                }
              >
                {job.status}
              </span>
            </div>
            <p className="text-sm font-medium text-lime-600 dark:text-lime-400">
              {job.companyName}
            </p>
            <p className="text-sm text-zinc-500 dark:text-zinc-400">
              {job.location}
            </p>
            <p className="text-sm text-zinc-600 dark:text-zinc-300">
              {countByJob.get(job.id) ?? 0} applications
            </p>
            <div className="mt-1 flex items-center justify-between border-t border-zinc-100 pt-3 dark:border-zinc-800">
              <Link
                href={`/jobs/${job.id}`}
                className="text-sm font-medium text-lime-600 underline-offset-2 hover:underline dark:text-lime-400"
              >
                View
              </Link>
              <CloseJobButton jobId={job.id} currentStatus={job.status} />
            </div>
          </div>
        ))}
      </div>
    );
  }

  // TABLE VIEW (default).
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
