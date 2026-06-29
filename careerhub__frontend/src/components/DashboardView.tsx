// src/components/DashboardView.tsx
// Assignment 2.3 — Part 7: the bridge between the Zustand store (client-only)
// and the async ListingsTable (server-only).
//
// The problem: ListingsTable is an async Server Component, so it cannot read the
// store. The store lives in the browser. We need the store's `view` and
// `showClosedJobs` to drive what the server component rendered.
//
// The pattern: the Server page pre-renders BOTH variants of ListingsTable (one
// table, one grid; each already filtered for closed jobs both ways is overkill,
// so we render the two views and let this Client wrapper pick which to show, and
// it also applies the showClosedJobs toggle by hiding closed rows is handled
// server-side). This wrapper is a Client Component: it reads the store with
// selectors and shows the matching pre-rendered server output. Because both
// variants were produced on the server from the same fetch, switching is instant
// and needs no refetch.

"use client";

import { ReactNode } from "react";
import { useDashboardStore } from "@/stores/dashboardStore";

interface Props {
  tableView: ReactNode;
  gridView: ReactNode;
  tableViewAll: ReactNode;
  gridViewAll: ReactNode;
}

export function DashboardView({
  tableView,
  gridView,
  tableViewAll,
  gridViewAll,
}: Props) {
  // One selector per value.
  const view = useDashboardStore((s) => s.view);
  const showClosedJobs = useDashboardStore((s) => s.showClosedJobs);

  if (view === "grid") {
    return <>{showClosedJobs ? gridViewAll : gridView}</>;
  }
  return <>{showClosedJobs ? tableViewAll : tableView}</>;
}
