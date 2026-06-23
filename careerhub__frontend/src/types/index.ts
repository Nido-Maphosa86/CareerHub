// src/types/index.ts
// Matches the CareerHub .NET API.

export type EmploymentType =
  | "FullTime"
  | "PartTime"
  | "Contract"
  | "Internship"
  | "Freelance"
  | "Temporary";

// One job as returned by GET /api/v1/Jobs (inside the paginated wrapper).
export interface JobListing {
  id: string;
  title: string;
  description: string;
  companyName: string;
  location: string;
  type: EmploymentType;
  salaryMin: number;
  salaryMax: number;
  salaryDisplay: string;
  postedAt: string;
  isActive: boolean;
  applicationCount: number;
  closingDate: string;
  status: string;
}

// ---- Applications -----------------------------------------------------
// The request body for POST /api/v1/applications/{listingId}.
// The job id is in the URL and the applicant comes from the JWT, so neither
// appears here. Field names/casing match the backend ApplyRequest exactly.
export interface ApplicationRequest {
  fullName: string;
  email: string;
  phone?: string;
  yearsOfExperience: number;
  coverLetter: string;
  linkedInUrl?: string;
  availableImmediately: boolean;
  noticePeriodWeeks: number;
}

// What the backend returns on a successful apply (201).
export interface ApplicationResponse {
  jobListingId: string;
  jobTitle: string;
  companyName: string;
  applicantId: string;
  applicantName: string;
  submittedAt: string;
  status: string;
}

// ---- Auth -------------------------------------------------------------
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
}

// Decoded from the JWT for display + role-gating in the UI.
export interface AuthUser {
  username: string;
  role: string;
}
