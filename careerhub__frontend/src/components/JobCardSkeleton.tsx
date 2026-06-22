// src/components/JobCardSkeleton.tsx
// Loading placeholders shown while useQuery is pending.
// Mirror the JobCard layout so the grid doesn't jump when data arrives.

function JobCardSkeleton() {
  return (
    <div className="flex h-full flex-col rounded-xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950">
      {/* eyebrow */}
      <div className="mb-2 h-3 w-20 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      {/* title */}
      <div className="h-5 w-3/4 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      {/* location */}
      <div className="mt-3 h-3 w-1/2 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      {/* description */}
      <div className="mt-4 space-y-2">
        <div className="h-3 w-full animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-3 w-4/5 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
      <div className="flex-1" />
      {/* badge row */}
      <div className="mt-4 flex justify-between">
        <div className="h-5 w-20 animate-pulse rounded-full bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-4 w-8 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
      {/* footer */}
      <div className="mt-3 border-t border-zinc-100 pt-3 dark:border-zinc-800">
        <div className="flex justify-between">
          <div className="h-4 w-28 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 w-16 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      </div>
    </div>
  );
}

// A grid of skeletons, used while the whole list is loading.
export function JobListSkeleton() {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <JobCardSkeleton />
      <JobCardSkeleton />
      <JobCardSkeleton />
      <JobCardSkeleton />
      <JobCardSkeleton />
      <JobCardSkeleton />
    </div>
  );
}

export { JobCardSkeleton };
