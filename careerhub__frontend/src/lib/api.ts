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
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

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

// GET /api/v1/Jobs — unwraps the paginated envelope.
export async function fetchJobs(): Promise<JobListing[]> {
  const res = await fetch(`${BASE_URL}/Jobs`);

  if (!res.ok) {
    throw new Error(`Failed to fetch jobs: ${res.status}`);
  }

  const json: PagedResponse<JobListing> = await res.json();
  return json.data;
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

  if (!res.ok) {
    if (res.status === 401) {
      throw new Error("Incorrect username or password.");
    }
    throw new Error(await problemMessage(res, `Login failed: ${res.status}`));
  }

  return res.json();
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

  return res.json();
}
