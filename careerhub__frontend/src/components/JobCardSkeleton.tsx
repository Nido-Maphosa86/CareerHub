// src/components/JobCardSkeleton.tsx
// Loading placeholders shown while useQuery is pending.
// Shapes mirror the real JobCard so the layout doesn't jump when data arrives.

export function JobCardSkeleton() {
  return (
    <div className="w-full rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
      <div className="flex items-start justify-between gap-3">
        <div className="flex-1 space-y-2">
          <div className="h-4 w-1/2 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
          <div className="h-3 w-1/3 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
        </div>
        <div className="h-5 w-16 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
      </div>

      <div className="mt-3 space-y-2">
        <div className="h-3 w-full animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
        <div className="h-3 w-4/5 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
      </div>

      <div className="mt-3 flex justify-between">
        <div className="h-3 w-20 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
        <div className="h-3 w-24 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
      </div>
    </div>
  );
}

// Several skeletons stacked, used when the whole list is loading.
export function JobListSkeleton() {
  return (
    <div className="space-y-3">
      <JobCardSkeleton />
      <JobCardSkeleton />
      <JobCardSkeleton />
    </div>
  );
}
