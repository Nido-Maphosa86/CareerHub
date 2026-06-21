
export type EmploymentType =
  | "FullTime"
  | "PartTime"
  | "Contract"
  | "Internship"
  | "Freelance";


export type JobListingStatus = "Active" | "Closed";


export interface JobListing {
  id: string;

  title: string;
  company: string;       
  location: string;

  
  employmentType: EmploymentType;

  salaryMin: number;
  salaryMax: number;


  postedAt: string;

  isActive: boolean;
  applicantCount: number;
}
// src/types/index.ts