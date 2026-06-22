// src/lib/api.ts
// The single place that talks to the backend.
// Components never call fetch() directly — they call functions from here.

import { JobListing } from "@/types";


//CareerHub API wraps list responses in a pagination envelope.
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
// Throws if the response is not OK so TanStack Query catches the error.
export async function fetchJobs(): Promise<JobListing[]> {

  // Build the URL from the env var so we can switch between mock and real API.
  //Instead of hardcoding the URL, it uses an environment variable (NEXT_PUBLIC_API_URL).
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs`);//calls fetch() to get job data.
   


  //checks if the response was successful (status 200–299).
  //TanStack Query will then know something went wrong and show an error state in your UI
  // Throw on non-2xx so useQuery's isError branch fires.
  if (!res.ok) {
    throw new Error(`Failed to fetch jobs: ${res.status}`);
  }
  
  //If everything is fine, it converts the response into JSON and returns it.
  const json: PagedResponse<JobListing> = await res.json();
  return json.data
}
