// src/app/dashboard/layout.tsx
// Assignment 2.1 — Part 5: the employer dashboard layout.
//
// A Server Component (no "use client"). It adds a two-column structure — a
// fixed-width sidebar plus a flexible content area — INSIDE the root layout's
// shell. It does not add its own full-page <main> padding; the root layout
// already provides that.
//
// Because this layout sits above every /dashboard/* route, it persists across
// navigations within the dashboard: moving between dashboard pages does not
// re-run this function or re-mount the sidebar (see README, item 4).

import Link from "next/link";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-6 md:flex-row">
      {/* Fixed-width sidebar. */}
      <aside className="w-full shrink-0 md:w-56">
        <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950">
          <h2 className="mb-3 px-2 text-xs font-bold uppercase tracking-widest text-lime-600 dark:text-lime-400">
            Employer Dashboard
          </h2>
          <nav className="flex flex-col gap-1">
            <Link
              href="/dashboard/listings"
              className="rounded-lg px-2 py-1.5 text-sm font-medium text-zinc-700 transition-colors hover:bg-lime-50 hover:text-lime-700 dark:text-zinc-300 dark:hover:bg-lime-400/10 dark:hover:text-lime-300"
            >
              All Listings
            </Link>
            <Link
              href="/jobs"
              className="rounded-lg px-2 py-1.5 text-sm font-medium text-zinc-700 transition-colors hover:bg-lime-50 hover:text-lime-700 dark:text-zinc-300 dark:hover:bg-lime-400/10 dark:hover:text-lime-300"
            >
              View as Candidate
            </Link>
          </nav>
        </div>
      </aside>

      {/* Flexible content area. */}
      <div className="min-w-0 flex-1">{children}</div>
    </div>
  );
}
