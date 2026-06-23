// src/lib/api.ts
// The single place that talks to the backend.
// Components never call fetch() directly — they call functions from here.

import {
  JobListing,
  ApplicationRequest,
  ApplicationResponse,
  LoginRequest,
  LoginResponse,
} from "@/types";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL;

// The CareerHub API wraps list responses in a pagination envelope.
interface PagedResponse<T> {
  data: T[];              // The actual list of items (jobs).
  page: number;           // Current page number.
  pageSize: number;       // How many items per page.
  totalCount: number;     // Total number of items across all pages.
  totalPages: number;     // Total number of pages.
  hasNextPage: boolean;   // True if there is another page after this one.
  hasPreviousPage: boolean; // True if there is a page before this one.
}
//  This matches how the backend sends job listings: wrapped in pagination info.
// fetchJobs() will unwrap this and return just the job data.



// Problem Details shape returned by the API on errors (RFC 7807).
interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}


// Pull a readable message out of an error response body.
async function problemMessage(res: Response, fallback: string): Promise<string> {
  const problem: ProblemDetails = await res.json().catch(() => ({}));
  return problem.detail ?? problem.title ?? fallback;
}
// This helper function extracts a human-friendly error message from the response.
// If the backend sends { detail: "Email is required" }, we show that.
// If not, we fall back to a generic message (like "Login failed: 401").


// GET /api/v1/Jobs — unwraps the paginated envelope.
export async function fetchJobs(): Promise<JobListing[]> {
  const res = await fetch(`${BASE_URL}/Jobs`);

  if (!res.ok) {
    throw new Error(`Failed to fetch jobs: ${res.status}`);
  }
  // If the response is not 200–299, throw an error.
  // TanStack Query will catch this and show an error state in the UI.

  const json: PagedResponse<JobListing> = await res.json();
  return json.data;
  //unwrap the pagination, and return just the job list.
}

// POST /api/v1/auth/login — returns a JWT for the seeded demo users.
export async function login(
  credentials: LoginRequest
): Promise<LoginResponse> {
  const res = await fetch(`${BASE_URL}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(credentials),
  });
  // Send username + password to the backend.
  // Backend responds with a JWT token if login succeeds.

  if (!res.ok) {
    if (res.status === 401) {
      throw new Error("Incorrect username or password.");
    }
    throw new Error(await problemMessage(res, `Login failed: ${res.status}`));
  }
  //  If login fails:
  // - 401 → show "Incorrect username or password."
  // - Other errors → show the problem details or fallback message.

  return res.json();
  //  If login succeeds, return the JWT token.
}


// POST /api/v1/applications/{listingId} — requires an Applicant Bearer token.
// The job id is the URL segment; the form fields are the body.
export async function submitApplication(
  listingId: string,
  application: ApplicationRequest,
  token: string
): Promise<ApplicationResponse> {
  const res = await fetch(`${BASE_URL}/applications/${listingId}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(application),
  });
  // Send the application form data to the backend.
  // Include the JWT token in the Authorization header (proves the user is logged in).
  // The job ID goes in the URL, the applicant details go in the body.

  if (!res.ok) {
    // Friendlier messages for the common auth/role/duplicate cases.
    if (res.status === 401) {
      throw new Error("Your session has expired. Please log in again.");
    }
    if (res.status === 403) {
      throw new Error("Only applicant accounts can apply for jobs.");
    }
    if (res.status === 429) {
      throw new Error("Too many applications. Please try again later.");
    }
    // 409 (already applied / listing closed) and others carry a useful detail.
    throw new Error(
      await problemMessage(res, `Application failed: ${res.status}`)
    );
  }
  // 👉 Handle different error cases:
  // - 401 → session expired
  // - 403 → wrong role (e.g., recruiter trying to apply)
  // - 429 → too many requests
  // - 409 → already applied or job closed (backend sends detail message)

  return res.json();
  // 👉 If successful, return the ApplicationResponse (id, jobId, email, submittedAt, etc.).
}
