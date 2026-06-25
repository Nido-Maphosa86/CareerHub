// src/app/jobs/loading.tsx
// Assignment 2.1 — Part 3: route-level loading UI.
//
// Next.js automatically wraps the route's page in a <Suspense> boundary and
// shows this file's output while the Server Component's async work (the fetch)
// is pending. It is not a component you call — the App Router renders it for
// you (see README, "loading.tsx vs a manual loading state").
//
// The skeleton mirrors the real grid: placeholder cards in the same layout,
// not a generic spinner.

function SkeletonCard() {
  return (
    <div className="flex h-full flex-col rounded-xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mb-2 h-3 w-20 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      <div className="h-5 w-3/4 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      <div className="mt-3 h-3 w-1/2 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      <div className="flex-1" />
      <div className="mt-6 flex items-center justify-between">
        <div className="h-5 w-20 animate-pulse rounded-full bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-3 w-12 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
    </div>
  );
}

export default function Loading() {
  return (
    <div>
      <div className="mb-8">
        <div className="h-9 w-56 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="mt-2 h-4 w-72 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>

      {/* Six placeholder cards — matches the mock/sample data size. */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <SkeletonCard key={i} />
        ))}
      </div>
    </div>
  );
}
