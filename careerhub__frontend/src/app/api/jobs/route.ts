// src/app/api/jobs/route.ts
// Next.js Route Handler — acts as a mock backend on the server side.
// GET http://localhost:3000/api/jobs returns the seed data as JSON.
// Replace this later by pointing NEXT_PUBLIC_API_URL at the real CareerHub API.

import { NextResponse } from "next/server";
import { JobListing } from "@/types";

// Seed data — same shape as Assignment 1.1.
//seed data for the jobs that will be displayed on the front end
const jobs: JobListing[] = [
  {
    id: "1",
    title: "Junior Backend Developer",
    company: "BitCube",
    location: "Bloemfontein, ZA",
    employmentType: "fulltime",
    salary: "R 18 000 - R 25 000",
    postedAt: "2026-06-10",
    description:
      "Work on the CareerHub API in .NET 10 with EF Core and PostgreSQL.",
  },
  {
    id: "2",
    title: "Frontend Intern",
    company: "Skye Labs",
    location: "Remote",
    employmentType: "internship",
    salary: "R 8 000 stipend",
    postedAt: "2026-06-12",
    description:
      "Build features in Next.js and TypeScript alongside a small team.",
  },
  {
    id: "3",
    title: "DevOps Contractor",
    company: "Nido Systems",
    location: "Johannesburg, ZA",
    employmentType: "contract",
    salary: "R 600/hour",
    postedAt: "2026-06-15",
    description:
      "Six-month contract to set up CI/CD pipelines and Docker-based deployments.",
  },
  {
    id: "4",
    title: "UX Designer (Part-Time)",
    company: "Maphosa Studio",
    location: "Cape Town, ZA",
    employmentType: "parttime",
    salary: "R 200/hour",
    postedAt: "2026-06-08",
    description:
      "Help shape the look and feel of a new job-search product. 20 hours per week.",
  },
  {
    id: "5",
    title: "Freelance Mobile Developer",
    company: "Independent",
    location: "Remote",
    employmentType: "freelance",
    salary: "Negotiable",
    postedAt: "2026-06-01",
    description: "Short engagement to ship a React Native MVP in eight weeks.",
  },
  {
    id: "6",
    title: "Senior .NET Engineer",
    company: "CareerHub Inc.",
    location: "Pretoria, ZA",
    employmentType: "closed",
    salary: "R 70 000+",
    postedAt: "2026-05-20",
    description: "Position has been filled. Thank you to everyone who applied.",
  },
];

//get endpoint for the jobs route, returns the seed data as JSON
export async function GET() {
  return NextResponse.json(jobs);
}
