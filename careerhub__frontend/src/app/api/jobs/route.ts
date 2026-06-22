// src/app/api/jobs/route.ts
// Mock fallback that mirrors the real CareerHub API shape (paginated wrapper).
// Not used while NEXT_PUBLIC_API_URL points at the real backend, but handy
// for offline development. Point the env var here to use it.

import { NextResponse } from "next/server";
import { JobListing } from "@/types";

const jobs: JobListing[] = [
  {
    id: "777ff351-1825-4b07-a9bc-2f5df2dc6958",
    title: "Software Developer",
    description: "Developing and maintaining web applications.",
    companyName: "Amazon",
    location: "Bloemfontein, ZA",
    type: "PartTime",
    salaryMin: 30000,
    salaryMax: 40000,
    salaryDisplay: "R30,000 – R40,000/month",
    postedAt: "2026-06-22T08:26:04.866998Z",
    isActive: true,
    applicationCount: 0,
    closingDate: "2027-01-01T00:00:00Z",
    status: "Active",
  },
];

export async function GET() {
  return NextResponse.json({
    data: jobs,
    page: 1,
    pageSize: 20,
    totalCount: jobs.length,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
  });
}
