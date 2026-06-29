// src/components/DashboardToolbar.tsx
// Assignment 2.3 — Part 7: the dashboard toolbar (a Client Component).
//
// It reads from the Zustand store using one selector per value (not object
// destructuring) so the component only re-renders when the specific value it
// uses changes. It renders the Table/Grid view toggle and the "Show closed
// jobs" checkbox.

"use client";

import { useDashboardStore } from "@/stores/dashboardStore";
import { LayoutGrid, Table2 } from "lucide-react";

export function DashboardToolbar() {
  // One useStore call per value — selectors, not destructuring.
  const view = useDashboardStore((s) => s.view);
  const setView = useDashboardStore((s) => s.setView);
  const showClosedJobs = useDashboardStore((s) => s.showClosedJobs);
  const toggleShowClosedJobs = useDashboardStore((s) => s.toggleShowClosedJobs);

  return (
    <div className="mb-4 flex items-center justify-between gap-4">
      {/* View toggle. */}
      <div className="flex rounded-lg border border-zinc-300 p-0.5 dark:border-zinc-700">
        <button
          type="button"
          onClick={() => setView("table")}
          className={`flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-semibold transition-colors ${
            view === "table"
              ? "bg-lime-400 text-black"
              : "text-zinc-500 hover:text-zinc-800 dark:text-zinc-400 dark:hover:text-zinc-100"
          }`}
        >
          <Table2 className="h-4 w-4" />
          Table
        </button>
        <button
          type="button"
          onClick={() => setView("grid")}
          className={`flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-semibold transition-colors ${
            view === "grid"
              ? "bg-lime-400 text-black"
              : "text-zinc-500 hover:text-zinc-800 dark:text-zinc-400 dark:hover:text-zinc-100"
          }`}
        >
          <LayoutGrid className="h-4 w-4" />
          Grid
        </button>
      </div>

      {/* Show closed jobs checkbox. */}
      <label className="flex cursor-pointer items-center gap-2 text-sm text-zinc-600 dark:text-zinc-300">
        <input
          type="checkbox"
          checked={showClosedJobs}
          onChange={toggleShowClosedJobs}
          className="h-4 w-4 rounded border-zinc-300 text-lime-500 focus:ring-lime-500 dark:border-zinc-600"
        />
        Show closed jobs
      </label>
    </div>
  );
}
