// src/components/ui/badge.tsx
// shadcn/ui style Badge component, with custom CareerHub variants for
// each EmploymentType. Uses class-variance-authority (cva) for variants.

import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-md px-2.5 py-0.5 text-xs font-semibold transition-colors",
  {
    variants: {
      variant: {
        default: "bg-slate-200 text-slate-900 dark:bg-slate-700 dark:text-slate-100",
        fulltime: "bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-200",
        parttime: "bg-sky-100 text-sky-800 dark:bg-sky-900 dark:text-sky-200",
        contract: "bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200",
        internship: "bg-violet-100 text-violet-800 dark:bg-violet-900 dark:text-violet-200",
        freelance: "bg-pink-100 text-pink-800 dark:bg-pink-900 dark:text-pink-200",
        closed: "bg-slate-300 text-slate-700 line-through dark:bg-slate-800 dark:text-slate-400",
      },
    },
    defaultVariants: { variant: "default" },
  }
);

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

export function Badge({ className, variant, ...props }: BadgeProps) {
  return <div className={cn(badgeVariants({ variant }), className)} {...props} />;
}

export { badgeVariants };
