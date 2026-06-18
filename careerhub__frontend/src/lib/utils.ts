// src/lib/utils.ts
//
// The cn utility combines two libraries:
//
// clsx — merges conditional class values into a single string.
//   cn("p-4", isSelected && "border-blue-500")
//   → "p-4 border-blue-500" or "p-4"
//
// tailwind-merge — resolves conflicts between Tailwind utility classes.
//   Without it: cn("p-4", "p-6") → "p-4 p-6" (both apply, last wins in CSS
//   but ONLY because of stylesheet order — unreliable across Tailwind versions)
//   With it:    cn("p-4", "p-6") → "p-6" (the conflict is resolved correctly)
//
// A concrete CareerHub example from JobCard:
//   cn("border-gray-200", isSelected && "border-blue-500")
//   String concatenation: "border-gray-200 border-blue-500" — both border-color
//   classes target the same CSS property. The winner depends on which class
//   appears later in Tailwind's generated stylesheet, which is determined by
//   Tailwind's internal ordering, not the order in the string. This is
//   unpredictable. tailwind-merge detects the conflict and keeps only
//   border-blue-500, producing a reliable result every time.

// Import clsx → helps combine class names conditionally
// Example: clsx("a", condition && "b") → returns "a b" if condition is true
import { clsx, type ClassValue } from "clsx";

// Import tailwind-merge → removes conflicting Tailwind classes
// Example: "border-gray-200 border-blue-500" → tailwind-merge keeps only "border-blue-500"
import { twMerge } from "tailwind-merge";

// Export cn function → combines clsx and tailwind-merge
// clsx handles conditional logic, tailwind-merge fixes conflicts
// This is used everywhere instead of template literals for class names
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
