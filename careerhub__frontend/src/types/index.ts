// src/types/index.ts
// Matches the CareerHub .NET API.

export type EmploymentType =
  | "FullTime"
  | "PartTime"
  | "Contract"
  | "Internship"
  | "Freelance"
  | "Temporary";
// EmploymentType is a union type — it can only be one of these exact strings.
// This matches how the backend labels job types.
// Example: a job listing might have type = "FullTime".


// One job as returned by GET /api/v1/Jobs (inside the paginated wrapper).
export interface JobListing {
  id: string;              // Unique identifier for the job (like a primary key).
  title: string;           // Job title (e.g., "Software Engineer").
  description: string;     // Full job description text.
  companyName: string;     // Name of the company offering the job.
  location: string;        // Where the job is based (city, country, remote).
  type: EmploymentType;    // The type of job (must be one of the EmploymentType values above).
  salaryMin: number;       // Minimum salary offered.
  salaryMax: number;       // Maximum salary offered.
  salaryDisplay: string;   // Human-readable salary string (e.g., "R50,000 - R70,000").
  postedAt: string;        // Date the job was posted (ISO string).
  isActive: boolean;       // Whether the job is still open for applications.
  applicationCount: number;// How many people have applied so far.
  closingDate: string;     // Deadline for applications.
  status: string;          // Current status (e.g., "Active", "Closed").
}
// This interface describes exactly what one job listing looks like when fetched from the API.
// It ensures the frontend knows the shape of job data.





// ---- Applications -----------------------------------------------------
// The request body for POST /api/v1/applications/{listingId}.
// The job id is in the URL and the applicant comes from the JWT, so neither
// appears here. Field names/casing match the backend ApplyRequest exactly.
export interface ApplicationRequest {
  fullName: string;            // Candidate's full name.
  email: string;               // Candidate's email address.
  phone?: string;              // Optional phone number (can be left out).
  yearsOfExperience: number;   // Candidate's years of work experience.
  coverLetter: string;         // Candidate's cover letter text.
  linkedInUrl?: string;        // Optional LinkedIn profile link.
  availableImmediately: boolean; // True if candidate can start right away.
  noticePeriodWeeks: number;   // If not available immediately, how many weeks notice is needed.
}
// This matches exactly what the frontend form will send when applying for a job.
// It links directly to Assignment 1.4 Part 5 (ApplicationForm schema).


// What the backend returns on a successful apply (201).
export interface ApplicationResponse {
  jobListingId: string;   // The job that was applied to.
  jobTitle: string;       // Title of the job applied for.
  companyName: string;    // Company offering the job.
  applicantId: string;    // Unique ID of the applicant (from backend).
  applicantName: string;  // Name of the applicant (from backend).
  submittedAt: string;    // When the application was submitted (ISO date string).
  status: string;         // Current status of the application (e.g., "Submitted").
}
// This is the shape of the data the backend sends back after a successful application.
// It matches Assignment 1.4 Part 2 (mock backend response).



// ---- Auth -------------------------------------------------------------
export interface LoginRequest {
  username: string;   // The username entered by the user.
  password: string;   // The password entered by the user.
}
// This is what the frontend sends when logging in.


export interface LoginResponse {
  token: string;      // The JWT token returned by the backend after login.
}
// This token is used to prove the user is logged in and to access protected endpoints.


// Decoded from the JWT for display + role-gating in the UI.
export interface AuthUser {
  username: string;   // Username decoded from the token.
  role: string;       // Role (e.g., "Admin", "Candidate") used for permissions in the UI.
}
// This lets the frontend know who is logged in and what they are allowed to do.

