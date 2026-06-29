// src/app/dashboard/listings/page.tsx
// Assignment 2.2 — Part 5: streaming dashboard with two Suspense boundaries.
// Assignment 2.3 — Part 7: adds the store-driven view toggle + closed-jobs filter.
//
// The store lives in the browser and an async Server Component cannot read it.
// So the page pre-renders all four ListingsTable variants on the server (table
// and grid × showing or hiding closed jobs) and passes them as props to the
// Client wrapper <DashboardView>, which reads the Zustand store and shows the
// matching variant. The four ListingsTable instances pass view/showClosedJobs
// as PROPS; their identical fetches are de-duplicated by Next.js within one
// render, so this is not four times the network cost.

import { Suspense } from "react";
import {
  ApplicationsSummary,
  ApplicationsSummarySkeleton,
} from "@/components/ApplicationsSummary";
import {
  ListingsTable,
  ListingsTableSkeleton,
} from "@/components/ListingsTable";
import { DashboardToolbar } from "@/components/DashboardToolbar";
import { DashboardView } from "@/components/DashboardView";

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

      {/* Store-driven toolbar (client). */}
      <DashboardToolbar />

      {/* Slower component — its own boundary. The client wrapper picks which
          pre-rendered server variant to show based on the store. */}
      <Suspense fallback={<ListingsTableSkeleton />}>
        <DashboardView
          tableViewAll={<ListingsTable view="table" showClosedJobs={true} />}
          tableView={<ListingsTable view="table" showClosedJobs={false} />}
          gridViewAll={<ListingsTable view="grid" showClosedJobs={true} />}
          gridView={<ListingsTable view="grid" showClosedJobs={false} />}
        />
      </Suspense>
    </div>
  );
}
