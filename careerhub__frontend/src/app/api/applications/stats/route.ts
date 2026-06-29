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

import { NextResponse } from "next/server";

interface PagedResponse<T> {
  data: T[];
}

interface RealJob {
  id: string;
  applicationCount: number;
}

// GET /api/applications/stats — one { jobId, applicationCount } per job.
export async function GET() {
  try {
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs`, {
      cache: "no-store",
    });

    // If the jobs source is unavailable, return an empty array rather than error.
    if (!res.ok) {
      return NextResponse.json([], { status: 200 });
    }

    const json: PagedResponse<RealJob> = await res.json();

    const stats = json.data.map((job) => ({
      jobId: job.id,
      applicationCount: job.applicationCount ?? 0,
    }));

    return NextResponse.json(stats, { status: 200 });
  } catch {
    // Network failure (e.g. backend down) — still return a valid array.
    return NextResponse.json([], { status: 200 });
  }
}

// Any non-GET method is not allowed.
export async function POST() {
  return NextResponse.json(
    { title: "Method Not Allowed", detail: "This endpoint only supports GET.", status: 405 },
    { status: 405, headers: { Allow: "GET" } }
  );
}
