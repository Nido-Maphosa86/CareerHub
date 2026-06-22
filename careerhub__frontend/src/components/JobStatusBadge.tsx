// src/components/JobStatusBadge.tsx
// Picks the right badge colour for a given employment type.

import { Badge } from "@/components/ui/badge";
import { EmploymentType } from "@/types";

// Map each EmploymentType to the matching badge variant.
// Typed as Record so TypeScript catches missing keys at compile time.
type BadgeVariant =
  | "fulltime"
  | "parttime"
  | "contract"
  | "internship"
  | "freelance"
  | "closed";

const variantMap: Record<EmploymentType, BadgeVariant> = {
  fulltime: "fulltime",
  parttime: "parttime",
  contract: "contract",
  internship: "internship",
  freelance: "freelance",
  closed: "closed",
};

// Human-friendly label for each type (e.g. "fulltime" -> "Full-time").
const labelMap: Record<EmploymentType, string> = {
  fulltime: "Full-time",
  parttime: "Part-time",
  contract: "Contract",
  internship: "Internship",
  freelance: "Freelance",
  closed: "Closed",
};

interface Props {
  type: EmploymentType;
}

export function JobStatusBadge({ type }: Props) {
  return <Badge variant={variantMap[type]}>{labelMap[type]}</Badge>;
}
