
"use client";

import { JobList } from "@/src/components/JobList";
import { JobListing } from "@/src/types";
import { useState } from "react";



const jobs: JobListing[] = [
  {
    id: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    title: "Senior Full Stack Developer",
    company: "BitCube",
    location: "Bloemfontein",
    employmentType: "FullTime",
    salaryMin: 55000,
    salaryMax: 75000,
    
    postedAt: new Date().toISOString(),
    isActive: true,
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
    
    postedAt: new Date(Date.now() - 14 * 24 * 60 * 60 * 1000).toISOString(),
    isActive: true,
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
    
    postedAt: new Date(Date.now() - 60 * 24 * 60 * 60 * 1000).toISOString(),

    isActive: false,
    applicantCount: 31,
  },
];


export default function Home() {
  
  const [selectedId, setSelectedId] = useState<string | null>(null);

  
  const selectedJob = jobs.find((j) => j.id === selectedId) ?? null;

  
  function handleSelect(id: string) {
    setSelectedId((prev) => (prev === id ? null : id));
  }

  return (
    <main className="min-h-screen bg-gray-50">
            <header className="bg-white border-b border-gray-200 px-8 py-5">
        <h1 className="text-2xl font-bold text-gray-900">CareerHub</h1>
        <p className="text-sm text-gray-500 mt-0.5">
          Find your next opportunity
        </p>
      </header>

      <div className="max-w-6xl mx-auto px-8 py-8">

        {selectedJob !== null && (
          <div className="mb-6 p-5 bg-blue-50 border border-blue-200 rounded-xl">
            <p className="text-xs font-semibold text-blue-500 uppercase tracking-wider mb-1">
              Selected Position
            </p>
            <h2 className="text-lg font-bold text-blue-900">
              {selectedJob.title}
            </h2>
            <p className="text-sm text-blue-700 mt-0.5">
              {selectedJob.company} · {selectedJob.location}
            </p>
            <p className="text-xs text-blue-500 mt-2">
              Click the card again to deselect
            </p>
          </div>
        )}

        {}
        <JobList
          jobs={jobs}
          selectedId={selectedId}
          onSelect={handleSelect}
        />
      </div>
    </main>
  );
}