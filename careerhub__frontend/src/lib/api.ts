// src/lib/api.ts
// The single place that talks to the backend.
// Components never call fetch() directly — they call functions from here.

import { JobListing } from "@/types";

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

// Fetches the list of jobs from the API.
// Unwraps the paginated envelope so callers get a plain JobListing[].
// Throws if the response is not OK so TanStack Query catches the error.
export async function fetchJobs(): Promise<JobListing[]> {
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs`);

  if (!res.ok) {
    throw new Error(`Failed to fetch jobs: ${res.status}`);
  }

  const json: PagedResponse<JobListing> = await res.json();
  return json.data;
}
