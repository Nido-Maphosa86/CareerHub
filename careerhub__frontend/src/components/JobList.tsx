// src/components/JobList.tsx
//
// Assignment 1.2 changes:
// - Dark mode variants added to empty state and result count


// Import cn helper → fixes Tailwind class conflicts
import { cn } from "../lib/utils";
// Import JobListing type → defines job shape
import { JobListing } from "../types/indexx";
// Import JobCard component → shows individual job cards
import { JobCard } from "./JobCards";

// Props for JobList → jobs array, selected job ID, and onSelect function
interface JobListProps {
  jobs: JobListing[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}

// JobList component
export function JobList({ jobs, selectedId, onSelect }: JobListProps) {
  // If no jobs exist, show empty state message
  // Early return → avoids rendering grid when jobs.length === 0
  if (jobs.length === 0) {
    return (
      // Empty state container → centered vertically and horizontally
      <div className="flex flex-col items-center justify-center py-24">
        {/* Empty state icon → cn() used, dark mode variant added */}
        <svg
          className={cn("w-12 h-12 mb-4 opacity-40", "text-gray-400 dark:text-gray-500")}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          {/* Path for folder icon */}
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={1.5}
            d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"
          />
        </svg>
        {/* Empty state text → cn() used, dark mode variant added */}
        <p className={cn("text-lg font-medium", "text-gray-500 dark:text-gray-400")}>
          No job listings found
        </p>
        {/* Subtext → cn() used, dark mode variant added */}
        <p className={cn("text-sm mt-1", "text-gray-400 dark:text-gray-500")}>
          New opportunities are posted regularly — check back soon.
        </p>
      </div>
    );
  }

  // If jobs exist, show result count and grid of JobCards
  return (
    <div>
      {/* Result count → cn() used, dark mode variant added */}
      <p className={cn("text-sm mb-4", "text-gray-500 dark:text-gray-400")}>
        Showing{" "}
        {/* Highlighted number → cn() used, dark mode variant added */}
        <span className={cn("font-medium", "text-gray-700 dark:text-gray-200")}>
          {jobs.length}
        </span>{" "}
        {jobs.length === 1 ? "job" : "jobs"}
      </p>

      {/* Responsive grid → 1 column on mobile, 2 on tablet, 3 on desktop */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {/* Loop through jobs and render JobCard for each */}
        {jobs.map((job) => (
          <JobCard
            key={job.id} // unique key for React
            job={job} // job data
            isSelected={job.id === selectedId} // check if job is selected
            onSelect={onSelect} // function to handle selection
          />
        ))}
      </div>
    </div>
  );
}
