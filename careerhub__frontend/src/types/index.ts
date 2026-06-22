// src/types/index.ts
// All shared TypeScript types live here.

// The kinds of employment a job listing can be.
// Used to colour-code the JobStatusBadge.
//restricts jobs to only these values
export type EmploymentType =
  | "fulltime"
  | "parttime"
  | "contract"
  | "internship"
  | "freelance"
  | "closed";

// A single job listing returned by the API (and rendered as a JobCard).
//defines what should each job listing look like and what properties it should have
export interface JobListing {
  id: string;
  title: string;
  company: string;
  location: string;
  employmentType: EmploymentType;
  salary: string;
  postedAt: string; // ISO date string, e.g. "2025-06-01"
  description: string;
}
