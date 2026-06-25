// src/app/api/applications/stats/route.ts
// Assignment 2.2 — Part 2: application statistics endpoint.
//
// Returns application counts grouped by job, shaped as
//   { jobId: string; applicationCount: number }[]
//
// ADAPTATION (real backend): our live jobs come from the real CareerHub.Api,
// which already returns an `applicationCount` per job. So instead of hardcoding
// counts against a static mock array (whose ids would never match the real
// jobs), this endpoint derives the stats from the real /Jobs response. That way
// every jobId here matches a real job id and the dashboard join shows true
// counts. The endpoint still behaves exactly as the assignment requires:
// GET-only, returns an array, empty array (not 404) when there are no jobs.


//route handler that return how many applications have been submitted for each job. The response is an array of objects, each containing a jobId and the corresponding applicationCount. If there are no jobs or if the backend is unavailable, it returns an empty array.
import { NextResponse } from "next/server";

// Shape of the paginated response returned by the real backend.
interface PagedResponse<T> {
  data: T[];
}

// Shape of a job object from the real backend, including applicationCount.
interface RealJob {
  id: string;
  applicationCount: number;
}

// GET /api/applications/stats — one { jobId, applicationCount } per job.
export async function GET() {
  try {
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs`, {
      cache: "no-store", // Always fetch fresh data for application stats.
    });

    // If the jobs source is unavailable, return an empty array instead of error.
    if (!res.ok) {
      return NextResponse.json([], { status: 200 });
    }

    const json: PagedResponse<RealJob> = await res.json();

    // Map each job to the required shape: { jobId, applicationCount }.
    const stats = json.data.map((job) => ({
      jobId: job.id,
      applicationCount: job.applicationCount ?? 0, // Default to 0 if missing.
    }));

    return NextResponse.json(stats, { status: 200 });
  } catch {
    // Network failure (e.g. backend down) — still return a valid empty array.
    return NextResponse.json([], { status: 200 });
  }
}

//this ednpoint only supports GET requests. If a POST request is made, it returns a 405 Method Not Allowed response with an appropriate message and the Allow header set to "GET".
// Any non-GET method is not allowed.
export async function POST() {
  return NextResponse.json(
    {
      title: "Method Not Allowed",
      detail: "This endpoint only supports GET.",
      status: 405,
    },
    { status: 405, headers: { Allow: "GET" } }
  );
}
