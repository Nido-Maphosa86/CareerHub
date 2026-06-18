// src/components/JobCard.tsx
//
// Assignment 1.2 changes:
// - All template literals replaced with cn()
// - Dark mode variants added to every colour class
// - JobStatusBadge replaces inline badge and expired label
// - Expired card (isActive: false) shown by fading the whole card (opacity)


// Import cn helper → fixes Tailwind class conflicts
import { cn } from "../lib/utils";
// Import JobListing type → defines job shape
import { JobListing } from "../types/indexx";
// Import JobStatusBadge → shows badge for employment type and active/closed state
import { JobStatusBadge } from "./JobStatusBadge";

// ── Props ──
// JobCard receives a job, whether it’s selected, and a function to call when clicked
interface JobCardProps {
  job: JobListing;
  isSelected: boolean;
  onSelect: (id: string) => void;
}

// ── Helpers ──
// Format salary range into readable string (e.g. R55,000 – R75,000 pm)
function formatSalary(min: number, max: number): string {
  const fmt = (n: number) =>
    "R" + n.toLocaleString("en-ZA", { maximumFractionDigits: 0 });
  return `${fmt(min)} – ${fmt(max)} pm`;
}

// Convert ISO date into relative text (today, yesterday, X days ago, months, years)
function relativeDate(iso: string): string {
  const posted   = new Date(iso);
  const now      = new Date();
  const diffMs   = now.getTime() - posted.getTime();
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDays === 0) return "today";
  if (diffDays === 1) return "yesterday";
  if (diffDays < 30)  return `${diffDays} days ago`;

  const diffMonths = Math.floor(diffDays / 30);
  if (diffMonths === 1) return "1 month ago";
  if (diffMonths < 12)  return `${diffMonths} months ago`;

  const diffYears = Math.floor(diffMonths / 12);
  return diffYears === 1 ? "1 year ago" : `${diffYears} years ago`;
}

// ── Component ──
export function JobCard({ job, isSelected, onSelect }: JobCardProps) {
  return (
    // Outer card container
    // onClick → selects/deselects job
    // className uses cn() instead of template literals
    // Dark mode variants added for background, border, ring, shadow
    // If job is expired (isActive=false), card is faded with opacity-60
    <div
      onClick={() => onSelect(job.id)}
      className={cn(
        "relative border rounded-xl p-5 cursor-pointer",
        "transition-all duration-150",
        "bg-white shadow-sm",
        "dark:bg-gray-800 dark:shadow-none",
        isSelected && "border-blue-500 ring-2 ring-blue-300 shadow-md",
        isSelected && "dark:border-blue-400 dark:ring-blue-700",
        !isSelected && "border-gray-200 hover:border-gray-300 hover:shadow",
        !isSelected && "dark:border-gray-700 dark:hover:border-gray-500",
        !job.isActive && "opacity-60",
      )}
    >
      {/* Badge section → replaced inline badge with JobStatusBadge */}
      <div className="mb-3">
        <JobStatusBadge
          employmentType={job.employmentType}
          isActive={job.isActive}
        />
      </div>

      {/* Job title → cn() used, dark mode variant added */}
      <h2 className={cn("text-base font-semibold", "text-gray-900 dark:text-gray-50")}>
        {job.title}
      </h2>

      {/* Company + location → cn() used, dark mode variant added */}
      <p className={cn("text-sm mt-0.5", "text-gray-500 dark:text-gray-400")}>
        {job.company} · {job.location}
      </p>

      {/* Salary → cn() used, dark mode variant added */}
      <p className={cn("text-sm font-medium mt-2", "text-gray-700 dark:text-gray-300")}>
        {formatSalary(job.salaryMin, job.salaryMax)}
      </p>

      {/* Posted date → cn() used, dark mode variant added */}
      <p className={cn("text-xs mt-1", "text-gray-400 dark:text-gray-500")}>
        Posted {relativeDate(job.postedAt)}
      </p>

      {/* Applicant count → only shows if > 0, cn() used, dark mode variant added */}
      {job.applicantCount > 0 && (
        <p className={cn("text-xs mt-1", "text-gray-500 dark:text-gray-400")}>
          {job.applicantCount}{" "}
          {job.applicantCount === 1 ? "applicant" : "applicants"}
        </p>
      )}
    </div>
  );
}
