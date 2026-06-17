
import { JobListing, EmploymentType } from "../types/indexx";


interface JobCardProps {
  job: JobListing;
  isSelected: boolean;
  onSelect: (id: string) => void;
}


const badgeStyles: Record<EmploymentType, string> = {
  FullTime:   "bg-blue-100 text-blue-700",
  PartTime:   "bg-purple-100 text-purple-700",
  Contract:   "bg-orange-100 text-orange-700",
  Internship: "bg-teal-100 text-teal-700",
  Freelance:  "bg-pink-100 text-pink-700",
};

const badgeLabels: Record<EmploymentType, string> = {
  FullTime:   "Full Time",
  PartTime:   "Part Time",
  Contract:   "Contract",
  Internship: "Internship",
  Freelance:  "Freelance",
};

function formatSalary(min: number, max: number): string {
  const fmt = (n: number) =>
    "R" + n.toLocaleString("en-ZA", { maximumFractionDigits: 0 });
  return `${fmt(min)} – ${fmt(max)} pm`;
}


function relativeDate(iso: string): string {
  const posted = new Date(iso);
  const now    = new Date();
  const diffMs = now.getTime() - posted.getTime();
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

export function JobCard({ job, isSelected, onSelect }: JobCardProps) {
  return (
    <div
      onClick={() => onSelect(job.id)}
      className={[
        "relative border rounded-xl p-5 bg-white shadow-sm cursor-pointer",
        "transition-all duration-150",
        isSelected
          ? "border-blue-500 ring-2 ring-blue-300 shadow-md"
          : "border-gray-200 hover:border-gray-300 hover:shadow",
      ].join(" ")}
    >

      {!job.isActive && (
        <span className="absolute top-3 right-3 text-xs font-semibold px-2 py-1 rounded-full bg-red-100 text-red-600">
          Closed
        </span>
      )}

      
      <h2 className={`text-base font-semibold text-gray-900 ${!job.isActive ? "pr-16" : ""}`}>
        {job.title}
      </h2>

      
      <p className="text-sm text-gray-500 mt-0.5">
        {job.company} · {job.location}
      </p>

      
      <span
        className={`inline-block mt-3 text-xs font-medium px-2.5 py-0.5 rounded-full ${badgeStyles[job.employmentType]}`}
      >
        {badgeLabels[job.employmentType]}
      </span>

      
      <p className="text-sm font-medium text-gray-700 mt-2">
        {formatSalary(job.salaryMin, job.salaryMax)}
      </p>

      
      <p className="text-xs text-gray-400 mt-1">
        Posted {relativeDate(job.postedAt)}
      </p>


      {job.applicantCount > 0 && (
        <p className="text-xs text-gray-500 mt-1">
          {job.applicantCount} {job.applicantCount === 1 ? "applicant" : "applicants"}
        </p>
      )}
    </div>
  );
}
//