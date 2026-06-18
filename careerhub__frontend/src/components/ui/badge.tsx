// src/components/ui/badge.tsx
//
// shadcn/ui Badge component — source lives in YOUR project, not in node_modules.
// This means:
//   - You own it. You can edit it freely without waiting for a library release.
//   - A shadcn/ui version bump never breaks your build — you only pull changes
//     when you explicitly run `npx shadcn add badge` again (and review the diff).
//   - Contrast with @mui/material: a major version that renames a prop breaks
//     every usage site immediately across your entire build.
//
// badgeVariants uses cva (class-variance-authority) — a library that maps a
// variant prop value to a set of Tailwind class strings. It replaces a manual
// switch/if-else, keeping all variant-to-class mappings in one place.

import { cn } from "@/src/lib/utils";
import { cva, type VariantProps } from "class-variance-authority";

export const badgeVariants = cva(
  // Base classes applied to every badge regardless of variant
  "inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
  {
    variants: {
      variant: {
        default:
          "border-transparent bg-gray-900 text-gray-50 dark:bg-gray-50 dark:text-gray-900",
        secondary:
          "border-transparent bg-gray-100 text-gray-900 dark:bg-gray-800 dark:text-gray-50",
        destructive:
          "border-transparent bg-red-500 text-white dark:bg-red-900 dark:text-red-50",
        outline:
          "text-gray-900 dark:text-gray-50",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
);

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

import * as React from "react";

function Badge({ className, variant, ...props }: BadgeProps) {
  return (
    <div className={cn(badgeVariants({ variant }), className)} {...props} />
  );
}

export { Badge };
