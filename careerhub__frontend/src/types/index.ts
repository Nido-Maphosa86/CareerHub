// src/types/index.ts
// Matches the CareerHub .NET API response shape.

// Values match the backend's JobType enum (serialized as PascalCase strings).
export type EmploymentType =
  | "FullTime"
  | "PartTime"
  | "Contract"
  | "Internship"
  | "Freelance"
  | "Temporary";

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
  salaryDisplay: string; // pre-formatted by the backend, e.g. "R30,000 – R40,000/month"
  postedAt: string; // ISO timestamp
  isActive: boolean;
  applicationCount: number;
  closingDate: string; // ISO timestamp
  status: string; // e.g. "Active", "Closed"
}
