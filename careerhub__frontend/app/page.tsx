// This tells Next.js the file runs in the browser.
// Without it, useState/useEffect won’t work.
"use client";

// Import the JobList component (shows all jobs)
import { JobList } from "@/src/components/JobList";
// Import cn helper (fixes Tailwind class conflicts)
import { cn } from "@/src/lib/utils";
// Import JobListing type (defines job shape)
import { JobListing } from "@/src/types/indexx";
// Import React hooks for state and side effects
import { useState, useEffect } from "react";


// ── Fake job data ──
// This is sample data shaped like the API response.
// Later replaced with a real API call.
// Covers different cases: inactive job, 0 applicants, different types, posted today, posted long ago.
const jobs: JobListing[] = [
  {
    // Unique ID for the job
    id: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    // Job title
    title: "Senior Full Stack Developer",
    // Company name
    company: "BitCube",
    // Job location
    location: "Bloemfontein",
    // Employment type
    employmentType: "FullTime",
    // Minimum salary
    salaryMin: 55000,
    // Maximum salary
    salaryMax: 75000,
    // Posted today
    postedAt: new Date().toISOString(),
    // Job is active
    isActive: true,
    // Number of applicants
    applicantCount: 7,
  },
  {
    id: "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    title: "DevOps Engineer",
    company: "Takealot",
    location: "Cape Town",
    employmentType: "FullTime",
    salaryMin: 60000,
    salaryMax: 85000,
    // Posted 5 days ago
    postedAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
    isActive: true,
    applicantCount: 12,
  },
  {
    id: "c3d4e5f6-a7b8-9012-cdef-123456789012",
    title: "React Frontend Developer",
    company: "FNB",
    location: "Johannesburg",
    employmentType: "Contract",
    salaryMin: 45000,
    salaryMax: 65000,
    // Posted 14 days ago
    postedAt: new Date(Date.now() - 14 * 24 * 60 * 60 * 1000).toISOString(),
    isActive: true,
    // 0 applicants
    applicantCount: 0,
  },
  {
    id: "d4e5f6a7-b8c9-0123-defa-234567890123",
    title: "Junior Software Developer",
    company: "Standard Bank",
    location: "Johannesburg",
    employmentType: "Internship",
    salaryMin: 20000,
    salaryMax: 30000,
    // Posted 45 days ago
    postedAt: new Date(Date.now() - 45 * 24 * 60 * 60 * 1000).toISOString(),
    isActive: true,
    applicantCount: 23,
  },
  {
    id: "e5f6a7b8-c9d0-1234-efab-345678901234",
    title: "Data Analyst",
    company: "Discovery Health",
    location: "Sandton",
    employmentType: "PartTime",
    salaryMin: 25000,
    salaryMax: 40000,
    // Posted 3 days ago
    postedAt: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(),
    isActive: true,
    applicantCount: 4,
  },
  {
    id: "f6a7b8c9-d0e1-2345-fabc-456789012345",
    title: "Backend Engineer (.NET)",
    company: "Vodacom",
    location: "Remote",
    employmentType: "FullTime",
    salaryMin: 65000,
    salaryMax: 90000,
    // Posted 60 days ago
    postedAt: new Date(Date.now() - 60 * 24 * 60 * 60 * 1000).toISOString(),
    // Job is inactive
    isActive: false,
    applicantCount: 31,
  },
];

// Key used in sessionStorage to save selected job
const SESSION_KEY = "careerhub-selected-job";

// ── Home component ──
export default function Home() {
  // State: which job is selected (or null if none)
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // Effect 1: Restore selection when page loads
  // Runs once (empty [] means only on mount)
  useEffect(() => {
    const stored = sessionStorage.getItem(SESSION_KEY);
    if (stored) {
      // Only restore if job still exists
      const stillExists = jobs.some((j) => j.id === stored);
      if (stillExists) {
        setSelectedId(stored);
      }
    }
  }, []);

  // Effect 2: Save selection whenever it changes
  // Runs every time selectedId changes
  useEffect(() => {
    if (selectedId !== null) {
      // Save selected job ID
      sessionStorage.setItem(SESSION_KEY, selectedId);
    } else {
      // Remove key if deselected
      sessionStorage.removeItem(SESSION_KEY);
    }
  }, [selectedId]);

  // Find the full job object from selectedId
  const selectedJob = jobs.find((j) => j.id === selectedId) ?? null;

  // Handle click: toggle selection
  function handleSelect(id: string) {
    setSelectedId((prev) => (prev === id ? null : id));
  }

  return (
    // Page wrapper with light/dark background
    <main className={cn("min-h-screen", "bg-gray-50 dark:bg-gray-900")}>
      <div className="max-w-6xl mx-auto px-8 py-8">

        {/* Summary panel → only shows if a job is selected */}
        {selectedJob !== null && (
          <div
            className={cn(
              "mb-6 p-5 rounded-xl border",
              "bg-blue-50 border-blue-200",
              "dark:bg-blue-950 dark:border-blue-800",
            )}
          >
            <p
              className={cn(
                "text-xs font-semibold uppercase tracking-wider mb-1",
                "text-blue-500 dark:text-blue-400",
              )}
            >
              Selected Position
            </p>
            <h2 className={cn("text-lg font-bold", "text-blue-900 dark:text-blue-100")}>
              {selectedJob.title}
            </h2>
            <p className={cn("text-sm mt-0.5", "text-blue-700 dark:text-blue-300")}>
              {selectedJob.company} · {selectedJob.location}
            </p>
            <p className={cn("text-xs mt-2", "text-blue-500 dark:text-blue-400")}>
              Click the card again to deselect
            </p>
          </div>
        )}

        {/* JobList → shows all jobs and handles selection */}
        <JobList
          jobs={jobs}
          selectedId={selectedId}
          onSelect={handleSelect}
        />
      </div>
    </main>
  );
}
