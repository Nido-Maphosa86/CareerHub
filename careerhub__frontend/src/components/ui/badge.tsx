// src/components/ui/badge.tsx
// shadcn/ui style Badge with custom CareerHub variants per employment type.
// Tuned for the lime/black theme — quiet fills, readable in both modes.

import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-full px-2.5 py-0.5 text-[11px] font-medium uppercase tracking-wider ring-1 ring-inset transition-colors",
  {
    variants: {
      variant: {
        default:
          "bg-zinc-100 text-zinc-700 ring-zinc-200 dark:bg-zinc-800 dark:text-zinc-300 dark:ring-zinc-700",
        fulltime:
          "bg-lime-100 text-lime-800 ring-lime-300 dark:bg-lime-400/10 dark:text-lime-300 dark:ring-lime-400/30",
        parttime:
          "bg-sky-100 text-sky-800 ring-sky-300 dark:bg-sky-400/10 dark:text-sky-300 dark:ring-sky-400/30",
        contract:
          "bg-amber-100 text-amber-800 ring-amber-300 dark:bg-amber-400/10 dark:text-amber-300 dark:ring-amber-400/30",
        internship:
          "bg-violet-100 text-violet-800 ring-violet-300 dark:bg-violet-400/10 dark:text-violet-300 dark:ring-violet-400/30",
        freelance:
          "bg-pink-100 text-pink-800 ring-pink-300 dark:bg-pink-400/10 dark:text-pink-300 dark:ring-pink-400/30",
        closed:
          "bg-zinc-200 text-zinc-500 ring-zinc-300 line-through dark:bg-zinc-800 dark:text-zinc-500 dark:ring-zinc-700",
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
