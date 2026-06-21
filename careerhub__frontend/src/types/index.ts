// src/types/index.ts
//
// These interfaces mirror the exact shapes returned by the CareerHub API.
// They are not placeholders — they are the contract.
//
// When the backend changes a field name (e.g. salaryMin → minimumSalary),
// the TypeScript compiler will flag every component that reads salaryMin
// as an error. That compile error is the correct failure mode — not a
// silent runtime bug where the value is undefined and nothing shows up.

// Mirrors CareerHub.Api/Models/JobType.cs
// The API serialises this enum as strings (JsonStringEnumConverter),
// not integers — "FullTime" not 0.
export type EmploymentType =
  | "FullTime"
  | "PartTime"
  | "Contract"
  | "Internship"
  | "Freelance";

// Mirrors CareerHub.Api/Models/JobListingStatus.cs
export type JobListingStatus = "Active" | "Closed";

// Mirrors the shape of a single item in the data array returned by
// GET /api/v1/jobs → PagedResponse<JobResponse>
// Field names match the camelCase JSON produced by the API.
export interface JobListing {
  // C# Guid serialised as a lowercase hyphenated string: "a1b2c3d4-..."
  id: string;

  title: string;
  company: string;       // mapped from companyName in the API response
  location: string;

  // Union type — not string. "FullTime" | "PartTime" | "Contract" | "Internship" | "Freelance"
  // If the API adds a new value (e.g. "Freelance") and this union is not updated,
  // TypeScript will flag every place that handles these values as exhaustively
  // as a compile error — not a silent runtime mismatch.
  employmentType: EmploymentType;

  salaryMin: number;
  salaryMax: number;

  // C# DateTime serialised as ISO 8601: "2026-06-01T08:00:00Z"
  postedAt: string;

  isActive: boolean;
  applicantCount: number;
}
