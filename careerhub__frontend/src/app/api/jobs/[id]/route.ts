// src/app/api/jobs/[id]/route.ts
// Assignment 2.1 — Part 2: single-job Route Handler (mock).
//
// NOTE: The live app pages fetch a single job from the real CareerHub.Api
// (GET /api/v1/Jobs/{id}), which already returns 404 Problem Details when a
// job is missing and 405 on a non-GET. This handler is the assignment's
// required standalone artifact: it proves the Route-Handler params pattern and
// the 200 / 404 / 405 contract on the frontend origin itself.

import { NextResponse } from "next/server";

// A small mock job table. In Next.js 15 a Route Handler's params is async,
// so the GET signature awaits it (same pattern as a page).
const jobs = [
  {
    id: "d129dcf5-353a-49ce-82c2-8372bd52c779",
    title: "Software Developer",
    company: "Amazon",
    location: "Bloemfontein, ZA",
    status: "Active",
    description: "Developing and maintaining web applications for a fast-moving team.",
  },
  {
    id: "a1b2c3d4-1111-2222-3333-444455556666",
    title: "Junior Backend Engineer",
    company: "BitCube",
    location: "Remote, ZA",
    status: "Active",
    description: "Work on a .NET 10 API with EF Core and PostgreSQL. Mentorship provided.",
  },
  {
    id: "e5f6a7b8-5555-6666-7777-888899990000",
    title: "Senior .NET Engineer",
    company: "CareerHub Inc.",
    location: "Pretoria, ZA",
    status: "Closed",
    description: "This position has been filled — thank you to all applicants.",
  },
];

// GET /api/jobs/{id} — returns the job, or 404 Problem Details if unknown.
export async function GET(
  _request: Request,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params; // Next 15: params is a Promise — await it.

  const job = jobs.find((j) => j.id === id);

  if (!job) {
    return NextResponse.json(
      {
        title: "Job not found",
        detail: `No job exists with id '${id}'.`,
        status: 404,
      },
      { status: 404 }
    );
  }

  return NextResponse.json(job, { status: 200 });
}

// Any non-GET method is not allowed on this route.
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
