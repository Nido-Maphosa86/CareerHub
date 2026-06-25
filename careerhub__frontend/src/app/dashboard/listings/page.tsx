// src/app/dashboard/listings/page.tsx
// Assignment 2.2 — Part 5: the streaming employer dashboard.
//
// The page no longer fetches or awaits any data. It renders the heading and
// static UI immediately, then hands the data work to two self-contained
// components, each behind its OWN <Suspense> boundary. Next.js streams the
// heading to the browser first, shows each skeleton while its component's data
// is pending, and swaps in each component independently the moment its own
// fetch resolves — the fast summary does not wait for the slower table.

import { Suspense } from "react";
import {
  ApplicationsSummary,
  ApplicationsSummarySkeleton,
} from "@/components/ApplicationsSummary";
import {
  ListingsTable,
  ListingsTableSkeleton,
} from "@/components/ListingsTable";

export default async function DashboardListingsPage() {
  return (
    <div>
      {/* Static heading — arrives before either component resolves. */}
      <div className="mb-6">
        <h1 className="text-2xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
          All Listings
        </h1>
        <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
          Manage your job listings and track applications.
        </p>
      </div>

      {/* Fast component — its own boundary, resolves first. */}
      <div className="mb-6">
        <Suspense fallback={<ApplicationsSummarySkeleton />}>
          <ApplicationsSummary />
        </Suspense>
      </div>

      {/* Slower component — separate boundary, resolves independently. */}
      <Suspense fallback={<ListingsTableSkeleton />}>
        <ListingsTable />
      </Suspense>
    </div>
  );
}
