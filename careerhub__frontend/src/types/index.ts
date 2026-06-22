// src/types/index.ts
// All shared TypeScript types live here.

// The kinds of employment a job listing can be.
// Used to colour-code the JobStatusBadge.
//restricts jobs to only these values
// Values match the backend's JobType enum (serialized as PascalCase strings).
export type EmploymentType =
  | "FullTime"
  | "PartTime"
  | "Contract"
  | "Internship"
  | "Freelance"
  | "Temporary";

// A single job listing returned by the API (and rendered as a JobCard).
//defines what should each job listing look like and what properties it should have
// One job as returned by GET /api/v1/Jobs (inside the paginated wrapper).
export interface JobListing {
  id: string; // GUID
  title: string;
  description: string;
  companyName: string;
  location: string;
  type: EmploymentType;
  salaryMin: number;
  salaryMax: number;
  salaryDisplay: string; // pre-formatted by the backend
  postedAt: string; // ISO timestamp
  isActive: boolean;
  applicationCount: number;
  closingDate: string; // ISO timestamp
  status: string; // e.g. "Active", "Closed"
}