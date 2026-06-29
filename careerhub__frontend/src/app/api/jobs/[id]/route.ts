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
  const { id } = await params; // Next 15: params is a Promise — await it.

  const job = mockJobs.find((j) => j.id === id);

  if (!job) {
    return NextResponse.json(
      { title: "Job not found", detail: `No job exists with id '${id}'.`, status: 404 },
      { status: 404 }
    );
  }

  return NextResponse.json(job, { status: 200 });
}

// PATCH /api/jobs/{id} — updates the job's status in the mock array.
// Body: { "status": "Closed" }.
export async function PATCH(
  request: Request,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params;

  // Find the job first — 404 if it does not exist.
  const job = mockJobs.find((j) => j.id === id);
  if (!job) {
    return NextResponse.json(
      { title: "Job not found", detail: `No job exists with id '${id}'.`, status: 404 },
      { status: 404 }
    );
  }

  // Parse the body. A missing/!invalid body or a missing status field -> 400.
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

  // Mutate in place — persists for the life of the server process.
  job.status = body.status;

  return NextResponse.json(job, { status: 200 });
}

// Any other method is not allowed on this route.
export async function POST() {
  return NextResponse.json(
    { title: "Method Not Allowed", detail: "This endpoint supports GET and PATCH.", status: 405 },
    { status: 405, headers: { Allow: "GET, PATCH" } }
  );
}
