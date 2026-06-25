// src/app/api/jobs/[id]/route.ts
// Assignment 2.1 — Part 2: single-job Route Handler (GET).
// Assignment 2.2 — Part 2: adds a PATCH handler to update job status.
//
// NOTE: The live app pages fetch from the real CareerHub.Api. This handler is
// the assignments' standalone artifact — it proves the GET / PATCH / 404 / 405
// contract on the frontend origin. It reads/writes the shared mockJobs array so
// a PATCH persists for the life of the server process (Part 2 requirement).

import { NextResponse } from "next/server";
import { mockJobs } from "@/lib/mockJobs";

// GET /api/jobs/{id} — returns the job, or 404 Problem Details if unknown.
export async function GET(
  _request: Request,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params; // In Next.js 15, params is a Promise — must await it.

  const job = mockJobs.find((j) => j.id === id); // Look for a job with matching id.

  if (!job) {
    // If no job found, return a 404 error in Problem Details format.
    return NextResponse.json(
      { title: "Job not found", detail: `No job exists with id '${id}'.`, status: 404 },
      { status: 404 }
    );
  }

  // If job exists, return it with status 200.
  return NextResponse.json(job, { status: 200 });
}

// PATCH /api/jobs/{id} — updates the job's status in the mock array.
// Body: { "status": "Closed" }.
export async function PATCH(
  request: Request,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params;

  // Find the job first — return 404 if it does not exist.
  const job = mockJobs.find((j) => j.id === id);
  if (!job) {
    return NextResponse.json(
      { title: "Job not found", detail: `No job exists with id '${id}'.`, status: 404 },
      { status: 404 }
    );
  }

  // Parse the request body. If parsing fails or status is missing, return 400.
  let body: { status?: string };
  try {
    body = await request.json();
  } catch {
    body = {};
  }

  if (!body.status) {
    return NextResponse.json(
      { title: "Bad request", detail: "A 'status' field is required in the body.", status: 400 },
      { status: 400 }
    );
  }

  // Update the job in place. Because mockJobs is mutable, this persists until server restart.
  job.status = body.status;

  // Return the updated job with status 200.
  return NextResponse.json(job, { status: 200 });
}

// Any other method is not allowed on this route.
export async function POST() {
  return NextResponse.json(
    { title: "Method Not Allowed", detail: "This endpoint supports GET and PATCH.", status: 405 },
    { status: 405, headers: { Allow: "GET, PATCH" } }
  );
}
