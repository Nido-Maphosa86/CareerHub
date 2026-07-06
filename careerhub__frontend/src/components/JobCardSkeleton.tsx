// src/components/JobCardSkeleton.tsx
// Assignment 3.1 — Part 5: the skeleton PAIRED with JobLinkCard.
//
// "Paired" means this mirrors JobLinkCard's exact box model: the same
// rounded-xl border, the same p-5 padding, the same flex-col h-full layout, and
// placeholder bars sitting exactly where the eyebrow, title, location, and
// footer row sit in the real card. Because the outer shell is identical,
// swapping skeleton -> real card causes no layout shift. The previous version
// had a description block and a footer border that JobLinkCard does NOT have, so
// it caused a jump on swap; that drift is fixed here. If JobLinkCard's structure
// changes, this file must change with it — that is the pairing contract.


//paired with the real job card, 
// so that when the skeleton is replaced with the real card, there is no layout shift
function JobCardSkeleton() {
  return (
    <div className="flex h-full flex-col overflow-hidden rounded-xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950">
      {/* Company eyebrow (matches the lime uppercase line). */}
      <div className="mb-1 h-3 w-24 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />

      {/* Title (text-lg, ~3/4 width). */}
      <div className="mt-1 h-5 w-3/4 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />

      {/* Location row. */}
      <div className="mt-2 h-3.5 w-1/2 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />

      {/* Spacer — same flex-1 the real card uses to push the footer down. */}
      <div className="flex-1" />

      {/* Footer: badge left, status right. */}
      <div className="mt-4 flex items-center justify-between">
        <div className="h-5 w-20 animate-pulse rounded-full bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-3 w-12 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
    </div>
  );
}

// A grid of skeletons shown while the list loads. Defaults to six cards (the
// count is justified in the README — enough to read as "a list is loading"
// without overstating how many results will return).
export function JobListSkeleton({ count = 6 }: { count?: number }) {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: count }).map((_, i) => (
        <JobCardSkeleton key={i} />
      ))}
    </div>
  );
}

export { JobCardSkeleton };
