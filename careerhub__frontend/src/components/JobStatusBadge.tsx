// src/components/JobStatusBadge.tsx
// Picks the right badge colour for a given employment type.
// src/components/JobStatusBadge.tsx
// Picks the right badge colour for a given employment type.

import { Badge } from "@/components/ui/badge";
import { EmploymentType } from "@/types";

// Map each EmploymentType to the matching badge variant.
type BadgeVariant =
  | "fulltime"
  | "parttime"
  | "contract"
  | "internship"
  | "freelance"
  | "closed";

const variantMap: Record<EmploymentType, BadgeVariant> = {
  FullTime: "fulltime",
  PartTime: "parttime",
  Contract: "contract",
  Internship: "internship",
  Freelance: "freelance",
  Temporary: "contract", // reuse contract styling for temporary
};

// Human-friendly label for each type.
const labelMap: Record<EmploymentType, string> = {
  FullTime: "Full-time",
  PartTime: "Part-time",
  Contract: "Contract",
  Internship: "Internship",
  Freelance: "Freelance",
  Temporary: "Temporary",
};

interface Props {
  type: EmploymentType;
}

export function JobStatusBadge({ type }: Props) {
  return <Badge variant={variantMap[type]}>{labelMap[type]}</Badge>;
}