
import { JobListing } from "../types";
import { JobCard } from "./JobCard";


interface JobListProps {
  jobs: JobListing[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}




export function JobList({ jobs, selectedId, onSelect }: JobListProps) {
 
  if (jobs.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-gray-400">
        <svg
          className="w-12 h-12 mb-4 opacity-40"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={1.5}
            d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"
          />
        </svg>
        <p className="text-lg font-medium text-gray-500">No job listings found</p>
        <p className="text-sm mt-1">
          New opportunities are posted regularly — check back soon.
        </p>
      </div>
    );
  }

  return (
    <div>
      {}
      <p className="text-sm text-gray-500 mb-4">
        Showing <span className="font-medium text-gray-700">{jobs.length}</span>{" "}
        {jobs.length === 1 ? "job" : "jobs"}
      </p>

      
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {jobs.map((job) => (
          <JobCard
            key={job.id}
            job={job}
            isSelected={job.id === selectedId}
            onSelect={onSelect}
          />
        ))}
      </div>
    </div>
  );
}