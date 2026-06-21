// src/components/JobStatusBadge.tsx
//
// Single responsibility: maps employmentType and isActive values to Badge
// components with authoritative visual representations.
//
// WHY THIS IS A SEPARATE COMPONENT (single responsibility principle):
//
// JobCard's responsibility is layout and interactivity — showing the card,
// handling clicks, managing selected state visuals.
// Badge colour logic is a separate concern. If the employment type colour
// scheme changes (e.g. Contract moves from orange to yellow), with this
// component extracted you change ONE file. Without extraction, you search
// every file in the codebase that renders an employment type and update each.
// In a team with 10 components consuming employment types, that is 10 places
// to update, test, and review. One missed update means inconsistency.
//
// The mapping from value to colour is defined exactly once here.
// There is no conditional colour logic at the call site — JobCard does not
// know what colour "Contract" is. It just renders <JobStatusBadge />.

import { cn } from "../lib/utils";
import { EmploymentType } from "../types";
import { Badge } from "./ui/badge";



// ── Employment type colour map ─────────────────────────────────────────────────
// Record<EmploymentType, string> ensures TypeScript enforces that EVERY value
// in the union is mapped. If a new value ("Freelance") is added to the union
// and this record is not updated, TypeScript produces:
//   "Property 'Freelance' is missing in type"
// That compile error surfaces at the definition — not scattered at usage sites.

const employmentTypeClasses: Record<EmploymentType, string> = {
  FullTime:
    "border-transparent bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-200",
  PartTime:
    "border-transparent bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-200",
  Contract:
    "border-transparent bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-200",
  Internship:
    "border-transparent bg-teal-100 text-teal-700 dark:bg-teal-900 dark:text-teal-200",
  Freelance:
    "border-transparent bg-pink-100 text-pink-700 dark:bg-pink-900 dark:text-pink-200",
};

const employmentTypeLabels: Record<EmploymentType, string> = {
  FullTime:   "Full Time",
  PartTime:   "Part Time",
  Contract:   "Contract",
  Internship: "Internship",
  Freelance:  "Freelance",
};

// ── Props ─────────────────────────────────────────────────────────────────────

interface JobStatusBadgeProps {
  employmentType: EmploymentType;  // union type — not string
  isActive: boolean;
}

// ── Component ──────────────────────────────────────────────────────────────────

// Named export — not a default export.
export function JobStatusBadge({ employmentType, isActive }: JobStatusBadgeProps) {
  return (
    <div className="flex flex-wrap gap-1.5">
      {/* Employment type badge — always renders.
          Colour derived from the map above — no conditional at call site. */}
      <Badge
        className={cn(employmentTypeClasses[employmentType])}
      >
        {employmentTypeLabels[employmentType]}
      </Badge>

      {/* Active status badge — only renders when isActive is false.
          When isActive is true, nothing renders here.
          The element does not exist in the DOM — not hidden, absent. */}
      {!isActive && (
        <Badge
          className={cn(
            "border-transparent bg-red-100 text-red-700",
            "dark:bg-red-900 dark:text-red-200"
          )}
        >
          Closed
        </Badge>
      )}
    </div>
  );
}
