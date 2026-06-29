// src/lib/mockJobs.ts
// Shared mock job data for the standalone Route Handlers required by the
// assignments (api/jobs/[id] and api/applications/stats).
//
// IMPORTANT: This is NOT the live data. The live pages (/jobs, /jobs/[id],
// /dashboard) read the real CareerHub.Api backend. These mock route handlers
// exist only as the assignments' required artifacts, so their direct
// proving-tests (GET/PATCH/405/404) pass on the frontend origin itself.
//
// The array is exported as a mutable module-level constant. A Route Handler's
// PATCH can mutate it in place, and because the module is cached for the life
// of the server process, the change persists across requests (Assignment 2.2,
// Part 2). This is the correct approach for a mock — a real backend would
// persist to a database instead.

export interface MockJob {
  id: string;
  title: string;
  company: string;
  location: string;
  status: string;
  description: string;
}

// Mutable on purpose — PATCH updates entries in place.
export const mockJobs: MockJob[] = [
  {
    id: "777ff351-1825-4b07-a9bc-2f5df2dc6958",
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
