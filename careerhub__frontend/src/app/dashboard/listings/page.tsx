// src/app/dashboard/listings/page.tsx
// Assignment 2.1 — Part 5: the employer listings table.
//
// A Server Component. Fetches the same jobs endpoint as /jobs but renders a
// data-dense table rather than a card grid — the employer view is a list, not
// a gallery.

import Link from "next/link";
import { JobListing } from "@/types";

interface PagedResponse<T> {
  data: T[];
}

export default async function DashboardListingsPage() {
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs`, {
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch jobs: ${res.status}`);
  }

  const json: PagedResponse<JobListing> = await res.json();
  const jobs = json.data;

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
          All Listings
        </h1>
        <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
          {jobs.length} {jobs.length === 1 ? "listing" : "listings"}
        </p>
      </div>
      
      {/* If there are no listings, show a clear empty state. */}
      {jobs.length === 0 ? (
        <div className="rounded-xl border border-dashed border-zinc-300 p-12 text-center dark:border-zinc-700">
          <p className="text-zinc-500 dark:text-zinc-400">
            No listings yet.
          </p>
        </div>
      ) : (
        <div className="overflow-x-auto rounded-xl border border-zinc-200 dark:border-zinc-800">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-zinc-200 bg-zinc-50 text-xs uppercase tracking-wider text-zinc-500 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-400">
              <tr>
                <th className="px-4 py-3 font-semibold">Title</th>
                <th className="px-4 py-3 font-semibold">Company</th>
                <th className="px-4 py-3 font-semibold">Location</th>
                <th className="px-4 py-3 font-semibold">Status</th>
                <th className="px-4 py-3 font-semibold">View</th>
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
                  <td className="px-4 py-3">
                    <Link // the view link navigates to the job's detail page
                      href={`/jobs/${job.id}`}
                      className="font-medium text-lime-600 underline-offset-2 hover:underline dark:text-lime-400"
                    >
                      View
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
